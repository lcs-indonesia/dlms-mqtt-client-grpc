using Google.Protobuf.WellKnownTypes;

namespace DlmsMqttClientGrpc.Application.DTOs.Dlms;
public static class DlmsMapper
{
    public static ProfileGenericReply ToProfileGenericReply(this ProfileGenericValuesDto source)
    {
        var result = new ProfileGenericReply();
        result.Times.AddRange(source.Times.Select(p => p.ToTimestamp()));
        foreach (var pair in source.DoubleValues)
        {
            var list = new DoubleList();
            list.Values.AddRange(pair.Value);
            result.NumberValues.Add(pair.Key, list);
        }
        foreach (var pair in source.StringValues)
        {
            var list = new StringList();
            list.Values.AddRange(pair.Value);
            result.StringValues.Add(pair.Key, list);
        }
        return result;
    }
}
