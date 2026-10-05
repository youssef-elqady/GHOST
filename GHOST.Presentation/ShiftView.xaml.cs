using System.Windows;
using System.Windows.Controls;
using GHOST.Application.Authentication;
using GHOST.Application.Day5;

namespace GHOST.Presentation;

public partial class ShiftView : UserControl
{
    private readonly IDay5Service service;
    private readonly ICurrentUserContext currentUser;

    private ShiftSummary? currentShift;

    public ShiftView(
        IDay5Service service,
        ICurrentUserContext currentUser)
    {
        this.service = service;
        this.currentUser = currentUser;

        InitializeComponent();
        Loaded += ShiftView_Loaded;
    }

    private async void ShiftView_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            currentShift = await service.GetOpenShiftAsync();

            if (currentShift is null)
            {
                StatusText.Text = "لا يوجد شيفت مفتوح";
                OpeningText.Text = "—";
                ExpectedText.Text = "—";
                CloseNoticeText.Text = "افتح شيفت من جهة الـ Staff/Cashier قبل تشغيل اللعب أو تسجيل مبيعات.";
                return;
            }

            StatusText.Text = "الشيفت مفتوح";
            OpeningText.Text = $"{currentShift.OpeningCash:N2} ج.م";
            ExpectedText.Text = $"{currentShift.ExpectedCash:N2} ج.م";
            CloseNoticeText.Text = $"بدأ في {currentShift.OpenedAt.LocalDateTime:yyyy/MM/dd HH:mm}";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void OpenShift_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        if (!currentUser.IsInRole("Cashier"))
        {
            ShowError("فتح الشيفت متاح لحساب Staff/Cashier فقط.");
            return;
        }

        if (!decimal.TryParse(OpeningCashBox.Text.Trim(), out var cash) || cash < 0)
        {
            ShowError("أدخل رصيدًا افتتاحيًا صحيحًا.");
            return;
        }

        try
        {
            await service.OpenShiftAsync(
                currentUser.RequireAuthenticated().Id,
                cash);

            OpeningCashBox.Clear();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void CloseShift_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        if (!currentUser.IsInRole("Cashier"))
        {
            ShowError("إغلاق الشيفت متاح لحساب Staff/Cashier فقط.");
            return;
        }

        if (currentShift is null)
        {
            ShowError("لا يوجد شيفت مفتوح.");
            return;
        }

        if (!decimal.TryParse(ActualCashBox.Text.Trim(), out var actual) || actual < 0)
        {
            ShowError("أدخل النقدية الفعلية بشكل صحيح.");
            return;
        }

        try
        {
            await service.CloseShiftAsync(
                currentUser.RequireAuthenticated().Id,
                currentShift.Id,
                new CloseShiftRequest(
                    actual,
                    string.IsNullOrWhiteSpace(DifferenceReasonBox.Text)
                        ? null
                        : DifferenceReasonBox.Text.Trim()));

            ActualCashBox.Clear();
            DifferenceReasonBox.Clear();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
        ErrorText.Text = string.Empty;
    }
}
