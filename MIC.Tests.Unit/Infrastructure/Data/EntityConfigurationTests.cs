using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MIC.Core.Domain.Entities;
using MIC.Infrastructure.Data.Persistence;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests EF configurations applied via ApplyConfigurationsFromAssembly and inline Configure* methods.
/// Uses an in-memory DbContext to inspect the compiled model.
/// </summary>
public sealed class EntityConfigurationTests : IDisposable
{
    private readonly MicDbContext _context;
    private readonly IModel _model;

    public EntityConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<MicDbContext>()
            .UseInMemoryDatabase($"MicDb_Config_{Guid.NewGuid():N}")
            .Options;

        _context = new MicDbContext(options);
        _context.Database.EnsureCreated();
        _model = _context.Model;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private IEntityType GetEntity<T>() where T : class
    {
        var entity = _model.FindEntityType(typeof(T));
        entity.Should().NotBeNull($"Entity type {typeof(T).Name} should be registered");
        return entity!;
    }

    private IProperty GetProperty<T>(string propertyName) where T : class
    {
        var entity = GetEntity<T>();
        var prop = entity.FindProperty(propertyName);
        prop.Should().NotBeNull($"Property {propertyName} should exist on {typeof(T).Name}");
        return prop!;
    }

    // ──────────────────────────────────────────────────────────────
    // UserConfiguration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void User_HasPrimaryKey()
    {
        var entity = GetEntity<User>();
        entity.FindPrimaryKey().Should().NotBeNull();
        entity.FindPrimaryKey()!.Properties.Should().ContainSingle(p => p.Name == "Id");
    }

    [Fact]
    public void User_Username_HasMaxLength50()
    {
        var prop = GetProperty<User>("Username");
        prop.GetMaxLength().Should().Be(50);
    }

    [Fact]
    public void User_Username_HasUniqueIndex()
    {
        var entity = GetEntity<User>();
        var indexes = entity.GetIndexes().Where(i => i.Properties.Any(p => p.Name == "Username"));
        indexes.Should().Contain(i => i.IsUnique);
    }

    [Fact]
    public void User_Email_HasMaxLength100()
    {
        var prop = GetProperty<User>("Email");
        prop.GetMaxLength().Should().Be(100);
    }

    [Fact]
    public void User_Email_HasUniqueIndex()
    {
        var entity = GetEntity<User>();
        var indexes = entity.GetIndexes().Where(i => i.Properties.Any(p => p.Name == "Email"));
        indexes.Should().Contain(i => i.IsUnique);
    }

