namespace AutoMappic;

/// <summary>
///   Exception thrown by AutoMappic when a runtime mapping operation cannot be completed.
/// </summary>
/// <remarks>
///   In a correctly configured project with the source generator enabled, this exception
///   should never be thrown for types registered in a <see cref="Profile" />.  If you see
///   it, it typically means either the generator did not run, or the call site was reached
///   through a code path the generator cannot intercept (e.g. via <c>dynamic</c>).
/// </remarks>
[Serializable]
public sealed class AutoMappicException : Exception
{
    /// <inheritdoc />
    public AutoMappicException(string message) : base(message) { }

    /// <inheritdoc />
    public AutoMappicException(string message, Exception innerException)
        : base(message, innerException) { }
}
