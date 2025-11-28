using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuickCode.Cli.Utilities;

public static class CliHelpers
{
    private static readonly object ProgressLock = new();
    private static int? ProgressTop;
    private static int ProgressHeight;
    private static int SpinnerFrame;
    private static readonly char[] SpinnerChars = { '|', '/', '-', '\\' };

    public static string GenerateSessionId()
    {
        var buffer = RandomNumberGenerator.GetBytes(16);
        var builder = new StringBuilder();
        foreach (var b in buffer)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }

    public static void RenderStepProgress(JsonElement allSteps, JsonElement allActions)
    {
        var lines = new List<string>
        {
            new string('=', 60),
            "Generation Progress",
            new string('=', 60)
        };
        
        // Only process steps if allSteps is a valid array
        if (allSteps.ValueKind == JsonValueKind.Array)
        {
            foreach (var step in allSteps.EnumerateArray())
            {
                var (status, durationLabel) = GetStepStatus(step, allActions);
                var description = step.TryGetProperty("description", out var descProp)
                    ? descProp.GetString() ?? "Unknown"
                    : "Unknown";
                lines.Add($"{status} - {description} [{durationLabel}]");
            }
        }
        else
        {
            // If no steps available, show a waiting message
            lines.Add("⏳ Waiting for generation steps...");
        }
        
        lines.Add(new string('=', 60));
        lines.Add(string.Empty);

        if (Console.IsOutputRedirected)
        {
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
            return;
        }

        lock (ProgressLock)
        {
            try
            {
                // Initialize progress area position on first render
                if (!ProgressTop.HasValue)
                {
                    ProgressTop = Console.CursorTop;
                }

                var startRow = ProgressTop.Value;
                var bufferWidth = SafeBufferWidth();

                // On Mac, use ANSI escape codes to move cursor up to the progress area
                // Calculate how many lines we need to move up
                var currentCursorTop = Console.CursorTop;
                var linesToMoveUp = currentCursorTop - startRow;

                // If we're not at the start position, move cursor up
                if (linesToMoveUp > 0)
                {
                    // Move cursor up using ANSI escape code
                    Console.Write($"\x1b[{linesToMoveUp}A");
                }
                else if (linesToMoveUp < 0)
                {
                    // If we're above the start position, move down
                    Console.Write($"\x1b[{-linesToMoveUp}B");
                }

                // Now render each line
                for (var i = 0; i < lines.Count; i++)
                {
                    // Move to beginning of line
                    Console.Write("\r");
                    // Clear the entire line
                    Console.Write("\x1b[K");
                    // Write the line content (truncate if too long)
                    var line = lines[i];
                    if (line.Length > bufferWidth)
                    {
                        line = line.Substring(0, bufferWidth);
                    }
                    Console.Write(line);
                    
                    // If not last line, move to next line
                    if (i < lines.Count - 1)
                    {
                        Console.Write("\n");
                    }
                }

                // Clear any remaining lines from previous render
                if (ProgressHeight > lines.Count)
                {
                    for (var i = lines.Count; i < ProgressHeight; i++)
                    {
                        Console.Write("\r\x1b[K");
                        if (i < ProgressHeight - 1)
                        {
                            Console.Write("\n");
                        }
                    }
                }

                // Update height
                ProgressHeight = lines.Count;
                
                // Move cursor to position after progress area
                // We're already at the last line of progress area, so we're good
            }
            catch
            {
                // Fallback: if cursor positioning fails, use simple approach
                try
                {
                    // Try to move cursor to start position using ANSI codes
                    if (ProgressTop.HasValue)
                    {
                        var currentTop = Console.CursorTop;
                        var diff = currentTop - ProgressTop.Value;
                        if (diff > 0)
                        {
                            Console.Write($"\x1b[{diff}A");
                        }
                    }

                    foreach (var line in lines)
                    {
                        Console.Write("\r\x1b[K");
                        Console.WriteLine(line);
                    }

                    ProgressHeight = lines.Count;
                }
                catch
                {
                    // Last resort: just write normally
                    foreach (var line in lines)
                    {
                        Console.WriteLine(line);
                    }
                    ProgressTop = null;
                    ProgressHeight = 0;
                }
            }
        }
    }

