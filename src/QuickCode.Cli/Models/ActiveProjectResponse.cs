using System.Text.Json.Serialization;

namespace QuickCode.Cli.Models;

public sealed class ActiveProjectResponse
{
    [JsonPropertyName("activeRunId")]
    public int ActiveRunId { get; set; }

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; set; }

    [JsonPropertyName("isFinished")]
    public bool IsFinished { get; set; }

    [JsonPropertyName("startDate")]
    public DateTimeOffset? StartDate { get; set; }
}

