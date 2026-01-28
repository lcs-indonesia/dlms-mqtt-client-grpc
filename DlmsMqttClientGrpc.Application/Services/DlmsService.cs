using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
    private static readonly JsonSerializerOptions writeJsonIntended = new() { WriteIndented = true };

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
                logger.LogDebug("Reading object {read}...", read);
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
            var data = JsonSerializer.Serialize(dict, writeJsonIntended);
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

        var indexOfG = args.IndexOf("-g");
        if (indexOfG == -1) throw new StatusCodeException(400, "Logical name should not to be empty");
        var reads = args[indexOfG + 1].Split(";");
        if (reads.Length > 1) throw new StatusCodeException(400, "Only accept one logical name request");
        var read = reads.First();
        var part = read.Split(":");
        if (part.Length != 2) throw new StatusCodeException(400, $"Invalid logical name: {read}");

        try
        {
            var index = int.Parse(part[1]);
            if (index != 2) throw new StatusCodeException(400, "Index should be 2");
            logger.LogDebug("Reading object {read}...", read);
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
    //disconnect connect
    public override Task<EmptyReply> ExecuteScriptTable(ExecuteScriptTableRequest request, ServerCallContext context)
    {
        var args = request.Args.ToList();
        var sliding = GetClient(args, isClearCache: false);
        var client = sliding.Value;
        using var _ = sliding.BeginRead();

        try
        {
            logger.LogDebug("Executing script {scriptId}...", request.ScriptId);
            client.ExecuteScript(request.ScriptLn, request.ScriptId,
                new() { SkipGettingAssociationView = request.SkipGettingAssociationView ?? false });
        }
        catch (Exception e)
        {
            sliding.Dispose();
            throw new StatusCodeException(500, $"Error on execute script table: {e.Message}", e);
        }

        return Task.FromResult(new EmptyReply());
    }
    public override Task<EmptyReply> SetDisconnectControl(SetDisconnectControlRequest request, ServerCallContext context)
    {
        var args = request.Args.ToList();
        var sliding = GetClient(args, isClearCache: false);
        var client = sliding.Value;
        using var _ = sliding.BeginRead();

        try
        {
            logger.LogDebug("Setting disconnect control {value}...", request.Value);
            client.SetDisconnectControl(request.Value,
                new() { SkipGettingAssociationView = request.SkipGettingAssociationView ?? false }, request.ScriptLn);
        }
        catch (Exception e)
        {
            sliding.Dispose();
            throw new StatusCodeException(500, $"Error on set disconnect control: {e.Message}", e);
        }

        return Task.FromResult(new EmptyReply());
    }

    private ISlidingItem<IDlmsClient> GetClient(List<string> args, bool? isClearCache)
    {
        if (!TryGetTopic(args, out var topic) || topic == null)
            throw new ArgumentNullException(topic, "Topic is null in args[].");
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
    private static bool TryGetTopic(List<string> args, out string? topic)
    {
        topic = null;
        var index = args.IndexOf("-q");
        if (index == -1) return false;
        if (args.Count <= index + 1) return false;

        topic = args[index + 1];
        return true;
    }
}
