using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Windows;
using Pastomatic.Services;
using Pastomatic.ViewModels;

namespace Pastomatic
{
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    var basePath = AppDomain.CurrentDomain.BaseDirectory;
                    config.SetBasePath(basePath);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(context.Configuration, services);
                })
                .UseSerilog((context, loggerConfiguration) =>
                {
                    ConfigureSerilog(context.Configuration, loggerConfiguration);
                })
                .Build();
        }

        private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(configuration);

            // Services
            services.AddSingleton<LowLevelKeyboardHookService>();
            services.AddSingleton<IHotkeyService, HotkeyService>();
            services.AddSingleton<IIconManager, IconManager>();
            services.AddSingleton<ISystemTrayService, SystemTrayService>();
            services.AddSingleton<IClipboardImageService, ClipboardImageService>();
            services.AddSingleton<IVisionLlmService, VisionLlmService>();
            services.AddSingleton<IImageSaveService, ImageSaveService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<ActionWindowViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }

        private void ConfigureSerilog(IConfiguration configuration, LoggerConfiguration loggerConfiguration)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "pastomatic-.log");

            loggerConfiguration
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            Log.Information("Pastomatic application starting up");

            // Initialize system tray
            var systemTray = _host.Services.GetRequiredService<ISystemTrayService>();
            systemTray.Initialize();

            // Initialize hotkeys
            var hotkeyService = _host.Services.GetRequiredService<IHotkeyService>();
            hotkeyService.RegisterHotkeys();

            // Create main window but keep it hidden (needed for DI and as dialog owner)
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            this.MainWindow = mainWindow;

            // Wire up tray events
            var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
            mainViewModel.Initialize(systemTray, _host.Services);

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log.Information("Pastomatic application shutting down");

            var systemTray = _host.Services.GetRequiredService<ISystemTrayService>();
            systemTray?.Dispose();

            var hotkeyService = _host.Services.GetRequiredService<IHotkeyService>();
            hotkeyService?.Dispose();

            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }

            Log.CloseAndFlush();

            base.OnExit(e);
        }
    }
}
