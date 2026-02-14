using FluentAssertions;
using MIC.Infrastructure.Data.Persistence.Configurations;
using Xunit;

namespace MIC.Tests.Unit.Infrastructure.Data;

/// <summary>
/// Tests for <see cref="ValueComparerFactory"/> — pure unit tests for equality, hashing, and snapshot semantics.
/// Internal class accessed via InternalsVisibleTo.
/// </summary>
public class ValueComparerFactoryTests
{
    #region StringListComparer

    [Fact]
    public void StringListComparer_EqualLists_AreEqual()
    {
        var a = new List<string> { "one", "two" };
        var b = new List<string> { "one", "two" };
        ValueComparerFactory.StringListComparer.Equals(a, b).Should().BeTrue();
    }

    [Fact]
    public void StringListComparer_DifferentLists_AreNotEqual()
    {
        var a = new List<string> { "one", "two" };
        var b = new List<string> { "one", "three" };
        ValueComparerFactory.StringListComparer.Equals(a, b).Should().BeFalse();
    }

    [Fact]
    public void StringListComparer_DifferentLength_AreNotEqual()
    {
        var a = new List<string> { "one" };
        var b = new List<string> { "one", "two" };
        ValueComparerFactory.StringListComparer.Equals(a, b).Should().BeFalse();
    }

    [Fact]
    public void StringListComparer_SameReference_AreEqual()
    {
        var a = new List<string> { "x" };
        ValueComparerFactory.StringListComparer.Equals(a, a).Should().BeTrue();
    }

    [Fact]
    public void StringListComparer_NullAndList_NotEqual()
    {
        var a = new List<string> { "x" };
        ValueComparerFactory.StringListComparer.Equals(null!, a).Should().BeFalse();
    }

    [Fact]
    public void StringListComparer_EqualLists_SameHash()
    {
        var a = new List<string> { "one", "two" };
        var b = new List<string> { "one", "two" };
        var hashA = ValueComparerFactory.StringListComparer.GetHashCode(a);
        var hashB = ValueComparerFactory.StringListComparer.GetHashCode(b);
        hashA.Should().Be(hashB);
    }

    [Fact]
    public void StringListComparer_Snapshot_ReturnsNewList()
    {
        var original = new List<string> { "a", "b" };
        var snapshot = ValueComparerFactory.StringListComparer.Snapshot(original);
        snapshot.Should().BeEquivalentTo(original);
        snapshot.Should().NotBeSameAs(original);
    }

    [Fact]
    public void StringListComparer_Snapshot_NullList_ReturnsNewEmpty()
    {
        var snapshot = ValueComparerFactory.StringListComparer.Snapshot(null!);
        snapshot.Should().NotBeNull();
        snapshot.Should().BeEmpty();
    }

    #endregion

    #region NullableStringListComparer

    [Fact]
    public void NullableStringListComparer_BothNull_AreEqual()
    {
        ValueComparerFactory.NullableStringListComparer.Equals(null, null).Should().BeTrue();
    }

    [Fact]
    public void NullableStringListComparer_OneNull_NotEqual()
    {
        var a = new List<string> { "x" };
        ValueComparerFactory.NullableStringListComparer.Equals(a, null).Should().BeFalse();
        ValueComparerFactory.NullableStringListComparer.Equals(null, a).Should().BeFalse();
    }

    [Fact]
    public void NullableStringListComparer_EqualLists_AreEqual()
    {
        var a = new List<string> { "one" };
        var b = new List<string> { "one" };
        ValueComparerFactory.NullableStringListComparer.Equals(a, b).Should().BeTrue();
    }

    [Fact]
    public void NullableStringListComparer_NullHash_IsZero()
    {
        ValueComparerFactory.NullableStringListComparer.GetHashCode(null!).Should().Be(0);
    }

    [Fact]
    public void NullableStringListComparer_Snapshot_Null_ReturnsNull()
    {
        var snapshot = ValueComparerFactory.NullableStringListComparer.Snapshot(null);
        snapshot.Should().BeNull();
    }

    #endregion

    #region StringDictionaryComparer

    [Fact]
    public void StringDictionaryComparer_EqualDicts_AreEqual()
    {
        var a = new Dictionary<string, string> { ["k1"] = "v1", ["k2"] = "v2" };
        var b = new Dictionary<string, string> { ["k1"] = "v1", ["k2"] = "v2" };
        ValueComparerFactory.StringDictionaryComparer.Equals(a, b).Should().BeTrue();
    }

    [Fact]
    public void StringDictionaryComparer_DifferentValues_NotEqual()
    {
        var a = new Dictionary<string, string> { ["k1"] = "v1" };
        var b = new Dictionary<string, string> { ["k1"] = "different" };
        ValueComparerFactory.StringDictionaryComparer.Equals(a, b).Should().BeFalse();
    }

    [Fact]
    public void StringDictionaryComparer_DifferentKeys_NotEqual()
    {
        var a = new Dictionary<string, string> { ["k1"] = "v1" };
        var b = new Dictionary<string, string> { ["k2"] = "v1" };
        ValueComparerFactory.StringDictionaryComparer.Equals(a, b).Should().BeFalse();
    }

