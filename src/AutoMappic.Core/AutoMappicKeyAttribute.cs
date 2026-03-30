using System;

namespace AutoMappic
{
    /// <summary>
    ///   Explicitly marks a property or field as the Primary Key for collection syncing
    ///   if the generator's convention inference (e.g., Id or [Key]) fails or is overridden.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class AutoMappicKeyAttribute : Attribute
    {
    }
}
