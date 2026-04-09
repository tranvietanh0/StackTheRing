#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Dreamteck.Splines;

public class SplineSetupEditor
{
    [MenuItem("Tools/Setup Conveyor Spline")]
    public static void SetupConveyorSpline()
    {
        var splineGO = GameObject.Find("ConveyorSpline");
        if (splineGO == null)
        {
            Debug.LogError("ConveyorSpline not found!");
            return;
        }

        var spline = splineGO.GetComponent<SplineComputer>();
        if (spline == null)
        {
            Debug.LogError("SplineComputer component not found!");
            return;
        }

        // Ellipse parameters matching ConveyorBorder
        Vector3 center = new Vector3(0f, 1.05f, 3.76f);
        float radiusX = 1.54f;  // 1.67 * 0.92
        float radiusZ = 2.38f;  // 2.59 * 0.92
        int pointCount = 24;

        // Create ellipse points
        SplinePoint[] points = new SplinePoint[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            float angle = (i / (float)pointCount) * Mathf.PI * 2f;
            Vector3 pos = center + new Vector3(
                Mathf.Sin(angle) * radiusX,
                0f,
                Mathf.Cos(angle) * radiusZ
            );

            points[i] = new SplinePoint();
            points[i].position = pos;
            points[i].normal = Vector3.up;
            points[i].size = 1f;
            points[i].color = Color.white;
        }

        spline.SetPoints(points);
        spline.Close();
        spline.type = Spline.Type.CatmullRom;

        Debug.Log($"Conveyor spline setup complete with {pointCount} points");
        EditorUtility.SetDirty(spline);
    }
}
#endif
