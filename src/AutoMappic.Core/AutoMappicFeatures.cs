namespace AutoMappic;

/// <summary>
///   Trimming feature switches for AutoMappic.
/// </summary>
/// <remarks>
///   <para>
///     AutoMappic's purpose is that mappings are generated at compile time. The reflective
///     fallback exists only for mappings the generator could not see - an open generic closed at
///     runtime, or a call site the generator could not intercept. In a trimmed or Native AOT
///     application that code is usually dead, but it was still rooted: <see cref="Mapper" />'s
///     constructor eagerly built a fallback delegate for every registered mapping, so the whole
///     reflective engine was reachable from any application that created a mapper.
///   </para>
///   <para>
///     That had two costs. The obvious one is size - an AOT binary carried a reflection engine it
///     never called. The subtler one is that whole-program trim analysis correctly reported the
///     reflective code as unsafe, and there was nothing a consumer could do about it, because the
///     path was rooted by the library rather than by their own code.
///   </para>
///   <para>
///     <see cref="IsReflectionFallbackEnabled" /> is substituted to <see langword="false" /> by
///     <c>ILLink.Substitutions.xml</c> whenever the application is trimmed or published with
///     Native AOT. The trimmer constant-folds the property, removes the branch that builds the
///     fallback, and with it the entire reflective engine - so the code is gone rather than
///     merely unreachable, and it produces no trim warnings because there is nothing left to
///     analyse. This is the same mechanism <c>System.Text.Json</c> uses for
///     <c>IsReflectionEnabledByDefault</c>.
///   </para>
///   <para>
///     Outside trimming the property is <see langword="true" /> and behaviour is unchanged. It
///     can also be set explicitly through the
///     <c>AutoMappic.IsReflectionFallbackEnabled</c> switch, for example in a project file:
///     <code>
///     &lt;RuntimeHostConfigurationOption Include="AutoMappic.IsReflectionFallbackEnabled" Value="false" /&gt;
///     </code>
///   </para>
/// </remarks>
internal static class AutoMappicFeatures
{
    /// <summary>
    ///   Gets a value indicating whether the reflective mapping fallback is available.
    /// </summary>
    /// <remarks>
    ///   Kept as a trivial expression-bodied property over a single <see cref="AppContext" />
    ///   switch so that the trimmer can replace the whole body with <c>return false</c>. An
    ///   initialised static property would introduce a class constructor that the trimmer has to
    ///   keep; anything more complex would not be substitutable at all.
    /// </remarks>
    internal static bool IsReflectionFallbackEnabled =>
        !AppContext.TryGetSwitch("AutoMappic.IsReflectionFallbackEnabled", out bool enabled) || enabled;
}
