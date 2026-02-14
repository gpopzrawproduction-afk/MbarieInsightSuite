using System;
using FluentAssertions;
using MIC.Core.Domain.Exceptions;
using Xunit;

namespace MIC.Tests.Unit.Domain;

/// <summary>
/// Tests for MICException and its exception hierarchy covering error codes, user messages, and metadata.
/// </summary>
public class ExceptionTests
{
    #region EmailException

    [Fact]
    public void EmailException_HasDefaultErrorCode()
    {
        var ex = new EmailException("Test error");
        ex.ErrorCode.Should().Be("EMAIL_ERROR");
    }

    [Fact]
    public void EmailException_HasCustomErrorCode()
    {
        var ex = new EmailException("Test error", "CUSTOM_CODE");
        ex.ErrorCode.Should().Be("CUSTOM_CODE");
    }

    [Fact]
    public void EmailException_HasMessage()
    {
        var ex = new EmailException("Something went wrong");
        ex.Message.Should().Be("Something went wrong");
    }

    [Fact]
    public void EmailException_SupportsUserMessage()
    {
        var ex = new EmailException("Technical error", userMessage: "Please try again");
        ex.UserMessage.Should().Be("Please try again");
    }

    [Fact]
    public void EmailException_SupportsInnerException()
    {
        var inner = new InvalidOperationException("Root cause");
        var ex = new EmailException("Wrapper", innerException: inner);
        ex.InnerException.Should().BeSameAs(inner);
    }

    #endregion

    #region EmailAuthException

    [Fact]
    public void EmailAuthException_HasCorrectErrorCode()
    {
        var ex = new EmailAuthException("Auth failed");
        ex.ErrorCode.Should().Be("EMAIL_AUTH_FAILED");
    }

    [Fact]
    public void EmailAuthException_HasDefaultUserMessage()
    {
        var ex = new EmailAuthException("Auth failed");
        ex.UserMessage.Should().Contain("authenticate");
    }

    [Fact]
    public void EmailAuthException_SupportsCustomUserMessage()
    {
        var ex = new EmailAuthException("Auth failed", "Custom auth message");
        ex.UserMessage.Should().Be("Custom auth message");
    }

    #endregion

    #region EmailSyncException

    [Fact]
    public void EmailSyncException_HasCorrectErrorCode()
    {
        var ex = new EmailSyncException("Sync failed");
        ex.ErrorCode.Should().Be("EMAIL_SYNC_FAILED");
    }

    [Fact]
    public void EmailSyncException_HasDefaultUserMessage()
    {
        var ex = new EmailSyncException("Sync failed");
        ex.UserMessage.Should().Contain("synchronization");
    }

    #endregion

    #region DatabaseException

    [Fact]
    public void DatabaseException_HasCorrectErrorCode()
    {
        var ex = new DatabaseException("DB error");
        ex.ErrorCode.Should().Be("DATABASE_ERROR");
    }

    [Fact]
    public void DatabaseException_HasDefaultUserMessage()
    {
        var ex = new DatabaseException("Connection timeout");
        ex.UserMessage.Should().Contain("database error");
    }

    #endregion

    #region SettingsException

    [Fact]
    public void SettingsException_HasCorrectErrorCode()
    {
        var ex = new SettingsException("Save failed");
        ex.ErrorCode.Should().Be("SETTINGS_ERROR");
    }

    [Fact]
    public void SettingsException_HasDefaultUserMessage()
    {
        var ex = new SettingsException("Save failed");
        ex.UserMessage.Should().Contain("Settings");
    }

    #endregion

    #region NotificationException

    [Fact]
    public void NotificationException_HasCorrectErrorCode()
    {
        var ex = new NotificationException("Send failed");
        ex.ErrorCode.Should().Be("NOTIFICATION_ERROR");
    }

    [Fact]
    public void NotificationException_HasDefaultUserMessage()
    {
        var ex = new NotificationException("Send failed");
        ex.UserMessage.Should().Contain("Notification");
    }

    #endregion

    #region MICException Metadata

    [Fact]
    public void AddMetadata_AddsKeyValue()
    {
        var ex = new EmailException("Test");
        ex.AddMetadata("key1", "value1");

        ex.Metadata.Should().ContainKey("key1");
        ex.Metadata["key1"].Should().Be("value1");
    }

    [Fact]
    public void AddMetadata_ReturnsSameException_ForFluent()
    {
        var ex = new EmailException("Test");
        var result = ex.AddMetadata("key", "value");

        result.Should().BeSameAs(ex);
    }

    [Fact]
    public void AddMetadata_IgnoresNullOrWhitespaceKey()
    {
        var ex = new EmailException("Test");
        ex.AddMetadata(null!, "value");
        ex.AddMetadata("", "value");
        ex.AddMetadata("   ", "value");

        ex.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void AddMetadata_OverwritesExistingKey()
    {
        var ex = new EmailException("Test");
        ex.AddMetadata("key", "old");
        ex.AddMetadata("key", "new");

        ex.Metadata["key"].Should().Be("new");
    }

    [Fact]
    public void Metadata_IsCaseInsensitive()
    {
        var ex = new EmailException("Test");
        ex.AddMetadata("Key", "value");

        ex.Metadata.Should().ContainKey("key");
        ex.Metadata.Should().ContainKey("KEY");
    }

    [Fact]
    public void ErrorCode_DefaultsFallback_WhenWhitespace()
    {
        var ex = new EmailException("Test", "  ");
        ex.ErrorCode.Should().Be("MIC_ERROR");
    }

    [Fact]
    public void Metadata_IsInitiallyEmpty()
    {
        var ex = new EmailException("Test");
        ex.Metadata.Should().BeEmpty();
    }

    #endregion
}
