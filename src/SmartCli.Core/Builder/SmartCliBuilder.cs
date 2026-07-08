using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartCli.Core.Abstractions;
using SmartCli.Core.Agent;
using SmartCli.Core.Tools;

namespace SmartCli.Core.Builder;

/// <summary>
/// Fluent builder for constructing an <see cref="ISmartCliAgent"/>. This is the single
/// construction path — the DI extension method drives this same builder internally.
/// </summary>
public sealed class SmartCliBuilder
{
    private IChatClient? _chatClient;
    private readonly List<AITool> _tools = [];
    private readonly SmartCliOptions _options = new();
    private ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
    private bool _includeBuiltInTools;

    /// <summary>Supply the underlying model provider. Required before <see cref="Build"/>.</summary>
    public SmartCliBuilder WithChatClient(IChatClient chatClient)
    {
        _chatClient = chatClient;
        return this;
    }

    /// <summary>Override the default system prompt.</summary>
    public SmartCliBuilder WithSystemPrompt(string systemPrompt)
    {
        _options.SystemPrompt = systemPrompt;
        return this;
    }

    /// <summary>Supply a logger factory so the agent and tools can emit logs.</summary>
    public SmartCliBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        return this;
    }

    /// <summary>Register a tool from any delegate (a method, static method, or lambda).</summary>
    public SmartCliBuilder AddTool(Delegate function)
    {
        _tools.Add(AIFunctionFactory.Create(function));
        return this;
    }

    /// <summary>Register a pre-built tool.</summary>
    public SmartCliBuilder AddTool(AITool tool)
    {
        _tools.Add(tool);
        return this;
    }

    /// <summary>Register multiple pre-built tools at once.</summary>
    public SmartCliBuilder AddTools(IEnumerable<AITool> tools)
    {
        _tools.AddRange(tools);
        return this;
    }

    /// <summary>Include the built-in file / tip / time tools shipped with the library.</summary>
    public SmartCliBuilder AddBuiltInTools()
    {
        _includeBuiltInTools = true;
        return this;
    }

    /// <summary>Construct the agent. Throws if no chat client was supplied.</summary>
    public ISmartCliAgent Build()
    {
        if (_chatClient is null)
            throw new InvalidOperationException(
                "A chat client is required. Call WithChatClient(...) before Build().");

        // The builder owns enabling function invocation, so consumers can pass a raw client.
        var pipeline = new ChatClientBuilder(_chatClient)
            .UseFunctionInvocation()
            .Build();

        var tools = new List<AITool>(_tools);
        if (_includeBuiltInTools)
        {
            var builtIn = new BuiltInTools(_loggerFactory.CreateLogger<BuiltInTools>());
            tools.AddRange(builtIn.GetTools());
        }

        var chatOptions = new ChatOptions { Tools = tools };

        return new SmartCliAgent(
            pipeline,
            chatOptions,
            _options,
            _loggerFactory.CreateLogger<SmartCliAgent>());
    }
}
