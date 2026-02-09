using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using MIC.Core.Domain.Entities;
using MIC.Desktop.Avalonia.ViewModels;
using Xunit;

namespace MIC.Tests.Unit.ViewModels;

public sealed class AddEmailAccountViewModelTests
{
    [Fact]
    public void Constructor_SetsDefaultState()
    {
        var vm = new AddEmailAccountViewModel();

        vm.EmailAddress.Should().BeEmpty();
        vm.DisplayName.Should().BeEmpty();
        vm.SelectedProvider.Should().Be(EmailProvider.Gmail);
        vm.Providers.Should().Contain(new[] { EmailProvider.Gmail, EmailProvider.Outlook });
        vm.CanAuthorize.Should().BeFalse();
        vm.HasStatusMessage.Should().BeFalse();
    }

    [Fact]
    public void EmailAddressChange_WithValidInput_EnablesAuthorize()
    {
        var vm = new AddEmailAccountViewModel();

        vm.DisplayName = "Alicia";
        vm.EmailAddress = "alicia@example.com";

        vm.CanAuthorize.Should().BeTrue();
    }

    [Fact]
    public void EmailAddressChange_WithInvalidEmail_DisablesAuthorize()
    {
        var vm = new AddEmailAccountViewModel();

        vm.DisplayName = "Alicia";
        vm.EmailAddress = "alicia@example.com";
        vm.EmailAddress = "invalid-email";

        vm.CanAuthorize.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeCommand_WhenGmail_SetsSuccessStatus()
    {
        var vm = new AddEmailAccountViewModel
        {
            SelectedProvider = EmailProvider.Gmail
        };

        await vm.AuthorizeCommand.Execute().ToTask();

        vm.HasStatusMessage.Should().BeTrue();
        vm.StatusMessage.Should().Be("Authorization completed successfully!");
    }

    [Fact]
    public async Task AuthorizeCommand_WhenOutlook_SetsSuccessStatus()
    {
        var vm = new AddEmailAccountViewModel
        {
            SelectedProvider = EmailProvider.Outlook
        };

        await vm.AuthorizeCommand.Execute().ToTask();

        vm.HasStatusMessage.Should().BeTrue();
        vm.StatusMessage.Should().Be("Authorization completed successfully!");
    }

    [Fact]
    public async Task CancelCommand_SetsCancelledStatus()
    {
        var vm = new AddEmailAccountViewModel();

        await vm.CancelCommand.Execute().ToTask();

        vm.StatusMessage.Should().Be("Operation cancelled.");
    }
}
