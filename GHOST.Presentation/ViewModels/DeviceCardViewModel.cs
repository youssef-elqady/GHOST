using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GHOST.Application.Devices;
using GHOST.Domain.Enums;

namespace GHOST.Presentation.ViewModels;

public sealed partial class DeviceCardViewModel : ObservableObject
{
    private readonly TimeSpan? _runtimeSnapshot;
    private readonly DateTimeOffset _snapshotAtUtc;
    private readonly decimal _currentRatePerHour;

    private string _runtime;
    private string _currentAmount;

    public DeviceCardViewModel(DeviceSummary summary)
    {
        Id = summary.Id;
        Name = summary.Name;
        Type = summary.DeviceType;
        Status = summary.Status;

        RoomName = string.IsNullOrWhiteSpace(summary.RoomName)
            ? "بدون غرفة"
            : summary.RoomName;

        ActiveSessionId = summary.ActiveSessionId;
        ActiveSessionMode = summary.ActiveSessionMode;

        SingleRate = summary.SingleRate;
        MultiRate = summary.MultiRate;

        _currentRatePerHour = summary.CurrentPrice ?? 0m;

        HasAirConditioning = summary.HasAirConditioning;

        CurrentPrice = summary.CurrentPrice is null
            ? "لا توجد جلسة"
            : $"{summary.CurrentPrice:N2} ج.م / ساعة";

        SingleRateText = $"{summary.SingleRate:N2} ج.م / ساعة";
        MultiRateText = $"{summary.MultiRate:N2} ج.م / ساعة";

        AirConditioningText = summary.HasAirConditioning
            ? "تكييف"
            : "بدون تكييف";

        ActiveSession = summary.ActiveSessionId is null
            ? "لا توجد جلسة"
            : summary.ActiveSessionId.Value.ToString("N")[..8];

        _runtimeSnapshot = summary.Runtime;
        _snapshotAtUtc = DateTimeOffset.UtcNow;

        _runtime = summary.Runtime is null
            ? "—"
            : FormatRuntime(summary.Runtime.Value);

        _currentAmount = summary.CurrentAmount is null
            ? "—"
            : $"{summary.CurrentAmount:N2} ج.م";
    }

    public Guid Id { get; }

    public Guid? ActiveSessionId { get; }

    public SessionMode? ActiveSessionMode { get; }

    [ObservableProperty]
    private SessionMode selectedSessionMode = SessionMode.Single;

    // ============================================================
    // SESSION MODE DISPLAY
    // ============================================================

    public bool HasActiveSessionMode =>
        ActiveSessionId.HasValue &&
        ActiveSessionMode.HasValue;

    public bool IsActiveSingle =>
        ActiveSessionMode == SessionMode.Single;

    public bool IsActiveMulti =>
        ActiveSessionMode == SessionMode.Multi;

    public string ActiveSessionModeText =>
        ActiveSessionMode switch
        {
            SessionMode.Single => "فردي",
            SessionMode.Multi => "مالتي",
            _ => "—"
        };

    public string ActiveSessionRateText =>
        ActiveSessionMode switch
        {
            SessionMode.Single => $"{SingleRate:N2} ج.م / ساعة",
            SessionMode.Multi => $"{MultiRate:N2} ج.م / ساعة",
            _ => "—"
        };

    public string SelectedSessionModeText =>
        SelectedSessionMode == SessionMode.Single
            ? "فردي"
            : "مالتي";

    public bool IsSingleSelected =>
        SelectedSessionMode == SessionMode.Single;

    public bool IsMultiSelected =>
        SelectedSessionMode == SessionMode.Multi;

    public bool ShowSessionModeSelector =>
        CanStartSession;

    public bool ShowActiveSessionMode =>
        HasActiveSessionMode;

    [RelayCommand]
    private void SelectSingle()
    {
        if (!CanStartSession)
            return;

        SelectedSessionMode = SessionMode.Single;
    }

    [RelayCommand]
    private void SelectMulti()
    {
        if (!CanStartSession)
            return;

        SelectedSessionMode = SessionMode.Multi;
    }

