using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MIC.Core.Application;
using MIC.Core.Application.Authentication.Common;
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data;
using MIC.Infrastructure.Data.Persistence;
using MIC.Infrastructure.Identity;
using MIC.Infrastructure.Identity.Services;
using Testcontainers.PostgreSql;
using Xunit;

namespace MIC.Tests.Integration.Infrastructure;

/// <summary>
/// Base fixture for integration tests that need real database and DI wiring.
/// Spins up a PostgreSQL container, configures application services, and seeds data helpers.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;

    protected ServiceProvider ServiceProvider { get; private set; } = default!;
    protected IDbContextFactory<MicDbContext> DbContextFactory { get; private set; } = default!;
    protected TestSessionService SessionService { get; private set; } = default!;

    public virtual async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("mic_integration")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        await _postgresContainer.StartAsync();

        var configuration = BuildConfiguration(_postgresContainer.GetConnectionString());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);

        SessionService = new TestSessionService();
        services.AddSingleton<ISessionService>(SessionService);
        services.AddApplication();
        services.AddDataInfrastructure(configuration);
        services.AddIdentityInfrastructure();

        ServiceProvider = services.BuildServiceProvider();
        DbContextFactory = ServiceProvider.GetRequiredService<IDbContextFactory<MicDbContext>>();

        await using var context = await DbContextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public virtual async Task DisposeAsync()
    {
        if (ServiceProvider != null)
        {
            await ServiceProvider.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }
    }

    protected virtual IConfiguration BuildConfiguration(string connectionString)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:MicDatabase"] = connectionString,
            ["Database:Provider"] = "Postgres",
            ["JwtSettings:SecretKey"] = "integration-testing-secret-key-ensure-32-length-minimum",
            ["JwtSettings:Issuer"] = "MIC.IntegrationTests",
            ["JwtSettings:Audience"] = "MIC.IntegrationTests",
            ["JwtSettings:ExpirationHours"] = "1"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    protected IServiceScope CreateScope() => ServiceProvider.CreateScope();

    protected async Task ExecuteDbContextAsync(Func<MicDbContext, Task> action)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        await action(context);
    }

    protected async Task<T> QueryDbContextAsync<T>(Func<MicDbContext, Task<T>> query)
    {
        await using var context = await DbContextFactory.CreateDbContextAsync();
        return await query(context);
    }

    protected async Task<User> SeedUserAsync(
        string username,
        string password,
        string? email = null,
        UserRole role = UserRole.Admin,
        bool setAsCurrentUser = true)
    {
        var passwordHasher = ServiceProvider.GetRequiredService<IPasswordHasher>();
        var (hash, salt) = passwordHasher.HashPassword(password);

        var safeFullName = string.IsNullOrWhiteSpace(username)
            ? "Integration User"
            : char.ToUpper(username[0]) + (username.Length > 1 ? username[1..] : string.Empty);

        var user = new User
        {
            Username = username,
            Email = email ?? $"{username}@example.com",
            PasswordHash = hash,
            Salt = salt,
            FullName = safeFullName,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await ExecuteDbContextAsync(async context =>
        {
            var existing = await context.Users
                .Where(u => u.Username == username)
                .ToListAsync();

            if (existing.Count > 0)
            {
                context.Users.RemoveRange(existing);
                await context.SaveChangesAsync();
            }

            context.Users.Add(user);
            await context.SaveChangesAsync();
        });

        if (setAsCurrentUser)
        {
            SessionService.SetUser(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                IsActive = true,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
            SessionService.SetToken("integration-test-token");
        }

        return user;
    }

    protected async Task<EmailAccount> SeedEmailAccountAsync(Guid userId, string emailAddress, EmailProvider provider = EmailProvider.Outlook)
    {
        var account = new EmailAccount(emailAddress, provider, userId);

        await ExecuteDbContextAsync(async context =>
        {
            context.EmailAccounts.Add(account);
            await context.SaveChangesAsync();
        });

        return account;
    }

    protected async Task<EmailMessage> SeedEmailAsync(
        Guid userId,
        Guid accountId,
        string subject,
        EmailFolder folder = EmailFolder.Inbox,
        bool isRead = false,
        bool requiresResponse = false,
        DateTime? receivedAt = null)
    {
        var received = receivedAt ?? DateTime.UtcNow;
        var email = new EmailMessage(
            messageId: Guid.NewGuid().ToString("N"),
            subject: subject,
            fromAddress: "sender@example.com",
            fromName: "Sender",
            toRecipients: "recipient@example.com",
            sentDate: received.AddMinutes(-5),
            receivedDate: received,
            bodyText: $"Body for {subject}",
            userId: userId,
            emailAccountId: accountId,
            folder: folder);

        email.SetAIAnalysis(
            EmailPriority.Normal,
            EmailCategory.General,
            SentimentType.Neutral,
            hasActionItems: requiresResponse,
            requiresResponse: requiresResponse,
            summary: null);

        if (isRead)
        {
            email.MarkAsRead();
        }

        await ExecuteDbContextAsync(async context =>
        {
            context.EmailMessages.Add(email);
            await context.SaveChangesAsync();
        });

        return email;
    }

    protected void SetCurrentUser(User user)
    {
        SessionService.SetUser(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = true,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
        SessionService.SetToken("integration-test-token");
    }

    protected sealed class TestSessionService : ISessionService
    {
        private UserDto _currentUser = new();
        private string _token = string.Empty;

        public bool IsAuthenticated => _currentUser.Id != Guid.Empty;

        public void SetToken(string token) => _token = token ?? string.Empty;

        public void SetUser(UserDto user)
        {
            _currentUser = user ?? new UserDto();
        }

        public string GetToken() => _token;

        public UserDto GetUser() => _currentUser;

        public void Clear()
        {
            _currentUser = new UserDto();
            _token = string.Empty;
        }
    }
}