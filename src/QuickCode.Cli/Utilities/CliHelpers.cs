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
        if (allSteps.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var lines = new List<string>
        {
            new string('=', 60),
            "Generation Progress",
            new string('=', 60)
        };

        foreach (var step in allSteps.EnumerateArray())
        {
            var actionId = step.TryGetProperty("actionId", out var actionIdProp)
                ? actionIdProp.GetInt32()
                : 0;

            var action = allActions.ValueKind == JsonValueKind.Array
                ? allActions.EnumerateArray().FirstOrDefault(a =>
                    a.TryGetProperty("id", out var idProp) && idProp.GetInt32() == actionId)
                : default;

            var description = step.TryGetProperty("description", out var descProp)
                ? descProp.GetString() ?? "Unknown"
                : "Unknown";

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

            lines.Add($"{status} - {description} [{durationLabel}]");
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
            // Initialize progress area position on first render
            if (!ProgressTop.HasValue)
            {
                ProgressTop = Console.CursorTop;
            }

            try
            {
                var startRow = ProgressTop.Value;
                var bufferWidth = SafeBufferWidth();

                // Move cursor to start of progress area
                Console.SetCursorPosition(0, startRow);

                // Clear and rewrite each line
                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    // Clear the line
                    Console.Write("\x1b[2K");
                    // Write the new content
                    Console.Write(line);
                    // If not last line, move to next line
                    if (i < lines.Count - 1)
                    {
                        Console.WriteLine();
                    }
                }

                // Clear any remaining lines from previous render
                if (ProgressHeight > lines.Count)
                {
                    for (var i = lines.Count; i < ProgressHeight; i++)
                    {
                        Console.Write("\x1b[2K");
                        if (i < ProgressHeight - 1)
                        {
                            Console.WriteLine();
                        }
                    }
                }

                // Update height and position cursor after progress area
                ProgressHeight = lines.Count;
                Console.SetCursorPosition(0, startRow + ProgressHeight);
            }
            catch
            {
                // Fallback: if cursor positioning fails, use simple approach
                try
                {
                    if (ProgressTop.HasValue)
                    {
                        Console.SetCursorPosition(0, ProgressTop.Value);
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
            if (ProgressTop is null || ProgressHeight <= 0)
            {
                ProgressTop = null;
                ProgressHeight = 0;
                return;
            }

            var width = Math.Max(1, SafeBufferWidth());
            var targetTop = Math.Clamp(ProgressTop.Value, 0, Math.Max(Console.BufferHeight - 1, 0));
            SetCursorSafely(targetTop);

            for (var i = 0; i < ProgressHeight; i++)
            {
                WritePaddedLine(string.Empty, width);
            }

            SetCursorSafely(targetTop);
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
            return Console.BufferWidth;
        }
        catch
        {
            return 120;
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

