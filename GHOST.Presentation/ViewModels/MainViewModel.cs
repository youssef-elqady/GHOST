using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GHOST.Application.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace GHOST.Presentation.ViewModels;

public sealed partial class MainViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    public ObservableCollection<DeviceCardViewModel> Devices { get; } = [];
    public ObservableCollection<RoomViewModel> Rooms { get; } = [];
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string loadError = string.Empty;
    public string AdministrationNotice => "إدارة الأجهزة محمية: يلزم تسجيل دخول مسؤول أو مدير قبل تفعيل أوامر الإدارة.";
    public bool CanManageDevices => false;
    [RelayCommand] private async Task RefreshAsync()
    {
        IsLoading = true; LoadError = string.Empty;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dashboard = scope.ServiceProvider.GetRequiredService<IDeviceDashboardService>();
            var devices = await dashboard.GetDevicesAsync(); var rooms = await dashboard.GetRoomsAsync();
            Devices.Clear(); foreach (var device in devices) Devices.Add(new DeviceCardViewModel(device));
            Rooms.Clear(); foreach (var room in rooms) Rooms.Add(new RoomViewModel(room));
        }
        catch (Exception) { LoadError = "تعذر تحميل الأجهزة. تم تسجيل المشكلة."; }
        finally { IsLoading = false; }
    }
}
