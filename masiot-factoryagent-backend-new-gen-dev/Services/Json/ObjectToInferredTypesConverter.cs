using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Services.Json;

/// <summary>
/// Десериализует значения типа object в CLR-типы (string, long, double, bool,
/// Dictionary, List) вместо JsonElement. Newtonsoft.Json, которым сериализуются
/// сообщения RabbitMQ и параметры workflow, не умеет писать JsonElement
/// (получается {"ValueKind": N}), а ParameterValidationService ожидает примитивы.
/// </summary>
public class ObjectToInferredTypesConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.True:
                return true;
            case JsonTokenType.False:
                return false;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var l))
                    return l;
                return reader.GetDouble();
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.StartArray:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(Read(ref reader, typeof(object), options));
                }
                return list;
            case JsonTokenType.StartObject:
                var dict = new Dictionary<string, object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var key = reader.GetString()!;
                    reader.Read();
                    dict[key] = Read(ref reader, typeof(object), options);
                }
                return dict;
            default:
                using (var doc = JsonDocument.ParseValue(ref reader))
                {
                    return doc.RootElement.Clone();
                }
        }
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value.GetType() == typeof(object))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
