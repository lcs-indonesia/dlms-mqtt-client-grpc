using System.Text.Json;
using DlmsMqttClientGrpc.Application.Extensions;
using DlmsMqttClientGrpc.Application.Interfaces;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DlmsMqttClientGrpc.Application.Services;

public class DlmsService(
    IDlmsClientManager dlmsClientManager,
    ILogger<DlmsService> logger) : DlmsProto.DlmsProtoBase
{
    public override Task<ConnectAndReadReply> ConnectAndRead(ConnectAndReadRequest request, ServerCallContext context)
    {
        var dict = new Dictionary<string, IEnumerable<object>>();
        var args = request.Args.ToArray();
        var client = dlmsClientManager.GetConnection(request.SessionId, args);

        var reads = args[Array.IndexOf(args, "-g") + 1].Split(";");

        foreach (var read in reads)
        {
            if (dict.ContainsKey(read)) continue;
            logger.LogDebug("Reading object {0}...", read);
            var part = read.Split(":");
            var values = client.ReadObject([new(part[0], int.Parse(part[1]))]);
            dict.Add(read, values);
        }
        var data = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        logger.LogDebug("Reply data: {data}", data);
        var proto = data.ToProtoStruct();
        return Task.FromResult<ConnectAndReadReply>(new()
        {
            Value = proto
        });
    }
}
