using UnityEngine;

/// <summary>
/// Provides static methods for calculating points and derivatives on cubic Bezier curves.
/// </summary>
public static class Bezier
{
    /// <summary>
    /// Calculates a point on a cubic Bezier curve defined by four control points.
    /// </summary>
    /// <param name="p0">The first control point (start point).</param>
    /// <param name="p1">The second control point.</param>
    /// <param name="p2">The third control point.</param>
    /// <param name="p3">The fourth control point (end point).</param>
    /// <param name="t">The parameter along the curve, clamped between 0 and 1.</param>
    /// <returns>The Vector3 position on the curve at parameter t.</returns>
    public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * oneMinusT * p0 +
            3f * oneMinusT * oneMinusT * t * p1 +
            3f * oneMinusT * t * t * p2 +
            t * t * t * p3;
    }

    /// <summary>
    /// Calculates the first derivative (velocity vector) of a cubic Bezier curve.
    /// The magnitude of the vector represents the speed, and the direction indicates the tangent.
    /// </summary>
    /// <param name="p0">The first control point (start point).</param>
    /// <param name="p1">The second control point.</param>
    /// <param name="p2">The third control point.</param>
    /// <param name="p3">The fourth control point (end point).</param>
    /// <param name="t">The parameter along the curve, clamped between 0 and 1.</param>
    /// <returns>The Vector3 representing the velocity vector on the curve at parameter t.</returns>
    public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            3f * oneMinusT * oneMinusT * (p1 - p0) +
            6f * oneMinusT * t * (p2 - p1) +
            3f * t * t * (p3 - p2);
    }
} 