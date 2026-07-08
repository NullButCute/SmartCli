using System.ClientModel;
using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using SmartCli.Core.Abstractions;
using SmartCli.Core.DependencyInjection;

var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var token = config["GitHubModels:Token"]
    ?? throw new InvalidOperationException("Run: dotnet user-secrets set GitHubModels:Token <token>");

var services = new ServiceCollection();

// 1. Console logging is the APP's responsibility — not the library's. The library only
//    emits log events; here we decide they should show up as single-line console output.
services.AddLogging(b => b
    .AddSimpleConsole(o => o.SingleLine = true)
    .SetMinimumLevel(LogLevel.Information));

// 2. Register the model provider. Swap this block for OpenAI, Azure, or Ollama and nothing
//    in the library changes — it depends only on IChatClient.
services.AddSingleton<IChatClient>(_ =>
    new OpenAI.Chat.ChatClient(
            "openai/gpt-4o-mini",
            new ApiKeyCredential(token),
            new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") })
        .AsIChatClient());

// 3. Register the agent — built-in tools plus one custom tool of our own.
services.AddSmartCli(builder => builder
    .AddBuiltInTools()
    .AddTool(Percentage));

using var provider = services.BuildServiceProvider();
var agent = provider.GetRequiredService<ISmartCliAgent>();

Console.WriteLine("Smart CLI Assistant — ask me something (type 'exit' to quit).\n");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("you › ");
    Console.ResetColor();

    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("ai  › ");
    Console.ResetColor();

    await foreach (var chunk in agent.AskStreamingAsync(input))
        Console.Write(chunk);
    Console.WriteLine("\n");
}

// A custom tool defined right in the app — this is how any consumer extends the library
// with domain logic the library never knew about.
[Description("Calculates X percent of a number, for any general percentage question.")]
static string Percentage(
    [Description("The base value")] double value,
    [Description("The percentage to take")] double percent)
    => $"{percent}% of {value} is {value * percent / 100}.";
