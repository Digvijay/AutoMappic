using System;

namespace AutoMappic
{
    /// <summary>
    ///   Allows explicit mapping of a destination property from a specific source member name.
    ///   This is applied automatically by the AM0015 Smart-Match Code-Fix.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class MapPropertyAttribute(string sourceMember) : Attribute
    {
        /// <summary>
        /// Gets the name of the source property to map from.
        /// </summary>
        public string SourceMember { get; } = sourceMember;
    }
}
