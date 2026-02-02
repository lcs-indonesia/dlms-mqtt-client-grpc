using NATS.Client.Core;

namespace DlmsMqttExecutor.Infrastructure.Nats.Delegates;

public delegate Task NatsHandlerDelegate<TRequest>(TRequest request);
