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
            Vector3 delta = t.InverseTransformPoint(newWorldLabelPos) - annotation.positionOffset;
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject go && go.TryGetComponent<Annotation>(out var a))
                {
                    Undo.RecordObject(a, "Move Label Position");
                    a.positionOffset += delta;
                    EditorUtility.SetDirty(a);
                }
            }
        }
        Handles.Label(worldLabelPos + Vector3.up * 0.1f, "Label Position", EditorStyles.whiteMiniLabel);

        if (!annotation.showLine) return;

        // 2. Handle for Line Start Offset
        EditorGUI.BeginChangeCheck();
        Vector3 worldStart = t.TransformPoint(annotation.lineStartOffset);
        float handleSize = HandleUtility.GetHandleSize(worldStart) * 0.15f;
        Vector3 newWorldStart = Handles.FreeMoveHandle(worldStart, handleSize, Vector3.one * 0.5f, Handles.DotHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Vector3 delta = t.InverseTransformPoint(newWorldStart) - annotation.lineStartOffset;
            foreach (var obj in Selection.objects)
            {
                if (obj is GameObject go && go.TryGetComponent<Annotation>(out var a))
                {
                    Undo.RecordObject(a, "Move Line Start Offset");
                    a.lineStartOffset += delta;
                    EditorUtility.SetDirty(a);
                }
            }
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
                Vector3 newWorldPt = Handles.FreeMoveHandle(worldPt, pSize, Vector3.one * 0.5f, Handles.SphereHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 delta = t.InverseTransformPoint(newWorldPt) - annotation.intermediatePoints[i];
                    foreach (var obj in Selection.objects)
                    {
                        if (obj is GameObject go && go.TryGetComponent<Annotation>(out var a))
                        {
                            if (a.intermediatePoints != null && i < a.intermediatePoints.Count)
                            {
                                Undo.RecordObject(a, "Move Intermediate Point " + i);
                                a.intermediatePoints[i] += delta;
                                EditorUtility.SetDirty(a);
                            }
                        }
                    }
                }

                // Small Delete Button Handle
                if (Handles.Button(worldPt + Vector3.up * pSize * 1.5f, t.rotation, pSize * 0.5f, pSize * 0.5f, Handles.RectangleHandleCap))
                {
                    foreach (var obj in Selection.objects)
                    {
                        if (obj is GameObject go && go.TryGetComponent<Annotation>(out var a))
                        {
                            if (a.intermediatePoints != null && i < a.intermediatePoints.Count)
                            {
                                Undo.RecordObject(a, "Delete Intermediate Point " + i);
                                a.intermediatePoints.RemoveAt(i);
                                EditorUtility.SetDirty(a);
                            }
                        }
                    }
                    break; 
                }
                
                Handles.Label(worldPt + Vector3.right * pSize, "Pt " + i, EditorStyles.miniLabel);
            }
        }

        // 4. Scene View UI Overlay (Show if active or if selected elsewhere while locked)
        if (Selection.activeObject == annotation.gameObject || !System.Array.Exists(Selection.objects, obj => obj == annotation.gameObject))
        {
           Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 150, 100));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Multi-Edit Points", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Point to All"))
            {
                foreach (var obj in Selection.objects)
                {
                    if (obj is GameObject go && go.TryGetComponent<Annotation>(out var a))
                    {
                        Undo.RecordObject(a, "Add Intermediate Point");
                        Vector3 lastPt = a.intermediatePoints.Count > 0 
                            ? a.intermediatePoints[a.intermediatePoints.Count - 1] 
                            : a.lineStartOffset;
                        a.intermediatePoints.Add(Vector3.Lerp(lastPt, a.positionOffset, 0.5f));
                        EditorUtility.SetDirty(a);
                    }
                }
            }
            if (GUILayout.Button("Clear All Points"))
            {
                foreach (var obj in Selection.objects)
                {
                    if (obj is GameObject go && go.TryGetComponent<Annotation>(out var a))
                    {
                        Undo.RecordObject(a, "Clear Intermediate Points");
                        a.intermediatePoints.Clear();
                        EditorUtility.SetDirty(a);
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();
        }
    }
}
