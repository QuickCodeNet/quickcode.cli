using System.Text.Json;

namespace QuickCode.Cli.Configuration;

public sealed class ConfigService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _configPath;
    private readonly object _lock = new();

    public ConfigService()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configPath = Path.Combine(home, ".quickcode", "config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
    }

    public QuickCodeConfig Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_configPath))
            {
                return new QuickCodeConfig();
            }

            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<QuickCodeConfig>(json, SerializerOptions) ?? new QuickCodeConfig();
        }
    }

    public void Save(QuickCodeConfig config)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(config, SerializerOptions);
            File.WriteAllText(_configPath, json);
        }
    }

    public (string projectName, string projectEmail, string secret) ResolveProjectCredentials(
        QuickCodeConfig config,
        string? projectName,
        string? email,
        string? secret)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new InvalidOperationException("Project name is required for this command.");
        }

        var projectConfig = config.Projects.TryGetValue(projectName, out var pConfig)
            ? pConfig
            : null;

        var resolvedEmail = email
                            ?? projectConfig?.Email
                            ?? throw new InvalidOperationException("Project email is required. Pass --email or set it via 'config --project <name> --set email=...'.");

        var resolvedSecret = secret
                             ?? projectConfig?.SecretCode
                             ?? throw new InvalidOperationException("Project secret code is required. Pass --secret-code or set it via 'config --project <name> --set secret_code=...'.");

        return (projectName, resolvedEmail, resolvedSecret);
    }
}

