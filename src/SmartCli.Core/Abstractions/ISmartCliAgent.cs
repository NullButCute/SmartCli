namespace SmartCli.Core.Abstractions;

/// <summary>
/// A single conversational AI agent session that can call registered tools.
/// Each instance keeps its own conversation history, so treat one instance as one conversation.
/// </summary>
public interface ISmartCliAgent
{
    /// <summary>Sends a message and returns the full reply once generation finishes.</summary>
    Task<string> AskAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>Sends a message and streams the reply in chunks as it is generated.</summary>
    IAsyncEnumerable<string> AskStreamingAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>Clears the conversation, resetting back to just the system prompt.</summary>
    void Reset();
}
