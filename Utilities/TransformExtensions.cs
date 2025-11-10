using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension methods for common <see cref="Transform"/> operations.
/// Designed to simplify local resets, traversal, and bulk manipulation in Unity.
/// </summary>
public static class TransformExtensions
{
    #region Reset Methods
    /// <summary>
    /// Resets local position, rotation, and scale to their defaults.
    /// </summary>
    public static void ResetLocal(this Transform t)
    {
        t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        t.localScale = Vector3.one;
    }

    /// <summary>
    /// Resets only local scale and rotation.
    /// </summary>
    public static void ResetLocalScaleAndRotation(this Transform t)
    {
        t.localScale = Vector3.one;
        t.localRotation = Quaternion.identity;
    }
    #endregion


    #region Movement
    /// <summary>
    /// Moves the transform upward in world space by a specified distance.
    /// </summary>
    public static void MoveUp(this Transform t, float distance)
        => t.position += Vector3.up * distance;
    #endregion


    #region Position Setters
    /// <summary>
    /// Sets only the X component of the local position.
    /// </summary>
    public static void SetLocalX(this Transform t, float x)
        => t.localPosition = new Vector3(x, t.localPosition.y, t.localPosition.z);

    /// <summary>
    /// Sets only the Y component of the local position.
    /// </summary>
    public static void SetLocalY(this Transform t, float y)
        => t.localPosition = new Vector3(t.localPosition.x, y, t.localPosition.z);

    /// <summary>
    /// Sets only the Z component of the local position.
    /// </summary>
    public static void SetLocalZ(this Transform t, float z)
        => t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, z);
    #endregion


    #region Hierarchy Search
    /// <summary>
    /// Recursively searches all descendants of the transform for a child by name.
    /// </summary>
    public static Transform FindDeep(this Transform t, string name)
    {
        foreach (Transform child in t)
        {
            if (child.name == name)
                return child;

            Transform result = child.FindDeep(name);
            if (result != null)
                return result;
        }
        return null;
    }

    /// <summary>
    /// Destroys all immediate child GameObjects under this transform.
    /// </summary>
    public static void DestroyAllChildren(this Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.Destroy(t.GetChild(i).gameObject);
    }

    /// <summary>
    /// Returns all children of the transform. Optionally includes all descendants recursively.
    /// </summary>
    public static List<Transform> GetAllChildren(this Transform t, bool includeDescendants)
    {
        List<Transform> children = new();
        foreach (Transform child in t)
        {
            children.Add(child);
            if (includeDescendants)
                children.AddRange(child.GetAllChildren(true));
        }
        return children;
    }
    #endregion
}
