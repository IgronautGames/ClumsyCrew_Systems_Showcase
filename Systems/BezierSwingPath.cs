using UnityEngine;

/// <summary>
/// Simple quadratic Bézier curve visualizer and evaluator.
/// Useful for animating arcs, swings, or projectile paths.
/// 
/// - Evaluates positions along a 3-point Bézier curve.
/// - Optionally draws the curve in the editor for debugging.
/// </summary>
[ExecuteAlways]
public class BezierSwingPath : MonoBehaviour
{
    [Header("Control Points")]
    public Transform point0; // Start
    public Transform point1; // Control
    public Transform point2; // End

    [Header("Preview Settings")]
    [Range(0f, 1f)] public float previewT = 0f;
    public bool drawGizmos = true;

    /// <summary>
    /// Returns a world-space position along the quadratic Bézier curve.
    /// </summary>
    public Vector3 GetPointOnCurve(float t)
    {
        // Quadratic Bézier formula: B(t) = (1−t)²P0 + 2(1−t)tP1 + t²P2
        return Mathf.Pow(1 - t, 2) * point0.position +
               2 * (1 - t) * t * point1.position +
               Mathf.Pow(t, 2) * point2.position;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || point0 == null || point1 == null || point2 == null)
            return;

        // Draw main curve
        Gizmos.color = Color.yellow;
        Vector3 prev = point0.position;

        for (int i = 1; i <= 30; i++)
        {
            float t = i / 30f;
            Vector3 point = GetPointOnCurve(t);
            Gizmos.DrawLine(prev, point);
            prev = point;
        }

        // Draw preview marker at current T
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(GetPointOnCurve(previewT), 0.025f);
    }
}
