using System;
using System.Reactive.Disposables;
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
            var disposables = new CompositeDisposable();
            var sampleProvider = GetSampleProvider(fileName, disposables);
            var wrapper = new AutoDisposeSampleProvider(sampleProvider, (IDisposable)disposables);
            AddMixerInput(wrapper);
        }

        private static ISampleProvider GetSampleProvider(string fileName, CompositeDisposable disposables)
        {
            try
            {
                var wfr = new WaveFileReader(fileName);
                return ReadWaveFile(wfr, disposables);
            }
            catch { }
            try
            {
                var mfr = new Mp3FileReader(fileName);
                return ReadMp3File(mfr, disposables);
            }
            catch { }
            throw new ArgumentException("This audio file is not supported yet.");
        }

        private static ISampleProvider ReadWaveFile(WaveFileReader reader, CompositeDisposable disposables)
        {
            disposables.Add(reader);
            // if resampling is needed, do it.
            if (reader.WaveFormat.SampleRate != SampleRate)
            {
                var resampler = new MediaFoundationResampler(reader,
                        WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, ChannelCount));
                disposables.Add(resampler);
                return resampler.ToSampleProvider();
            }
            return reader.ToSampleProvider();
        }

        private static ISampleProvider ReadMp3File(Mp3FileReader reader, CompositeDisposable disposables)
        {
            disposables.Add(reader);
            return reader.ToSampleProvider();
        }

        public static void PlaySound(AudioCache cache)
        {
            AddMixerInput(new AudioCacheSampleProvider(cache));
        }

        private static void AddMixerInput(ISampleProvider input)
        {
            _mixer.AddMixerInput(PrepareSound(input));
        }

        private static ISampleProvider PrepareSound(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == _mixer.WaveFormat.Channels &&
                input.WaveFormat.SampleRate == _mixer.WaveFormat.SampleRate)
            {
                // correctly formatted wave audio file.
                return input;
            }
            if (input.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
            {
                // mono -> stereo
                return new MonoToStereoSampleProvider(input);
            }
            else
            {
            }
            throw new ArgumentException("This audio source is not supported yet.");
        }
    }
}
