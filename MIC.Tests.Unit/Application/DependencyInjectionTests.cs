using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Application;
using MIC.Core.Application.Authentication.Commands.LoginCommand;
using Xunit;

namespace MIC.Tests.Unit.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersMediatRAndValidators()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMediator>().Should().NotBeNull();
        provider.GetRequiredService<IValidator<LoginCommand>>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplication_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplication();

        result.Should().BeSameAs(services);
    }
}
