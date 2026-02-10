using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MIC.Desktop.Avalonia.Services;


public class FirstRunSetupService : MIC.Core.Application.Common.Interfaces.IFirstRunSetupService
{
    private readonly string _setupFilePath;
    private readonly string _configFilePath;

    public FirstRunSetupService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var mbariePath = Path.Combine(appDataPath, "MBARIE");
        Directory.CreateDirectory(mbariePath); // Ensure directory exists
        _setupFilePath = Path.Combine(mbariePath, "setup.json");
        _configFilePath = Path.Combine(mbariePath, "appsettings.runtime.json");
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

        // Generate a secure JWT secret key
        var jwtSecretKey = GenerateSecureKey(64); // 64 bytes = 512 bits

        // Create runtime configuration with the generated key
        var runtimeConfig = new RuntimeConfig
        {
            JwtSettings = new JwtRuntimeSettings
            {
                SecretKey = jwtSecretKey
            }
        };

        // Save runtime configuration
        var configJson = JsonSerializer.Serialize(runtimeConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_configFilePath, configJson);

        // Mark setup as complete
        var setupData = new SetupData
        {
            SetupCompleted = true,
            SetupDate = DateTime.UtcNow
        };

        var setupJson = JsonSerializer.Serialize(setupData, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_setupFilePath, setupJson);
    }

    private static string GenerateSecureKey(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
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

    public string GetRuntimeJwtSecretKey()
    {
        if (!File.Exists(_configFilePath))
            return null;

        try
        {
            var content = File.ReadAllText(_configFilePath);
            var runtimeConfig = JsonSerializer.Deserialize<RuntimeConfig>(content);
            return runtimeConfig?.JwtSettings?.SecretKey;
        }
        catch
        {
            return null;
        }
    }

    private class SetupData
    {
        public bool SetupCompleted { get; set; }
        public DateTime SetupDate { get; set; }
    }

    private class RuntimeConfig
    {
        public JwtRuntimeSettings JwtSettings { get; set; }
    }

    private class JwtRuntimeSettings
    {
        public string SecretKey { get; set; }
    }
}
