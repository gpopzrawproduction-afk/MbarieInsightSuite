using System;
using FluentValidation.TestHelper;
using MIC.Core.Application.Alerts.Commands.CreateAlert;
using MIC.Core.Application.Alerts.Commands.DeleteAlert;
using MIC.Core.Application.Alerts.Commands.UpdateAlert;
using MIC.Core.Application.Authentication.Commands.LoginCommand;
using MIC.Core.Domain.Entities;
using Xunit;

namespace MIC.Tests.Unit.Application.Validators;

public class AlertAndAuthValidatorsTests
{
    [Fact]
    public void CreateAlertCommandValidator_WithValidCommand_HasNoErrors()
    {
        var validator = new CreateAlertCommandValidator();
        var command = new CreateAlertCommand("Alert", "Description", AlertSeverity.Critical, "System");

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateAlertCommandValidator_WithInvalidFields_HasErrors()
    {
        var validator = new CreateAlertCommandValidator();
        var command = new CreateAlertCommand("", "", AlertSeverity.Critical, "") with
        {
            Description = new string('x', 2001)
        };

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AlertName);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.Source);
    }

    [Fact]
    public void UpdateAlertCommandValidator_WithResolvedStatusRequiresNotes()
    {
        var validator = new UpdateAlertCommandValidator();
        var command = new UpdateAlertCommand
        {
            AlertId = Guid.NewGuid(),
            UpdatedBy = "operator",
            NewStatus = AlertStatus.Resolved,
            ResolutionNotes = null
        };

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ResolutionNotes)
            .WithErrorMessage("Resolution notes are required when resolving an alert.");
    }

    [Fact]
    public void UpdateAlertCommandValidator_WithValidResolvedStatus_HasNoErrors()
    {
        var validator = new UpdateAlertCommandValidator();
        var command = new UpdateAlertCommand
        {
            AlertId = Guid.NewGuid(),
            UpdatedBy = "operator",
            NewStatus = AlertStatus.Resolved,
            ResolutionNotes = "Issue fixed"
        };

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DeleteAlertCommandValidator_WithMissingFields_HasErrors()
    {
        var validator = new DeleteAlertCommandValidator();
        var command = new DeleteAlertCommand(Guid.Empty, "");

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AlertId);
        result.ShouldHaveValidationErrorFor(x => x.DeletedBy);
    }

    [Fact]
    public void LoginCommandValidator_WithInvalidCredentials_HasErrors()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("ab", "123");

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void LoginCommandValidator_WithValidCredentials_HasNoErrors()
    {
        var validator = new LoginCommandValidator();
        var command = new LoginCommand("admin", "securepw");

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
