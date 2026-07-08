using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartCli.Core.Abstractions;
using SmartCli.Core.Builder;

namespace SmartCli.Core.DependencyInjection;

/// <summary>
/// DI extension methods that register a SmartCli agent using the fluent builder internally.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="ISmartCliAgent"/> in the container. The application must have
    /// already registered an <see cref="IChatClient"/>. Configure tools, prompt, and options
    /// via the <paramref name="configure"/> callback, which receives the same builder used
    /// by direct consumers.
    /// </summary>
    public static IServiceCollection AddSmartCli(
        this IServiceCollection services,
        Action<SmartCliBuilder>? configure = null)
    {
        // Scoped: each DI scope (e.g. each web request) gets its own conversation.
        services.AddScoped<ISmartCliAgent>(sp =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();
            var loggerFactory = sp.GetService<ILoggerFactory>();

            var builder = new SmartCliBuilder().WithChatClient(chatClient);
            if (loggerFactory is not null)
                builder.WithLoggerFactory(loggerFactory);

            configure?.Invoke(builder);
            return builder.Build();
        });

        return services;
    }
}
