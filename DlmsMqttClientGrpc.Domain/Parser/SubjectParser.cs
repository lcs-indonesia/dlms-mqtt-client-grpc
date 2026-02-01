using System.Text.RegularExpressions;

namespace DlmsMqttClientGrpc.Domain.Parser;

public static partial class SubjectParser
{
    /// <summary>
    /// Gets the segment at the specified index in the subject.
    /// </summary>
    public static string GetSegmentAt(this ReadOnlySpan<char> subject, int serviceIndex)
    {
        for (int i = 0; i < serviceIndex; i++)
        {
            var index = subject.IndexOf('.');
            if (index < 0) return string.Empty;
            index += 1;
            subject = subject[index..];
        }
        var end = subject.IndexOf('.');
        return end == -1 ? subject.ToString() : subject[..end].ToString();
    }

    /// <summary>
    /// Gets the index of the service in the subject.
    /// </summary>
    public static int GetServiceIndex(this string subject)
    {
        var match = ServiceRegex().Match(subject);
        return match.Groups[1].Value.Split('.').Length;
    }
    [GeneratedRegex(@"^(.*?)(<service>)(.*?)$")]
    public static partial Regex ServiceRegex();
    [GeneratedRegex(@"^(.*?)\[(\d+)\.\.(\d+)\](.*?)$")]
    public static partial Regex IndexRangeRegex();
}
