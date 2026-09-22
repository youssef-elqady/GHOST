using System.Configuration;
using System.Data;
using System.Windows;
using GHOST.Application.Authentication;

using GHOST.Infrastructure;
using GHOST.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GHOST.Presentation;

public partial class App : System.Windows.Application
{
    private ServiceProvider? serviceProvider;
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(@"C:\ProgramData\GHOST\Logs\ghost-.log", rollingInterval: RollingInterval.Day).CreateLogger();
        DispatcherUnhandledException += (_, args) => { Log.Error(args.Exception, "Unhandled UI exception"); MessageBox.Show("حدث خطأ غير متوقع. تم تسجيل المشكلة ويمكن متابعة التشغيل.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Error); args.Handled = true; };
        serviceProvider = new ServiceCollection().AddGhostInfrastructure().BuildServiceProvider();
        try
        {
            using var scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            var setup = scope.ServiceProvider.GetRequiredService<IAdminSetupService>();
            if (await setup.IsSetupRequiredAsync())
            {
                if (new AdminSetupWindow(setup).ShowDialog() != true) { Shutdown(); return; }
            }
            new MainWindow().Show();
        }
        catch (Exception exception) { Log.Fatal(exception, "Application initialization failed"); MessageBox.Show("تعذر بدء قاعدة البيانات. راجع سجلات التطبيق.", "GHOST", MessageBoxButton.OK, MessageBoxImage.Error); Shutdown(-1); }
    }
    protected override void OnExit(ExitEventArgs e) { Log.CloseAndFlush(); serviceProvider?.Dispose(); base.OnExit(e); }
}

