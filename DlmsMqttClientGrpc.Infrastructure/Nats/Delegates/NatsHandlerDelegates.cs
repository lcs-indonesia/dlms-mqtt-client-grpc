using NATS.Client.Core;

namespace DlmsMqttClientGrpc.Infrastructure.Nats.Delegates;

public delegate Task NatsHandlerDelegate<TRequest>(TRequest request);
