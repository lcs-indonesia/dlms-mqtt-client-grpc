using System.ComponentModel;
using DlmsMqttClientGrpc.Application.Exceptions;
using DlmsMqttClientGrpc.Presentation.Extensions;
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
            logger.LogError(gxe, "Error occured.");
            throw new RpcException(new(StatusCode.Unknown, gxe.Message, gxe));
        }
        catch (InvalidEnumArgumentException iae)
        {
            logger.LogError(iae, "Error occured.");
            throw new RpcException(new(StatusCode.InvalidArgument, iae.Message));
        }
        catch (StatusCodeException se)
        {
            logger.LogError(se, "Error occured.");
            throw new RpcException(new(StatusCode.Unknown, se.Message + "@" + se.StatusCode));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unhandled gRPC error");
            var msg = LogExtensions.IsDebug ? e.Message : "Internal server Error";
            throw new RpcException(new(StatusCode.Internal, msg));
        }
    }
}
