using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace StarryEyes.Models.Subsystems.Notifications.Audio
{
    public static class AudioPlayer
    {
        private const int SampleRate = 44100;
        private const int ChannelCount = 2;

        private static readonly MixingSampleProvider _mixer;

        static AudioPlayer()
        {
            var output = new WaveOutEvent();
            _mixer = new MixingSampleProvider(
                WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, ChannelCount));
            _mixer.ReadFully = true;
            output.Init(_mixer);
            output.Play();
        }

        public static void PlaySound(string fileName)
        {
            AddMixerInput(new AutoDisposeFileReader(new AudioFileReader(fileName)));
        }

        public static void PlaySound(AudioCache cache)
        {
            AddMixerInput(new AudioCacheSampleProvider(cache));
        }

        private static void AddMixerInput(ISampleProvider input)
        {
            _mixer.AddMixerInput(input);
        }
    }
}
