using NAudio.CoreAudioApi;

namespace BotonMicrofono;

internal sealed class DeviceItem
{
    public MMDevice Device { get; }
    public string Id => Device.ID;
    public DeviceItem(MMDevice device) => Device = device;
    public override string ToString() => Device.FriendlyName;
}
