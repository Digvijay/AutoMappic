using System;

namespace AutoMappic
{
    /// <summary>
    ///   Explicitly declares a mapping configuration on the destination class itself,
    ///   making it a "standalone" mapping discovered by the source generator without a Profile.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AutoMapAttribute : Attribute
    {
        /// <summary>The source type to map from.</summary>
        public Type SourceType { get; }

        /// <summary>Naming strategy for source member names (defaults to <see cref="PascalCaseNamingConvention" />).</summary>
        public Type? SourceNamingConvention { get; set; }

        /// <summary>Naming strategy for destination member names (defaults to <see cref="PascalCaseNamingConvention" />).</summary>
        public Type? DestinationNamingConvention { get; set; }

        /// <summary>When true, the generator also creates the reverse mapping.</summary>
        public bool ReverseMap { get; set; }

        /// <summary>When true, enables identity management for this mapping.</summary>
        public bool EnableIdentityManagement { get; set; }

        /// <summary>When true, enables "Smart-Sync" with orphan deletion for collections.</summary>
        public bool DeleteOrphans { get; set; }

        /// <summary>When true, suppresses all AM0001 (unmapped) and AM0015 (smart-match) errors for this mapping.</summary>
        public bool IgnoreUnmapped { get; set; }

        /// <inheritdoc/>
        public AutoMapAttribute(Type sourceType)
        {
            SourceType = sourceType;
        }
    }
}
