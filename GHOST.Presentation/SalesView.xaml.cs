using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GHOST.Application.Day5;
using GHOST.Application.Devices;

namespace GHOST.Presentation;

public partial class SalesView : UserControl
{
    private sealed record SessionChoice(
        Guid SessionId,
        string DisplayName,
        string DeviceName,
        string RuntimeText,
        decimal GamingAmount);

    private sealed class CartLine
    {
        public Guid ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal UnitPrice { get; init; }
        public int Quantity { get; set; }
        public decimal Total => UnitPrice * Quantity;
    }

    private readonly IDay5Service day5Service;
    private readonly IDeviceDashboardService dashboardService;
    private readonly ObservableCollection<CartLine> cart = [];

    private List<SessionChoice> sessions = [];

    public SalesView(
        IDay5Service day5Service,
        IDeviceDashboardService dashboardService)
    {
        this.day5Service = day5Service;
        this.dashboardService = dashboardService;

        InitializeComponent();
        CartGrid.ItemsSource = cart;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e) =>
        await RefreshAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e) =>
        await RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            var devices = await dashboardService.GetDevicesAsync();

            sessions = devices
                .Where(x => x.ActiveSessionId.HasValue)
                .Select(x => new SessionChoice(
                    x.ActiveSessionId!.Value,
                    $"{x.Name} — {x.RoomName ?? "بدون غرفة"}",
                    x.Name,
                    FormatRuntime(x.Runtime),
                    x.CurrentAmount ?? 0m))
                .ToList();

            SessionBox.ItemsSource = sessions;

            if (sessions.Count == 0)
            {
                SessionBox.SelectedIndex = -1;
                ShowSelectedSession(null);
                return;
            }

            if (SessionBox.SelectedValue is Guid selected &&
                sessions.Any(x => x.SessionId == selected))
            {
                SessionBox.SelectedValue = selected;
            }
            else
            {
                SessionBox.SelectedIndex = 0;
            }

            UpdateSelectedSession();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "GHOST", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SessionBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e) =>
        UpdateSelectedSession();

    private async void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        await AddByBarcodeAsync();
    }

    private async void AddByBarcode_Click(object sender, RoutedEventArgs e) =>
        await AddByBarcodeAsync();

    private async Task AddByBarcodeAsync()
    {
        if (SessionBox.SelectedValue is not Guid)
        {
            MessageBox.Show(
                "اختر جهازًا عليه جلسة نشطة أولًا.",
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var barcode = BarcodeBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(barcode))
        {
            MessageBox.Show(
                "أدخل الباركود أو استخدم قارئ الباركود.",
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            var product = await day5Service.GetProductByBarcodeAsync(barcode);

            if (product is null)
            {
                MessageBox.Show(
                    "لم يتم العثور على منتج نشط بهذا الباركود.",
                    "GHOST",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var line = cart.FirstOrDefault(x => x.ProductId == product.Id);

            if (line is null)
            {
                cart.Add(new CartLine
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    UnitPrice = product.SellingPrice,
                    Quantity = 1
                });
            }
            else
            {
                if (line.Quantity >= product.StockQuantity)
                {
                    MessageBox.Show(
                        "لا يمكن تجاوز الكمية المتاحة في المخزون.",
                        "GHOST",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                line.Quantity++;
                CartGrid.Items.Refresh();
            }

            BarcodeBox.Clear();
            BarcodeBox.Focus();
            UpdateTotals();

            CartNoticeText.Text =
                "تمت الإضافة إلى الحساب الحالي. الحفظ النهائي سيتم عند إكمال ربط الفاتورة.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "GHOST", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: CartLine line })
        {
            cart.Remove(line);
            UpdateTotals();
        }
    }

    private void ClearCart_Click(object sender, RoutedEventArgs e)
    {
        cart.Clear();
        UpdateTotals();
        CartNoticeText.Text = "تم مسح السلة.";
    }

    private void UpdateSelectedSession()
    {
        if (SessionBox.SelectedItem is SessionChoice session)
        {
            ShowSelectedSession(session);
            return;
        }

        ShowSelectedSession(null);
    }

    private void ShowSelectedSession(SessionChoice? session)
    {
        SelectedDeviceText.Text =
            session is null ? "لا توجد جلسة نشطة" : session.DeviceName;

        SelectedRuntimeText.Text =
            session is null ? "مدة اللعب: —" : $"مدة اللعب: {session.RuntimeText}";

        SelectedGamingAmountText.Text =
            session is null ? "وقت اللعب: —" : $"وقت اللعب: {session.GamingAmount:N2} ج.م";
    }

    private void UpdateTotals()
    {
        ProductsTotalText.Text =
            $"{cart.Sum(x => x.Total):N2} ج.م";
    }

    private static string FormatRuntime(TimeSpan? runtime)
    {
        if (runtime is null)
            return "—";

        var value = runtime.Value;
        return $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
    }
}