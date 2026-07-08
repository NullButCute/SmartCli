# SmartCli

A small, reusable .NET library for building **tool-calling AI agents** on top of
[Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai).

Give it any `IChatClient` (OpenAI, Azure OpenAI, GitHub Models, Ollama), register a few C#
methods as tools, and the model decides which to call, extracts the arguments from plain
language, runs them, and weaves the results into its reply.

The repo ships two projects:

| Project | What it is |
|---|---|
| **`SmartCli.Core`** | The library — provider-agnostic, no console, publishable as a NuGet package |
| **`SmartCli.App`** | A thin console app that consumes the library (reference implementation) |

## Design in one picture

```
SmartCli.App  ──references──▶  SmartCli.Core  ──depends on──▶  Microsoft.Extensions.AI
 (executable)                    (library)                       (interfaces only)
```

The arrow points one way. The library never references the app, never references a *concrete*
model provider, and never writes to the console. That one-directional flow is what makes it
reusable anywhere — a web app, a background service, another CLI.

## Two ways to consume it

**A. Fluent builder** — direct construction:

```csharp
ISmartCliAgent agent = new SmartCliBuilder()
    .WithChatClient(myChatClient)      // any IChatClient
    .WithSystemPrompt("You are ...")   // optional
    .AddBuiltInTools()                 // file / tip / time tools
    .AddTool(MyCustomTool)             // your own method
    .Build();

await foreach (var chunk in agent.AskStreamingAsync("what's 18% tip on $64?"))
    Console.Write(chunk);
```

**B. Dependency injection** — the idiomatic .NET path (drives the same builder internally):

```csharp
services.AddSingleton<IChatClient>(/* your provider */);
services.AddSmartCli(b => b.AddBuiltInTools().AddTool(MyCustomTool));

// then resolve ISmartCliAgent wherever you need it
```

## Writing a tool

A tool is just a C# method. The `[Description]` attributes are the **only** signal the model
gets for when and how to call it — they steer routing, so write them precisely:

```csharp
[Description("Calculates X percent of a number, for any general percentage question.")]
static string Percentage(
    [Description("The base value")] double value,
    [Description("The percentage to take")] double percent)
    => $"{percent}% of {value} is {value * percent / 100}.";
```

Register it with `.AddTool(Percentage)`. `AIFunctionFactory` reflects over the signature to build
the JSON schema the model calls against — no manual schema writing.

## Building and running the sample

```bash
# scaffold is already in src/ — just restore and set your token
dotnet restore

dotnet user-secrets init --project src/SmartCli.App
dotnet user-secrets set GitHubModels:Token "<your token>" --project src/SmartCli.App

dotnet run --project src/SmartCli.App
```

Get a GitHub Models token at GitHub → Settings → Developer settings → fine-grained tokens →
**Models: Read-only**.

Then try:

- `what time is it?` — a no-argument tool call
- `what's 18% tip on $64?` — built-in tip tool
- `what's 25% of 100?` — the custom percentage tool
- `how many cs files are in <path>?` — file tool with two arguments
- `what's the capital of France?` — no tool fires; the model just answers

Logging is configured in the **app**, so tool invocations show up as console log lines — the
library itself stays silent and lets the host decide.

## Swapping providers

Because the library depends only on `IChatClient`, changing model providers is a change in the
**app**, not the library. For example, to run fully local with Ollama, register an Ollama-backed
`IChatClient` instead of the GitHub Models one — the agent and tools are untouched.

## License

MIT
