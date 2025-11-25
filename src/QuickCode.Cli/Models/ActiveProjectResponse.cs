using System.Text.Json;
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
    [JsonConverter(typeof(NullableDateTimeOffsetConverter))]
    public DateTimeOffset? StartDate { get; set; }
}

public class NullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(stringValue, out var dateTimeOffset))
            {
                return dateTimeOffset;
            }
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            // Handle Unix timestamp (milliseconds)
            if (reader.TryGetInt64(out var timestamp))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
            }
        }

        // If we can't parse it, return null instead of throwing
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

