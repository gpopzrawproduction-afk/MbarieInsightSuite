using System;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using ReactiveUI;
using MIC.Desktop.Avalonia.Services;

namespace MIC.Desktop.Avalonia.ViewModels;

public class FirstRunSetupViewModel : ViewModelBase
{
    private readonly IFirstRunSetupService _firstRunSetupService;
    private readonly Func<Task> _onSetupComplete;

    public FirstRunSetupViewModel(IFirstRunSetupService firstRunSetupService, Func<Task> onSetupComplete)
    {
        _firstRunSetupService = firstRunSetupService;
        _onSetupComplete = onSetupComplete;
        
        // Create an observable that monitors the properties for validation
        var canFinishSetup = this.WhenAnyValue(
            x => x.Email,
            x => x.Password,
            x => x.ConfirmPassword,
            (email, password, confirmPassword) =>
                !string.IsNullOrWhiteSpace(email) &&
                !string.IsNullOrWhiteSpace(password) &&
                password.Length >= 12 &&
                password == confirmPassword &&
                IsValidEmail(email));
        
        FinishSetupCommand = ReactiveCommand.CreateFromTask(FinishSetupAsync, canFinishSetup);
    }

    private string _email = "";
    public string Email
    {
        get => _email;
        set => this.RaiseAndSetIfChanged(ref _email, value);
    }

    private string _password = "";
    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    private string _confirmPassword = "";
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => this.RaiseAndSetIfChanged(ref _confirmPassword, value);
    }

    private string _errorMessage = "";
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public ICommand FinishSetupCommand { get; }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private async Task FinishSetupAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = "";
            
            // Basic validation
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Email is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Password is required.";
                return;
            }

            if (Password.Length < 12)
            {
                ErrorMessage = "Password must be at least 12 characters long.";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return;
            }

            if (!IsValidEmail(Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return;
            }

            // Check password strength
            if (!IsPasswordStrong(Password))
            {
                ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.";
                return;
            }

            // Complete the setup
            await _firstRunSetupService.CompleteFirstRunSetupAsync(Email, Password);

            // Notify completion
            await _onSetupComplete();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Setup failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool IsPasswordStrong(string password)
    {
        if (password.Length < 12) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;
        return true;
    }

    public void UpdateCanExecute()
    {
        // ReactiveCommand automatically handles CanExecute changes via the observable
        // No action needed
    }
}