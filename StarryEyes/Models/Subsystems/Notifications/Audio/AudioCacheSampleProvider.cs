using System;
using NAudio.Wave;

namespace StarryEyes.Models.Subsystems.Notifications.Audio
{
    public class AudioCacheSampleProvider : ISampleProvider
    {
        private readonly AudioCache _cache;
        private long _position;

        public AudioCacheSampleProvider(AudioCache cache)
        {
            this._cache = cache;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = this._cache.AudioData.Length - this._position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(this._cache.AudioData, this._position, buffer, offset, samplesToCopy);
            this._position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat { get { return this._cache.WaveFormat; } }
    }
}
