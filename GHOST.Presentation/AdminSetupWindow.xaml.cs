using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GHOST.Application.Authentication;
using Serilog;

namespace GHOST.Presentation;

public partial class AdminSetupWindow : Window
{
    private const int MinUsernameLength = 3;
    private const int MinPasswordLength = 12;

    private static readonly SolidColorBrush TrackBrush = Freeze("#292E38");
    private static readonly SolidColorBrush MutedBrush = Freeze("#969DA9");
    private static readonly SolidColorBrush DangerBrush = Freeze("#E47777");
    private static readonly SolidColorBrush WarningBrush = Freeze("#E7B04B");
    private static readonly SolidColorBrush GoodBrush = Freeze("#8FD19E");
    private static readonly SolidColorBrush SuccessBrush = Freeze("#63B894");

    private readonly IAdminSetupService setupService;
    private readonly System.Windows.Controls.Border[] segments;
    private bool isBusy;

    public AdminSetupWindow(IAdminSetupService setupService)
    {
        this.setupService = setupService;

        InitializeComponent();

        segments = [seg1, seg2, seg3, seg4];
        MaxHeight = SystemParameters.WorkArea.Height;

        // Events are wired here (not in XAML) so a missing handler can never break the build.
        username.TextChanged += (_, _) => UpdateHints();
        password.PasswordChanged += (_, _) => UpdateHints();
        confirmation.PasswordChanged += (_, _) => UpdateHints();

        PreviewKeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            await CreateAdminAsync();
        };
        PreviewKeyUp += (_, _) => UpdateCapsLock();
        GotKeyboardFocus += (_, _) => UpdateCapsLock();
        LostKeyboardFocus += (_, _) => UpdateCapsLock();

        SourceInitialized += (_, _) => DarkTitleBar.Apply(this);

