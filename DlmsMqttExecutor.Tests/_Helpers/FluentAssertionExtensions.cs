using FluentAssertions;
using FluentAssertions.Equivalency;

namespace DlmsMqttExecutor.Tests._Helpers;

public static class FluentAssertionExtensions
{
    public static EquivalencyOptions<T> DateTimeCloseTo<T>(this EquivalencyOptions<T> o, TimeSpan precision) =>
        o.Using<DateTime>(p => p.Subject.Should().BeCloseTo(p.Expectation, precision))
        .WhenTypeIs<DateTime>();
}
