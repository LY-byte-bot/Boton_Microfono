using Microsoft.Win32;

namespace BotonMicrofono;

internal static class StartupManager
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "BotonMicrofonoLY";

    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
            }
            catch { return false; }
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true)
            ?? throw new InvalidOperationException("No se pudo abrir la configuración de inicio de Windows.");

        if (enabled)
            key.SetValue(ValueName, $"\"{Application.ExecutablePath}\"", RegistryValueKind.String);
        else
            key.DeleteValue(ValueName, false);
    }
}
