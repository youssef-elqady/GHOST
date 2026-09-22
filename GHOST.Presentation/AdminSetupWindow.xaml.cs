using System.Windows;
using GHOST.Application.Authentication;

namespace GHOST.Presentation;

public partial class AdminSetupWindow : Window
{
    private readonly IAdminSetupService setupService;
    public AdminSetupWindow(IAdminSetupService setupService) { this.setupService = setupService; InitializeComponent(); }
    private async void CreateAdmin_Click(object sender, RoutedEventArgs e)
    {
        if (password.Password != confirmation.Password) { error.Text = "كلمتا المرور غير متطابقتين."; return; }
        try { await setupService.CompleteSetupAsync(username.Text, password.Password); DialogResult = true; }
        catch (Exception exception) { error.Text = exception.Message; }
    }
}
