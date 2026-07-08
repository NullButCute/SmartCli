using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SmartCli.Core.Tools;

/// <summary>
/// The default tool set shipped with SmartCli: file inspection, tip math, and current time.
/// These log activity through <see cref="ILogger"/> — never directly to the console — so the
/// consuming application decides where that output goes (console, file, sink, or nowhere).
/// </summary>
public sealed class BuiltInTools
{
    private readonly ILogger _logger;

    public BuiltInTools(ILogger? logger = null) => _logger = logger ?? NullLogger.Instance;

    /// <summary>Returns the tools as <see cref="AITool"/> instances ready to register.</summary>
    public IEnumerable<AITool> GetTools() =>
    [
        AIFunctionFactory.Create(ListFiles),
        AIFunctionFactory.Create(CountFilesByExtension),
        AIFunctionFactory.Create(CalculateTip),
        AIFunctionFactory.Create(GetCurrentDateTime),
    ];

    [Description("Lists the names of files in a given directory on the user's computer.")]
    private string ListFiles(
        [Description("The full path to the directory")] string directoryPath)
    {
        _logger.LogInformation("Tool invoked: ListFiles({Path})", directoryPath);
        if (!Directory.Exists(directoryPath))
            return $"Error: the directory '{directoryPath}' does not exist.";

        var files = Directory.GetFiles(directoryPath).Select(Path.GetFileName).ToArray();
        return files.Length == 0
            ? "The directory is empty."
            : $"Found {files.Length} files: {string.Join(", ", files)}";
    }

    [Description("Counts how many files with a specific extension exist in a directory.")]
    private string CountFilesByExtension(
        [Description("The full path to the directory")] string directoryPath,
        [Description("The file extension without the dot, e.g. 'cs' or 'txt'")] string extension)
    {
        _logger.LogInformation("Tool invoked: CountFilesByExtension({Path}, {Ext})", directoryPath, extension);
        if (!Directory.Exists(directoryPath))
            return $"Error: the directory '{directoryPath}' does not exist.";

        var count = Directory.GetFiles(directoryPath, $"*.{extension}").Length;
        return $"There are {count} .{extension} files in that directory.";
    }

    [Description("Calculates a tip and total for a bill amount.")]
    private string CalculateTip(
        [Description("The bill amount before tip")] double billAmount,
        [Description("The tip percentage, e.g. 15 for 15%")] double tipPercent)
    {
        _logger.LogInformation("Tool invoked: CalculateTip({Bill}, {Pct})", billAmount, tipPercent);
        var tip = billAmount * tipPercent / 100;
        return $"Tip: {tip:C}. Total with tip: {(billAmount + tip):C}.";
    }

    [Description("Gets the current date and time on the user's computer.")]
    private string GetCurrentDateTime()
    {
        _logger.LogInformation("Tool invoked: GetCurrentDateTime()");
        return DateTime.Now.ToString("dddd, dd MMMM yyyy, HH:mm");
    }
}
