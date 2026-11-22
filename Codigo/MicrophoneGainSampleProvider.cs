using NAudio.Wave;

namespace BotonMicrofono;

/// <summary>Volumen lineal hasta 100 % y preamplificación logarítmica hasta +20 dB.</summary>
internal sealed class MicrophoneGainSampleProvider : ISampleProvider
{
    private readonly ISampleProvider source;
    private int level = 100;
    public WaveFormat WaveFormat => source.WaveFormat;

    public int Level
    {
        get => level;
        set => level = Math.Clamp(value, 0, 200);
    }

    public MicrophoneGainSampleProvider(ISampleProvider source) => this.source = source;

    public int Read(float[] buffer, int offset, int count)
    {
        var read = source.Read(buffer, offset, count);
        var gain = level <= 100
            ? level / 100f
            : MathF.Pow(10f, ((level - 100) / 5f) / 20f); // 200 % equivale a +20 dB (10x).

        for (var i = offset; i < offset + read; i++)
            buffer[i] *= gain;
        return read;
    }
}
