using System.Linq;
using System.Text.RegularExpressions;

namespace AutoMappic.Generator.Pipeline;

internal static class NamingUtility
{
    private static readonly Regex PascalSplitter = new(@"([A-Z][a-z0-9]*|[a-z0-9]+|[A-Z]+(?=[A-Z]|$))", RegexOptions.Compiled);

    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var parts = PascalSplitter.Matches(name).Cast<Match>().Select(m => m.Value.ToLowerInvariant());
        return string.Join("_", parts);
    }

    public static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var parts = PascalSplitter.Matches(name).Cast<Match>().Select(m => m.Value.ToLowerInvariant());
        return string.Join("-", parts);
    }

    public static string Normalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return name.Replace("_", "").Replace("-", "").ToLowerInvariant();
    }
}
