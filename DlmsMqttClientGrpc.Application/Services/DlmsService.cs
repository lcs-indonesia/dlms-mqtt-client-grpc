using System.Text.Json;
using DlmsMqttClientGrpc.Application.DTOs.Dlms;
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
        var args = request.Args.ToList();
        var sliding = GetClient(args, request.CustomArgs.IsClearCache);
        var client = sliding.Value;
        using var _ = sliding.BeginRead();

        var reads = args[args.IndexOf("-g") + 1].Split(";");
        var dict = new Dictionary<string, object>();
        try
        {
            foreach (var read in reads)
            {
                if (dict.ContainsKey(read)) continue;
                logger.LogDebug("Reading object {0}...", read);
                var part = read.Split(":");
                var value = client.ReadObject(new KeyValuePair<string, int>(part[0], int.Parse(part[1])), new()
                {
                    Skip = request.CustomArgs.Skip,
                    Take = request.CustomArgs.Take,
                    From = request.CustomArgs.From?.ToDateTime(),
                    To = request.CustomArgs.To?.ToDateTime(),
                    SkipGettingAssociationView = request.CustomArgs.SkipGettingAssociationView ?? false,
                });
                dict.Add(read, value);
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

    public override Task<ProfileGenericReply> ConnectAndReadProfileGeneric(
            ConnectAndReadRequest request, ServerCallContext context)
    {
        var args = request.Args.ToList();
        var sliding = GetClient(args, request.CustomArgs.IsClearCache);
        var client = sliding.Value;
        using var _ = sliding.BeginRead();

        var reads = args[args.IndexOf("-g") + 1].Split(";");
        if (reads.Length == 0) throw new StatusCodeException(400, "Logical name should not to be empty");
        else if (reads.Length > 1) throw new StatusCodeException(400, "Only accept one logical name request");
        var read = reads.First();

        try
        {
            var part = read.Split(":");
            var index = int.Parse(part[1]);
            if (index != 2) throw new StatusCodeException(400, "Index should be 2");
            logger.LogDebug("Reading object {0}...", read);
            var value = client.ReadProfileGenericValue(new KeyValuePair<string, int>(part[0], index), new()
            {
                Skip = request.CustomArgs.Skip,
                Take = request.CustomArgs.Take,
                From = request.CustomArgs.From?.ToDateTime(),
                To = request.CustomArgs.To?.ToDateTime(),
                SkipGettingAssociationView = request.CustomArgs.SkipGettingAssociationView ?? false,
            });

            return Task.FromResult(value.ToProfileGenericReply());
        }
        catch (Exception e)
        {
            sliding.Dispose();
            throw new StatusCodeException(500, $"Error on dlms read object: {e.Message}", e);
        }
    }

    private ISlidingItem<IDlmsClient> GetClient(List<string> args, bool? isClearCache)
    {
        if (!TryGetTopic(args, out var topic) || topic == null)
            throw new ArgumentNullException("Topic is null in args[].");
        ISlidingItem<IDlmsClient> sliding;
        try
        {
            sliding = dlmsClientManager.GetConnection(topic, args, isClearCache ?? false);
        }
        catch (Exception e)
        {
            throw new StatusCodeException(500, $"Error on get dlms connection: {e.Message}", e);
        }
        return sliding;
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
