using DlmsMqttClientGrpc.Application.Services;
using Grpc.Core;
using Microsoft.Extensions.Hosting;

namespace DlmsMqttClientGrpc.Infrastructure.Grpc;

public class GrpcDlmsHandler(
    DlmsService dlmsService,
    IHostApplicationLifetime lifetime) : DlmsProto.DlmsProtoBase

{
    public override Task<ConnectAndReadReply> ConnectAndRead(ConnectAndReadRequest request, ServerCallContext context) =>
        dlmsService.ConnectAndRead(request, GenerateLinkedCts(context.CancellationToken).Token);
    public override Task<ProfileGenericReply> ConnectAndReadProfileGeneric(
        ConnectAndReadRequest request, ServerCallContext context) =>
        dlmsService.ConnectAndReadProfileGeneric(request, GenerateLinkedCts(context.CancellationToken).Token);
    public override Task<EmptyReply> ExecuteScriptTable(ExecuteScriptTableRequest request, ServerCallContext context) =>
        dlmsService.ExecuteScriptTable(request, GenerateLinkedCts(context.CancellationToken).Token);
    public override Task<EmptyReply> SetDisconnectControl(SetDisconnectControlRequest request, ServerCallContext context) =>
        dlmsService.SetDisconnectControl(request, GenerateLinkedCts(context.CancellationToken).Token);

    private CancellationTokenSource GenerateLinkedCts(CancellationToken cancellationToken) =>
        CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping, cancellationToken);
}
