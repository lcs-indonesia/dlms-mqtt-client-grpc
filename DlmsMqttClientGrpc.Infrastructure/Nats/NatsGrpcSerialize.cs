using System.Buffers;
using Google.Protobuf;
using NATS.Client.Core;

namespace DlmsMqttClientGrpc.Infrastructure.Nats;

public class NatsGrpcSerialize<T> : INatsSerialize<T> where T : IMessage<T>
{
    public void Serialize(IBufferWriter<byte> bufferWriter, T value)
    {
        var size = value.CalculateSize();
        var span = bufferWriter.GetSpan(size)[..size];

        value.WriteTo(span);
        bufferWriter.Advance(size);
    }
}
