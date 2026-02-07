using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Desktop.Avalonia.ViewModels;
using MIC.Desktop.Avalonia.Views;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Desktop.Avalonia
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    // Check for existing session (remember me functionality)
                    var sessionService = Program.ServiceProvider?.GetRequiredService<UserSessionService>();
                    MainWindowViewModel mainVm = Program.ServiceProvider!.GetRequiredService<MainWindowViewModel>();
                    if (sessionService != null && sessionService.IsLoggedIn && !string.IsNullOrEmpty(sessionService.GetToken()))
                    {
                        // User is already logged in - go straight to main window
                        var mainWindow = new MainWindow
                        {
                            DataContext = mainVm
                        };
                        desktop.MainWindow = mainWindow;
                    }
                    else
                    {
                        // Show login window
                        var loginWindow = new LoginWindow();
                        loginWindow.Show();
                        desktop.MainWindow = loginWindow;
                    }

                    // Prime notification bridge so domain events flow into the center
                    Program.ServiceProvider?.GetRequiredService<NotificationEventBridge>();

                    // Wire global exception handlers
                    var errorHandling = Program.ServiceProvider?.GetService<IErrorHandlingService>();
                    if (errorHandling != null)
                    {
                        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                        {
                            if (args.ExceptionObject is Exception exception)
                            {
                                errorHandling.HandleException(exception, "Unhandled exception", isCritical: true);
                            }
                        };

                        TaskScheduler.UnobservedTaskException += (_, args) =>
                        {
                            errorHandling.HandleException(args.Exception, "Unobserved task exception", isCritical: true);
                            args.SetObserved();
                        };
                    }

                    // Subscribe to theme switching
                    mainVm.ThemeRequested += (_, theme) =>
                    {
                        switch (theme)
                        {
                            case MainWindowViewModel.ThemeType.Light:
                                SwitchTheme("avares://MIC.Desktop.Avalonia/Themes/LightTheme.axaml");
                                break;
                            case MainWindowViewModel.ThemeType.Dark:
                                SwitchTheme("avares://MIC.Desktop.Avalonia/Themes/DarkTheme.axaml");
                                break;
                            case MainWindowViewModel.ThemeType.System:
                                SwitchTheme(null); // Use default
                                break;
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                // Fallback with error handling
                Console.WriteLine($"Error in OnFrameworkInitializationCompleted: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var errorWindow = new MainWindow();
                    desktop.MainWindow = errorWindow;
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void SwitchTheme(string? themeXaml)
        {
            // Theme switching via RequestedThemeVariant for Avalonia 11+
            if (string.IsNullOrEmpty(themeXaml))
            {
                RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Default;
            }
            else if (themeXaml.Contains("LightTheme"))
            {
                RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light;
            }
            else if (themeXaml.Contains("DarkTheme"))
            {
                RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;
            }
        }
    }
}
