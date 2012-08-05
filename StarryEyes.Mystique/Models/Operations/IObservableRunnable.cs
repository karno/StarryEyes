using System;
using System.Reactive;

namespace StarryEyes.Mystique.Models.Operations
{
    public interface IRunnerQueueable
    {
        IObservable<Unit> Run();
    }
}
