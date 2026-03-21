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
    private static readonly Regex Splitter = new(@"([A-Z][a-z0-9]*|[a-z0-9]+|[A-Z]+(?=[A-Z]|$))", RegexOptions.Compiled, System.TimeSpan.FromMilliseconds(200));

    /// <inheritdoc />
    public string[] Split(string name) =>
        Splitter.Matches(name).Cast<Match>().Select(m => m.Value).ToArray();
}

/// <summary>A naming convention for camelCase names (e.g. "customerName").</summary>
public sealed class CamelCaseNamingConvention : INamingConvention
{
    private static readonly Regex Splitter = new(@"([A-Z][a-z0-9]*|[a-z0-9]+|[A-Z]+(?=[A-Z]|$))", RegexOptions.Compiled, System.TimeSpan.FromMilliseconds(200));

    /// <inheritdoc />
    public string[] Split(string name) =>
        Splitter.Matches(name).Cast<Match>().Select(m => m.Value).ToArray();
}

/// <summary>A naming convention for snake_case names (e.g. "customer_name").</summary>
public sealed class LowerUnderscoreNamingConvention : INamingConvention
{
    private static readonly char[] Separator = { '_' };

    /// <inheritdoc />
    public string[] Split(string name) => name.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
}

/// <summary>A naming convention for kebab-case names (e.g. "customer-name").</summary>
public sealed class KebabCaseNamingConvention : INamingConvention
{
    private static readonly char[] Separator = { '-' };

    /// <inheritdoc />
    public string[] Split(string name) => name.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
}
