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
        if (annotation == null) return;

        Transform t = annotation.transform;
        
        // 1. Handle for Label Offset (positionOffset)
        EditorGUI.BeginChangeCheck();
        Vector3 worldLabelPos = t.TransformPoint(annotation.positionOffset);
        Vector3 newWorldLabelPos = Handles.PositionHandle(worldLabelPos, t.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(annotation, "Move Label Position");
            annotation.positionOffset = t.InverseTransformPoint(newWorldLabelPos);
            EditorUtility.SetDirty(annotation);
        }
        Handles.Label(worldLabelPos + Vector3.up * 0.1f, "Label Position", EditorStyles.whiteMiniLabel);

        if (!annotation.showLine) return;

        // 2. Handle for Line Start Offset
        EditorGUI.BeginChangeCheck();
        Vector3 worldStart = t.TransformPoint(annotation.lineStartOffset);
        float handleSize = HandleUtility.GetHandleSize(worldStart) * 0.15f;
        Vector3 newWorldStart = Handles.FreeMoveHandle(worldStart, t.rotation, handleSize, Vector3.one * 0.5f, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(annotation, "Move Line Start Offset");
            annotation.lineStartOffset = t.InverseTransformPoint(newWorldStart);
            EditorUtility.SetDirty(annotation);
        }
        Handles.Label(worldStart + Vector3.up * 0.05f, "Line Start", EditorStyles.miniLabel);

        // 3. Handles for Intermediate Points
        if (annotation.intermediatePoints != null)
        {
            for (int i = 0; i < annotation.intermediatePoints.Count; i++)
            {
                Vector3 worldPt = t.TransformPoint(annotation.intermediatePoints[i]);
                float pSize = HandleUtility.GetHandleSize(worldPt) * 0.12f;
                
                // Move Handle
                EditorGUI.BeginChangeCheck();
                Vector3 newWorldPt = Handles.FreeMoveHandle(worldPt, t.rotation, pSize, Vector3.one * 0.5f, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(annotation, "Move Intermediate Point " + i);
                    annotation.intermediatePoints[i] = t.InverseTransformPoint(newWorldPt);
                    EditorUtility.SetDirty(annotation);
                }

                // Small Delete Button Handle
                if (Handles.Button(worldPt + Vector3.up * pSize * 1.5f, t.rotation, pSize * 0.5f, pSize * 0.5f, Handles.RectangleHandleCap))
                {
                    Undo.RecordObject(annotation, "Delete Intermediate Point " + i);
                    annotation.intermediatePoints.RemoveAt(i);
                    EditorUtility.SetDirty(annotation);
                    break; 
                }
                
                Handles.Label(worldPt + Vector3.right * pSize, "Pt " + i, EditorStyles.miniLabel);
            }
        }

        // 4. Scene View UI Overlay
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 150, 100));
        GUILayout.BeginVertical("box");
        GUILayout.Label("Line Points", EditorStyles.boldLabel);
        if (GUILayout.Button("Add Point"))
        {
            Undo.RecordObject(annotation, "Add Intermediate Point");
            Vector3 lastPt = annotation.intermediatePoints.Count > 0 
                ? annotation.intermediatePoints[annotation.intermediatePoints.Count - 1] 
                : annotation.lineStartOffset;
            
            // Add point halfway to label as a guess
            annotation.intermediatePoints.Add(Vector3.Lerp(lastPt, annotation.positionOffset, 0.5f));
            EditorUtility.SetDirty(annotation);
        }
        if (GUILayout.Button("Clear Points"))
        {
            Undo.RecordObject(annotation, "Clear Intermediate Points");
            annotation.intermediatePoints.Clear();
            EditorUtility.SetDirty(annotation);
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
}