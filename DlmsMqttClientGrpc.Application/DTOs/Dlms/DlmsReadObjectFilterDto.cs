namespace DlmsMqttClientGrpc.Application.DTOs.Dlms;

public class DlmsReadObjectFilterDto
{
    public uint? Skip { get; set; }
    public uint? Take { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

}