    private static (string status, string durationLabel) GetStepStatus(JsonElement step, JsonElement allActions)
    {
        var actionId = step.TryGetProperty("actionId", out var actionIdProp)
            ? actionIdProp.GetInt32()
            : 0;

        var action = allActions.ValueKind == JsonValueKind.Array
            ? allActions.EnumerateArray().FirstOrDefault(a =>
                a.TryGetProperty("id", out var idProp) && idProp.GetInt32() == actionId)
            : default;

        var status = "⏳ Waiting";
        double? elapsedSeconds = null;

        if (action.ValueKind != JsonValueKind.Undefined)
        {
            if (action.TryGetProperty("isCompleted", out var completedProp) && completedProp.GetBoolean())
            {
                status = "✅ Completed";
                if (action.TryGetProperty("elapsedTime", out var elapsedProp) &&
                    elapsedProp.TryGetDouble(out var elapsedMs))
                {
                    elapsedSeconds = elapsedMs / 1000d;
                }
            }
            else if (action.TryGetProperty("startDate", out var startProp) &&
                     startProp.ValueKind != JsonValueKind.Null &&
                     startProp.ValueKind != JsonValueKind.Undefined)
            {
                status = "🔄 In Progress";
                if (startProp.ValueKind == JsonValueKind.String &&
                    DateTimeOffset.TryParse(startProp.GetString(), out var start))
                {
                    elapsedSeconds = Math.Max(0, (DateTimeOffset.UtcNow - start).TotalSeconds);
                }
            }
        }

        string durationLabel;
        if (status == "🔄 In Progress")
        {
            // Animated spinner like Docker
            SpinnerFrame = (SpinnerFrame + 1) % SpinnerChars.Length;
            durationLabel = $"{SpinnerChars[SpinnerFrame]}";
        }
        else if (elapsedSeconds.HasValue)
        {
            durationLabel = FormatDuration(elapsedSeconds.Value);
        }
        else
        {
            durationLabel = "--";
        }

        return (status, durationLabel);
    }

    public static void ResetProgressArea()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        lock (ProgressLock)
        {
            ProgressTop = null;
            ProgressHeight = 0;
        }
    }

    public static void ReleaseProgressArea()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        lock (ProgressLock)
        {
            ProgressTop = null;
            ProgressHeight = 0;
        }
    }

    public static void RenderModuleList(JsonElement modules)
    {
        if (modules.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("No modules found.");
            return;
        }

        var rows = new List<(string Name, string Template, string DbType, string Pattern)>();
        foreach (var module in modules.EnumerateArray())
        {
            rows.Add((
                Name: module.TryGetProperty("moduleName", out var nameProp) ? nameProp.GetString() ?? "-" : "-",
                Template: module.TryGetProperty("moduleTemplateKey", out var templateProp) ? templateProp.GetString() ?? "-" : "-",
                DbType: module.TryGetProperty("dbTypeKey", out var dbTypeProp) ? dbTypeProp.GetString() ?? "-" : "-",
                Pattern: module.TryGetProperty("architecturalPatternKey", out var patternProp) ? patternProp.GetString() ?? "-" : "-"
            ));
        }

        if (rows.Count == 0)
        {
            Console.WriteLine("No modules found.");
            return;
        }

        Console.WriteLine($"Modules ({rows.Count}):");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("{0,-25} {1,-20} {2,-12} {3,-12}", "Name", "Template", "DB Type", "Pattern");
        Console.WriteLine(new string('-', 80));
        foreach (var row in rows)
        {
            Console.WriteLine("{0,-25} {1,-20} {2,-12} {3,-12}", row.Name, row.Template, row.DbType, row.Pattern);
        }
        Console.WriteLine(new string('-', 80));
    }

    public static bool AreAllActionsCompleted(JsonElement allActions)
    {
        if (allActions.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var action in allActions.EnumerateArray())
        {
            if (!action.TryGetProperty("isCompleted", out var completedProp) ||
                !completedProp.GetBoolean())
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatDuration(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
        {
            return "--";
        }

        var ts = TimeSpan.FromSeconds(seconds);

        if (seconds >= 3600)
        {
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        }

        if (seconds >= 60)
        {
            return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        }

        return $"{seconds:F1}s";
    }

    private static int SafeBufferWidth()
    {
        try
        {
            var width = Console.BufferWidth;
            return width > 0 ? width : 120;
        }
        catch
        {
            return 120;
        }
    }

    private static int SafeBufferHeight()
    {
        try
        {
            var height = Console.BufferHeight;
            return height > 0 ? height : 1000;
        }
        catch
        {
            return 1000;
        }
    }

    private static void SetCursorSafely(int top)
    {
        try
        {
            Console.SetCursorPosition(0, Math.Max(0, top));
        }
        catch
        {
            // Ignore when cursor cannot be moved (e.g. redirected output)
        }
    }

    private static void WritePaddedLine(string content, int width)
    {
        if (content.Length >= width)
        {
            Console.WriteLine(content);
            return;
        }

        Console.Write(content);
        Console.Write(new string(' ', width - content.Length));
        Console.WriteLine();
    }
}

