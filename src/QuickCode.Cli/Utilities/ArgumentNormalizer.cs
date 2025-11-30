using System;
using System.Collections.Generic;

namespace QuickCode.Cli.Utilities;

internal static class ArgumentNormalizer
{
    private static readonly HashSet<string> ProjectFirstVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "create",
        "check",
        "forgot-secret",
        "verify-secret",
        "get-dbmls",
        "update-dbmls",
        "validate",
        "generate"
    };

    private static readonly HashSet<string> RootCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "config",
        "project",
        "module",
        "create",
        "check",
        "forgot-secret",
        "verify-secret",
        "get-dbmls",
        "update-dbmls",
        "validate",
        "generate",
        "status"
    };

    public static string[] Normalize(string[] args)
    {
        if (args.Length >= 2)
        {
            var potentialProject = args[0];
            var potentialVerb = args[1];

            if (!potentialProject.StartsWith("-", StringComparison.Ordinal) &&
                potentialVerb.Equals("config", StringComparison.OrdinalIgnoreCase))
            {
                var normalized = new List<string> { "config", "--project", potentialProject };
                normalized.AddRange(args.Skip(2));
                return normalized.ToArray();
            }

            if (!potentialProject.StartsWith("-", StringComparison.Ordinal) &&
                potentialVerb.Equals("modules", StringComparison.OrdinalIgnoreCase))
            {
                // Handle "demo modules", "demo modules add", "demo modules remove"
                if (args.Length >= 3)
                {
                    var subCommand = args[2];
                    if (subCommand.Equals("add", StringComparison.OrdinalIgnoreCase))
                    {
                        var normalized = new List<string> { "module", "add", "--project", potentialProject };
                        normalized.AddRange(args.Skip(3));
                        return normalized.ToArray();
                    }
                    if (subCommand.Equals("remove", StringComparison.OrdinalIgnoreCase))
                    {
                        var normalized = new List<string> { "module", "remove", "--project", potentialProject };
                        normalized.AddRange(args.Skip(3));
                        return normalized.ToArray();
                    }
                }
                // Default: "demo modules" -> "module list --project demo"
                var normalizedList = new List<string> { "module", "list", "--project", potentialProject };
                normalizedList.AddRange(args.Skip(2));
                return normalizedList.ToArray();
            }

            if (!potentialProject.StartsWith("-", StringComparison.Ordinal) &&
                ProjectFirstVerbs.Contains(potentialVerb) &&
                !RootCommands.Contains(potentialProject))
            {
                var normalized = new string[args.Length];
                normalized[0] = potentialVerb;
                normalized[1] = potentialProject;
                Array.Copy(args, 2, normalized, 2, args.Length - 2);
                return normalized;
            }
        }

        return args;
    }
}