    [Fact]
    public void StringDictionaryComparer_DifferentCount_NotEqual()
    {
        var a = new Dictionary<string, string> { ["k1"] = "v1" };
        var b = new Dictionary<string, string> { ["k1"] = "v1", ["k2"] = "v2" };
        ValueComparerFactory.StringDictionaryComparer.Equals(a, b).Should().BeFalse();
    }

    [Fact]
    public void StringDictionaryComparer_SameReference_Equal()
    {
        var a = new Dictionary<string, string> { ["k"] = "v" };
        ValueComparerFactory.StringDictionaryComparer.Equals(a, a).Should().BeTrue();
    }

    [Fact]
    public void StringDictionaryComparer_NullAndDict_NotEqual()
    {
        var a = new Dictionary<string, string> { ["k"] = "v" };
        ValueComparerFactory.StringDictionaryComparer.Equals(null!, a).Should().BeFalse();
    }

    [Fact]
    public void StringDictionaryComparer_EqualDicts_SameHash()
    {
        var a = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        var b = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        ValueComparerFactory.StringDictionaryComparer.GetHashCode(a)
            .Should().Be(ValueComparerFactory.StringDictionaryComparer.GetHashCode(b));
    }

    [Fact]
    public void StringDictionaryComparer_NullDict_HashIsZero()
    {
        ValueComparerFactory.StringDictionaryComparer.GetHashCode(null!).Should().Be(0);
    }

    [Fact]
    public void StringDictionaryComparer_Snapshot_ReturnsNewDict()
    {
        var original = new Dictionary<string, string> { ["k"] = "v" };
        var snapshot = ValueComparerFactory.StringDictionaryComparer.Snapshot(original);
        snapshot.Should().BeEquivalentTo(original);
        snapshot.Should().NotBeSameAs(original);
    }

    [Fact]
    public void StringDictionaryComparer_Snapshot_Null_ReturnsEmpty()
    {
        var snapshot = ValueComparerFactory.StringDictionaryComparer.Snapshot(null!);
        snapshot.Should().NotBeNull();
        snapshot.Should().BeEmpty();
    }

    #endregion

    #region NullableStringDictionaryComparer

    [Fact]
    public void NullableStringDictionaryComparer_BothNull_Equal()
    {
        ValueComparerFactory.NullableStringDictionaryComparer.Equals(null, null).Should().BeTrue();
    }

    [Fact]
    public void NullableStringDictionaryComparer_OneNull_NotEqual()
    {
        var a = new Dictionary<string, string> { ["k"] = "v" };
        ValueComparerFactory.NullableStringDictionaryComparer.Equals(a, null).Should().BeFalse();
    }

    [Fact]
    public void NullableStringDictionaryComparer_Snapshot_Null_ReturnsNull()
    {
        var snapshot = ValueComparerFactory.NullableStringDictionaryComparer.Snapshot(null);
        snapshot.Should().BeNull();
    }

    #endregion

    #region ObjectDictionaryComparer

    [Fact]
    public void ObjectDictionaryComparer_EqualDicts_AreEqual()
    {
        var a = new Dictionary<string, object> { ["k1"] = "v1" };
        var b = new Dictionary<string, object> { ["k1"] = "v1" };
        ValueComparerFactory.ObjectDictionaryComparer.Equals(a, b).Should().BeTrue();
    }

    [Fact]
    public void ObjectDictionaryComparer_DifferentDicts_NotEqual()
    {
        var a = new Dictionary<string, object> { ["k1"] = "v1" };
        var b = new Dictionary<string, object> { ["k1"] = "v2" };
        ValueComparerFactory.ObjectDictionaryComparer.Equals(a, b).Should().BeFalse();
    }

    [Fact]
    public void ObjectDictionaryComparer_EmptyDicts_Equal()
    {
        var a = new Dictionary<string, object>();
        var b = new Dictionary<string, object>();
        ValueComparerFactory.ObjectDictionaryComparer.Equals(a, b).Should().BeTrue();
    }

    [Fact]
    public void ObjectDictionaryComparer_NullDicts_Equal()
    {
        ValueComparerFactory.ObjectDictionaryComparer.Equals(null!, null!).Should().BeTrue();
    }

    [Fact]
    public void ObjectDictionaryComparer_Snapshot_ReturnsNewDict()
    {
        var original = new Dictionary<string, object> { ["k"] = "v" };
        var snapshot = ValueComparerFactory.ObjectDictionaryComparer.Snapshot(original);
        snapshot.Should().NotBeSameAs(original);
    }

    [Fact]
    public void ObjectDictionaryComparer_NullDict_SerializesAsEmpty()
    {
        // Should produce "{}" for null dict
        var hash = ValueComparerFactory.ObjectDictionaryComparer.GetHashCode(null!);
        hash.Should().NotBe(0); // hash of "{}" is not zero
    }

    #endregion
}
