using GHOST.Application.Devices;

namespace GHOST.Presentation.ViewModels;

public sealed class RoomViewModel(RoomSummary summary)
{
    public string Name { get; } = summary.Name;
    public int DeviceCount { get; } = summary.DeviceCount;
}
