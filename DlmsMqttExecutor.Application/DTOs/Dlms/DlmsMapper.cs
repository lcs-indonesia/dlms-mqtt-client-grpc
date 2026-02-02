using Google.Protobuf.WellKnownTypes;

namespace DlmsMqttExecutor.Application.DTOs.Dlms;

public static class DlmsMapper
{
    public static ProfileGenericReply ToProfileGenericReply(this ProfileGenericValuesDto source)
    {
        var result = new ProfileGenericReply();
        result.NumberSchema.AddRange(source.DobleSchema);
        result.StringSchema.AddRange(source.StringSchema);
        result.Rows.AddRange(source.Rows.Select(p => p.ToProfileGenericRow()));
        return result;
    }
    public static ProfileGenericRow ToProfileGenericRow(this ProfileGenericRowDto source)
    {
        var result = new ProfileGenericRow()
        {
            Time = source.Time.ToTimestamp()
        };
        result.NumberValues.AddRange(source.DoubleValues);
        result.StringValues.AddRange(source.StringValues);
        return result;
    }
}
