using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MIC.Core.Application.Authentication;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.Configuration;
using MIC.Infrastructure.Identity.Services;
using MIC.Infrastructure.Identity.TokenStorage;

namespace MIC.Infrastructure.Identity;

public static class IdentityDependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        // Register JWT settings from configuration
        services.AddOptions<JwtSettings>()
            .Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("JwtSettings").Bind(settings);
            });

        // Register services
        services.AddSingleton<ICredentialManager, WindowsCredentialManager>();
        services.AddSingleton<IDataProtector, WindowsDpapiDataProtector>();
        services.AddSingleton<ITokenStorageService, WindowsCredentialTokenStorageService>();
        services.AddKeyedSingleton<IEmailOAuth2Service, GmailOAuthService>("Gmail");
        services.AddKeyedSingleton<IEmailOAuth2Service, OutlookOAuthService>("Outlook");
        services.AddScoped<IEmailOAuth2Service, EmailOAuth2RouterService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<JwtSettings>>();
            var settings = options.Value;
            
            // Try to get the runtime JWT secret key from first-run setup
            var firstRunSetupService = provider.GetService<MIC.Core.Application.Common.Interfaces.IFirstRunSetupService>();
            var runtimeSecretKey = firstRunSetupService?.GetRuntimeJwtSecretKey();
            
            // Use the runtime secret key if available, otherwise fall back to config
            var secretKey = !string.IsNullOrWhiteSpace(runtimeSecretKey) ? runtimeSecretKey : settings.SecretKey;
            
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException(
                    "JWT SecretKey is not configured. Please run first-time setup or set JwtSettings:SecretKey in configuration.");
            }
            
            // Ensure the secret key meets minimum security requirements
            if (secretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT SecretKey must be at least 32 characters long for security purposes.");
            }
            
            return new JwtTokenService(secretKey, TimeSpan.FromHours(settings.ExpirationHours));
        });
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        
        // Register localization service for multilingual support
        services.AddSingleton<ILocalizationService, LocalizationService>();

        return services;
    }
}
