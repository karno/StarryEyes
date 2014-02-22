using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace StarryEyes.Models.Subsystems.Notifications.Audio
{
    public sealed class AudioCache
    {
        public float[] AudioData { get; private set; }

        public WaveFormat WaveFormat { get; private set; }

        public AudioCache(string audioFileName)
        {
            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }
                AudioData = wholeFile.ToArray();
            }
        }
    }
}
