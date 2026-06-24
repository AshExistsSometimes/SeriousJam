using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws the optimal route from Start to Boss using the LevelManager main path.
/// Supports optional corner smoothing for minimap display.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MinimapPathRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelManager levelManager;

    [Header("Height")]
    [SerializeField] private float pathHeight = 15f;

    [Header("Smoothing")]
    [SerializeField] private bool smoothCorners = true;
    [SerializeField] private int pointsPerCorner = 8;
    [SerializeField] private float cornerRadius = 8f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        BuildPath();
    }

    /// <summary>
    /// Rebuilds the minimap route line.
    /// </summary>
    public void BuildPath()
    {
        if (levelManager == null)
            return;

        List<Vector3> points = new();

        foreach (RoomNode node in levelManager.MainPath)
        {
            Vector3 pos = levelManager.GridToWorldPosition(node.GridPosition);
            pos.y += pathHeight;

            points.Add(pos);
        }

        if (smoothCorners && points.Count >= 3)
            points = GenerateSmoothedPath(points);

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }

    /// <summary>
    /// Generates rounded corners using quadratic interpolation.
    /// </summary>
    private List<Vector3> GenerateSmoothedPath(List<Vector3> original)
    {
        List<Vector3> result = new();

        result.Add(original[0]);

        for (int i = 1; i < original.Count - 1; i++)
        {
            Vector3 previous = original[i - 1];
            Vector3 current = original[i];
            Vector3 next = original[i + 1];

            Vector3 dirA = (current - previous).normalized;
            Vector3 dirB = (next - current).normalized;

            Vector3 startCorner = current - dirA * cornerRadius;
            Vector3 endCorner = current + dirB * cornerRadius;

            result.Add(startCorner);

            for (int j = 1; j < pointsPerCorner; j++)
            {
                float t = j / (float)pointsPerCorner;

                Vector3 a = Vector3.Lerp(startCorner, current, t);
                Vector3 b = Vector3.Lerp(current, endCorner, t);

                result.Add(Vector3.Lerp(a, b, t));
            }

            result.Add(endCorner);
        }

        result.Add(original[original.Count - 1]);

        return result;
    }
}