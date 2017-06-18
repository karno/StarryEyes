using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starcluster.Threading
{
    internal class LimitedTaskScheduler : TaskScheduler
    {
        public static TaskFactory GetTaskFactory(int maxParallel)
        {
            return new TaskFactory(new LimitedTaskScheduler(maxParallel));
        }

        [ThreadStatic]
        private static bool _threadIsProcessing;

        private readonly LinkedList<Task> _tasks = new LinkedList<Task>();

        private readonly int _maximumConcurrencyLevel;

        private int _currentRunningThreads;
        public sealed override int MaximumConcurrencyLevel => _maximumConcurrencyLevel;

        public LimitedTaskScheduler(int maximumConcurrencyLevel)
        {
            if (maximumConcurrencyLevel < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumConcurrencyLevel));
            }
            _maximumConcurrencyLevel = maximumConcurrencyLevel;
        }

        protected override void QueueTask(Task task)
        {
            QueueExternalTask(task);
        }

        public void QueueExternalTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (_currentRunningThreads >= _maximumConcurrencyLevel)
                {
                    return;
                }
                _currentRunningThreads++;
                RunNewTask();
            }
        }

        public void PushExternalTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddFirst(task);
                if (_currentRunningThreads >= _maximumConcurrencyLevel)
                {
                    return;
                }
                _currentRunningThreads++;
                RunNewTask();
            }
        }

        private void RunNewTask()
        {
            Task.Run(() =>
            {
                _threadIsProcessing = true;
                try
                {
                    while (true)
                    {
                        Task task;
                        lock (_tasks)
                        {
                            if (_tasks.Count == 0)
                            {
                                break;
                            }
                            task = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }
                        TryExecuteTask(task);
                    }
                }
                finally
                {
                    _threadIsProcessing = false;
                    _currentRunningThreads--;
                }
            }, CancellationToken.None);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (_threadIsProcessing)
            {
                return false;
            }
            if (taskWasPreviouslyQueued)
            {
                TryDequeue(task);
            }
            return TryExecuteTask(task);
        }

        protected override bool TryDequeue(Task task)
        {
            lock (_tasks)
            {
                return _tasks.Remove(task);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken)
                {
                    return _tasks.ToArray();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }
    }
}