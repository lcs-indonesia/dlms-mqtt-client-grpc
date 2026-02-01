using DlmsMqttClientGrpc.Application.Interfaces;
using DlmsMqttClientGrpc.Application.Settings;
using DlmsMqttClientGrpc.Infrastructure.Nats;
using DlmsMqttClientGrpc.Tests._Helpers;
using Microsoft.Extensions.Options;
using Moq;
using NATS.Client.Core;

namespace DlmsMqttClientGrpc.Tests.Infrastructure.Nats.NatsDlmsHandlerTest;

public class NatsDlmsHandlerTestBase : TestBase
{
    internal readonly NatsDlmsHandler service;
    internal readonly NatsConnection nc;
    internal readonly AppSettings appSettings;
    internal readonly NatsListener natsListener;
    internal readonly Mock<IDlmsClientManager> dlmsClientManagerMock;
    internal readonly Mock<ISlidingItem<IDlmsClient>> slidingClientMock;
    internal readonly Mock<IDlmsClient> dlmsClientMock;


    public NatsDlmsHandlerTestBase()
    {
        dlmsClientManagerMock = new Mock<IDlmsClientManager>();
        slidingClientMock = new Mock<ISlidingItem<IDlmsClient>>();
        dlmsClientMock = new Mock<IDlmsClient>();

        slidingClientMock.Setup(p => p.Value).Returns(dlmsClientMock.Object);
        dlmsClientManagerMock.Setup(p => p.GetConnection(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>()))
            .Returns(slidingClientMock.Object);
        fixture.AddMockSingleton(dlmsClientManagerMock);

        appSettings = fixture.GetService<IOptions<AppSettings>>().Value;
        natsListener = fixture.GetService<NatsListener>();
        service = fixture.GetService<NatsDlmsHandler>();
        nc = new NatsConnection();
    }
}
