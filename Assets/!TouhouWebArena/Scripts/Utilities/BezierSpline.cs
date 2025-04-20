using UnityEngine;
using System;

public class BezierSpline : MonoBehaviour
{
    // Array to hold control points. The number of points must be 1 + 3n (where n is number of curves)
    // Example: 4 points = 1 curve, 7 points = 2 curves, 10 points = 3 curves, etc.
    [SerializeField]
    private Vector3[] points;

    // Property to get the number of control points
    public int ControlPointCount => points.Length;

    // Property to get the number of curves in the spline
    public int CurveCount => (points.Length - 1) / 3;

    // Gets a control point by index
    public Vector3 GetControlPoint(int index)
    {
        return points[index];
    }

    // Sets a control point by index
    public void SetControlPoint(int index, Vector3 point)
    {
        points[index] = point;
    }

    // Gets a point along the spline at parameter t (0 to 1 covers the whole spline)
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

    // Gets the velocity vector along the spline at parameter t
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

    // Gets the normalized direction vector along the spline at parameter t
    public Vector3 GetDirection(float t)
    {
        return GetVelocity(t).normalized;
    }

    // Initializes the spline with a single curve when reset or added
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