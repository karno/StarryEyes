using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StarryEyes.Models.Operations
{
    /// <summary>
    /// Manage ALL operations in Agalmatophilia.
    /// </summary>
    public static class OperationQueueRunner
    {
        static OperationQueueRunner()
        {
            MaxConcurrency = 16;
        }

        /// <summary>
        /// Max concurrency number of dispatching operation.
        /// </summary>
        public static int MaxConcurrency { get; set; }

        private static int _runningConcurrency;

        private static readonly Mutex _operationQueueMutex = new Mutex(false, "STARRYEYES_OPERATION_QUEUE_MUTEX");

        private static readonly Queue<IRunnerQueueable> _highPriorityQueue = new Queue<IRunnerQueueable>();

        private static readonly Queue<IRunnerQueueable> _middlePriorityQueue = new Queue<IRunnerQueueable>();

        private static readonly Queue<IRunnerQueueable> _lowPriorityQueue = new Queue<IRunnerQueueable>();

        /// <summary>
        /// Queue item to reserve running.
        /// </summary>
        public static void Enqueue(IRunnerQueueable runnable, OperationPriority priority = OperationPriority.Middle)
        {
            switch (priority)
            {
                case OperationPriority.High:
                    Lock(() => _highPriorityQueue.Enqueue(runnable));
                    break;
                case OperationPriority.Middle:
                    Lock(() => _middlePriorityQueue.Enqueue(runnable));
                    break;
                case OperationPriority.Low:
                    Lock(() => _lowPriorityQueue.Enqueue(runnable));
                    break;
            }
            SpawnNewRunner();
        }

        /// <summary>
        /// Spawn new running thread
        /// </summary>
        private static void SpawnNewRunner()
        {
            int cv = Interlocked.Increment(ref _runningConcurrency);
            if (cv > MaxConcurrency)
            {
                // already run max count of threads.
                Interlocked.Decrement(ref _runningConcurrency);
                return;
            }


            var operation = DequeueOperation();

            if (operation == null)
            {
                // operation is not available.
                Interlocked.Decrement(ref _runningConcurrency);
            }
            else
            {
                // operation acquired.
                Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        while (operation != null)
                        {
                            await operation.Run().LastOrDefaultAsync();
                            operation = DequeueOperation();
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _runningConcurrency);
                        SpawnNewRunner();
                    }
                });
            }
        }

        private static IRunnerQueueable DequeueOperation()
        {
            return Lock(() =>
            {
                if (_highPriorityQueue.Count > 0)
                    return _highPriorityQueue.Dequeue();
                if (_middlePriorityQueue.Count > 0)
                    return _middlePriorityQueue.Dequeue();
                if (_lowPriorityQueue.Count > 0)
                    return _lowPriorityQueue.Dequeue();
                return null;
            });
        }

        private static void Lock(Action action)
        {
            Lock(() => { action(); return new object(); });
        }

        private static T Lock<T>(Func<T> func)
        {
            if (!_operationQueueMutex.WaitOne(10000))
                throw new InvalidOperationException("DEAD LOCK In Operation Queue Runner.");
            try
            {
                return func();
            }
            finally
            {
                _operationQueueMutex.ReleaseMutex();
            }
        }
    }

    public enum OperationPriority
    {
        /// <summary>
        /// Higher priority than the default(middle) priority.
        /// </summary>
        High,
        /// <summary>
        /// Normal priority.
        /// </summary>
        Middle,
        /// <summary>
        /// Lower prioriry than the default(middle) priority.
        /// </summary>
        Low,
    }
}