using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;
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

services.AddLogging(b => b
    .AddSimpleConsole(o => o.SingleLine = true)
    .SetMinimumLevel(LogLevel.Information));

services.AddSingleton<IChatClient>(_ =>
    new OpenAI.Chat.ChatClient(
            "openai/gpt-4o-mini",
            new ApiKeyCredential(token),
            new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") })
        .AsIChatClient());

services.AddSmartCli(builder => builder
    .AddBuiltInTools()
    .AddTool(Percentage)
    .WithCaching());

using var provider = services.BuildServiceProvider();
var agent = provider.GetRequiredService<ISmartCliAgent>();

Console.WriteLine("Smart CLI Assistant — ask me something (type 'exit' to quit, '/reset' to clear history).");
Console.WriteLine("To see caching work: ask something, type /reset, then ask the EXACT same thing again.\n");

while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("you › ");
    Console.ResetColor();

    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    if (input.Trim().Equals("/reset", StringComparison.OrdinalIgnoreCase))
    {
        agent.Reset();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("(conversation reset — history is back to just the system prompt)\n");
        Console.ResetColor();
        continue;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("ai  › ");
    Console.ResetColor();

    var stopwatch = Stopwatch.StartNew();
    await foreach (var chunk in agent.AskStreamingAsync(input))
        Console.Write(chunk);
    stopwatch.Stop();

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"\n  ({stopwatch.ElapsedMilliseconds} ms)");
    Console.ResetColor();
    Console.WriteLine();
}

// A custom tool defined right in the app — a tool the library never heard of.
[Description("Calculates X percent of a number, for any general percentage question.")]
static string Percentage(
    [Description("The base value")] double value,
    [Description("The percentage to take")] double percent)
    => $"{percent}% of {value} is {value * percent / 100}.";