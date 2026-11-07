using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace AgendaiFisio.Validations;

// Exige um offset explícito na entrada e entrega sempre um DateTime em UTC ao serviço.
public sealed class DataHoraUtcJsonConverter : JsonConverter<DateTime>
{
    private static readonly Regex OffsetExplicito = new(
        @"(?:Z|[+-]\d{2}:\d{2})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String ||
            reader.GetString() is not { } texto ||
            !OffsetExplicito.IsMatch(texto))
        {
            throw new JsonException("A data e hora devem estar em ISO 8601 com Z ou offset explícito.");
        }

        try
        {
            return reader.GetDateTimeOffset().UtcDateTime;
        }
        catch (FormatException ex)
        {
            throw new JsonException("A data e hora informadas são inválidas.", ex);
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new JsonException("A data e hora devem estar em UTC.");

        writer.WriteStringValue(value);
    }
}
