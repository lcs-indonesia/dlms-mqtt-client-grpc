using DlmsMqttExecutor.Application.Services;
using Google.Protobuf;

namespace DlmsMqttExecutor.Infrastructure.Nats;

public class NatsDlmsHandler
{
    private readonly Dictionary<string, Func<byte[], CancellationToken, Task<byte[]>>> routes;
    private readonly DlmsService dlmsService;


    public NatsDlmsHandler(DlmsService dlmsService)
    {
        this.dlmsService = dlmsService;
        routes = MapHandlers();
    }

    public Task<byte[]> InvokeHandler(string service, byte[] buffer, CancellationToken cancellationToken)
    {
        if (!routes.TryGetValue(service, out var handler)) return Task.FromResult<byte[]>([]);
        return handler(buffer, cancellationToken);
    }

    public Task<ConnectAndReadReply> ConnectAndRead(ConnectAndReadRequest request, CancellationToken cancellationToken) =>
         dlmsService.ConnectAndRead(request, cancellationToken);

    public Task<ProfileGenericReply> ConnectAndReadProfileGeneric(ConnectAndReadRequest request,
        CancellationToken cancellationToken) => dlmsService.ConnectAndReadProfileGeneric(request, cancellationToken);

    public Task<EmptyReply> ExecuteScriptTable(ExecuteScriptTableRequest request, CancellationToken cancellationToken) =>
        dlmsService.ExecuteScriptTable(request, cancellationToken);

    public Task<EmptyReply> SetDisconnectControl(SetDisconnectControlRequest request, CancellationToken cancellationToken) =>
        dlmsService.SetDisconnectControl(request, cancellationToken);

    private Dictionary<string, Func<byte[], CancellationToken, Task<byte[]>>> MapHandlers() => new()
    {
        {nameof(DlmsProto.DlmsProtoBase.ConnectAndRead),
            async(buffer,ct) =>(await ConnectAndRead(ConnectAndReadRequest.Parser.ParseFrom(buffer), ct)).ToByteArray()},
        {nameof(DlmsProto.DlmsProtoBase.ConnectAndReadProfileGeneric),
            async(buffer,ct) =>(await ConnectAndReadProfileGeneric(ConnectAndReadRequest.Parser.ParseFrom(buffer), ct)).ToByteArray()},
        {nameof(DlmsProto.DlmsProtoBase.ExecuteScriptTable),
            async(buffer,ct) =>(await ExecuteScriptTable(ExecuteScriptTableRequest.Parser.ParseFrom(buffer), ct)).ToByteArray()},
        {nameof(DlmsProto.DlmsProtoBase.SetDisconnectControl),
            async(buffer,ct) =>(await SetDisconnectControl(SetDisconnectControlRequest.Parser.ParseFrom(buffer), ct)).ToByteArray()}
    };
}
