namespace SmartCli.Core.Abstractions;

/// <summary>Configuration for a SmartCli agent.</summary>
public class SmartCliOptions
{
    /// <summary>The default system prompt used when none is supplied.</summary>
    public const string DefaultSystemPrompt =
        "You are a concise, helpful assistant. You have access to tools; use them when " +
        "relevant rather than guessing. If no tool fits the request, just answer directly.";

    /// <summary>The system prompt that establishes the agent's behavior.</summary>
    public string SystemPrompt { get; set; } = DefaultSystemPrompt;
}
