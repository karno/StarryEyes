using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Reactive.Concurrency;

namespace StarryEyes.Mystique.Models.Operations
{
    /// <summary>
    /// Manage ALL operations in Agalmatophilia.
    /// </summary>
    public static class OperationQueueRunner
    {
        static OperationQueueRunner()
        {
            MaxConcurrency = 4;
        }

        /// <summary>
        /// Max concurrency number of dispatching operation.
        /// </summary>
        public static int MaxConcurrency { get; set; }

        private static int runningConcurrency = 0;

        private static Mutex operationQueueMutex = new Mutex(false, "STARRYEYES_OPERATION_QUEUE_MUTEX");

        private static Queue<IRunnerQueueable> highPriorityQueue = new Queue<IRunnerQueueable>();

        private static Queue<IRunnerQueueable> middlePriorityQueue = new Queue<IRunnerQueueable>();

        private static Queue<IRunnerQueueable> lowPriorityQueue = new Queue<IRunnerQueueable>();

        private static object opsListLocker = new object();

        /// <summary>
        /// Queue item to reserve running.
        /// </summary>
        public static void Enqueue(IRunnerQueueable runnable, OperationPriority priority = OperationPriority.Middle)
        {
            switch (priority)
            {
                case OperationPriority.High:
                    Lock(() => highPriorityQueue.Enqueue(runnable));
                    break;
                case OperationPriority.Middle:
                    Lock(() => middlePriorityQueue.Enqueue(runnable));
                    break;
                case OperationPriority.Low:
                    Lock(() => lowPriorityQueue.Enqueue(runnable));
                    break;
            }
            SpawnNewRunner();
        }

        /// <summary>
        /// Spawn new running thread
        /// </summary>
        private static void SpawnNewRunner()
        {
            int cv = Interlocked.Increment(ref runningConcurrency);
            if (cv > MaxConcurrency)
            {
                // already run max count of threads.
                Interlocked.Decrement(ref runningConcurrency);
                return;
            }
            else
            {
                Observable.Start(() => IssueOperations(), new NewThreadScheduler())
                    .Finally(() => Interlocked.Decrement(ref runningConcurrency))
                    .Subscribe();
            }
        }

        /// <summary>
        /// Issue all operations when operation is enabled.
        /// </summary>
        private static void IssueOperations()
        {
            var operation = Lock(() =>
            {
                if (highPriorityQueue.Count > 0)
                    return highPriorityQueue.Dequeue();
                else if (middlePriorityQueue.Count > 0)
                    return middlePriorityQueue.Dequeue();
                else if (lowPriorityQueue.Count > 0)
                    return lowPriorityQueue.Dequeue();
                else
                    return null;
            });
            if (operation != null)
                operation.Run()
                    .Finally(() => IssueOperations())
                    .Subscribe();
            else
                return;
        }

        private static void Lock(Action action)
        {
            Lock(() => { action(); return new object(); });
        }

        private static T Lock<T>(Func<T> func)
        {
            if (!operationQueueMutex.WaitOne(10000))
                throw new InvalidOperationException("DEAD LOCK In Operation Queue Runner.");
            try
            {
                return func();
            }
            finally
            {
                operationQueueMutex.ReleaseMutex();
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