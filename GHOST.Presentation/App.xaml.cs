using GHOST.Application.Authentication;
using GHOST.Infrastructure;
using GHOST.Infrastructure.Persistence;
using GHOST.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Windows;

namespace GHOST.Presentation;

public partial class App : System.Windows.Application
{
    private ServiceProvider? serviceProvider;
    private IServiceScope? applicationScope;

    private async void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            // =========================================================
            // LOGGING
            // =========================================================

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    @"C:\ProgramData\GHOST\Logs\ghost-.log",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();


            // =========================================================
            // GLOBAL UI EXCEPTION HANDLER
            // =========================================================

            DispatcherUnhandledException += OnDispatcherUnhandledException;


            // =========================================================
            // DATABASE / SERVICES
            // =========================================================

            var databasePath =
                Environment.GetEnvironmentVariable("GHOST_DATABASE_PATH");

            serviceProvider = new ServiceCollection()
                .AddGhostInfrastructure(databasePath)
                .AddSingleton<MainViewModel>()
                .BuildServiceProvider();


            // =========================================================
            // APPLICATION SCOPE
            // =========================================================

            applicationScope = serviceProvider.CreateScope();

            var services = applicationScope.ServiceProvider;


            // =========================================================
            // DATABASE INITIALIZATION
            // =========================================================

            var databaseInitializer =
                services.GetRequiredService<DatabaseInitializer>();

            await databaseInitializer.InitializeAsync();


            // =========================================================
            // FIRST RUN ADMIN SETUP
            // =========================================================

            var adminSetupService =
                services.GetRequiredService<IAdminSetupService>();

            var setupRequired =
                await adminSetupService.IsSetupRequiredAsync();

            if (setupRequired)
            {
                var setupWindow =
                    new AdminSetupWindow(adminSetupService);

                var setupResult =
                    setupWindow.ShowDialog();

                if (setupResult != true)
                {
                    Shutdown();
                    return;
                }
            }


            // =========================================================
            // LOGIN
            // =========================================================

            var authenticationService =
                services.GetRequiredService<IAuthenticationService>();

            var loginWindow =
                new LoginWindow(authenticationService);

            var loginResult =
                loginWindow.ShowDialog();

            if (loginResult != true)
            {
                Shutdown();
                return;
            }


            // =========================================================
            // VERIFY CURRENT USER
            // =========================================================

            var currentUserContext =
                services.GetRequiredService<ICurrentUserContext>();

            if (currentUserContext.Current is null)
            {
                MessageBox.Show(
                    "تعذر إنشاء جلسة دخول صالحة.",
                    "GHOST",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
                return;
            }


            // =========================================================
            // MAIN WINDOW
            // =========================================================

            var mainViewModel =
                services.GetRequiredService<MainViewModel>();

            var mainWindow =
                new MainWindow(
                    mainViewModel,
                    services.GetRequiredService<IServiceScopeFactory>(),
                    currentUserContext);

            MainWindow = mainWindow;

            mainWindow.Show();
        }
        catch (Exception exception)
        {
            Log.Fatal(
                exception,
                "Application initialization failed.");

            MessageBox.Show(
                "تعذر بدء تشغيل GHOST.\n\nراجع ملف السجل لمعرفة تفاصيل المشكلة.",
                "GHOST",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }


    // =============================================================
    // GLOBAL UI EXCEPTION HANDLER
    // =============================================================

    private void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs args)
    {
        Log.Error(
            args.Exception,
            "Unhandled UI exception.");

        MessageBox.Show(
            "حدث خطأ غير متوقع في التطبيق.\n\nتم تسجيل تفاصيل الخطأ.",
            "GHOST",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        args.Handled = true;
    }


    // =============================================================
    // APPLICATION EXIT
    // =============================================================

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            applicationScope?.Dispose();
            serviceProvider?.Dispose();
        }
        finally
        {
            Log.CloseAndFlush();
        }

        base.OnExit(e);
    }
}