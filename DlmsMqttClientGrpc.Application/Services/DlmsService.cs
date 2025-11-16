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
        var args = request.Args.ToList();
        if (!TryGetTopic(args, out var topic) || topic == null)
            throw new ArgumentNullException("Topic is null in args[].");
        var sliding = dlmsClientManager.GetConnection(topic, args);
        var client = sliding.Value;
        using var _ = sliding.BeginRead();

        var reads = args[args.IndexOf("-g") + 1].Split(";");
        try
        {
            foreach (var read in reads)
            {
                if (dict.ContainsKey(read)) continue;
                logger.LogDebug("Reading object {0}...", read);
                var part = read.Split(":");
                var values = client.ReadObject([new(part[0], int.Parse(part[1]))]);
                dict.Add(read, values);
            }
        }
        catch (Exception)
        {
            sliding.Dispose();
            throw;
        }
        var data = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
        logger.LogDebug("Reply data: {data}", data);
        var proto = data.ToProtoStruct();
        return Task.FromResult<ConnectAndReadReply>(new()
        {
            Value = proto
        });


    }
    private bool TryGetTopic(List<string> args, out string? topic)
    {
        topic = null;
        var index = args.IndexOf("-q");
        if (index == -1) return false;
        if (args.Count <= index + 1) return false;

        topic = args[index + 1];
        return true;
    }
}
