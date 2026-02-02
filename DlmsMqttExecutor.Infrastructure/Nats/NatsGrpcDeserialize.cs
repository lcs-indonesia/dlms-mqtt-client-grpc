using System.Buffers;
using Google.Protobuf;
using NATS.Client.Core;

namespace DlmsMqttExecutor.Infrastructure.Nats;

public class NatsGrpcDeserialize<T>(MessageParser<T> parser) : INatsDeserialize<T> where T : IMessage<T>
{
    public T? Deserialize(in ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment) return parser.ParseFrom(buffer.FirstSpan);
        return parser.ParseFrom(buffer.ToArray());
    }

    public INatsDeserialize<T> CombineWith(INatsDeserialize<T> next)
    {
        return this;
    }
}
