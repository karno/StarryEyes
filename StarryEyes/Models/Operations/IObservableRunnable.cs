using System;
using System.Reactive;

namespace StarryEyes.Models.Operations
{
    public interface IRunnerQueueable
    {
        IObservable<Unit> Run();
    }
}
