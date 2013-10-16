using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StarryEyes.Albireo.Threading
{
    public class LimitedTaskScheduler : TaskScheduler
    {
        public static TaskFactory GetTaskFactory(int maxParallel)
        {
            return new TaskFactory(new LimitedTaskScheduler(maxParallel));
        }

        [ThreadStatic]
        private static bool _threadIsProcessing;

        private readonly LinkedList<Task> _tasks = new LinkedList<Task>();

        private readonly int _maxConcurrency;

        private int _currentRunningThreads;

        public LimitedTaskScheduler(int maxConcurrency)
        {
            if (maxConcurrency < 1)
            {
                throw new ArgumentOutOfRangeException("maxConcurrency");
            }
            this._maxConcurrency = maxConcurrency;
        }

        protected override void QueueTask(Task task)
        {
            lock (_tasks)
            {
                _tasks.AddLast(task);
                if (this._currentRunningThreads >= this._maxConcurrency)
                {
                    return;
                }
                this._currentRunningThreads++;
                this.RunNewTask();
            }
        }

        private void RunNewTask()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
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
                        this.TryExecuteTask(task);
                    }
                }
                finally
                {
                    _threadIsProcessing = false;
                    this._currentRunningThreads--;
                }
            }, null);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (_threadIsProcessing)
            {
                return false;
            }
            if (taskWasPreviouslyQueued)
            {
                this.TryDequeue(task);
            }
            return this.TryExecuteTask(task);
        }

        protected override bool TryDequeue(Task task)
        {
            lock (_tasks)
            {
                return _tasks.Remove(task);
            }
        }

        public sealed override int MaximumConcurrencyLevel
        {
            get { return this._maxConcurrency; }
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
