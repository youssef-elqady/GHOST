using CommunityToolkit.Mvvm.ComponentModel;
using GHOST.Application.Devices;
using GHOST.Domain.Enums;

namespace GHOST.Presentation.ViewModels;

public sealed class DeviceCardViewModel : ObservableObject
{
    public DeviceCardViewModel(DeviceSummary summary)
    {
        Id = summary.Id; Name = summary.Name; Type = summary.DeviceType; Status = summary.Status; RoomName = summary.RoomName ?? "—";
        CurrentPrice = summary.CurrentPrice is null ? "غير محدد" : $"{summary.CurrentPrice:N2} EGP/ساعة";
        ActiveSession = summary.ActiveSessionId is null ? "لا توجد جلسة" : summary.ActiveSessionId.Value.ToString("N")[..8];
        Runtime = summary.Runtime is null ? "—" : $"{(int)summary.Runtime.Value.TotalHours:00}:{summary.Runtime.Value.Minutes:00}:{summary.Runtime.Value.Seconds:00}";
        CurrentAmount = summary.CurrentAmount is null ? "—" : $"{summary.CurrentAmount:N2} EGP";
    }
    public Guid Id { get; }
    public string Name { get; }
    public string Type { get; }
    public DeviceStatus Status { get; }
    public string StatusText => Status switch { DeviceStatus.Available => "متاح", DeviceStatus.Running => "قيد التشغيل", DeviceStatus.Paused => "متوقف مؤقتاً", DeviceStatus.Reserved => "محجوز", DeviceStatus.Maintenance => "صيانة", DeviceStatus.Offline => "غير متصل", _ => Status.ToString() };
    public string RoomName { get; }
    public string CurrentPrice { get; }
    public string ActiveSession { get; }
    public string Runtime { get; }
    public string CurrentAmount { get; }
}
