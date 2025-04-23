using UnityEngine;
using System;

/// <summary>
/// Represents a sequence of connected cubic Bezier curves.
/// Provides methods to access control points and calculate points, velocities,
/// and directions along the spline in world space.
/// Requires an array of control points where the number of points is 1 + 3n.
/// </summary>
public class BezierSpline : MonoBehaviour
{
    // Array to hold control points. The number of points must be 1 + 3n (where n is number of curves)
    // Example: 4 points = 1 curve, 7 points = 2 curves, 10 points = 3 curves, etc.
    [SerializeField]
    private Vector3[] points;

    /// <summary>
    /// Gets the total number of control points defining the spline.
    /// </summary>
    public int ControlPointCount => points.Length;

    /// <summary>
    /// Gets the number of individual cubic Bezier curves that make up the spline.
    /// </summary>
    public int CurveCount => (points.Length - 1) / 3;

    /// <summary>
    /// Gets the control point at the specified index.
    /// </summary>
    /// <param name="index">The index of the control point.</param>
    /// <returns>The local position of the control point.</returns>
    public Vector3 GetControlPoint(int index)
    {
        return points[index];
    }

    /// <summary>
    /// Sets the control point at the specified index to a new position.
    /// </summary>
    /// <param name="index">The index of the control point to set.</param>
    /// <param name="point">The new local position for the control point.</param>
    public void SetControlPoint(int index, Vector3 point)
    {
        points[index] = point;
    }

    /// <summary>
    /// Gets the world space position on the spline corresponding to the parameter t.
    /// </summary>
    /// <param name="t">The parameter along the spline, clamped between 0 (start) and 1 (end).</param>
    /// <returns>The world space position on the spline.</returns>
    public Vector3 GetPoint(float t)
    {
        int i;
        if (t >= 1f)
        {
            t = 1f;
            i = points.Length - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }
        // Transform the local Bezier point to world space
        return transform.TransformPoint(Bezier.GetPoint(
            points[i], points[i + 1], points[i + 2], points[i + 3], t));
    }

    /// <summary>
    /// Gets the world space velocity vector (tangent) on the spline at parameter t.
    /// The magnitude represents the speed if t changes linearly.
    /// </summary>
    /// <param name="t">The parameter along the spline, clamped between 0 and 1.</param>
    /// <returns>The world space velocity vector.</returns>
    public Vector3 GetVelocity(float t)
    {
        int i;
        if (t >= 1f)
        {
            t = 1f;
            i = points.Length - 4;
        }
        else
        {
            t = Mathf.Clamp01(t) * CurveCount;
            i = (int)t;
            t -= i;
            i *= 3;
        }
        // Calculate local velocity and transform direction to world space
        // Note: Transforming a direction requires handling scale differently than a point
        return transform.TransformPoint(Bezier.GetFirstDerivative(
            points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
    }

    /// <summary>
    /// Gets the normalized world space direction vector (tangent) on the spline at parameter t.
    /// </summary>
    /// <param name="t">The parameter along the spline, clamped between 0 and 1.</param>
    /// <returns>The normalized world space direction vector.</returns>
    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t).normalized;
    }

    /// <summary>
    /// Resets the spline to a default single cubic Bezier curve configuration.
    /// Called automatically when the component is added or reset in the Inspector.
    /// </summary>
    public void Reset()
    {
        points = new Vector3[] {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 1f, 0f), // Control point 1
            new Vector3(2f, -1f, 0f),// Control point 2
            new Vector3(3f, 0f, 0f)  // End point
        };
    }

    // TODO: Add methods for adding/removing curves, and enforcing constraints between control points (for smoothness)
} 