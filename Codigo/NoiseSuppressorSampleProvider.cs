using NAudio.Dsp;
using NAudio.Wave;

namespace BotonMicrofono;

/// <summary>Filtro pasa-altos y expansor descendente local con transiciones suaves.</summary>
internal sealed class NoiseSuppressorSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private volatile State state;
    public WaveFormat WaveFormat => source.WaveFormat;

    public NoiseSuppressorSampleProvider(ISampleProvider source, AppSettings settings)
    {
        this.source = source;
        state = State.Create(WaveFormat, settings);
    }

    public void Configure(AppSettings settings) => state = State.Create(WaveFormat, settings);

    public int Read(float[] buffer, int offset, int count)
    {
        var read = source.Read(buffer, offset, count);
        var current = state;
        if (!current.Enabled) return read;

        for (var i = offset; i < offset + read; i++)
        {
            var channel = (i - offset) % current.Channels;
            var sample = current.HighPass[channel].Transform(buffer[i]);
            var magnitude = MathF.Abs(sample);
            var envelopeCoefficient = magnitude > current.Envelope ? current.EnvelopeAttack : current.EnvelopeRelease;
            current.Envelope = envelopeCoefficient * current.Envelope + (1f - envelopeCoefficient) * magnitude;
            var desiredGain = current.Envelope >= current.Threshold ? 1f : current.ClosedGain;
            var gainCoefficient = desiredGain > current.Gain ? current.OpenAttack : current.CloseRelease;
            current.Gain = gainCoefficient * current.Gain + (1f - gainCoefficient) * desiredGain;
            buffer[i] = sample * current.Gain;
        }
        return read;
    }

    private sealed class State
    {
        public bool Enabled { get; init; }
        public int Channels { get; init; }
        public BiQuadFilter[] HighPass { get; init; } = [];
        public float Threshold { get; init; }
        public float ClosedGain { get; init; }
        public float EnvelopeAttack { get; init; }
        public float EnvelopeRelease { get; init; }
        public float OpenAttack { get; init; }
        public float CloseRelease { get; init; }
        public float Envelope;
        public float Gain = 1f;

        public static State Create(WaveFormat format, AppSettings settings)
        {
            var channels = Math.Max(1, format.Channels);
            var filters = new BiQuadFilter[channels];
            for (var i = 0; i < channels; i++)
                filters[i] = BiQuadFilter.HighPassFilter(format.SampleRate, 80f, 0.707f);
            return new State
            {
                Enabled = settings.NoiseSuppressionEnabled,
                Channels = channels,
                HighPass = filters,
                Threshold = DbToLinear(Math.Clamp(settings.NoiseThresholdDb, -70f, -20f)),
                ClosedGain = DbToLinear(-Math.Clamp(settings.NoiseReductionDb, 0f, 48f)),
                EnvelopeAttack = Coefficient(format.SampleRate, 0.008f),
                EnvelopeRelease = Coefficient(format.SampleRate, 0.120f),
                OpenAttack = Coefficient(format.SampleRate, 0.012f),
                CloseRelease = Coefficient(format.SampleRate, 0.220f)
            };
        }

        private static float DbToLinear(float db) => MathF.Pow(10f, db / 20f);
        private static float Coefficient(int sampleRate, float seconds) => MathF.Exp(-1f / (sampleRate * seconds));
    }
}
