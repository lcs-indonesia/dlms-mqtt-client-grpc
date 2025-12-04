using FluentAssertions;

namespace Infrastructure.Dlms.DlmsClientTest;

public class ReadProfileGenericValue : DlmsCLientTestBase
{

    [Fact]
    public async Task ShouldReadProperly()
    {
        var pair = new KeyValuePair<string, int>("1.0.99.1.0.255", 2);

        using var _ = sliding.BeginRead();
        var result = sliding.Value.ReadProfileGenericValue(pair, new());

        result.Times.Should().NotBeEmpty();
    }

}
