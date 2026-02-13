using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using MIC.Core.Application.Emails.Commands.SendEmail;
using MIC.Core.Application.Emails.Commands.ReplyEmail;
using MIC.Core.Application.Emails.Commands.DeleteEmail;
using MIC.Core.Application.Common.Interfaces;

namespace MIC.Core.Application;

/// <summary>
/// Dependency injection registration for application services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application layer services
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
