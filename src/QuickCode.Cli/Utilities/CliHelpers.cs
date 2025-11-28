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
                    var currentTop = Console.CursorTop;
                    // On Mac, sometimes CursorTop can be 0 or negative, so we ensure it's valid
                    if (currentTop < 0)
                    {
                        currentTop = 0;
                    }
                    ProgressTop = currentTop;
                }

                var startRow = ProgressTop.Value;
                var bufferWidth = SafeBufferWidth();
                var bufferHeight = SafeBufferHeight();

                // Validate startRow is within buffer bounds
                if (startRow < 0 || startRow >= bufferHeight)
                {
                    // Reset if invalid
                    ProgressTop = Console.CursorTop;
                    startRow = ProgressTop.Value;
                }

                // Render each line at the correct position
                for (var i = 0; i < lines.Count; i++)
                {
                    var currentRow = startRow + i;
                    
                    // Skip if row is out of bounds
                    if (currentRow < 0 || currentRow >= bufferHeight)
                    {
                        continue;
                    }
                    
                    try
                    {
                        Console.SetCursorPosition(0, currentRow);
                        // Clear the entire line
                        Console.Write("\x1b[K");
                        // Write the line content (truncate if too long)
                        var line = lines[i];
                        if (line.Length > bufferWidth)
                        {
                            line = line.Substring(0, bufferWidth);
                        }
                        Console.Write(line);
                    }
                    catch
                    {
                        // If we can't set cursor position, try fallback
                        // Write the line normally (this will cause scrolling but at least it's visible)
                        Console.WriteLine(lines[i]);
                    }
                }

                // Clear any remaining lines from previous render
                if (ProgressHeight > lines.Count)
                {
                    for (var i = lines.Count; i < ProgressHeight; i++)
                    {
                        var currentRow = startRow + i;
                        
                        if (currentRow < 0 || currentRow >= bufferHeight)
                        {
                            continue;
                        }
                        
                        try
                        {
                            Console.SetCursorPosition(0, currentRow);
                            Console.Write("\x1b[K");
                        }
                        catch
                        {
                            // Skip if we can't set cursor position
                            continue;
                        }
                    }
                }

                // Update height
                ProgressHeight = lines.Count;
                
                // Move cursor to position after progress area (but don't print anything)
                // This ensures other console writes appear below the progress area
                try
                {
                    var newCursorTop = startRow + ProgressHeight;
                    if (newCursorTop >= 0 && newCursorTop < bufferHeight)
                    {
                        Console.SetCursorPosition(0, newCursorTop);
                    }
                }
                catch
                {
                    // If we can't move cursor, that's okay
                }
            }
            catch
            {
                // Fallback: if cursor positioning fails, use simple approach
                try
                {
                    if (ProgressTop.HasValue)
                    {
                        try
                        {
                            Console.SetCursorPosition(0, ProgressTop.Value);
                        }
                        catch
                        {
                            // Can't set cursor, just write normally
                        }
                    }

                    foreach (var line in lines)
                    {
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

