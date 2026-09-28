using AutoMappic.Generator.Pipeline;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
///   Comprehensive tests for the Levenshtein-based fuzzy matching engine
///   used by the AM0015 Smart-Match analyzer.
/// </summary>
public sealed class FuzzyMatcherTests
{
    // ─── Exact & Identity ────────────────────────────────────────────────

    /// <summary> Identical strings must score 1.0 </summary>
    [Fact]
    public void GetSimilarity_ExactMatch_ReturnsOne()
    {
        double score = MappingFuzzer.GetSimilarity("FullName", "FullName");
        Assert.Equal(1.0, score);
    }

    /// <summary> Single character identical strings must score 1.0 </summary>
    [Fact]
    public void GetSimilarity_SingleCharExactMatch_ReturnsOne()
    {
        double score = MappingFuzzer.GetSimilarity("X", "X");
        Assert.Equal(1.0, score);
    }

    // ─── Zero / Near-Zero ────────────────────────────────────────────────

    /// <summary> Completely unrelated strings should score 0 </summary>
    [Fact]
    public void GetSimilarity_CompletelyDifferent_ReturnsZero()
    {
        double score = MappingFuzzer.GetSimilarity("Apple", "XYZ");
        Assert.Equal(0.0, score);
    }

    /// <summary> Empty source should return 0 </summary>
    [Fact]
    public void GetSimilarity_EmptySource_ReturnsZero()
    {
        double score = MappingFuzzer.GetSimilarity("", "Name");
        Assert.Equal(0.0, score);
    }

    /// <summary> Empty target should return 0 </summary>
    [Fact]
    public void GetSimilarity_EmptyTarget_ReturnsZero()
    {
        double score = MappingFuzzer.GetSimilarity("Name", "");
        Assert.Equal(0.0, score);
    }

    /// <summary> Both empty should return 0 (not divide-by-zero) </summary>
    [Fact]
    public void GetSimilarity_BothEmpty_ReturnsZero()
    {
        double score = MappingFuzzer.GetSimilarity("", "");
        Assert.Equal(0.0, score);
    }

    /// <summary> Null source treated as empty </summary>
    [Fact]
    public void GetSimilarity_NullSource_ReturnsZero()
    {
        double score = MappingFuzzer.GetSimilarity(null!, "Name");
        Assert.Equal(0.0, score);
    }

    /// <summary> Null target treated as empty </summary>
    [Fact]
    public void GetSimilarity_NullTarget_ReturnsZero()
    {
        double score = MappingFuzzer.GetSimilarity("Name", null!);
        Assert.Equal(0.0, score);
    }

    // ─── One Character Edits ─────────────────────────────────────────────

    /// <summary> Trailing 's' is a single insertion → high similarity </summary>
    [Fact]
    public void GetSimilarity_OneCharacterAppended_ReturnsHighSimilarity()
    {
        double score = MappingFuzzer.GetSimilarity("Name", "Names");
        Assert.Equal(0.8, score); // 1 - 1/5 = 0.8
    }

    /// <summary> Single character substitution </summary>
    [Fact]
    public void GetSimilarity_OneCharacterSubstitution_ReturnsHighSimilarity()
    {
        // "Naame" vs "Name" → distance 1, max 5
        double score = MappingFuzzer.GetSimilarity("Naame", "Name");
        Assert.Equal(0.8, score); // 1 - 1/5
    }

    /// <summary> Single character deletion </summary>
    [Fact]
    public void GetSimilarity_OneCharacterDeleted_ReturnsHighSimilarity()
    {
        // "Nme" vs "Name" → distance 1, max 4
        double score = MappingFuzzer.GetSimilarity("Nme", "Name");
        Assert.Equal(0.75, score); // 1 - 1/4
    }

    // ─── Prefix / Suffix Scenarios (Common Mapping Typos) ────────────────

    /// <summary> "FullName" vs "Name" — common flattening pattern </summary>
    [Fact]
    public void GetSimilarity_PrefixAdded_ReturnsExpectedScore()
    {
        double score = MappingFuzzer.GetSimilarity("FullName", "Name");
        Assert.Equal(0.5, score); // Levenshtein 4, Max 8
    }

