using System.ComponentModel;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Gurux.DLMS;

namespace DlmsMqttClientGrpc.Presentation.Interceptors;

public class GrpcCatchExceptionInterceptor(
    ILogger<GrpcCatchExceptionInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (GXDLMSException gxe)
        {
            throw new RpcException(new(StatusCode.Unknown, gxe.Message, gxe));
        }
        catch (InvalidEnumArgumentException iae)
        {
            throw new RpcException(new(StatusCode.InvalidArgument, iae.Message));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled gRPC error");
            throw new RpcException(new(StatusCode.Internal, "Internal server error."));
        }
    }
}
