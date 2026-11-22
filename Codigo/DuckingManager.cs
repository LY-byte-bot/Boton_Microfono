using System.Diagnostics;
using NAudio.CoreAudioApi;

namespace BotonMicrofono;

internal sealed class DuckingManager : IDisposable
{
    private readonly Dictionary<string, float> changedSessions = new(StringComparer.Ordinal);
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private double lastUpdate;
    private float currentFactor = 1f;
    private float targetFactor = 1f;
    public bool IsActive => changedSessions.Count > 0 || currentFactor < 0.999f;

    public void SetDucking(bool enabled, int targetPercent)
    {
        targetFactor = enabled ? Math.Clamp(targetPercent, 0, 100) / 100f : 1f;
    }

    public void Tick()
    {
        var now = clock.Elapsed.TotalSeconds;
        var elapsed = lastUpdate <= 0 ? 0.05 : Math.Clamp(now - lastUpdate, 0.01, 0.25);
        lastUpdate = now;

        var fadingDown = targetFactor < currentFactor;
        var duration = fadingDown ? 0.60 : 0.90;
        var distance = Math.Abs(targetFactor - currentFactor);
        if (distance > 0.0001f)
        {
            var fullRangeStep = (float)(elapsed / duration);
            currentFactor = fadingDown
                ? Math.Max(targetFactor, currentFactor - fullRangeStep)
                : Math.Min(targetFactor, currentFactor + fullRangeStep);
        }

        if (targetFactor < 0.999f) CaptureNewSessions();
        ApplyCurrentFactor();

        if (targetFactor >= 0.999f && currentFactor >= 0.999f)
        {
            RestoreExact();
            changedSessions.Clear();
            currentFactor = 1f;
        }
    }

    private void CaptureNewSessions()
    {
        VisitSessions((deviceId, session) =>
        {
            if (session.GetProcessID == (uint)Environment.ProcessId) return;
            var key = deviceId + "|" + session.GetSessionInstanceIdentifier;
            if (changedSessions.ContainsKey(key)) return;
            var volume = session.SimpleAudioVolume.Volume;
            if (volume >= 0.995f) changedSessions[key] = volume; // Solo las que estaban al 100 %.
        });
    }

    private void ApplyCurrentFactor()
    {
        if (changedSessions.Count == 0) return;
        VisitSessions((deviceId, session) =>
        {
            var key = deviceId + "|" + session.GetSessionInstanceIdentifier;
            if (changedSessions.TryGetValue(key, out var original))
                session.SimpleAudioVolume.Volume = original * currentFactor;
        });
    }

    private void RestoreExact()
    {
        VisitSessions((deviceId, session) =>
        {
            var key = deviceId + "|" + session.GetSessionInstanceIdentifier;
            if (changedSessions.TryGetValue(key, out var original))
                session.SimpleAudioVolume.Volume = original;
        });
    }

    public void RestoreImmediately()
    {
        RestoreExact();
        changedSessions.Clear();
        currentFactor = targetFactor = 1f;
    }

    private static void VisitSessions(Action<string, AudioSessionControl> action)
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                using (device)
                {
                    var sessions = device.AudioSessionManager.Sessions;
                    for (var i = 0; i < sessions.Count; i++)
                    {
                        using var session = sessions[i];
                        try { action(device.ID, session); } catch { }
                    }
                }
            }
        }
        catch { }
    }

    public void Dispose() => RestoreImmediately();
}
