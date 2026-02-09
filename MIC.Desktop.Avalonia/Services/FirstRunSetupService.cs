using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MIC.Desktop.Avalonia.Services;

public interface IFirstRunSetupService
{
    Task<bool> IsFirstRunAsync();
    Task CompleteFirstRunSetupAsync(string email, string password);
    Task<bool> IsSetupCompleteAsync();
}

public class FirstRunSetupService : IFirstRunSetupService
{
    private readonly string _setupFilePath;

    public FirstRunSetupService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var mbariePath = Path.Combine(appDataPath, "MBARIE");
        Directory.CreateDirectory(mbariePath); // Ensure directory exists
        _setupFilePath = Path.Combine(mbariePath, "setup.json");
    }

    public async Task<bool> IsFirstRunAsync()
    {
        return !File.Exists(_setupFilePath);
    }

    public async Task<bool> IsSetupCompleteAsync()
    {
        if (!File.Exists(_setupFilePath))
            return false;

        try
        {
            var content = await File.ReadAllTextAsync(_setupFilePath);
            var setupData = JsonSerializer.Deserialize<SetupData>(content);
            return setupData?.SetupCompleted == true;
        }
        catch
        {
            return false;
        }
    }

    public async Task CompleteFirstRunSetupAsync(string email, string password)
    {
        // Validate inputs
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format");

        if (!IsPasswordStrong(password))
            throw new ArgumentException("Password does not meet security requirements");

        // Mark setup as complete
        var setupData = new SetupData
        {
            SetupCompleted = true,
            SetupDate = DateTime.UtcNow
        };

        var setupJson = JsonSerializer.Serialize(setupData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_setupFilePath, setupJson);
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Use a simple regex for basic validation
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
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

    private class SetupData
    {
        public bool SetupCompleted { get; set; }
        public DateTime SetupDate { get; set; }
    }
}