namespace DlmsMqttClientGrpc.Infrastructure.Nats.Delegates;

public delegate Task<TReply> NatsRequestDelegate<TRequest, TReply>(
    string subject, TRequest request, CancellationToken cancellationToken);
