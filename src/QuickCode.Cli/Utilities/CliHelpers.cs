using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuickCode.Cli.Utilities;

public static class CliHelpers
{
    private static readonly object ProgressLock = new();
    private static int? ProgressTop;
    private static int ProgressHeight;

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
                durationLabel = "...";
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
            var width = Math.Max(1, SafeBufferWidth());

            if (ProgressTop is null)
            {
                ProgressTop = Console.CursorTop;
            }

            var targetTop = Math.Clamp(ProgressTop.Value, 0, Math.Max(Console.BufferHeight - 1, 0));
            SetCursorSafely(targetTop);

            foreach (var line in lines)
            {
                WritePaddedLine(line, width);
            }

            for (var i = lines.Count; i < ProgressHeight; i++)
            {
                WritePaddedLine(string.Empty, width);
            }

            ProgressHeight = lines.Count;
            var cursorRow = Math.Clamp(targetTop + ProgressHeight, 0, Math.Max(Console.BufferHeight - 1, 0));
            SetCursorSafely(cursorRow);
            ProgressTop = targetTop;
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

