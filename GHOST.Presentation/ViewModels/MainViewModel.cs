using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GHOST.Application.Authentication;
using GHOST.Application.Devices;
using GHOST.Application.Sessions;
using GHOST.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace GHOST.Presentation.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentUserContext _currentUser;
    private readonly DispatcherTimer _liveTimer;

    public MainViewModel(IServiceScopeFactory scopeFactory, ICurrentUserContext currentUser)
    {
        _scopeFactory = scopeFactory;
        _currentUser = currentUser;

        _liveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _liveTimer.Tick += OnLiveTimerTick;
        _liveTimer.Start();
    }

    public ObservableCollection<DeviceCardViewModel> Devices { get; } = [];
    public ObservableCollection<RoomViewModel> Rooms { get; } = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string loadError = string.Empty;

    [ObservableProperty]
    private Guid? paymentSessionId;

    [ObservableProperty]
    private decimal? amountDue;

    [ObservableProperty]
    private string paymentNotice = string.Empty;

    public string AdministrationNotice =>
        "إدارة الأجهزة محمية: يلزم تسجيل دخول مسؤول أو مدير قبل تفعيل أوامر الإدارة.";

    public string CurrentUserName =>
        _currentUser.Current?.Username ?? string.Empty;

    public bool CanManageDevices =>
        _currentUser.IsInRole("Admin") || _currentUser.IsInRole("Manager");

    // ============================================================
    // LIVE DASHBOARD INDICATORS
    // ============================================================

    public int TotalDevices => Devices.Count;

    public int ActiveSessionsCount =>
        Devices.Count(x => x.HasActiveSession);

    public int RunningDevicesCount =>
        Devices.Count(x => x.IsRunning);

    public int PausedSessionsCount =>
        Devices.Count(x => x.IsPaused);

    public int AvailableDevicesCount =>
        Devices.Count(x => x.IsAvailable);

    public int AttentionCount =>
        Devices.Count(x =>
            x.IsPaused ||
            x.IsMaintenance ||
            x.IsOffline);

    public int ReservedDevicesCount =>
        Devices.Count(x => x.IsReserved);

    public int TotalRooms => Rooms.Count;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        LoadError = string.Empty;

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var dashboard = scope.ServiceProvider
                .GetRequiredService<IDeviceDashboardService>();

            var devices = await dashboard.GetDevicesAsync();
            var rooms = await dashboard.GetRoomsAsync();

            Devices.Clear();
            foreach (var device in devices)
                Devices.Add(new DeviceCardViewModel(device));

            Rooms.Clear();
            foreach (var room in rooms)
                Rooms.Add(new RoomViewModel(room));

            NotifyDashboardCounters();
        }
        catch (Exception)
        {
            LoadError = "تعذر تحميل الأجهزة. تم تسجيل المشكلة.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartSessionAsync(DeviceCardViewModel device)
    {
        if (device.Status != DeviceStatus.Available)
        {
            LoadError = "الجهاز غير متاح لبدء جلسة.";
            return;
        }

        await ExecuteSessionAsync(service =>
            service.StartAsync(new StartSessionRequest(device.Id)));
    }

    [RelayCommand]
    private async Task PauseOrResumeAsync(DeviceCardViewModel device)
    {
        if (device.ActiveSessionId is null)
            return;

        await ExecuteSessionAsync(service =>
            device.Status == DeviceStatus.Running
                ? service.PauseAsync(device.ActiveSessionId.Value)
                : service.ResumeAsync(device.ActiveSessionId.Value));
    }

    [RelayCommand]
    private async Task EndSessionAsync(DeviceCardViewModel device)
    {
        if (device.ActiveSessionId is null)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<ISessionService>();

            await service.EndAsync(device.ActiveSessionId.Value);

            var summary = await service
                .GetPaymentSummaryAsync(device.ActiveSessionId.Value);

            PaymentSessionId = summary.SessionId;
            AmountDue = summary.AmountDue;
            PaymentNotice = "أدخل المبلغ المستلم لإتمام الدفع.";

            await RefreshAsync();
        }
        catch (Exception exception)
        {
            LoadError = exception.Message;
        }
    }

    [RelayCommand]
    private async Task PaySessionAsync(string amountReceived)
    {
        if (PaymentSessionId is null || AmountDue is null)
        {
            PaymentNotice = "اختر جلسة مكتملة أولًا.";
            return;
        }

        if (!decimal.TryParse(amountReceived, out var received) || received < 0)
        {
            PaymentNotice = "أدخل مبلغًا نقديًا صحيحًا وغير سالب.";
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<ISessionService>();

            await service.TakeCashPaymentAsync(
                new CashPaymentRequest(PaymentSessionId.Value, received));

            PaymentNotice =
                $"تم تسجيل الدفع. الباقي: {(received - AmountDue.Value):N2} EGP";

            PaymentSessionId = null;
            AmountDue = null;

            await RefreshAsync();
        }
        catch (Exception exception)
        {
            PaymentNotice = exception.Message;
        }
    }

    private async Task ExecuteSessionAsync(Func<ISessionService, Task> operation)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var service = scope.ServiceProvider
                .GetRequiredService<ISessionService>();

            await operation(service);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            LoadError = exception.Message;
        }
    }

    private void OnLiveTimerTick(object? sender, EventArgs e)
    {
        foreach (var device in Devices)
            device.UpdateLiveState();
    }

    private void NotifyDashboardCounters()
    {
        OnPropertyChanged(nameof(TotalDevices));
        OnPropertyChanged(nameof(ActiveSessionsCount));
        OnPropertyChanged(nameof(RunningDevicesCount));
        OnPropertyChanged(nameof(PausedSessionsCount));
        OnPropertyChanged(nameof(AvailableDevicesCount));
        OnPropertyChanged(nameof(AttentionCount));
        OnPropertyChanged(nameof(ReservedDevicesCount));
        OnPropertyChanged(nameof(TotalRooms));
    }

    public void Dispose()
    {
        _liveTimer.Stop();
        _liveTimer.Tick -= OnLiveTimerTick;
    }
}
