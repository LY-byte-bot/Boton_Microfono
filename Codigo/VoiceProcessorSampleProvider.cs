using NAudio.Dsp;
using NAudio.Wave;

namespace BotonMicrofono;

internal sealed class VoiceProcessorSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private volatile ProcessorState state;
    public WaveFormat WaveFormat => source.WaveFormat;

    public VoiceProcessorSampleProvider(ISampleProvider source, AppSettings settings)
    {
        this.source = source;
        state = ProcessorState.Create(WaveFormat, settings);
    }

    public void Configure(AppSettings settings) => state = ProcessorState.Create(WaveFormat, settings);

    public int Read(float[] buffer, int offset, int count)
    {
        var read = source.Read(buffer, offset, count);
        var current = state;
        for (var i = offset; i < offset + read; i++)
        {
            var channel = (i - offset) % current.Channels;
            var sample = buffer[i];
            if (current.EqualizerEnabled)
            {
                sample = current.Bass[channel].Transform(sample);
                sample = current.Mid[channel].Transform(sample);
                sample = current.Treble[channel].Transform(sample);
            }

            if (current.CompressorEnabled)
            {
                var magnitude = MathF.Abs(sample);
                var coefficient = magnitude > current.Envelope ? current.Attack : current.Release;
                current.Envelope = coefficient * current.Envelope + (1f - coefficient) * magnitude;
                var levelDb = 20f * MathF.Log10(Math.Max(current.Envelope, 0.000001f));
                var reductionDb = levelDb > current.ThresholdDb
                    ? current.ThresholdDb + (levelDb - current.ThresholdDb) / current.Ratio - levelDb
                    : 0f;
                sample *= DbToLinear(reductionDb + current.MakeupDb);
            }

            if (current.LimiterEnabled)
                sample = Math.Clamp(sample, -current.Ceiling, current.Ceiling);
            buffer[i] = sample;
        }
        return read;
    }

    private static float DbToLinear(float db) => MathF.Pow(10f, db / 20f);

    private sealed class ProcessorState
    {
        public int Channels { get; init; }
        public bool EqualizerEnabled { get; init; }
        public BiQuadFilter[] Bass { get; init; } = [];
        public BiQuadFilter[] Mid { get; init; } = [];
        public BiQuadFilter[] Treble { get; init; } = [];
        public bool CompressorEnabled { get; init; }
        public float ThresholdDb { get; init; }
        public float Ratio { get; init; }
        public float MakeupDb { get; init; }
        public float Attack { get; init; }
        public float Release { get; init; }
        public float Envelope;
        public bool LimiterEnabled { get; init; }
        public float Ceiling { get; init; }

        public static ProcessorState Create(WaveFormat format, AppSettings settings)
        {
            var channels = Math.Max(1, format.Channels);
            var bass = new BiQuadFilter[channels];
            var mid = new BiQuadFilter[channels];
            var treble = new BiQuadFilter[channels];
            for (var i = 0; i < channels; i++)
            {
                bass[i] = BiQuadFilter.LowShelf(format.SampleRate, 120f, 0.707f, settings.BassDb);
                mid[i] = BiQuadFilter.PeakingEQ(format.SampleRate, 1200f, 1f, settings.MidDb);
                treble[i] = BiQuadFilter.HighShelf(format.SampleRate, 6000f, 0.707f, settings.TrebleDb);
            }
            return new ProcessorState
            {
                Channels = channels,
                EqualizerEnabled = settings.EqualizerEnabled,
                Bass = bass, Mid = mid, Treble = treble,
                CompressorEnabled = settings.CompressorEnabled,
                ThresholdDb = Math.Clamp(settings.CompressorThresholdDb, -40f, -3f),
                Ratio = Math.Clamp(settings.CompressorRatio, 1f, 10f),
                MakeupDb = Math.Clamp(settings.CompressorMakeupDb, 0f, 18f),
                Attack = MathF.Exp(-1f / (format.SampleRate * 0.010f)),
                Release = MathF.Exp(-1f / (format.SampleRate * 0.120f)),
                LimiterEnabled = settings.LimiterEnabled,
                Ceiling = DbToLinear(Math.Clamp(settings.LimiterCeilingDb, -12f, 0f))
            };
        }
    }
}