        Loaded += (_, _) =>
        {
            CenterOnWorkArea();
            UpdateHints();
            StartGhostFloat();
            username.Focus();
        };
    }

    // ------------------------------------------------------------------
    // Live hints
    // ------------------------------------------------------------------

    private void UpdateHints()
    {
        // username
        var nameLength = username.Text.Trim().Length;
        if (nameLength >= MinUsernameLength)
        {
            usernameHint.Text = "✓ اسم المستخدم مناسب";
            usernameHint.Foreground = SuccessBrush;
        }
        else
        {
            usernameHint.Text = "3 أحرف على الأقل";
            usernameHint.Foreground = MutedBrush;
        }

        // password length + strength
        var pw = password.Password;
        var meetsLength = pw.Length >= MinPasswordLength;

        if (meetsLength)
        {
            passwordHint.Text = "✓ الطول مناسب";
            passwordHint.Foreground = SuccessBrush;
        }
        else
        {
            passwordHint.Text = $"{pw.Length} / {MinPasswordLength} — يلزم 12 حرفاً على الأقل";
            passwordHint.Foreground = MutedBrush;
        }

        ShowStrength(pw);

        // confirmation
        if (confirmation.Password.Length == 0)
        {
            matchHint.Text = string.Empty;
        }
        else if (confirmation.Password == pw)
        {
            matchHint.Text = "✓ كلمتا المرور متطابقتان";
            matchHint.Foreground = SuccessBrush;
        }
        else
        {
            matchHint.Text = "كلمتا المرور غير متطابقتين";
            matchHint.Foreground = DangerBrush;
        }
    }

    private void ShowStrength(string pw)
    {
        var level = 0;
        var label = string.Empty;
        SolidColorBrush color = TrackBrush;

        if (pw.Length > 0 && pw.Length < MinPasswordLength)
        {
            level = 1;
            label = "قصيرة";
            color = DangerBrush;
        }
        else if (pw.Length >= MinPasswordLength)
        {
            var mixedCase = pw.Any(char.IsUpper) && pw.Any(char.IsLower);
            var digitAndSymbol = pw.Any(char.IsDigit) && pw.Any(c => !char.IsLetterOrDigit(c));
            var isLong = pw.Length >= 16;

            level = Math.Min(4, 2 + (mixedCase ? 1 : 0) + (digitAndSymbol ? 1 : 0) + (isLong ? 1 : 0));

            (label, color) = level switch
            {
                2 => ("مقبولة", WarningBrush),
                3 => ("جيدة", GoodBrush),
                _ => ("قوية", SuccessBrush)
            };
        }

        for (var i = 0; i < segments.Length; i++)
            segments[i].Background = i < level ? color : TrackBrush;

        strengthLabel.Text = label;
        strengthLabel.Foreground = color;
    }

    private void UpdateCapsLock()
    {
        var typingPassword = password.IsKeyboardFocusWithin || confirmation.IsKeyboardFocusWithin;
        capsWarning.Visibility = typingPassword && Keyboard.IsKeyToggled(Key.CapsLock)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    // ------------------------------------------------------------------
    // Create admin
    // ------------------------------------------------------------------

    private async void CreateAdmin_Click(object sender, RoutedEventArgs e) => await CreateAdminAsync();

    private async Task CreateAdminAsync()
    {
        if (isBusy) return;
        HideError();

        var name = username.Text.Trim();

        if (name.Length < MinUsernameLength)
        {
            ShowError("اسم المستخدم قصير. اكتب 3 أحرف على الأقل.");
            username.Focus();
            return;
        }

        if (password.Password.Length < MinPasswordLength)
        {
            ShowError("كلمة المرور قصيرة. اكتب 12 حرفاً على الأقل.");
            password.Focus();
            return;
        }

        if (password.Password != confirmation.Password)
        {
            ShowError("كلمتا المرور غير متطابقتين. أعد كتابة التأكيد.");
            confirmation.Focus();
            return;
        }

        try
        {
            isBusy = true;
            createButton.IsEnabled = false;
            createButton.Content = "جارٍ إنشاء الحساب...";

            await setupService.CompleteSetupAsync(name, password.Password);

            DialogResult = true;
        }
        catch (InvalidOperationException)
        {
            ShowError("تم إعداد النظام من قبل. أغلق البرنامج وافتحه مرة أخرى.");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Admin setup failed.");
            ShowError("تعذر إنشاء الحساب. تم تسجيل تفاصيل المشكلة في ملف السجل.");
        }
        finally
        {
            isBusy = false;
            createButton.IsEnabled = true;
            createButton.Content = "إنشاء الحساب والبدء";
        }
    }

    private void ShowError(string message)
    {
        error.Text = message;
        errorContainer.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        error.Text = string.Empty;
        errorContainer.Visibility = Visibility.Collapsed;
    }

    // ------------------------------------------------------------------
    // Window helpers
    // ------------------------------------------------------------------

    /// <summary>SizeToContent + CenterScreen leaves the window slightly off-centre, so re-centre once loaded.</summary>
    private void CenterOnWorkArea()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - ActualWidth) / 2;
        Top = area.Top + Math.Max(0, (area.Height - ActualHeight) / 2);
    }

    /// <summary>A slow float on the ghost; skipped when Windows animations are turned off.</summary>
    private void StartGhostFloat()
    {
        if (!SystemParameters.ClientAreaAnimation) return;

        var animation = new DoubleAnimation(0, -6, TimeSpan.FromSeconds(2.2))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };

        GhostFloat.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, animation);
    }

    private static SolidColorBrush Freeze(string hex)
    {
        var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}

/// <summary>Makes the Windows title bar dark so it matches the dark window (Windows 10 20H1+ and Windows 11).</summary>
internal static class DarkTitleBar
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;

            var enabled = 1;
            // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (19 on early Windows 10 builds)
            if (DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
        }
        catch
        {
            // Cosmetic only: never let styling break startup.
        }
    }
}