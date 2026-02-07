using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MIC.Infrastructure.Data.Persistence.Configurations;

/// <summary>
/// Centralized ValueComparer declarations for EF Core JSON conversions.
/// </summary>
internal static class ValueComparerFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static ValueComparer<List<string>> StringListComparer { get; } = new(
        (left, right) => SequenceEquals(left, right),
        list => SequenceHashCode(list),
        list => list == null ? new List<string>() : new List<string>(list));

    public static ValueComparer<List<string>?> NullableStringListComparer { get; } = new(
        (left, right) => NullableSequenceEquals(left, right),
        list => SequenceHashCode(list),
        list => list == null ? null : new List<string>(list));

    public static ValueComparer<Dictionary<string, string>> StringDictionaryComparer { get; } = new(
        (left, right) => DictionaryEquals(left, right),
        dict => DictionaryHashCode(dict),
        dict => dict == null ? new Dictionary<string, string>(StringComparer.Ordinal) : new Dictionary<string, string>(dict, StringComparer.Ordinal));

    public static ValueComparer<Dictionary<string, string>?> NullableStringDictionaryComparer { get; } = new(
        (left, right) => NullableDictionaryEquals(left, right),
        dict => DictionaryHashCode(dict),
        dict => dict == null ? null : new Dictionary<string, string>(dict, StringComparer.Ordinal));

    public static ValueComparer<Dictionary<string, object>> ObjectDictionaryComparer { get; } = new(
        (left, right) => SerializeObjectDictionary(left) == SerializeObjectDictionary(right),
        dict => StringComparer.Ordinal.GetHashCode(SerializeObjectDictionary(dict)),
        dict => JsonSerializer.Deserialize<Dictionary<string, object>>(SerializeObjectDictionary(dict), SerializerOptions) ?? new Dictionary<string, object>());

    private static bool SequenceEquals(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool NullableSequenceEquals(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return SequenceEquals(left, right);
    }

    private static int SequenceHashCode(IReadOnlyList<string>? sequence)
    {
        if (sequence is null)
        {
            return 0;
        }

        unchecked
        {
            var hash = 17;
            foreach (var item in sequence)
            {
                hash = HashCode.Combine(hash, item is null ? 0 : StringComparer.Ordinal.GetHashCode(item));
            }

            return hash;
        }
    }

    private static bool DictionaryEquals(IDictionary<string, string>? left, IDictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left.Count != right.Count)
        {
            return false;
        }

        foreach (var kvp in left)
        {
            if (!right.TryGetValue(kvp.Key, out var otherValue))
            {
                return false;
            }

            if (!string.Equals(kvp.Value, otherValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool NullableDictionaryEquals(IDictionary<string, string>? left, IDictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return DictionaryEquals(left, right);
    }

    private static int DictionaryHashCode(IDictionary<string, string>? dictionary)
    {
        if (dictionary is null)
        {
            return 0;
        }

        unchecked
        {
            var hash = 17;
            foreach (var kvp in dictionary.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                hash = HashCode.Combine(hash, StringComparer.Ordinal.GetHashCode(kvp.Key), kvp.Value is null ? 0 : StringComparer.Ordinal.GetHashCode(kvp.Value));
            }

            return hash;
        }
    }

    private static string SerializeObjectDictionary(IDictionary<string, object>? dictionary)
    {
        if (dictionary is null || dictionary.Count == 0)
        {
            return "{}";
        }

        var ordered = dictionary
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        return JsonSerializer.Serialize(ordered, SerializerOptions);
    }
}
