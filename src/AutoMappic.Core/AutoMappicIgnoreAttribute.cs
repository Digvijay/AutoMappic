using System;

namespace AutoMappic
{
    /// <summary>
    ///   Indicates that a property should be ignored by the AutoMappic source generator.
    ///   This is a local alternative to <c>ForMemberIgnore</c> in a <c>Profile</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class AutoMappicIgnoreAttribute : Attribute
    {
    }
}
