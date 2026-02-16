using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

[CustomEditor(typeof(ExplodedView))]
[CanEditMultipleObjects]
public class ExplodedViewEditor : Editor
{
    private SerializedProperty explosionMode;
    private SerializedProperty explosionFactor;
    private SerializedProperty orchestrationFactor;
    private SerializedProperty sensitivity;
    private SerializedProperty center;
    private SerializedProperty useHierarchicalCenter;
    private SerializedProperty useBoundsCenter;
    private SerializedProperty orchestrateSubManagers;
    private ReorderableList subManagersList;
    private SerializedProperty orchestrateParts;
    private ReorderableList partsList;
    private SerializedProperty autoGroupChildren;
    private SerializedProperty showAnnotations;
    private SerializedProperty globalAnnotationScale;
    private SerializedProperty globalAnnotationOffset;
    private SerializedProperty globalMotionCurve;
    // We won't use the SerializedProperty for subManagers logic specifically, 
    // because we want to traverse the tree recursively via direct references.

    // Key: InstanceID, Value: IsExpanded. Static to persist between selections.
    private static Dictionary<int, bool> foldoutStates = new Dictionary<int, bool>();
    
    // Cache for SerializedObjects of sub-managers to ensure persistent editing (e.g. for AnimationCurves)
    private Dictionary<int, SerializedObject> soCache = new Dictionary<int, SerializedObject>();

    private void OnEnable()
    {
        explosionMode = serializedObject.FindProperty("explosionMode");
        explosionFactor = serializedObject.FindProperty("explosionFactor");
        orchestrationFactor = serializedObject.FindProperty("orchestrationFactor");
        sensitivity = serializedObject.FindProperty("sensitivity");
        center = serializedObject.FindProperty("center");
        useHierarchicalCenter = serializedObject.FindProperty("useHierarchicalCenter");
        useBoundsCenter = serializedObject.FindProperty("useBoundsCenter");
        autoGroupChildren = serializedObject.FindProperty("autoGroupChildren");
        orchestrateSubManagers = serializedObject.FindProperty("orchestrateSubManagers");
        
        // Setup ReorderableList for SubManagers
        SerializedProperty subManagersProp = serializedObject.FindProperty("subManagers");
        subManagersList = new ReorderableList(serializedObject, subManagersProp, true, true, false, false); // Draggable, Header, No Add/Remove (managed by recursion)
        
        subManagersList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Explosion Sequence (Top to Bottom)");
        };
        
