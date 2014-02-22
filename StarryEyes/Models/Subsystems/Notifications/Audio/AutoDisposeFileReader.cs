using NAudio.Wave;

namespace StarryEyes.Models.Subsystems.Notifications.Audio
{
    public sealed class AutoDisposeFileReader : ISampleProvider
    {
        private readonly AudioFileReader _reader;
        private bool _isDisposed;
        public AutoDisposeFileReader(AudioFileReader reader)
        {
            this._reader = reader;
            this.WaveFormat = reader.WaveFormat;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (this._isDisposed)
                return 0;
            int read = this._reader.Read(buffer, offset, count);
            if (read == 0)
            {
                this._reader.Dispose();
                this._isDisposed = true;
            }
            return read;
        }

        public WaveFormat WaveFormat { get; private set; }
    }
}
