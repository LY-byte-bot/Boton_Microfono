using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BotonMicrofono;

internal sealed class AudioRouter : IDisposable
{
    private WasapiCapture? capture;
    private WasapiOut? output;
    private BufferedWaveProvider? buffer;
    private MicrophoneGainSampleProvider? gainProvider;
    private NoiseSuppressorSampleProvider? noiseSuppressor;
    private VoiceProcessorSampleProvider? voiceProcessor;
    private readonly object sync = new();
    public bool Muted { get; set; } = true;
    public event Action<float>? LevelChanged;
    public int OutputLevel
    {
        get => gainProvider?.Level ?? pendingLevel;
        set
        {
            pendingLevel = Math.Clamp(value, 0, 200);
            if (gainProvider is not null) gainProvider.Level = pendingLevel;
        }
    }
    private int pendingLevel = 100;
    private AppSettings? pendingProcessingSettings;

    public void ConfigureProcessing(AppSettings settings)
    {
        pendingProcessingSettings = settings;
        noiseSuppressor?.Configure(settings);
        voiceProcessor?.Configure(settings);
    }
    public bool Running => capture is not null;

    public void Start(MMDevice input, MMDevice outputDevice)
    {
        Stop();
        lock (sync)
        {
            capture = new WasapiCapture(input);
            buffer = new BufferedWaveProvider(capture.WaveFormat)
            {
                BufferDuration = TimeSpan.FromMilliseconds(300),
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };
            var meter = new MeteringSampleProvider(buffer.ToSampleProvider(), 100);
            meter.StreamVolume += (_, e) =>
            {
                var peak = e.MaxSampleValues.Length == 0 ? 0f : e.MaxSampleValues.Max();
                LevelChanged?.Invoke(Math.Clamp(peak, 0f, 1f));
            };
            noiseSuppressor = new NoiseSuppressorSampleProvider(meter, pendingProcessingSettings ?? new AppSettings());
            gainProvider = new MicrophoneGainSampleProvider(noiseSuppressor) { Level = pendingLevel };
            voiceProcessor = new VoiceProcessorSampleProvider(gainProvider, pendingProcessingSettings ?? new AppSettings());
            output = new WasapiOut(outputDevice, AudioClientShareMode.Shared, true, 60);
            output.Init(voiceProcessor);
            capture.DataAvailable += OnDataAvailable;
            capture.RecordingStopped += OnRecordingStopped;
            output.Play();
            capture.StartRecording();
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!Muted) buffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);
        else
        {
            buffer?.ClearBuffer();
            LevelChanged?.Invoke(0f);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            System.Diagnostics.Debug.WriteLine(e.Exception);
    }

    public void Stop()
    {
        lock (sync)
        {
            if (capture is not null)
            {
                capture.DataAvailable -= OnDataAvailable;
                capture.RecordingStopped -= OnRecordingStopped;
                try { capture.StopRecording(); } catch { }
                capture.Dispose();
                capture = null;
            }
            if (output is not null)
            {
                try { output.Stop(); } catch { }
                output.Dispose();
                output = null;
            }
            buffer = null;
            gainProvider = null;
            noiseSuppressor = null;
            voiceProcessor = null;
        }
    }

    public void Dispose() => Stop();
}
