using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;
using GHOST.Domain.Enums;

namespace GHOST.Presentation;

public sealed partial class PlayOrderViewModel : ObservableObject, IDisposable
{
    private readonly IDay5Service day5Service;
    private readonly ICurrentUserContext currentUser;
    private readonly DispatcherTimer liveTimer;

    private DateTimeOffset snapshotAtUtc;
    private TimeSpan snapshotRuntime;
    private decimal snapshotPlayAmount;
    private decimal ratePerHour;
    private SessionStatus status;

    public PlayOrderViewModel(
        IDay5Service day5Service,
        ICurrentUserContext currentUser)
    {
        this.day5Service = day5Service;
        this.currentUser = currentUser;

        liveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        liveTimer.Tick += LiveTimer_Tick;
    }

    public ObservableCollection<ProductSummary> Products { get; } = [];

    [ObservableProperty]
    private ObservableCollection<ProductSummary> visibleProducts = [];

    [ObservableProperty]
    private ObservableCollection<CategorySummary> categories = [];

    [ObservableProperty]
    private Guid? selectedCategoryId;

    [ObservableProperty]
    private string deviceName = string.Empty;

    [ObservableProperty]
    private string roomName = "بدون غرفة";

    [ObservableProperty]
    private string modeText = "فردي";

    [ObservableProperty]
    private string runtimeText = "00:00:00";

    [ObservableProperty]
    private decimal playAmount;

    [ObservableProperty]
    private decimal productsAmount;

    [ObservableProperty]
    private decimal totalAmount;

    [ObservableProperty]
    private string notice = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public Guid SessionId { get; private set; }

    public string PlayAmountText => $"{PlayAmount:N2} ج.م";
    public string ProductsAmountText => $"{ProductsAmount:N2} ج.م";
    public string TotalAmountText => $"{TotalAmount:N2} ج.م";

    public async Task LoadAsync(Guid sessionId)
    {
        SessionId = sessionId;
        IsLoading = true;
        Notice = string.Empty;

        try
        {
            var summary = await day5Service.GetPlayDetailsAsync(sessionId);
            ApplySummary(summary);

            var products = await day5Service.GetProductsAsync(
                includeInactive: false);

            Products.Clear();
            foreach (var product in products)
                Products.Add(product);

            Categories.Clear();
            foreach (var category in await day5Service.GetCategoriesAsync(false))
                Categories.Add(category);

            RefreshVisibleProducts();
            liveTimer.Start();
        }
        catch (Exception ex)
        {
            Notice = ex.Message;
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddProductAsync(ProductSummary? product)
    {
        if (product is null)
            return;

        try
        {
            IsLoading = true;
            Notice = string.Empty;

            var actor = currentUser.RequireAuthenticated().Id;

            var summary = await day5Service.AddProductToPlayAsync(
                actor,
                new AddProductToPlayRequest(
                    SessionId,
                    product.Id,
                    1));

            ApplySummary(summary);
        }
        catch (Exception ex)
        {
            Notice = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedCategoryIdChanged(Guid? value)
    {
        RefreshVisibleProducts();
    }

    private void RefreshVisibleProducts()
    {
        VisibleProducts.Clear();

        var source = SelectedCategoryId is null
            ? Products
            : Products.Where(x => x.CategoryId == SelectedCategoryId.Value);

        foreach (var product in source)
            VisibleProducts.Add(product);
    }

    private void ApplySummary(PlayOrderSummary summary)
    {
        DeviceName = summary.DeviceName;
        RoomName = string.IsNullOrWhiteSpace(summary.RoomName)
            ? "بدون غرفة"
            : summary.RoomName;

        ModeText = summary.Mode == SessionMode.Multi
            ? "مالتي"
            : "فردي";

        status = summary.Status;
        ratePerHour = summary.RatePerHour;
        snapshotAtUtc = DateTimeOffset.UtcNow;
        snapshotPlayAmount = summary.PlayAmount;

        var paused = TimeSpan.FromSeconds(summary.TotalPausedSeconds);
        snapshotRuntime = summary.EndedAt is null
            ? DateTimeOffset.UtcNow - summary.StartedAt - paused
            : (summary.EndedAt.Value - summary.StartedAt - paused);

        if (snapshotRuntime < TimeSpan.Zero)
            snapshotRuntime = TimeSpan.Zero;

        PlayAmount = summary.PlayAmount;
        ProductsAmount = summary.ProductsAmount;
        TotalAmount = summary.TotalAmount;

        UpdateRuntimeAndAmount();

        OnPropertyChanged(nameof(PlayAmountText));
        OnPropertyChanged(nameof(ProductsAmountText));
        OnPropertyChanged(nameof(TotalAmountText));
    }

    private void LiveTimer_Tick(object? sender, EventArgs e)
    {
        UpdateRuntimeAndAmount();
    }

    private void UpdateRuntimeAndAmount()
    {
        var runtime = snapshotRuntime;

        if (status == SessionStatus.Running)
            runtime += DateTimeOffset.UtcNow - snapshotAtUtc;

        if (runtime < TimeSpan.Zero)
            runtime = TimeSpan.Zero;

        RuntimeText =
            $"{(int)runtime.TotalHours:00}:{runtime.Minutes:00}:{runtime.Seconds:00}";

        if (status == SessionStatus.Running && ratePerHour > 0)
        {
            var billableMinutes = Math.Ceiling(runtime.TotalMinutes);

            PlayAmount = decimal.Round(
                ratePerHour * ((decimal)billableMinutes / 60m),
                2,
                MidpointRounding.AwayFromZero);

            TotalAmount = PlayAmount + ProductsAmount;

            OnPropertyChanged(nameof(PlayAmountText));
            OnPropertyChanged(nameof(TotalAmountText));
        }
    }

    public void Dispose()
    {
        liveTimer.Stop();
        liveTimer.Tick -= LiveTimer_Tick;
    }
}
