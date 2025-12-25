namespace DlmsMqttClientGrpc.Application.DTOs.Dlms;

public class ProfileGenericValuesDto
{
    public List<string> DobleSchema { get; set; } = [];
    public List<string> StringSchema { get; set; } = [];
    public List<ProfileGenericRowDto> Rows { get; set; } = [];
}
public class ProfileGenericRowDto
{
    public DateTime Time { get; set; }
    public List<double> DoubleValues { get; set; } = [];
    public List<string> StringValues { get; set; } = [];
}
