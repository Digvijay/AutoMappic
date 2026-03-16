// This file makes 'record' types and 'init' property setters available when targeting
// netstandard2.0. Roslyn analyzers and source generators must target netstandard2.0 but
// are compiled with a modern C# LangVersion that emits uses of IsExternalInit.
// The type is defined here so the compiler can resolve it without a framework reference.

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    // Must be internal to avoid conflicts if the consuming project already defines it.
    internal static class IsExternalInit { }
}
