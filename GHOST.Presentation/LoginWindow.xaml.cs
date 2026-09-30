using System.Windows;
using System.Windows.Input;
using GHOST.Application.Authentication;

namespace GHOST.Presentation;

public partial class LoginWindow : Window
{
    private readonly IAuthenticationService authentication;

    private bool isSigningIn;


    public LoginWindow(
        IAuthenticationService authentication)
    {
        this.authentication = authentication;

        InitializeComponent();

        Loaded += LoginWindow_Loaded;

        username.KeyDown += Input_KeyDown;
        password.KeyDown += Input_KeyDown;
    }


    // =============================================================
    // WINDOW LOADED
    // =============================================================

    private void LoginWindow_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        username.Focus();
    }


    // =============================================================
    // ENTER KEY
    // =============================================================

    private async void Input_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        await SignInAsync();
    }


    // =============================================================
    // LOGIN BUTTON
    // =============================================================

    private async void SignIn_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SignInAsync();
    }


    // =============================================================
    // AUTHENTICATION
    // =============================================================

    private async Task SignInAsync()
    {
        if (isSigningIn)
            return;


        error.Text = string.Empty;

        errorContainer.Visibility =
            Visibility.Collapsed;

        statusText.Text = string.Empty;


        var enteredUsername =
            username.Text.Trim();

        var enteredPassword =
            password.Password;


        // =========================================================
        // VALIDATION
        // =========================================================

        if (string.IsNullOrWhiteSpace(enteredUsername))
        {
            ShowError(
                "من فضلك أدخل اسم المستخدم.");

            username.Focus();

            return;
        }


        if (string.IsNullOrWhiteSpace(enteredPassword))
        {
            ShowError(
                "من فضلك أدخل كلمة المرور.");

            password.Focus();

            return;
        }


        try
        {
            isSigningIn = true;

            loginButton.IsEnabled = false;

            statusText.Text =
                "جاري التحقق من بيانات الدخول...";


            // =====================================================
            // REAL AUTHENTICATION
            // =====================================================

            await authentication.SignInAsync(
                enteredUsername,
                enteredPassword);


            // =====================================================
            // SUCCESS
            // =====================================================

            statusText.Text =
                "تم تسجيل الدخول بنجاح.";

            DialogResult = true;
        }
        catch (UnauthorizedAccessException)
        {
            ShowError(
                "اسم المستخدم أو كلمة المرور غير صحيحين.");

            password.Clear();

            password.Focus();
        }
        catch (Exception)
        {
            ShowError(
                "تعذر تسجيل الدخول حاليًا.\nيرجى المحاولة مرة أخرى.");

            password.Clear();

            password.Focus();
        }
        finally
        {
            isSigningIn = false;

            loginButton.IsEnabled = true;

            if (DialogResult != true)
            {
                statusText.Text =
                    string.Empty;
            }
        }
    }


    // =============================================================
    // SHOW ERROR
    // =============================================================

    private void ShowError(
        string message)
    {
        error.Text = message;

        errorContainer.Visibility =
            Visibility.Visible;

        statusText.Text =
            string.Empty;
    }
}