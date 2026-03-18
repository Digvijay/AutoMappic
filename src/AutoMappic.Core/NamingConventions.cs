using System.Text.RegularExpressions;

namespace AutoMappic;

/// <summary>
///   Defines how a member name should be split into constituent words for matching.
/// </summary>
public interface INamingConvention
{
    /// <summary>Splits a member name into its semantic parts.</summary>
    string[] Split(string name);
}

/// <summary>A naming convention for PascalCase names (e.g. "CustomerName").</summary>
public sealed class PascalCaseNamingConvention : INamingConvention
{
    private static readonly Regex Splitter = new(@"([A-Z][a-z0-9]*)", RegexOptions.Compiled);

    /// <inheritdoc />
    public string[] Split(string name)
    {
        var matches = Splitter.Matches(name);
        var parts = new string[matches.Count];
        for (int i = 0; i < matches.Count; i++) parts[i] = matches[i].Value;
        return parts;
    }
}

/// <summary>A naming convention for snake_case names (e.g. "customer_name").</summary>
public sealed class LowerUnderscoreNamingConvention : INamingConvention
{
    private static readonly char[] Separator = { '_' };

    /// <inheritdoc />
    public string[] Split(string name) => name.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
}
