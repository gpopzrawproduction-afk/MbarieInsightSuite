using System;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application.Common.Interfaces;
using MIC.Infrastructure.Data;

namespace MIC.Tests.Unit.Infrastructure.Data;

public sealed class DataDependencyInjectionTests
{
    // ──────────────────────────────────────────────────────────────
    // Helper: invoke private static methods via reflection
    // ──────────────────────────────────────────────────────────────

    private static readonly Type DiType = typeof(DependencyInjection);

    private static string InvokeRedactPassword(string connectionString)
    {
        var method = DiType.GetMethod("RedactPassword", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("RedactPassword not found");
        return (string)method.Invoke(null, new object[] { connectionString })!;
    }

    private static string InvokeNormalizePostgreSqlConnectionString(string connectionString)
    {
        var method = DiType.GetMethod("NormalizePostgreSqlConnectionString", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("NormalizePostgreSqlConnectionString not found");
        return (string)method.Invoke(null, new object[] { connectionString })!;
    }

    private static string InvokeDetectProvider(IConfiguration configuration, string connectionString)
    {
        var method = DiType.GetMethod("DetectProvider", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("DetectProvider not found");
        return (string)method.Invoke(null, new object[] { configuration, connectionString })!;
    }

    private static string InvokeResolveConnectionString(IConfiguration configuration)
    {
        var method = DiType.GetMethod("ResolveConnectionString", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResolveConnectionString not found");
        return (string)method.Invoke(null, new object[] { configuration })!;
    }

    private static IConfiguration BuildConfig(
        Dictionary<string, string?>? values = null)
    {
        var builder = new ConfigurationBuilder();
        if (values is not null)
            builder.AddInMemoryCollection(values);
        return builder.Build();
    }

    // ──────────────────────────────────────────────────────────────
    // RedactPassword
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void RedactPassword_MasksPasswordValue()
    {
        var result = InvokeRedactPassword("Host=localhost;Password=secret123;Database=mic");
        result.Should().Contain("Password=****");
        result.Should().NotContain("secret123");
    }

    [Fact]
    public void RedactPassword_MasksPwdValue()
    {
        var result = InvokeRedactPassword("Host=localhost;Pwd=mypassword;Database=mic");
        result.Should().Contain("Pwd=****");
        result.Should().NotContain("mypassword");
    }

    [Fact]
    public void RedactPassword_ReturnsOriginal_WhenNoPassword()
    {
        var input = "Data Source=mic_dev.db";
        var result = InvokeRedactPassword(input);
        result.Should().Be(input);
    }

    [Fact]
    public void RedactPassword_HandlesNullOrWhitespace()
    {
        var result = InvokeRedactPassword("");
        result.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // NormalizePostgreSqlConnectionString
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Normalize_AppendsSSLMode_WhenMissing()
    {
        var result = InvokeNormalizePostgreSqlConnectionString("Host=localhost;Database=mic");
        result.Should().Contain("SSL Mode=Disable");
    }

    [Fact]
    public void Normalize_AppendsTrustServerCertificate_WhenMissing()
    {
        var result = InvokeNormalizePostgreSqlConnectionString("Host=localhost;Database=mic");
        result.Should().Contain("Trust Server Certificate=true");
    }

    [Fact]
    public void Normalize_DoesNotDuplicateSSLMode_WhenPresent()
    {
        var input = "Host=localhost;Database=mic;SSL Mode=Require";
        var result = InvokeNormalizePostgreSqlConnectionString(input);

        // Should not append another SSL Mode
        var count = System.Text.RegularExpressions.Regex.Matches(result, "SSL Mode", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        count.Should().Be(1);
    }

    [Fact]
    public void Normalize_DoesNotDuplicateTrustServerCert_WhenPresent()
    {
        var input = "Host=localhost;Database=mic;Trust Server Certificate=false";
        var result = InvokeNormalizePostgreSqlConnectionString(input);

        var count = System.Text.RegularExpressions.Regex.Matches(result, "Trust Server Certificate", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
        count.Should().Be(1);
    }

    [Fact]
    public void Normalize_ThrowsOnEmptyConnectionString()
    {
        var act = () => InvokeNormalizePostgreSqlConnectionString("  ");
        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    // ──────────────────────────────────────────────────────────────
    // DetectProvider
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void DetectProvider_ReturnsConfigured_WhenDatabaseProviderKeyExists()
    {
        var config = BuildConfig(new Dictionary<string, string?> { ["Database:Provider"] = "Postgres" });
        var result = InvokeDetectProvider(config, "anything");
        result.Should().Be("Postgres");
    }

    [Fact]
    public void DetectProvider_ReturnsSQLite_ForDataSourcePattern()
    {
        var config = BuildConfig();
        var result = InvokeDetectProvider(config, "Data Source=mic.db");
        result.Should().Be("SQLite");
    }

    [Fact]
    public void DetectProvider_ReturnsSQLite_ForFilenamePattern()
    {
        var config = BuildConfig();
        var result = InvokeDetectProvider(config, "Filename=mic.db");
        result.Should().Be("SQLite");
    }

    [Fact]
    public void DetectProvider_ReturnsPostgres_ForHostPattern()
    {
        var config = BuildConfig();
        var result = InvokeDetectProvider(config, "Host=localhost;Database=mic");
        result.Should().Be("Postgres");
    }

    [Fact]
    public void DetectProvider_ReturnsPostgres_ForUsernamePattern()
    {
        var config = BuildConfig();
        var result = InvokeDetectProvider(config, "Username=admin;Database=mic");
        result.Should().Be("Postgres");
    }

    [Fact]
    public void DetectProvider_ReturnsPostgres_ForPostgresUriScheme()
    {
        var config = BuildConfig();
        var result = InvokeDetectProvider(config, "postgres://user:pass@localhost/mic");
        result.Should().Be("Postgres");
    }

    [Fact]
    public void DetectProvider_DefaultsToSQLite_WhenNoPatternMatches()
    {
        var config = BuildConfig();
        var result = InvokeDetectProvider(config, "some-unknown-format");
        result.Should().Be("SQLite");
    }

    // ──────────────────────────────────────────────────────────────
    // ResolveConnectionString
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ResolveConnectionString_PrefersEnvironmentVariable()
    {
        var envKey = "MIC_CONNECTION_STRING";
        var original = Environment.GetEnvironmentVariable(envKey);
        try
        {
            Environment.SetEnvironmentVariable(envKey, "Data Source=from_env.db");
            var config = BuildConfig();
            var result = InvokeResolveConnectionString(config);
            result.Should().Be("Data Source=from_env.db");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, original);
        }
    }

    [Fact]
    public void ResolveConnectionString_FallsToPgConnection()
    {
        var envKey = "MIC_CONNECTION_STRING";
        var original = Environment.GetEnvironmentVariable(envKey);
        try
        {
            Environment.SetEnvironmentVariable(envKey, null);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MicDatabase"] = "Host=localhost;Database=mic"
            });
            var result = InvokeResolveConnectionString(config);
            result.Should().Be("Host=localhost;Database=mic");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, original);
        }
    }

    [Fact]
    public void ResolveConnectionString_FallsToSqliteConnection()
    {
        var envKey = "MIC_CONNECTION_STRING";
        var original = Environment.GetEnvironmentVariable(envKey);
        try
        {
            Environment.SetEnvironmentVariable(envKey, null);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MicSqlite"] = "Data Source=mic.db"
            });
            var result = InvokeResolveConnectionString(config);
            result.Should().Be("Data Source=mic.db");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, original);
        }
    }

    [Fact]
    public void ResolveConnectionString_ThrowsWhenNoneConfigured()
    {
        var envKey = "MIC_CONNECTION_STRING";
        var original = Environment.GetEnvironmentVariable(envKey);
        try
        {
            Environment.SetEnvironmentVariable(envKey, null);
            var config = BuildConfig();
            var act = () => InvokeResolveConnectionString(config);
            act.Should().Throw<TargetInvocationException>()
                .WithInnerException<InvalidOperationException>()
                .WithMessage("*connection string*");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, original);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // AddDataInfrastructure — SQLite path
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddDataInfrastructure_SQLite_RegistersExpectedServices()
    {
        var envKey = "MIC_CONNECTION_STRING";
        var original = Environment.GetEnvironmentVariable(envKey);
        try
        {
            Environment.SetEnvironmentVariable(envKey, null);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MicSqlite"] = "Data Source=test_di.db"
            });

            var services = new ServiceCollection();
            services.AddDataInfrastructure(config);

            var provider = services.BuildServiceProvider();

            // Spot-check key registrations
            services.Should().Contain(sd => sd.ServiceType == typeof(IUnitOfWork));
            services.Should().Contain(sd => sd.ServiceType == typeof(IAlertRepository));
            services.Should().Contain(sd => sd.ServiceType == typeof(IUserRepository));
            services.Should().Contain(sd => sd.ServiceType == typeof(IEmailRepository));
            services.Should().Contain(sd => sd.ServiceType == typeof(IChatHistoryRepository));
            services.Should().Contain(sd => sd.ServiceType == typeof(ISettingsService));
            services.Should().Contain(sd => sd.ServiceType == typeof(IRepository<>));
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, original);
        }
    }

    [Fact]
    public void AddDataInfrastructure_UnsupportedProvider_Throws()
    {
        var envKey = "MIC_CONNECTION_STRING";
        var original = Environment.GetEnvironmentVariable(envKey);
        try
        {
            Environment.SetEnvironmentVariable(envKey, null);
            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MicSqlite"] = "Data Source=test.db",
                ["Database:Provider"] = "MySQL"
            });

            var services = new ServiceCollection();
            var act = () => services.AddDataInfrastructure(config);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Unsupported*MySQL*");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, original);
        }
    }
}
