using System;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace StarryEyes.Models.Operations
{
    public abstract class OperationBase<T> : IRunnerQueueable
    {
        public OperationBase() { }

        private Subject<T> resultHandler = new Subject<T>();
        protected Subject<T> ResultHandler
        {
            get { return resultHandler; }
        }

        /// <summary>
        /// Run operation via operation queue.
        /// </summary>
        internal IObservable<T> Run(OperationPriority priority = OperationPriority.Middle)
        {
            OperationQueueRunner.Enqueue(this, priority);
            return resultHandler;
        }

        /// <summary>
        /// Run operation without operation queue
        /// </summary>
        public IObservable<T> RunImmediate()
        {
            return Observable.Start(() => RunCore())
                .SelectMany(_ => _);
        }

        /// <summary>
        /// Core operation(Synchronously)
        /// </summary>
        protected abstract IObservable<T> RunCore();

        protected IObservable<string> GetExceptionDetail(Exception ex)
        {
            var wex = ex as WebException;
            if (wex != null && wex.Response != null)
            {
                return Observable.Return(wex.Response)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .SelectMany(r => r.DownloadStringAsync())
                    .Select(s => ParseErrorMessage(s));
            }
            else
            {
                return Observable.Return(ex.Message);
            }
        }

        private string ParseErrorMessage(string error)
        {
            if (error.StartsWith("{error:") && error.EndsWith("}"))
                return error.Substring(7, error.Length - 8);
            else
                return error;
        }

        IObservable<Unit> IRunnerQueueable.Run()
        {
            var subject = new Subject<T>();
            var connectable = RunCore().Publish();
            connectable.Subscribe(resultHandler);
            connectable.Subscribe(subject);
            connectable.Connect();
            return subject.Select(_ => new Unit());
        }
    }
}