    [Fact]
    public void User_PasswordHash_IsRequired()
    {
        var prop = GetProperty<User>("PasswordHash");
        prop.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void User_Salt_HasMaxLength256()
    {
        var prop = GetProperty<User>("Salt");
        prop.GetMaxLength().Should().Be(256);
    }

    [Fact]
    public void User_FullName_HasMaxLength100()
    {
        var prop = GetProperty<User>("FullName");
        prop.GetMaxLength().Should().Be(100);
    }

    [Fact]
    public void User_JobPosition_HasMaxLength100()
    {
        var prop = GetProperty<User>("JobPosition");
        prop.GetMaxLength().Should().Be(100);
    }

    [Fact]
    public void User_Department_HasMaxLength100()
    {
        var prop = GetProperty<User>("Department");
        prop.GetMaxLength().Should().Be(100);
    }

    // ──────────────────────────────────────────────────────────────
    // EmailMessageConfiguration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void EmailMessage_HasPrimaryKey()
    {
        var entity = GetEntity<EmailMessage>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void EmailMessage_MessageId_HasMaxLength500()
    {
        var prop = GetProperty<EmailMessage>("MessageId");
        prop.GetMaxLength().Should().Be(500);
    }

    [Fact]
    public void EmailMessage_MessageId_HasUniqueIndex()
    {
        var entity = GetEntity<EmailMessage>();
        var indexes = entity.GetIndexes().Where(i => i.Properties.Any(p => p.Name == "MessageId"));
        indexes.Should().Contain(i => i.IsUnique);
    }

    [Fact]
    public void EmailMessage_Subject_HasMaxLength1000()
    {
        var prop = GetProperty<EmailMessage>("Subject");
        prop.GetMaxLength().Should().Be(1000);
    }

    [Fact]
    public void EmailMessage_FromAddress_IsRequired()
    {
        var prop = GetProperty<EmailMessage>("FromAddress");
        prop.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void EmailMessage_BodyText_IsRequired()
    {
        var prop = GetProperty<EmailMessage>("BodyText");
        prop.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void EmailMessage_AISummary_HasMaxLength2000()
    {
        var prop = GetProperty<EmailMessage>("AISummary");
        prop.GetMaxLength().Should().Be(2000);
    }

    [Fact]
    public void EmailMessage_HasIndex_OnUserId()
    {
        var entity = GetEntity<EmailMessage>();
        var indexes = entity.GetIndexes().Where(i => i.Properties.Any(p => p.Name == "UserId"));
        indexes.Should().NotBeEmpty();
    }

    [Fact]
    public void EmailMessage_HasIndex_OnReceivedDate()
    {
        var entity = GetEntity<EmailMessage>();
        var indexes = entity.GetIndexes().Where(i => i.Properties.Any(p => p.Name == "ReceivedDate"));
        indexes.Should().NotBeEmpty();
    }

    [Fact]
    public void EmailMessage_HasRelationship_ToAttachments()
    {
        var entity = GetEntity<EmailMessage>();
        var navs = entity.GetNavigations();
        navs.Should().Contain(n => n.Name == "Attachments");
    }

    // ──────────────────────────────────────────────────────────────
    // EmailAttachmentConfiguration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void EmailAttachment_HasPrimaryKey()
    {
        var entity = GetEntity<EmailAttachment>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void EmailAttachment_FileName_IsRequired()
    {
        var prop = GetProperty<EmailAttachment>("FileName");
        prop.IsNullable.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // EmailAccountConfiguration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void EmailAccount_HasPrimaryKey()
    {
        var entity = GetEntity<EmailAccount>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void EmailAccount_EmailAddress_IsRequired()
    {
        var prop = GetProperty<EmailAccount>("EmailAddress");
        prop.IsNullable.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // IntelligenceAlertConfiguration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void IntelligenceAlert_HasPrimaryKey()
    {
        var entity = GetEntity<IntelligenceAlert>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void IntelligenceAlert_AlertName_IsRequired()
    {
        var prop = GetProperty<IntelligenceAlert>("AlertName");
        prop.IsNullable.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // AssetMonitorConfiguration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void AssetMonitor_HasPrimaryKey()
    {
        var entity = GetEntity<AssetMonitor>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void AssetMonitor_AssetName_IsRequired()
    {
        var prop = GetProperty<AssetMonitor>("AssetName");
        prop.IsNullable.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // OperationalMetricConfiguration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void OperationalMetric_HasPrimaryKey()
    {
        var entity = GetEntity<OperationalMetric>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void OperationalMetric_MetricName_IsRequired()
    {
        var prop = GetProperty<OperationalMetric>("MetricName");
        prop.IsNullable.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // DecisionContextConfiguration
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void DecisionContext_HasPrimaryKey()
    {
        var entity = GetEntity<DecisionContext>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void DecisionContext_ContextName_IsRequired()
    {
        var prop = GetProperty<DecisionContext>("ContextName");
        prop.IsNullable.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    // Setting (inline config from MicDbContext.ConfigureSettings)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void Setting_HasPrimaryKey()
    {
        var entity = GetEntity<Setting>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void Setting_Key_HasMaxLength150()
    {
        var prop = GetProperty<Setting>("Key");
        prop.GetMaxLength().Should().Be(150);
    }

    [Fact]
    public void Setting_Category_HasMaxLength100()
    {
        var prop = GetProperty<Setting>("Category");
        prop.GetMaxLength().Should().Be(100);
    }

    [Fact]
    public void Setting_HasCompositeIndex_UserId_Category_Key()
    {
        var entity = GetEntity<Setting>();
        var indexes = entity.GetIndexes()
            .Where(i => i.Properties.Count == 3 &&
                         i.Properties.Any(p => p.Name == "UserId") &&
                         i.Properties.Any(p => p.Name == "Category") &&
                         i.Properties.Any(p => p.Name == "Key"));
        indexes.Should().NotBeEmpty("Setting should have composite index on UserId+Category+Key");
    }

    // ──────────────────────────────────────────────────────────────
    // SettingHistory (inline config)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void SettingHistory_HasPrimaryKey()
    {
        var entity = GetEntity<SettingHistory>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void SettingHistory_HasIndex_OnSettingId()
    {
        var entity = GetEntity<SettingHistory>();
        var indexes = entity.GetIndexes().Where(i => i.Properties.Any(p => p.Name == "SettingId"));
        indexes.Should().NotBeEmpty();
    }

    // ──────────────────────────────────────────────────────────────
    // UserSettings (inline config)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void UserSettings_HasPrimaryKey()
    {
        var entity = GetEntity<UserSettings>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    // ──────────────────────────────────────────────────────────────
    // ChatHistory (inline config)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public void ChatHistory_HasPrimaryKey()
    {
        var entity = GetEntity<ChatHistory>();
        entity.FindPrimaryKey().Should().NotBeNull();
    }

    [Fact]
    public void ChatHistory_Query_HasMaxLength4000()
    {
        var prop = GetProperty<ChatHistory>("Query");
        prop.GetMaxLength().Should().Be(4000);
    }

    [Fact]
    public void ChatHistory_Response_HasMaxLength8000()
    {
        var prop = GetProperty<ChatHistory>("Response");
        prop.GetMaxLength().Should().Be(8000);
    }

    [Fact]
    public void ChatHistory_HasIndex_OnUserId()
    {
        var entity = GetEntity<ChatHistory>();
        var indexes = entity.GetIndexes().Where(i => i.Properties.Any(p => p.Name == "UserId"));
        indexes.Should().NotBeEmpty();
    }
}
