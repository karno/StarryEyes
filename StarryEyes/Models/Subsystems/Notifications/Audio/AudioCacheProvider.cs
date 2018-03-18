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
            _cache = cache;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = _cache.AudioData.Length - _position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(_cache.AudioData, _position, buffer, offset, samplesToCopy);
            _position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat => _cache.WaveFormat;
    }
}