using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace StarryEyes.Albireo.Threading
{
    public class SerialTaskWorker : IDisposable
    {
        private readonly ConcurrentQueue<WorkObject> _tasks;
        private readonly ManualResetEventSlim _event;
        private readonly Thread _workerThread;
        private bool _isAlive;

        public SerialTaskWorker()
        {
            _isAlive = true;
            _tasks = new ConcurrentQueue<WorkObject>();
            _event = new ManualResetEventSlim(true);
            _workerThread = new Thread(Worker);
            _workerThread.Start();
        }

        public void Queue(Action action)
        {
            _tasks.Enqueue(new WorkObject { WorkAction = action });
            _event.Set();
        }

        public void Queue(Func<Task> task)
        {
            _tasks.Enqueue(new WorkObject { WorkTask = task });
            _event.Set();
        }

        private async void Worker()
        {
            while (_isAlive)
            {
                _event.Wait();
                while (true)
                {
                    _event.Reset();
                    WorkObject task;
                    if (!_tasks.TryDequeue(out task))
                    {
                        break;
                    }
                    if (task.WorkTask != null)
                    {
                        await task.WorkTask();
                    }
                    if (task.WorkAction != null)
                    {
                        task.WorkAction();
                    }
                }
            }
            _event.Dispose();
        }

        public void Dispose()
        {
            _isAlive = false;
            _event.Set();
        }

        private class WorkObject
        {
            public Func<Task> WorkTask { get; set; }

            public Action WorkAction { get; set; }
        }
    }
}
