using System;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Authentication;
using MIC.Core.Application.Common.Interfaces;
using MIC.Infrastructure.Identity;

namespace MIC.Tests.Unit.Infrastructure.Identity;

public sealed class IdentityDependencyInjectionTests
{
    private static IServiceCollection CreateServicesWithConfig(
        Dictionary<string, string?>? configValues = null)
    {
        var services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        if (configValues is not null)
            builder.AddInMemoryCollection(configValues);
        var config = builder.Build();
        services.AddSingleton<IConfiguration>(config);

        // Add logging so services can resolve ILogger
        services.AddLogging();

        return services;
    }

    // ──────────────────────────────────────────────────────────────
    // Service registrations
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddIdentityInfrastructure_RegistersPasswordHasher()
    {
        var services = CreateServicesWithConfig();
        services.AddIdentityInfrastructure();

        services.Should().Contain(sd => sd.ServiceType == typeof(IPasswordHasher));
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersAuthenticationService()
    {
        var services = CreateServicesWithConfig();
        services.AddIdentityInfrastructure();

        services.Should().Contain(sd => sd.ServiceType == typeof(IAuthenticationService));
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersLocalizationService()
    {
        var services = CreateServicesWithConfig();
        services.AddIdentityInfrastructure();

        services.Should().Contain(sd => sd.ServiceType == typeof(ILocalizationService));
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersEmailOAuth2Service()
    {
        var services = CreateServicesWithConfig();
        services.AddIdentityInfrastructure();

        services.Should().Contain(sd => sd.ServiceType == typeof(IEmailOAuth2Service));
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersTokenStorageService()
    {
        var services = CreateServicesWithConfig();
        services.AddIdentityInfrastructure();

        services.Should().Contain(sd => sd.ServiceType == typeof(ITokenStorageService));
    }

    [Fact]
    public void AddIdentityInfrastructure_RegistersJwtTokenService()
    {
        var services = CreateServicesWithConfig();
        services.AddIdentityInfrastructure();

        services.Should().Contain(sd => sd.ServiceType == typeof(IJwtTokenService));
    }

    // ──────────────────────────────────────────────────────────────
    // JWT factory validation
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void JwtFactory_ThrowsWhenSecretKeyMissing()
    {
        var services = CreateServicesWithConfig();
        services.AddIdentityInfrastructure();

        var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IJwtTokenService>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretKey*not configured*");
    }

    [Fact]
    public void JwtFactory_ThrowsWhenSecretKeyTooShort()
    {
        var services = CreateServicesWithConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "short_key_under_32"
        });
        services.AddIdentityInfrastructure();

        var provider = services.BuildServiceProvider();

        var act = () => provider.GetRequiredService<IJwtTokenService>();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 32 characters*");
    }

    [Fact]
    public void JwtFactory_SucceedsWithValidSecretKey()
    {
        var services = CreateServicesWithConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "this_is_a_long_enough_secret_key_for_jwt_testing_12345"
        });
        services.AddIdentityInfrastructure();

        var provider = services.BuildServiceProvider();

        var token = provider.GetRequiredService<IJwtTokenService>();

        token.Should().NotBeNull();
    }
}