    /// <summary> "UserName" vs "Name" — another common prefix </summary>
    [Fact]
    public void GetSimilarity_UserPrefix_ReturnsExpectedScore()
    {
        double score = MappingFuzzer.GetSimilarity("UserName", "Name");
        Assert.Equal(0.5, score); // Levenshtein 4, Max 8
    }

    /// <summary> "Email" vs "EmailAddress" — suffix added </summary>
    [Fact]
    public void GetSimilarity_SuffixAdded_ReturnsExpectedScore()
    {
        double score = MappingFuzzer.GetSimilarity("Email", "EmailAddress");
        // Levenshtein = 7, Max = 12 → 1 - 7/12 ≈ 0.4167
        Assert.True(score is > 0.4 and < 0.5);
    }

    // ─── Case Sensitivity ────────────────────────────────────────────────

    /// <summary> Levenshtein is case-sensitive — lowercase vs PascalCase should differ </summary>
    [Fact]
    public void GetSimilarity_CaseDifferent_IsNotOne()
    {
        double score = MappingFuzzer.GetSimilarity("name", "Name");
        // Distance = 1, Max = 4 → 0.75
        Assert.Equal(0.75, score);
    }

    /// <summary> Fully different casing on longer string </summary>
    [Fact]
    public void GetSimilarity_AllLowerVsAllUpper_ScoresLow()
    {
        double score = MappingFuzzer.GetSimilarity("fullname", "FULLNAME");
        // Every char differs → distance 8, max 8 → 0.0
        Assert.Equal(0.0, score);
    }

    // ─── Symmetry ────────────────────────────────────────────────────────

    /// <summary> Similarity must be symmetric: sim(a,b) == sim(b,a) </summary>
    [Fact]
    public void GetSimilarity_IsSymmetric()
    {
        double ab = MappingFuzzer.GetSimilarity("FirstName", "Name");
        double ba = MappingFuzzer.GetSimilarity("Name", "FirstName");
        Assert.Equal(ab, ba);
    }

    // ─── Realistic Property Name Scenarios ───────────────────────────────

    /// <summary> "CreatedAt" vs "CreatedDate" — common temporal naming drift </summary>
    [Fact]
    public void GetSimilarity_TemporalNamingDrift_ScoresReasonably()
    {
        double score = MappingFuzzer.GetSimilarity("CreatedAt", "CreatedDate");
        // Distance: "At" → "Date" requires edits, but the base "Created" is shared
        Assert.True(score > 0.5, $"Expected > 0.5 but got {score}");
    }

    /// <summary> "Qty" vs "Quantity" — abbreviation should still score </summary>
    [Fact]
    public void GetSimilarity_Abbreviation_ScoresModerately()
    {
        double score = MappingFuzzer.GetSimilarity("Qty", "Quantity");
        // Distance = 5, Max = 8 → 0.375
        Assert.True(score > 0.3, $"Expected > 0.3 but got {score}");
    }

    /// <summary> "Id" vs "Identifier" — very short vs long </summary>
    [Fact]
    public void GetSimilarity_VeryShortVsLong_ScoresLow()
    {
        double score = MappingFuzzer.GetSimilarity("Id", "Identifier");
        Assert.True(score < 0.3, $"Expected < 0.3 for 'Id' vs 'Identifier' but got {score}");
    }

    // ─── Threshold Boundary Tests ────────────────────────────────────────

    /// <summary> Verify a pair right at 0.5 is detected with default threshold </summary>
    [Fact]
    public void GetSimilarity_AtFiftyPercent_ExactBoundary()
    {
        // "FullName" vs "Name" = 0.5 exactly
        double score = MappingFuzzer.GetSimilarity("FullName", "Name");
        Assert.True(score >= 0.5);
    }

    /// <summary> Verify a pair just below 0.5 </summary>
    [Fact]
    public void GetSimilarity_BelowFiftyPercent_JustMisses()
    {
        // "Email" vs "EmailAddress" ≈ 0.42
        double score = MappingFuzzer.GetSimilarity("Email", "EmailAddress");
        Assert.True(score < 0.5);
    }
}