        subManagersList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = subManagersProp.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element, GUIContent.none);
        };

        orchestrateParts = serializedObject.FindProperty("orchestrateParts");
        SerializedProperty partsProp = serializedObject.FindProperty("parts");
        partsList = new ReorderableList(serializedObject, partsProp, true, true, false, false);
        
        partsList.drawHeaderCallback = (Rect rect) => {
            EditorGUI.LabelField(rect, "Parts Sequence (Top to Bottom)");
        };
        
        partsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
            var element = partsProp.GetArrayElementAtIndex(index);
            SerializedProperty transformProp = element.FindPropertyRelative("transform");
            if (transformProp.objectReferenceValue != null)
            {
                EditorGUI.LabelField(rect, transformProp.objectReferenceValue.name);
            }
            else
            {
                EditorGUI.LabelField(rect, "Empty Part");
            }
        };

        serializedObject.FindProperty("curveStrength"); // Ensure property exists for later if needed, but we'll use SerializedProperty for UI
        
        showAnnotations = serializedObject.FindProperty("showAnnotations");
        globalAnnotationScale = serializedObject.FindProperty("globalAnnotationScale");
        globalAnnotationOffset = serializedObject.FindProperty("globalAnnotationOffset");
        globalMotionCurve = serializedObject.FindProperty("globalMotionCurve");
    }

    private void OnDisable()
    {
        if (soCache != null)
        {
            foreach (var so in soCache.Values)
            {
                if (so != null) so.Dispose();
            }
            soCache.Clear();
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(explosionMode);
        
        ExplodedView component = (ExplodedView)target;
        bool isControlledByParent = false;
        if (component.transform.parent != null)
        {
            ExplodedView parentManager = component.transform.parent.GetComponent<ExplodedView>();
            if (parentManager != null && parentManager.orchestrateSubManagers)
            {
                isControlledByParent = true;
            }
        }

        EditorGUI.BeginDisabledGroup(isControlledByParent);
        EditorGUILayout.PropertyField(explosionFactor);
        EditorGUI.EndDisabledGroup();
        if (isControlledByParent)
        {
            EditorGUILayout.HelpBox("Controlled by Parent Orchestrator", MessageType.Info);
        }

        EditorGUILayout.PropertyField(sensitivity);
        EditorGUILayout.PropertyField(globalMotionCurve);

        if ((ExplodedView.ExplosionMode)explosionMode.enumValueIndex == ExplodedView.ExplosionMode.Spherical)
        {
            EditorGUILayout.PropertyField(center);
            EditorGUILayout.PropertyField(useHierarchicalCenter);
            EditorGUILayout.PropertyField(useBoundsCenter);
        }
        
        EditorGUILayout.PropertyField(autoGroupChildren);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoCreateTargets"), new GUIContent("Auto-Create Targets"));
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Orchestration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(orchestrateSubManagers, new GUIContent("Orchestrate Sequence"));
        if (orchestrateSubManagers.boolValue)
        {
            SerializedProperty linkFactors = serializedObject.FindProperty("linkExplosionFactors");
            EditorGUILayout.PropertyField(linkFactors, new GUIContent("Link to Main Explosion"));
            
            bool isLinked = linkFactors.boolValue;
            
            // If linked, show the explosion factor is driving it.
            // If NOT linked, we enable the slider (unless controlled by parent)
            
            EditorGUI.BeginDisabledGroup(isControlledByParent || isLinked);
            EditorGUILayout.PropertyField(orchestrationFactor, new GUIContent("Orchestration Factor"));
            EditorGUI.EndDisabledGroup();
            
            if (isLinked)
            {
                EditorGUILayout.HelpBox("Orchestration is driven by the Main Explosion Factor.", MessageType.Info);
                EditorGUI.indentLevel++;
                SerializedProperty separate = serializedObject.FindProperty("separateMovementAndOrchestration");
                EditorGUILayout.PropertyField(separate, new GUIContent("Sequential Mode (Parts then Subs)"));
                EditorGUI.indentLevel--;
            }
            
            if (subManagersList != null)
            {
                subManagersList.DoLayoutList();
                EditorGUILayout.HelpBox("Drag to reorder. Top explodes first (0.0), bottom last (1.0).", MessageType.Info);
            }
        }
        
        EditorGUILayout.PropertyField(orchestrateParts, new GUIContent("Orchestrate Parts"));
        if (orchestrateParts.boolValue && partsList != null)
        {
            partsList.DoLayoutList();
            EditorGUILayout.HelpBox("Parts Order: Top explodes first (0.0), bottom last (1.0). Controls local sequence.", MessageType.Info);
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Visual Fidelity & Debugging", EditorStyles.boldLabel);
        
        // Debug Overlays
        EditorGUILayout.LabelField("Debug Overlays", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("drawDebugLines"), new GUIContent("Draw Path Lines"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugLineColor"), GUIContent.none, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showHeatmap"), new GUIContent("Distance Heatmap"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("showPathLength"), new GUIContent("Path Lengths"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("applyDebugToChildren"), new GUIContent("Apply Globally"));

        EditorGUILayout.Space(5);
        
        // Annotations
        EditorGUILayout.LabelField("Annotation Overrides", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(showAnnotations, new GUIContent("Show Annotations"));
        EditorGUILayout.PropertyField(globalAnnotationScale, new GUIContent("Global Scale"));
        EditorGUILayout.PropertyField(globalAnnotationOffset, new GUIContent("Global Offset"));
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        ExplodedView root = (ExplodedView)target;

        if ((root.subManagers != null && root.subManagers.Count > 0) || (root.parts != null && root.parts.Count > 0))
        {
            EditorGUILayout.LabelField("Structure & Targets", EditorStyles.boldLabel);
            DrawSubManagersRecursive(root);
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Reset & Setup Explosion"))
        {
            foreach (var t in targets)
            {
                (t as ExplodedView).SetupExplosion();
            }
        }

        EditorGUILayout.Space();
        
        // --- Danger Zone ---
        GUI.backgroundColor = new Color(1f, 0.8f, 0.8f);
        bool showDanger = foldoutStates.ContainsKey(-999) ? foldoutStates[-999] : false;
        showDanger = EditorGUILayout.BeginFoldoutHeaderGroup(showDanger, "Danger Zone");
        foldoutStates[-999] = showDanger;
        
        if (showDanger)
        {
            EditorGUILayout.HelpBox("These actions will affect the entire hierarchy recursively.", MessageType.Warning);
            
            if (GUILayout.Button("Cleanup & Reset (Deletes Targets/Scripts)"))
            {
                if (EditorUtility.DisplayDialog("Exploded View Cleanup", "This will reset all part positions and delete all generated target objects/sub-managers. Continue?", "Yes", "Cancel"))
                {
                    foreach (var t in targets) (t as ExplodedView).Cleanup(false);
                }
            }

            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Deep Remove (Component + Children + Targets)"))
            {
                if (EditorUtility.DisplayDialog("Exploded View Deep Remove", "This will permanently remove the ExplodedView system and all generated objects from this hierarchy. Continue?", "Delete Everything", "Cancel"))
                {
                    foreach (var t in targets) (t as ExplodedView).Cleanup(true);
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        GUI.backgroundColor = Color.white;

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSubManagersRecursive(ExplodedView manager)
    {
        if (manager == null) return;
        
        
        bool isParentInTargetMode = manager.explosionMode == ExplodedView.ExplosionMode.Target || manager.explosionMode == ExplodedView.ExplosionMode.Curved;

        // 1. Draw Sub-Managers (Groups)
        if (manager.subManagers != null)
        {
            foreach (var sub in manager.subManagers)
            {
                if (sub == null) continue;

                int id = sub.GetInstanceID();
                bool isExpanded = foldoutStates.ContainsKey(id) ? foldoutStates[id] : false;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Header Row
                EditorGUILayout.BeginHorizontal();
                
                // Foldout for children
                bool hasSubChildManagers = sub.subManagers != null && sub.subManagers.Count > 0;
                bool hasLeafParts = false;
                // Check for leaf parts in sub to see if we should show foldout
                foreach(var p in sub.parts) {
                    if (p.transform != null && p.transform.GetComponent<ExplodedView>() == null) {
                        hasLeafParts = true;
                        break;
                    }
                }

                if (hasSubChildManagers || hasLeafParts)
                {
                    bool newExpanded = EditorGUILayout.Foldout(isExpanded, sub.gameObject.name, true);
                    if (newExpanded != isExpanded)
                    {
                        foldoutStates[id] = newExpanded;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(sub.gameObject.name, EditorStyles.boldLabel);
                }

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = sub.gameObject;
                }
                EditorGUILayout.EndHorizontal();

                // Controls for THIS sub-manager
                EditorGUI.BeginChangeCheck();
                ExplodedView.ExplosionMode subMode = (ExplodedView.ExplosionMode)EditorGUILayout.EnumPopup("Mode", sub.explosionMode);
                bool newUseBoundsCenter = sub.useBoundsCenter;
                if (subMode == ExplodedView.ExplosionMode.Spherical)
                {
                    newUseBoundsCenter = EditorGUILayout.Toggle("Use Bounds Center", sub.useBoundsCenter);
                }
                
                EditorGUI.BeginDisabledGroup(manager.orchestrateSubManagers);
                float newFactor = EditorGUILayout.Slider("Explosion", sub.explosionFactor, 0f, 1f);
                EditorGUI.EndDisabledGroup();
                
                float newSensitivity = EditorGUILayout.FloatField("Sensitivity", sub.sensitivity);

                // NEW: Orchestration Controls for Sub-Managers
                bool newOrchestrate = EditorGUILayout.Toggle("Orchestrator(Subs)", sub.orchestrateSubManagers);
                float newOrchestrationFactor = sub.orchestrationFactor;
                bool newSeparate = sub.separateMovementAndOrchestration;

                if (newOrchestrate)
                {
                    EditorGUI.indentLevel++;
                    newSeparate = EditorGUILayout.Toggle("Sequential Mode", sub.separateMovementAndOrchestration);
                    EditorGUI.BeginDisabledGroup(manager.orchestrateSubManagers);
                    newOrchestrationFactor = EditorGUILayout.Slider("Seq Factor", sub.orchestrationFactor, 0f, 1f);
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                }

                bool newOrchestrateParts = EditorGUILayout.Toggle("Orchestrator(Parts)", sub.orchestrateParts);
                
                // Use cached SO for sub-manager's local global curve
                int subID = sub.GetInstanceID();
                if (!soCache.ContainsKey(subID)) soCache[subID] = new SerializedObject(sub);
                SerializedObject cachedSubSO = soCache[subID];
                cachedSubSO.Update();
                EditorGUILayout.PropertyField(cachedSubSO.FindProperty("globalMotionCurve"), new GUIContent("Local Global Curve"));
                cachedSubSO.ApplyModifiedProperties();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sub, "Change Sub-Manager Settings");
                    sub.explosionMode = subMode;
                    sub.useBoundsCenter = newUseBoundsCenter;
                    sub.explosionFactor = newFactor;
                    sub.sensitivity = newSensitivity;
                    sub.orchestrateSubManagers = newOrchestrate;
                    sub.orchestrationFactor = newOrchestrationFactor;
                    sub.orchestrateParts = newOrchestrateParts;
                    sub.separateMovementAndOrchestration = newSeparate;
                    EditorUtility.SetDirty(sub);
                    
                    // Force repaint to ensure UI updates immediately
                    // EditorUtility.SetDirty might not force inspector repaint if selection is different
                    // But usually it does for nested editors. We can try to force it via SceneView or similar if needed.
                }

                // Sub-Manager Debug Settings
                EditorGUI.BeginChangeCheck();
                
                // Determine if we should disable local controls because the parent/root is overriding them
                bool isRootOverriding = ((ExplodedView)target).applyDebugToChildren && ((ExplodedView)target) != sub;
                
                EditorGUI.BeginDisabledGroup(isRootOverriding);
                bool subDrawLines = EditorGUILayout.Toggle("Draw Lines", sub.drawDebugLines);
                bool subShowHeatmap = EditorGUILayout.Toggle("Heatmap", sub.showHeatmap);
                bool subShowPathLength = EditorGUILayout.Toggle("Path Length", sub.showPathLength);
                EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sub, "Toggle Sub-Manager Debug Settings");
                    sub.drawDebugLines = subDrawLines;
                    sub.showHeatmap = subShowHeatmap;
                    sub.showPathLength = subShowPathLength;
                    EditorUtility.SetDirty(sub);
                }
                
                if (isRootOverriding)
                {
                    EditorGUILayout.HelpBox("Overridden by Global Settings", MessageType.None);
                }

                // NEW: Show Target Reference (Endpoint) from the parent's perspective
                if (isParentInTargetMode)
                {
                    var partData = manager.GetPartData(sub.transform);
                    if (partData != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        Transform newTarget = (Transform)EditorGUILayout.ObjectField("Endpoint", partData.targetTransform, typeof(Transform), true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(manager, "Change Target Reference");
                            partData.targetTransform = newTarget;
                            EditorUtility.SetDirty(manager);
                        }

                        // NEW: Control Points for Sub-Managers (Groups)
                        if (manager.explosionMode == ExplodedView.ExplosionMode.Curved)
                        {
                            EditorGUI.indentLevel++;
                            bool cpExpanded = foldoutStates.ContainsKey(id + 1000) ? foldoutStates[id + 1000] : false;
                            cpExpanded = EditorGUILayout.Foldout(cpExpanded, "Control Points (" + partData.controlPoints.Count + ")");
                            foldoutStates[id + 1000] = cpExpanded;
                            
                            if (cpExpanded)
                            {
                                for (int i = 0; i < partData.controlPoints.Count; i++)
                                {
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUI.BeginChangeCheck();
                                    partData.controlPoints[i] = (Transform)EditorGUILayout.ObjectField("Point " + i, partData.controlPoints[i], typeof(Transform), true);
                                    if (EditorGUI.EndChangeCheck())
                                    {
                                        Undo.RecordObject(manager, "Change Control Point");
                                        EditorUtility.SetDirty(manager);
                                    }
                                    if (GUILayout.Button("X", GUILayout.Width(20)))
                                    {
                                        Undo.RecordObject(manager, "Remove Control Point");
                                        partData.controlPoints.RemoveAt(i);
                                        EditorUtility.SetDirty(manager);
                                        break;
                                    }
                                    EditorGUILayout.EndHorizontal();
                                }
                                if (GUILayout.Button("Add Control Point", EditorStyles.miniButton))
                                {
                                    Undo.RecordObject(manager, "Add Control Point");
                                    partData.controlPoints.Add(null);
                                    EditorUtility.SetDirty(manager);
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }

                if (sub.explosionMode == ExplodedView.ExplosionMode.Target || sub.explosionMode == ExplodedView.ExplosionMode.Curved)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Check if we need to create targets (if any part is missing one)
                    bool needsTargets = false;
                    foreach(var p in sub.parts) if(p.targetTransform == null) { needsTargets = true; break; }

                    if (needsTargets)
                    {
                        if (GUILayout.Button("Create Targets", EditorStyles.miniButton))
                        {
                            Undo.RecordObject(sub, "Create Targets");
                            sub.InitializeTargetMode();
                            EditorUtility.SetDirty(sub);
                        }
                    }

                    if (GUILayout.Button("Select All Targets", EditorStyles.miniButton))
                    {
                        Transform container = sub.transform.Find("ExplosionTargets");
                        if (container != null)
                        {
                            List<GameObject> targetObjects = new List<GameObject>();
                            foreach (Transform t in container) targetObjects.Add(t.gameObject);
                            Selection.objects = targetObjects.ToArray();
                        }
                    }

                    GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                    if (GUILayout.Button("Delete Targets", EditorStyles.miniButton))
                    {
                        if (EditorUtility.DisplayDialog("Delete Targets", $"Delete all target objects for {sub.name}?", "Yes", "Cancel"))
                        {
                            Undo.RecordObject(sub, "Delete Targets");
                            sub.ClearTargets();
                            EditorUtility.SetDirty(sub);
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                }

                // Recursive Draw Children if expanded
                if (foldoutStates.ContainsKey(id) && foldoutStates[id])
                {
                    EditorGUI.indentLevel++;
                    DrawSubManagersRecursive(sub);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        // 2. Draw Leaf Parts (Renderers without Managers)
        foreach (var part in manager.parts)
        {
            if (part == null || part.transform == null) continue;
            if (part.transform.GetComponent<ExplodedView>() != null) continue; // Skip managers, they are handled above

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(part.transform.name, EditorStyles.miniLabel);
            if (GUILayout.Button("Select", GUILayout.Width(50))) Selection.activeGameObject = part.transform.gameObject;
            EditorGUILayout.EndHorizontal();

            // Use persistent SerializedObject for parts to fix "can't select curve" issue
            int managerID = manager.GetInstanceID();
            if (!soCache.ContainsKey(managerID)) soCache[managerID] = new SerializedObject(manager);
            SerializedObject partSO = soCache[managerID];
            partSO.Update();

            // Motion Quality Controls (Show for all modes)
            EditorGUI.indentLevel++;
            var partsProp = partSO.FindProperty("parts");
            int partIndex = manager.parts.IndexOf(part);
            if (partIndex >= 0)
            {
                var elementProp = partsProp.GetArrayElementAtIndex(partIndex);
                EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("motionCurve"));
                EditorGUILayout.PropertyField(elementProp.FindPropertyRelative("delay"));
                partSO.ApplyModifiedProperties();
            }
            EditorGUI.indentLevel--;

            if (isParentInTargetMode)
            {
                EditorGUI.BeginChangeCheck();
                Transform newTarget = (Transform)EditorGUILayout.ObjectField("Endpoint", part.targetTransform, typeof(Transform), true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(manager, "Change Target Reference");
                    part.targetTransform = newTarget;
                    EditorUtility.SetDirty(manager);
                }

                // Control Points List for Leaf Parts
                if (manager.explosionMode == ExplodedView.ExplosionMode.Curved)
                {
                    EditorGUI.indentLevel++;
                    bool cpExpanded = foldoutStates.ContainsKey(part.transform.GetInstanceID() + 5000) ? foldoutStates[part.transform.GetInstanceID() + 5000] : false;
                    cpExpanded = EditorGUILayout.Foldout(cpExpanded, "Control Points (" + part.controlPoints.Count + ")");
                    foldoutStates[part.transform.GetInstanceID() + 5000] = cpExpanded;
                    
                    if (cpExpanded)
                    {
                        for (int i = 0; i < part.controlPoints.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUI.BeginChangeCheck();
                            part.controlPoints[i] = (Transform)EditorGUILayout.ObjectField("Point " + i, part.controlPoints[i], typeof(Transform), true);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(manager, "Change Control Point");
                                EditorUtility.SetDirty(manager);
                            }
                            if (GUILayout.Button("X", GUILayout.Width(20)))
                            {
                                Undo.RecordObject(manager, "Remove Control Point");
                                part.controlPoints.RemoveAt(i);
                                EditorUtility.SetDirty(manager);
                                break;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        if (GUILayout.Button("Add Control Point", EditorStyles.miniButton))
                        {
                            Undo.RecordObject(manager, "Add Control Point");
                            part.controlPoints.Add(null);
                            EditorUtility.SetDirty(manager);
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void OnSceneGUI()
    {
        ExplodedView manager = (ExplodedView)target;
        if (manager == null) return;

        DrawDebugLinesRecursive(manager, manager);
        DrawDebugOverlays(manager, manager);
    }

    private void DrawDebugOverlays(ExplodedView manager, ExplodedView root)
    {
        if (manager == null) return;
        
        bool showHeatmap = root.applyDebugToChildren ? root.showHeatmap : manager.showHeatmap;
        bool showPathLength = root.applyDebugToChildren ? root.showPathLength : manager.showPathLength;

        // 1. Heatmap & Path Length
        if (showHeatmap || showPathLength)
        {
            float maxDist = manager.sensitivity * 2f; // Reasonable cap for normalization
            foreach (var part in manager.parts)
            {
                if (part == null || part.transform == null) continue;

                float dist = Vector3.Distance(part.originalLocalPosition, part.transform.localPosition);
                
                if (showHeatmap)
                {
                    float t = Mathf.Clamp01(dist / maxDist);
                    Color heatmapColor = Color.Lerp(Color.blue, Color.red, t);
                    heatmapColor.a = 0.5f;
                    
                    Handles.color = heatmapColor;
                    Renderer r = part.transform.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Handles.DrawWireCube(r.bounds.center, r.bounds.size);
                    }
                }

                if (showPathLength)
                {
                    Handles.Label(part.transform.position, $"Dist: {dist:F2}");
                }
            }
        }

        // Recurse to sub-managers
        if (manager.subManagers != null)
        {
            foreach (var sub in manager.subManagers)
            {
                if (sub != null) DrawDebugOverlays(sub, root);
            }
        }
    }

    private void DrawDebugLinesRecursive(ExplodedView manager, ExplodedView root)
    {
        if (manager == null) return;

        bool drawLines = root.applyDebugToChildren ? root.drawDebugLines : manager.drawDebugLines;

        if (drawLines)
        {
            Handles.color = root.applyDebugToChildren ? root.debugLineColor : manager.debugLineColor;
            foreach (var part in manager.parts)
            {
                if (part == null || part.transform == null) continue;

            Vector3 startPos = part.transform.parent != null 
                ? part.transform.parent.TransformPoint(part.originalLocalPosition) 
                : part.originalLocalPosition;
            
            Vector3 endPos = startPos;

            if (manager.explosionMode == ExplodedView.ExplosionMode.Spherical)
            {
                Vector3 worldDir = part.transform.parent != null 
                    ? part.transform.parent.TransformVector(part.direction) 
                    : part.direction;
                endPos = startPos + worldDir * manager.sensitivity;
            }
            else if (manager.explosionMode == ExplodedView.ExplosionMode.Target && part.targetTransform != null)
            {
                endPos = part.targetTransform.position;
                Handles.DrawDottedLine(startPos, endPos, 4f);
            }
            else if (manager.explosionMode == ExplodedView.ExplosionMode.Curved && part.targetTransform != null)
            {
                endPos = part.targetTransform.position;
                
                // Draw Bezier Curve with multi-point support
                List<Vector3> points = new List<Vector3>();
                points.Add(startPos);
                foreach (var cp in part.controlPoints) if (cp != null) points.Add(cp.position);
                points.Add(endPos);

                // Draw segments of the curve for high-fidelity visualization
                int segments = 20;
                Vector3 lastP = startPos;
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 nextP = GetBezierPointWorld(t, points);
                    Handles.DrawLine(lastP, nextP);
                    lastP = nextP;
                }

                // NEW: Draw lines connecting the control points (the "hull")
                Handles.color = new Color(manager.debugLineColor.r, manager.debugLineColor.g, manager.debugLineColor.b, 0.3f);
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Handles.DrawDottedLine(points[i], points[i + 1], 2f);
                    Handles.SphereHandleCap(0, points[i+1], Quaternion.identity, 0.03f * HandleUtility.GetHandleSize(points[i+1]), EventType.Repaint);
                }
                Handles.color = manager.debugLineColor;
            }

            if (manager.explosionMode != ExplodedView.ExplosionMode.Curved)
            {
                Handles.DrawDottedLine(startPos, endPos, 4f);
            }
            Handles.SphereHandleCap(0, startPos, Quaternion.identity, 0.05f * HandleUtility.GetHandleSize(startPos), EventType.Repaint);
            Handles.ArrowHandleCap(0, endPos, manager.explosionMode == ExplodedView.ExplosionMode.Spherical 
                ? Quaternion.LookRotation(endPos - startPos) 
                : Quaternion.identity, 0.2f * HandleUtility.GetHandleSize(endPos), EventType.Repaint);
            }
        }

        if (manager.subManagers != null)
        {
            foreach (var sub in manager.subManagers)
            {
                if (sub != null) DrawDebugLinesRecursive(sub, root);
            }
        }
    }

    private Vector3 GetBezierPointWorld(float t, List<Vector3> points)
    {
        if (points == null || points.Count < 2) return Vector3.zero;
        int n = points.Count;
        Vector3[] temp = new Vector3[n];
        for (int i = 0; i < n; i++) temp[i] = points[i];
        for (int j = 1; j < n; j++)
            for (int i = 0; i < n - j; i++)
                temp[i] = Vector3.Lerp(temp[i], temp[i + 1], t);
        return temp[0];
    }
}
