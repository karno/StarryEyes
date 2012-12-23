using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;

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

        private static readonly Mutex OperationQueueMutex = new Mutex(false, "STARRYEYES_OPERATION_QUEUE_MUTEX");

        private static readonly Queue<IRunnerQueueable> HighPriorityQueue = new Queue<IRunnerQueueable>();

        private static readonly Queue<IRunnerQueueable> MiddlePriorityQueue = new Queue<IRunnerQueueable>();

        private static readonly Queue<IRunnerQueueable> LowPriorityQueue = new Queue<IRunnerQueueable>();

        /// <summary>
        /// Queue item to reserve running.
        /// </summary>
        public static void Enqueue(IRunnerQueueable runnable, OperationPriority priority = OperationPriority.Middle)
        {
            switch (priority)
            {
                case OperationPriority.High:
                    Lock(() => HighPriorityQueue.Enqueue(runnable));
                    break;
                case OperationPriority.Middle:
                    Lock(() => MiddlePriorityQueue.Enqueue(runnable));
                    break;
                case OperationPriority.Low:
                    Lock(() => LowPriorityQueue.Enqueue(runnable));
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
                Observable.Defer(() => Observable.Return(operation))
                    // .ObserveOn(TaskPoolScheduler.Default)
                    .SelectMany(_ => _.Run())
                    .Finally(() =>
                    {
                        Interlocked.Decrement(ref _runningConcurrency);
                        SpawnNewRunner();
                    })
                    .Subscribe();
            }
        }

        private static IRunnerQueueable DequeueOperation()
        {
            return Lock(() =>
            {
                if (HighPriorityQueue.Count > 0)
                    return HighPriorityQueue.Dequeue();
                if (MiddlePriorityQueue.Count > 0)
                    return MiddlePriorityQueue.Dequeue();
                if (LowPriorityQueue.Count > 0)
                    return LowPriorityQueue.Dequeue();
                return null;
            });
        }

        private static void Lock(Action action)
        {
            Lock(() => { action(); return new object(); });
        }

        private static T Lock<T>(Func<T> func)
        {
            if (!OperationQueueMutex.WaitOne(10000))
                throw new InvalidOperationException("DEAD LOCK In Operation Queue Runner.");
            try
            {
                return func();
            }
            finally
            {
                OperationQueueMutex.ReleaseMutex();
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