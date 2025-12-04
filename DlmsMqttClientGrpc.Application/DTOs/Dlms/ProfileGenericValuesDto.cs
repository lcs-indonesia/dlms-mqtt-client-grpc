namespace DlmsMqttClientGrpc.Application.DTOs.Dlms;

public class ProfileGenericValuesDto
{
    public List<DateTime> Times { get; set; } = [];
    public Dictionary<string, List<double>> DoubleValues { get; set; } = [];
    public Dictionary<string, List<string>> StringValues { get; set; } = [];
}
