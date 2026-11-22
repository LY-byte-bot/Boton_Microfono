using System.Text.Json;

namespace BotonMicrofono;

internal sealed class AppSettings
{
    public string? InputDeviceId { get; set; }
    public string? OutputDeviceId { get; set; }
    public int Left { get; set; } = 20;
    public int Top { get; set; } = 20;
    public bool AlwaysOnTop { get; set; } = true;
    public bool Muted { get; set; } = true;
    public int OutputLevel { get; set; } = 100;
    public bool DuckingEnabled { get; set; } = true;
    public int DuckingLevel { get; set; } = 50;
    public bool CompressorEnabled { get; set; } = true;
    public float CompressorThresholdDb { get; set; } = -18f;
    public float CompressorRatio { get; set; } = 3f;
    public float CompressorMakeupDb { get; set; } = 6f;
    public bool LimiterEnabled { get; set; } = true;
    public float LimiterCeilingDb { get; set; } = -1f;
    public bool EqualizerEnabled { get; set; }
    public float BassDb { get; set; }
    public float MidDb { get; set; }
    public float TrebleDb { get; set; }
    public bool NoiseSuppressionEnabled { get; set; } = true;
    public float NoiseThresholdDb { get; set; } = -45f;
    public float NoiseReductionDb { get; set; } = 24f;
    public string ThemeName { get; set; } = "Clásico";
    public int ButtonSize { get; set; } = 58;

    private static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BotonMicrofono");
    private static string FilePath => Path.Combine(Folder, "config.json");

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new()
                : new();
        }
        catch { return new(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* La aplicación sigue funcionando aunque no pueda recordar ajustes. */ }
    }
}
