using System.Text.Json;
using DlmsMqttClientGrpc.Application.Exceptions;
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
        ISlidingItem<IDlmsClient> sliding;
        try
        {
            sliding = dlmsClientManager.GetConnection(topic, args, request.CustomArgs.IsClearCache ?? false);
        }
        catch (Exception e)
        {
            throw new StatusCodeException(500, $"Error on get dlms connection: {e.Message}", e);
        }
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
                var values = client.ReadObject([new(part[0], int.Parse(part[1]))], new()
                {
                    Skip = request.CustomArgs.Skip,
                    Take = request.CustomArgs.Take,
                    From = request.CustomArgs.From?.ToDateTime(),
                    To = request.CustomArgs.To?.ToDateTime(),
                });
                dict.Add(read, values);
            }
        }
        catch (Exception e)
        {
            sliding.Dispose();
            throw new StatusCodeException(500, $"Error on dlms read object: {e.Message}", e);
        }
        try
        {
            var data = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            logger.LogDebug("Reply data: {data}", data);
            var proto = data.ToProtoStruct();
            return Task.FromResult<ConnectAndReadReply>(new()
            {
                Value = proto
            });
        }
        catch (Exception e)
        {
            throw new StatusCodeException(500, $"Error on serializing/parsing data: {e.Message}", e);
        }
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
