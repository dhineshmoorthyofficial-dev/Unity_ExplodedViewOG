using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Annotation))]
[CanEditMultipleObjects]
public class AnnotationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        if (GUILayout.Button("Manual Refresh Links"))
        {
            foreach (var targetObject in targets)
            {
                ((Annotation)targetObject).RefreshLinks();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Annotation annotation = (Annotation)target;
        if (annotation == null || !annotation.showLine) return;

        Transform t = annotation.transform;
        
        // 1. Handle for Line Start Offset
        EditorGUI.BeginChangeCheck();
        Vector3 worldStart = t.TransformPoint(annotation.lineStartOffset);
        Vector3 newWorldStart = Handles.PositionHandle(worldStart, t.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(annotation, "Move Line Start Offset");
            annotation.lineStartOffset = t.InverseTransformPoint(newWorldStart);
            EditorUtility.SetDirty(annotation);
        }
        Handles.Label(worldStart, "Line Start");

        // 2. Handles for Intermediate Points
        if (annotation.intermediatePoints != null)
        {
            for (int i = 0; i < annotation.intermediatePoints.Count; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 worldPt = t.TransformPoint(annotation.intermediatePoints[i]);
                Vector3 newWorldPt = Handles.PositionHandle(worldPt, t.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(annotation, "Move Intermediate Point " + i);
                    annotation.intermediatePoints[i] = t.InverseTransformPoint(newWorldPt);
                    EditorUtility.SetDirty(annotation);
                }
                Handles.Label(worldPt, "Pt " + i);
            }
        }
    }
}