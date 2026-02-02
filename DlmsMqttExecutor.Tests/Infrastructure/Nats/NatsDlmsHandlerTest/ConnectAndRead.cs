using System.Text.Json;
using DlmsMqttExecutor.Application.DTOs.Dlms;
using DlmsMqttExecutor.Application.Extensions;
using DlmsMqttExecutor.Domain.Parser;
using DlmsMqttExecutor.Infrastructure.Nats;
using FluentAssertions;
using Moq;

namespace DlmsMqttExecutor.Tests.Infrastructure.Nats.NatsDlmsHandlerTest;

public class ConnectAndRead : NatsDlmsHandlerTestBase
{
    [Fact]
    public async Task Should_ParseHandler_Properly()
    {
        var req = new ConnectAndReadRequest();
        req.Args.AddRange(["-q", "topic", "-g", "0.0.1.0.0.255:2"]);
        req.CustomArgs = new() { SkipGettingAssociationView = true };
        var data = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var reply = new ConnectAndReadReply()
        {
            Value = JsonSerializer.Serialize(value: data, options: new() { WriteIndented = true }).ToProtoStruct()
        };
        dlmsClientMock.Setup(p => p.ReadObject(It.IsAny<KeyValuePair<string, int>>(), It.IsAny<DlmsReadObjectFilterDto>(),
            It.IsAny<CancellationToken>())).Returns(reply);

        await natsListener.SubscribeRequestAsync<byte[], byte[]>($"test.prefix.*.1", async (subject, data, ct) =>
        {
            var dlmsService = SubjectParser.GetSegmentAt(subject, 2);
            return await service.InvokeHandler(dlmsService, data, CancellationToken.None);
        }, CancellationToken.None);

        var res = await nc.RequestAsync(
            $"test.prefix.{nameof(DlmsProto.DlmsProtoBase.ConnectAndRead)}.1", req,
            requestSerializer: new NatsGrpcSerialize<ConnectAndReadRequest>(),
            replySerializer: new NatsGrpcDeserialize<ConnectAndReadReply>(ConnectAndReadReply.Parser),
            replyOpts: new() { Timeout = TimeSpan.FromSeconds(3) });

        res.Should().NotBeNull();
        res.Data.Should().NotBeNull();
        foreach (var pair in data)
            res.Data.Value.Fields.First().Value
                .StructValue.Fields.First().Value
                .StructValue.Fields.First().Value
                .StructValue.Fields[pair.Key]
                .StructValue.Fields["StringValue"]
                .StringValue.Should().Be(pair.Value);
    }
}
