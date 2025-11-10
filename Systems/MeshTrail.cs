using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally generates a dynamic mesh trail behind a moving object.
/// 
/// Ideal for cartoony motion streaks, melee swings, or fast-moving items.
/// 
/// - Automatically fades vertices over lifetime
/// - Fades alpha along the trail
/// - Disables itself automatically when inactive
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] Material trailMaterial;
    [SerializeField, Tooltip("Seconds before trail points are removed.")] float trailLife = 0.3f;
    [SerializeField, Tooltip("Width of the trail in world units.")] float width = 0.3f;
    [SerializeField, Tooltip("Maximum number of trail segments.")] int maxPoints = 16;

    [Header("Auto Disable")]
    [SerializeField, Tooltip("Seconds after last update before trail disables itself.")]
    float autoDisableDelay = 0.4f;

    Mesh trailMesh;
    MeshRenderer meshRenderer;
    readonly List<Vector3> points = new();
    readonly List<float> times = new();
    float disableTimer;

    void Awake()
    {
        trailMesh = new Mesh { name = "MeshTrail" };
        GetComponent<MeshFilter>().mesh = trailMesh;

        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = trailMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.allowOcclusionWhenDynamic = false;
        meshRenderer.enabled = false;
    }

    /// <summary>Enable trail rendering and reset its auto-disable timer.</summary>
    public void EnableTrail()
    {
        if (!meshRenderer.enabled)
            meshRenderer.enabled = true;

        disableTimer = autoDisableDelay;
    }

    void LateUpdate()
    {
        if (!meshRenderer.enabled)
            return;

        // Record new point
        points.Add(transform.position);
        times.Add(Time.time);

        // Remove expired points
        while (times.Count > 0 && Time.time - times[0] > trailLife)
        {
            times.RemoveAt(0);
            points.RemoveAt(0);
        }

        // Enforce point limit
        while (points.Count > maxPoints)
        {
            times.RemoveAt(0);
            points.RemoveAt(0);
        }

        RebuildMesh();

        // Disable when inactive for a while
        disableTimer -= Time.deltaTime;
        if (disableTimer <= 0f && points.Count == 0)
            meshRenderer.enabled = false;
    }

    void RebuildMesh()
    {
        if (points.Count < 2)
        {
            trailMesh.Clear();
            return;
        }

        int vCount = points.Count * 2;
        Vector3[] verts = new Vector3[vCount];
        Vector2[] uvs = new Vector2[vCount];
        Color[] cols = new Color[vCount];
        int[] tris = new int[(points.Count - 1) * 6];

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 forward = Vector3.zero;
            if (i > 0) forward += points[i] - points[i - 1];
            if (i < points.Count - 1) forward += points[i + 1] - points[i];
            forward.Normalize();

            Vector3 side = Vector3.Cross(forward, Vector3.up).normalized * width * 0.5f;
            float t = Mathf.InverseLerp(0, trailLife, Time.time - times[i]);

            verts[i * 2] = transform.InverseTransformPoint(points[i] + side);
            verts[i * 2 + 1] = transform.InverseTransformPoint(points[i] - side);

            uvs[i * 2] = new Vector2(t, 0);
            uvs[i * 2 + 1] = new Vector2(t, 1);

            Color c = new(1, 1, 1, 1 - t);
            cols[i * 2] = c;
            cols[i * 2 + 1] = c;
        }

        int ti = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            int vi = i * 2;
            tris[ti++] = vi;
            tris[ti++] = vi + 1;
            tris[ti++] = vi + 2;

            tris[ti++] = vi + 1;
            tris[ti++] = vi + 3;
            tris[ti++] = vi + 2;
        }

        trailMesh.Clear();
        trailMesh.vertices = verts;
        trailMesh.uv = uvs;
        trailMesh.colors = cols;
        trailMesh.triangles = tris;
        trailMesh.RecalculateBounds();
    }
}
