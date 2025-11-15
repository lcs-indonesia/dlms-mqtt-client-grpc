using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace DlmsMqttClientGrpc.Application.Extensions;

public static class ProtoExtensions
{
    private static readonly JsonParser Parser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
    public static Struct ToProtoStruct(this string json)
    {
        return Parser.Parse<Struct>(json);
    }
    public static Struct ToProtoStruct(this Dictionary<string, object> dict)
    {
        var s = new Struct();
        foreach (var kvp in dict)
        {
            s.Fields[kvp.Key] = ToProtoValue(kvp.Value);
        }
        return s;
    }

    public static Value ToProtoValue(object value)
    {
        if (value == null)
            return Value.ForNull();

        switch (value)
        {
            case string str:
                return Value.ForString(str);
            case bool b:
                return Value.ForBool(b);
            case int i:
                return Value.ForNumber(i);
            case long l:
                return Value.ForNumber(l);
            case float f:
                return Value.ForNumber(f);
            case double d:
                return Value.ForNumber(d);
            case decimal dec:
                return Value.ForNumber(Convert.ToDouble(dec));
            case DateTime dt:
                // store DateTime as ISO 8601 string
                return Value.ForString(dt.ToUniversalTime().ToString("O"));
            case IDictionary<string, object> subDict:
                return Value.ForStruct(ToProtoStruct(subDict.ToDictionary(k => k.Key, v => v.Value)));
            case IEnumerable<object> list:
                var listValue = new ListValue();
                foreach (var item in list)
                    listValue.Values.Add(ToProtoValue(item));
                return Value.ForList(listValue.Values.ToArray());
            default:
                // fallback: serialize to JSON string
                return Value.ForString(System.Text.Json.JsonSerializer.Serialize(value));
        }
    }
}
