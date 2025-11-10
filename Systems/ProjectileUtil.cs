using UnityEngine;

/// <summary>
/// Utility class for computing projectile launch velocity in Unity.
/// 
/// Formula: v₀ = (Δx - ½ * g * t²) / t
/// 
/// Example:
/// <code>
/// Vector3 velocity = ProjectileUtil.ComputeLaunchVelocity(start, target, 1.2f);
/// rigidbody.velocity = velocity;
/// </code>
/// </summary>
public static class ProjectileUtil
{
    /// <summary>
    /// Computes the initial velocity required to hit a target in a given time.
    /// </summary>
    /// <param name="origin">Starting position.</param>
    /// <param name="target">Target position.</param>
    /// <param name="time">Time to reach the target (in seconds).</param>
    public static Vector3 ComputeLaunchVelocity(Vector3 origin, Vector3 target, float time)
    {
        Vector3 displacement = target - origin;
        Vector3 gravity = Physics.gravity;
        // v0 = (Δx - ½ * g * t²) / t
        return (displacement - 0.5f * gravity * time * time) / time;
    }

    /// <summary>
    /// Computes launch velocity with a custom gravity vector (for local physics variations).
    /// </summary>
    public static Vector3 ComputeLaunchVelocity(Vector3 origin, Vector3 target, float time, Vector3 customGravity)
    {
        Vector3 displacement = target - origin;
        return (displacement - 0.5f * customGravity * time * time) / time;
    }
}
