using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MIC.Core.Intelligence;

namespace MIC.Tests.Unit.Core.Intelligence;

public sealed class IntelligenceDependencyInjectionTests
{
    [Fact]
    public void AddIntelligenceLayer_RegistersPositionQuestionnaireService()
    {
        var services = new ServiceCollection();

        services.AddIntelligenceLayer();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<PositionQuestionnaireService>();

        service.Should().NotBeNull();
    }

    [Fact]
    public void AddIntelligenceLayer_RegistersIntelligenceProcessor()
    {
        var services = new ServiceCollection();

        services.AddIntelligenceLayer();

        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IntelligenceProcessor>();

        service.Should().NotBeNull();
    }

    [Fact]
    public void AddIntelligenceLayer_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddIntelligenceLayer();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddIntelligenceLayer_RegistersAsScoped()
    {
        var services = new ServiceCollection();

        services.AddIntelligenceLayer();

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(PositionQuestionnaireService) &&
            sd.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IntelligenceProcessor) &&
            sd.Lifetime == ServiceLifetime.Scoped);
    }
}