    partial void OnSelectedSessionModeChanged(SessionMode value)
    {
        OnPropertyChanged(nameof(SelectedSessionModeText));
        OnPropertyChanged(nameof(IsSingleSelected));
        OnPropertyChanged(nameof(IsMultiSelected));
    }

    // ============================================================
    // BASIC DEVICE DATA
    // ============================================================

    public string Name { get; }

    public string Type { get; }

    public DeviceStatus Status { get; }

    public string StatusText =>
        Status switch
        {
            DeviceStatus.Available => "متاح",
            DeviceStatus.Running => "قيد التشغيل",
            DeviceStatus.Paused => "متوقف مؤقتًا",
            DeviceStatus.Reserved => "محجوز",
            DeviceStatus.Maintenance => "صيانة",
            DeviceStatus.Offline => "غير متصل",
            _ => Status.ToString()
        };

    public string RoomName { get; }

    public decimal SingleRate { get; }

    public decimal MultiRate { get; }

    public bool HasAirConditioning { get; }

    public string SingleRateText { get; }

    public string MultiRateText { get; }

    public string AirConditioningText { get; }

    public string CurrentPrice { get; }

    public string ActiveSession { get; }

    // ============================================================
    // LIVE SESSION
    // ============================================================

    public string Runtime
    {
        get => _runtime;
        private set => SetProperty(ref _runtime, value);
    }

    public string CurrentAmount
    {
        get => _currentAmount;
        private set => SetProperty(ref _currentAmount, value);
    }

    public bool IsAvailable =>
        Status == DeviceStatus.Available;

    public bool IsRunning =>
        Status == DeviceStatus.Running;

    public bool IsPaused =>
        Status == DeviceStatus.Paused;

    public bool IsReserved =>
        Status == DeviceStatus.Reserved;

    public bool IsMaintenance =>
        Status == DeviceStatus.Maintenance;

    public bool IsOffline =>
        Status == DeviceStatus.Offline;

    public bool HasActiveSession =>
        ActiveSessionId.HasValue;

    public bool CanStartSession =>
        Status == DeviceStatus.Available;

    public bool CanPauseSession =>
        Status == DeviceStatus.Running &&
        HasActiveSession;

    public bool CanResumeSession =>
        Status == DeviceStatus.Paused &&
        HasActiveSession;

    public bool CanEndSession =>
        (Status == DeviceStatus.Running ||
         Status == DeviceStatus.Paused) &&
        HasActiveSession;

    // ============================================================
    // LIVE UPDATE
    // ============================================================

    public void UpdateLiveState()
    {
        if (!HasActiveSession || _runtimeSnapshot is null)
        {
            if (Runtime != "—")
                Runtime = "—";

            if (CurrentAmount != "—")
                CurrentAmount = "—";

            return;
        }

        var runtime = _runtimeSnapshot.Value;

        if (Status == DeviceStatus.Running)
        {
            var elapsed = DateTimeOffset.UtcNow - _snapshotAtUtc;

            if (elapsed > TimeSpan.Zero)
                runtime += elapsed;
        }

        Runtime = FormatRuntime(runtime);

        UpdateLiveAmount(runtime);
    }

    private void UpdateLiveAmount(TimeSpan runtime)
    {
        if (_currentRatePerHour <= 0)
        {
            CurrentAmount = "0.00 ج.م";
            return;
        }

        var billableMinutes = Math.Ceiling(runtime.TotalMinutes);

        var amount =
            _currentRatePerHour *
            ((decimal)billableMinutes / 60m);

        amount = decimal.Round(
            amount,
            2,
            MidpointRounding.AwayFromZero);

        CurrentAmount = $"{amount:N2} ج.م";
    }

    private static string FormatRuntime(TimeSpan runtime)
    {
        if (runtime < TimeSpan.Zero)
            runtime = TimeSpan.Zero;

        return $"{(int)runtime.TotalHours:00}:{runtime.Minutes:00}:{runtime.Seconds:00}";
    }
}