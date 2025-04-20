using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor
{
    private BezierSpline spline;
    private Transform handleTransform;
    private Quaternion handleRotation;

    private const int curveStepsPerCurve = 10; // How many line segments to use for drawing each curve
    private const float directionScale = 0.5f; // How long the direction tangent lines should be

    private void OnSceneGUI()
    {
        // Get the target spline object
        spline = target as BezierSpline;
        if (spline == null || spline.ControlPointCount < 4) // Need at least 4 points for one curve
        {
            return;
        }

        // Get the transform and rotation for handles
        handleTransform = spline.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ?
            handleTransform.rotation : Quaternion.identity;

        // Draw handles for each control point and update if moved
        Vector3 p0 = ShowPoint(0);
        for (int i = 1; i < spline.ControlPointCount; i += 3)
        {
            Vector3 p1 = ShowPoint(i);
            Vector3 p2 = ShowPoint(i + 1);
            Vector3 p3 = ShowPoint(i + 2);

            // Draw the control lines (handles) in gray
            Handles.color = Color.gray;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);

            // Draw the Bezier curve itself in white
            Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
            p0 = p3; // Continue drawing from the end of the last curve
        }

        // Optional: Draw direction indicators
        // ShowDirections();
    }

    // Helper method to draw a position handle for a point and update the spline if it's moved
    private Vector3 ShowPoint(int index)
    {
        // Convert local point to world space
        Vector3 point = handleTransform.TransformPoint(spline.GetControlPoint(index));

        // Begin check to see if the handle value changes
        EditorGUI.BeginChangeCheck();
        point = Handles.DoPositionHandle(point, handleRotation);

        // If the handle was moved...
        if (EditorGUI.EndChangeCheck())
        {
            // Record state for Undo
            Undo.RecordObject(spline, "Move Spline Point");
            // Mark object as dirty so changes are saved
            EditorUtility.SetDirty(spline);
            // Convert world space position back to local space and update the spline
            spline.SetControlPoint(index, handleTransform.InverseTransformPoint(point));
        }
        return point; // Return the world space position
    }

    // Optional: Helper method to draw direction vectors along the spline
    private void ShowDirections()
    {
        Handles.color = Color.green;
        Vector3 point = spline.GetPoint(0f);
        Handles.DrawLine(point, point + spline.GetDirection(0f) * directionScale);
        int steps = curveStepsPerCurve * spline.CurveCount;
        for (int i = 1; i <= steps; i++)
        {
            point = spline.GetPoint(i / (float)steps);
            Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directionScale);
        }
    }

    // TODO: Add buttons in OnInspectorGUI to add/remove curves?
} 