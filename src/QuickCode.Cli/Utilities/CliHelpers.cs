using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuickCode.Cli.Utilities;

public static class CliHelpers
{
    private static readonly object ProgressLock = new();
    private static int SpinnerFrame;
    private static readonly string[] SpinnerChars = { "→", "↘", "↓", "↙", "←", "↖", "↑", "↗" };
    private static int? _progressTop;
    private static int _progressHeight;

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
        var stepLines = new List<string>();
        
        // Only process steps if allSteps is a valid array
        if (allSteps.ValueKind == JsonValueKind.Array)
        {
            foreach (var step in allSteps.EnumerateArray())
            {
                var (status, durationLabel) = GetStepStatus(step, allActions);
                var description = step.TryGetProperty("description", out var descProp)
                    ? descProp.GetString() ?? "Unknown"
                    : "Unknown";
                stepLines.Add($"{status} - {description} [{durationLabel}]");
            }
        }
        else
        {
            // If no steps available, show a waiting message
            stepLines.Add("⏳ Waiting for generation steps...");
        }

        if (Console.IsOutputRedirected)
        {
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("Generation Progress");
            Console.WriteLine(new string('=', 60));
            foreach (var line in stepLines)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine(new string('=', 60));
            return;
        }

        lock (ProgressLock)
        {
            try
            {
                // Initialize progress area position on first render
                if (!_progressTop.HasValue)
                {
                    // On first render, write a newline to ensure we're on a fresh line
                    // Then save the cursor position
                    Console.WriteLine();
                    _progressTop = Console.CursorTop - 1; // -1 because WriteLine advanced cursor
                }

                var startRow = _progressTop.Value;
                var bufferWidth = SafeBufferWidth();
                var currentCursorTop = Console.CursorTop;

                // Calculate how many lines to move
                var linesToMove = currentCursorTop - startRow;

                // Move cursor to start of progress area
                if (linesToMove > 0)
                {
                    // Need to move up
                    Console.Write($"\x1b[{linesToMove}A");
                }
                else if (linesToMove < 0)
                {
                    // Need to move down (shouldn't normally happen)
                    Console.Write($"\x1b[{-linesToMove}B");
                }

                // Render each line, overwriting existing content
                for (var i = 0; i < stepLines.Count; i++)
                {
                    // Move to beginning of line and clear it
                    Console.Write("\r\x1b[K");
                    
                    var line = stepLines[i];
                    if (line.Length > bufferWidth)
                    {
                        line = line.Substring(0, bufferWidth);
                    }
                    Console.Write(line);
                    
                    // If not last line, move to next line
                    if (i < stepLines.Count - 1)
                    {
                        Console.Write("\n");
                    }
                }

                // Clear any remaining lines from previous render
                if (_progressHeight > stepLines.Count)
                {
                    for (var i = stepLines.Count; i < _progressHeight; i++)
                    {
                        Console.Write("\n\r\x1b[K");
                    }
                }

                // Update height
                _progressHeight = stepLines.Count;
                
                // Move cursor to end of progress area (after last line)
                var expectedFinalRow = startRow + stepLines.Count;
                var actualFinalRow = Console.CursorTop;
                var finalAdjustment = actualFinalRow - expectedFinalRow;
                
                if (finalAdjustment != 0)
                {
                    if (finalAdjustment > 0)
                    {
                        Console.Write($"\x1b[{finalAdjustment}A");
                    }
                    else
                    {
                        Console.Write($"\x1b[{-finalAdjustment}B");
                    }
                }
                
                // Move to beginning of line
                Console.Write("\r");
            }
            catch
            {
                // Fallback: if cursor positioning fails, use simple approach
                foreach (var line in stepLines)
                {
                    Console.WriteLine(line);
                }
                _progressTop = null;
                _progressHeight = 0;
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
            // Animated spinner using Unicode Braille characters
                SpinnerFrame = (SpinnerFrame + 1) % SpinnerChars.Length;
            durationLabel = SpinnerChars[SpinnerFrame];
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
            _progressTop = null;
            _progressHeight = 0;
        }
    }

