using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Tests.Unit.Infrastructure.Data;

public class MicDbContextFactoryTests : IDisposable
{
    private readonly Dictionary<string, string?> _originalEnv = new();

    public MicDbContextFactoryTests()
    {
        Capture("MIC_CONNECTION_STRING");
        Capture("USE_SQLITE");
        Capture("DOTNET_ENVIRONMENT");
    }

    [Fact]
    public void CreateDbContext_WhenMicConnectionStringPresent_UsesPostgresOptions()
    {
        Environment.SetEnvironmentVariable("MIC_CONNECTION_STRING", "Host=localhost;Port=5432;Database=mic;Username=postgres;Password=secret");
        Environment.SetEnvironmentVariable("USE_SQLITE", "false");

        var factory = new MicDbContextFactory();
        using var context = factory.CreateDbContext(Array.Empty<string>());

        context.Database.GetDbConnection().ConnectionString.Should().Contain("Host=localhost");
        context.Database.ProviderName.Should().Contain("Npgsql");
    }

    [Fact]
    public void CreateDbContext_WhenNoEnvDefaultsToSqlite()
    {
        Environment.SetEnvironmentVariable("MIC_CONNECTION_STRING", null);
        Environment.SetEnvironmentVariable("USE_SQLITE", "true");

        var factory = new MicDbContextFactory();
        using var context = factory.CreateDbContext(Array.Empty<string>());

        context.Database.ProviderName.Should().Contain("Sqlite");
        context.Database.GetDbConnection().ConnectionString.Should().Contain("mic_dev.db");
    }

    private void Capture(string key)
    {
        _originalEnv[key] = Environment.GetEnvironmentVariable(key);
    }

    public void Dispose()
    {
        foreach (var entry in _originalEnv)
        {
            Environment.SetEnvironmentVariable(entry.Key, entry.Value);
        }
    }
}
