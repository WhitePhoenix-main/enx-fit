using System.Text.Json;
using System.Text.Json.Serialization;
using enx_fit.Models;

namespace enx_fit.Serialization;

public sealed class ExerciseIdJsonConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var legacyId))
            return ExerciseIds.FromLegacy(legacyId);
        if (reader.TokenType == JsonTokenType.String && ExerciseIds.TryParse(reader.GetString(), out var id)) return id;
        throw new JsonException("Invalid exercise ID.");
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