    public static void ShowCompletionSummary(JsonElement allSteps, JsonElement allActions)
    {
        if (allSteps.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var totalSeconds = 0.0;
        var completedSteps = 0;

        foreach (var step in allSteps.EnumerateArray())
        {
            var actionId = step.TryGetProperty("actionId", out var actionIdProp)
                ? actionIdProp.GetInt32()
                : 0;

            var action = allActions.ValueKind == JsonValueKind.Array
                ? allActions.EnumerateArray().FirstOrDefault(a =>
                    a.TryGetProperty("id", out var idProp) && idProp.GetInt32() == actionId)
                : default;

            if (action.ValueKind != JsonValueKind.Undefined)
            {
                if (action.TryGetProperty("isCompleted", out var completedProp) && completedProp.GetBoolean())
                {
                    completedSteps++;
                    if (action.TryGetProperty("elapsedTime", out var elapsedProp) &&
                        elapsedProp.TryGetDouble(out var elapsedMs))
                    {
                        totalSeconds += elapsedMs / 1000d;
                    }
                }
            }
        }

        if (completedSteps > 0)
        {
            var totalDuration = FormatDuration(totalSeconds);
            Console.WriteLine();
            Console.WriteLine(new string('=', 60));
            Console.WriteLine($"✅ Generation completed successfully!");
            Console.WriteLine($"   Total steps: {completedSteps}");
            Console.WriteLine($"   Total duration: {totalDuration}");
            Console.WriteLine(new string('=', 60));
        }
    }

    public static void ReleaseProgressArea()
    {
        ResetProgressArea();
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

    public static void RenderTemplatesList(JsonElement templates)
    {
        if (templates.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("No templates found.");
            return;
        }

        var rows = new List<(string Key, string Description)>();
        foreach (var template in templates.EnumerateArray())
        {
            var key = template.TryGetProperty("key", out var keyProp) ? keyProp.GetString() ?? "-" : "-";
            var description = template.TryGetProperty("description", out var descProp) 
                ? descProp.GetString() ?? "-"
                : template.TryGetProperty("name", out var nameProp) 
                    ? nameProp.GetString() ?? "-" 
                    : "-";
            
            rows.Add((Key: key, Description: description));
        }

        if (rows.Count == 0)
        {
            Console.WriteLine("No templates found.");
            return;
        }

        Console.WriteLine($"Available Templates ({rows.Count}):");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("{0,-30} {1,-50}", "Template Key", "Description");
        Console.WriteLine(new string('-', 80));
        foreach (var row in rows)
        {
            // Truncate description if too long
            var description = row.Description.Length > 50 
                ? row.Description.Substring(0, 47) + "..." 
                : row.Description;
            Console.WriteLine("{0,-30} {1,-50}", row.Key, description);
        }
        Console.WriteLine(new string('-', 80));
    }

    public static bool AreAllActionsCompleted(JsonElement allActions)
    {
        if (allActions.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var actionsArray = allActions.EnumerateArray().ToList();
        if (actionsArray.Count == 0)
        {
            return false;
        }

        foreach (var action in actionsArray)
        {
            if (!action.TryGetProperty("isCompleted", out var completedProp) ||
                !completedProp.GetBoolean())
            {
                return false;
            }
        }

        return true;
    }

    public static bool AreAllStepsCompleted(JsonElement allSteps, JsonElement allActions)
    {
        if (allSteps.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var stepsArray = allSteps.EnumerateArray().ToList();
        if (stepsArray.Count == 0)
        {
            return false;
        }

        foreach (var step in stepsArray)
        {
            var actionId = step.TryGetProperty("actionId", out var actionIdProp)
                ? actionIdProp.GetInt32()
                : 0;

            var action = allActions.ValueKind == JsonValueKind.Array
                ? allActions.EnumerateArray().FirstOrDefault(a =>
                    a.TryGetProperty("id", out var idProp) && idProp.GetInt32() == actionId)
                : default;

            // If action exists, check if it's completed
            if (action.ValueKind != JsonValueKind.Undefined)
            {
                if (!action.TryGetProperty("isCompleted", out var completedProp) ||
                    !completedProp.GetBoolean())
                {
                    return false;
                }
            }
            // If action doesn't exist, we can't determine completion status
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

