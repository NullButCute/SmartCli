using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCli.Core.Abstractions;

namespace SmartCli.Core.Agent;

/// <summary>
/// Default <see cref="ISmartCliAgent"/> implementation. Wraps an <see cref="IChatClient"/>
/// (which already has function invocation enabled by the builder) plus a set of tools and options.
/// </summary>
public sealed class SmartCliAgent : ISmartCliAgent
{
    private readonly IChatClient _chatClient;
    private readonly ChatOptions _chatOptions;
    private readonly SmartCliOptions _options;
    private readonly ILogger<SmartCliAgent> _logger;
    private readonly List<ChatMessage> _history;

    public SmartCliAgent(
        IChatClient chatClient,
        ChatOptions chatOptions,
        SmartCliOptions options,
        ILogger<SmartCliAgent>? logger = null)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _chatOptions = chatOptions ?? throw new ArgumentNullException(nameof(chatOptions));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<SmartCliAgent>.Instance;
        _history = [new ChatMessage(ChatRole.System, _options.SystemPrompt)];
    }

    public async Task<string> AskAsync(string message, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        await foreach (var chunk in AskStreamingAsync(message, cancellationToken))
            sb.Append(chunk);
        return sb.ToString();
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        _logger.LogDebug("User message received ({Length} chars)", message.Length);
        _history.Add(new ChatMessage(ChatRole.User, message));

        var reply = new StringBuilder();
        await foreach (var update in _chatClient.GetStreamingResponseAsync(_history, _chatOptions, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                reply.Append(update.Text);
                yield return update.Text;
            }
        }

        _history.Add(new ChatMessage(ChatRole.Assistant, reply.ToString()));
        _logger.LogDebug("Assistant reply complete ({Length} chars)", reply.Length);
    }

    public void Reset()
    {
        _history.Clear();
        _history.Add(new ChatMessage(ChatRole.System, _options.SystemPrompt));
        _logger.LogDebug("Conversation reset.");
    }
}
