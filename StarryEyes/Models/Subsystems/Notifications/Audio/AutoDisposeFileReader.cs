using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using NAudio.Wave;

namespace StarryEyes.Models.Subsystems.Notifications.Audio
{
    public sealed class AutoDisposeSampleProvider : ISampleProvider, IDisposable
    {
        private bool _isDisposed;
        private readonly ISampleProvider _provider;
        private readonly CompositeDisposable _disposables;

        public WaveFormat WaveFormat { get; private set; }

        public AutoDisposeSampleProvider(ISampleProvider provider,
             params IDisposable[] disposables)
            : this(provider, (IEnumerable<IDisposable>)disposables)
        {
        }

        public AutoDisposeSampleProvider(ISampleProvider provider,
             IEnumerable<IDisposable> disposables)
        {
            this._provider = provider;
            this._disposables = new CompositeDisposable(disposables);
            this.WaveFormat = provider.WaveFormat;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (this._isDisposed)
            {
                return 0;
            }
            var read = this._provider.Read(buffer, offset, count);
            if (read == 0)
            {
                this.Dispose();
            }
            return read;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _disposables.Dispose();
            }
        }
    }
}
