using System.Text.Json.Serialization;

namespace QuickCode.Cli.Configuration;

public sealed class QuickCodeConfig
{
    public string ApiUrl { get; set; } = "https://api.quickcode.net/";

    public Dictionary<string, ProjectConfig> Projects { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ProjectConfig
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("secret_code")]
    public string? SecretCode { get; set; }
}

