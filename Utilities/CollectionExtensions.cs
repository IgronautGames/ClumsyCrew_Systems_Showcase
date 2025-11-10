using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handy extension methods for working with generic collections in Unity.
/// Includes random selection, null-safety checks, and utility shortcuts.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Returns a random element from a list, or default if the list is empty.
    /// </summary>
    public static T GetRandom<T>(this IList<T> list)
        => list.Count == 0 ? default : list[Random.Range(0, list.Count)];

    /// <summary>
    /// Returns an element at a given variation index (if >= 0), 
    /// otherwise returns a random element.
    /// </summary>
    public static T GetRandomOrSpecific<T>(this IList<T> list, int variation)
    {
        if (list.Count == 0)
            return default;

        return variation > -1 ? list[variation] : list[Random.Range(0, list.Count)];
    }

    /// <summary>
    /// Checks whether a collection is null or contains no elements.
    /// </summary>
    public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        => collection == null || collection.Count == 0;

    /// <summary>
    /// Checks whether a collection is not null and has at least one element.
    /// </summary>
    public static bool IsNotNullAndHasElements<T>(this ICollection<T> collection)
        => collection != null && collection.Count > 0;
}
