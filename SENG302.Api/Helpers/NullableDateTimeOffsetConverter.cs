using System.Text.Json;
using System.Text.Json.Serialization;

namespace SENG302.Api.Helpers;

/// <summary>
/// Written by ChatGPT 5.6 Luna
/// Prompt: "When using [FromBody], if a badly formatted value is sent through, how can I make it set it to null instead of breaking?"
/// </summary>
public class NullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;


        var readerString = reader.GetString();
        // Stupidest edge case
        if (reader.TokenType == JsonTokenType.String && readerString != null && readerString.StartsWith("0000"))
        {
            return DateTimeOffset.MinValue;
        }

        if (reader.TokenType == JsonTokenType.String &&
            DateTimeOffset.TryParse(readerString, out var value))
        {
            return value;
        }

        return null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset? value,
        JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value);
        else
            writer.WriteNullValue();
    }
}