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
    private SerializedProperty onlyMoveImmediateChildren;
    private SerializedProperty autoCreateTargets;
    private SerializedProperty linkExplosionFactors;
    private SerializedProperty separateMovementAndOrchestration;
    private SerializedProperty drawDebugLines;
    private SerializedProperty debugLineColor;
    private SerializedProperty showHeatmap;
    private SerializedProperty showPathLength;
    private SerializedProperty applyDebugToChildren;
    private SerializedProperty annotationControlPoint;
    private SerializedProperty overrideLineSettings;
    private SerializedProperty masterLineColor;
    private SerializedProperty masterLineWidth;
    private SerializedProperty overrideFadeSettings;
    private SerializedProperty masterUseFadeIn;
    private SerializedProperty masterFadeInStart;
    private SerializedProperty masterFadeInEnd;
    private SerializedProperty masterUseFadeOut;
    private SerializedProperty masterFadeOutStart;
    private SerializedProperty masterFadeOutEnd;
    private SerializedProperty overrideVisualSettings;
    private SerializedProperty masterLookAtCamera;
    private SerializedProperty overrideUISettings;
    private SerializedProperty masterBackgroundColor;
    private SerializedProperty masterTextColor;
    private SerializedProperty masterFontSize;
    private SerializedProperty masterBGSize;
    private SerializedProperty curveStrength;
    // We won't use the SerializedProperty for subManagers logic specifically, 
    // because we want to traverse the tree recursively via direct references.

    // Key: InstanceID, Value: IsExpanded. Static to persist between selections.
    private static Dictionary<int, bool> foldoutStates = new Dictionary<int, bool>();

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
        subManagersList = new ReorderableList(serializedObject, subManagersProp, true, true, false, false);
        
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
        
        showAnnotations = serializedObject.FindProperty("showAnnotations");
        globalAnnotationScale = serializedObject.FindProperty("globalAnnotationScale");
        globalAnnotationOffset = serializedObject.FindProperty("globalAnnotationOffset");
        globalMotionCurve = serializedObject.FindProperty("globalMotionCurve");
        
        onlyMoveImmediateChildren = serializedObject.FindProperty("onlyMoveImmediateChildren");
        autoCreateTargets = serializedObject.FindProperty("autoCreateTargets");
        linkExplosionFactors = serializedObject.FindProperty("linkExplosionFactors");
        separateMovementAndOrchestration = serializedObject.FindProperty("separateMovementAndOrchestration");
        
        drawDebugLines = serializedObject.FindProperty("drawDebugLines");
        debugLineColor = serializedObject.FindProperty("debugLineColor");
        showHeatmap = serializedObject.FindProperty("showHeatmap");
        showPathLength = serializedObject.FindProperty("showPathLength");
        applyDebugToChildren = serializedObject.FindProperty("applyDebugToChildren");
        
        annotationControlPoint = serializedObject.FindProperty("annotationControlPoint");
        
        overrideLineSettings = serializedObject.FindProperty("overrideLineSettings");
        masterLineColor = serializedObject.FindProperty("masterLineColor");
        masterLineWidth = serializedObject.FindProperty("masterLineWidth");
        
        overrideFadeSettings = serializedObject.FindProperty("overrideFadeSettings");
        masterUseFadeIn = serializedObject.FindProperty("masterUseFadeIn");
        masterFadeInStart = serializedObject.FindProperty("masterFadeInStart");
        masterFadeInEnd = serializedObject.FindProperty("masterFadeInEnd");
        masterUseFadeOut = serializedObject.FindProperty("masterUseFadeOut");
        masterFadeOutStart = serializedObject.FindProperty("masterFadeOutStart");
        masterFadeOutEnd = serializedObject.FindProperty("masterFadeOutEnd");
        
        overrideVisualSettings = serializedObject.FindProperty("overrideVisualSettings");
        masterLookAtCamera = serializedObject.FindProperty("masterLookAtCamera");
        
        overrideUISettings = serializedObject.FindProperty("overrideUISettings");
        masterBackgroundColor = serializedObject.FindProperty("masterBackgroundColor");
        masterTextColor = serializedObject.FindProperty("masterTextColor");
        masterFontSize = serializedObject.FindProperty("masterFontSize");
        masterBGSize = serializedObject.FindProperty("masterBGSize");
        curveStrength = serializedObject.FindProperty("curveStrength");
    }

    private void OnDisable()
    {
    }

    public override void OnInspectorGUI()
    {
        if (explosionMode == null) OnEnable();
        if (explosionMode == null) return;

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

        int rootStepCount = 0;
        if (component.orchestrateSubManagers && component.linkExplosionFactors)
        {
            if (component.separateMovementAndOrchestration) 
                rootStepCount = (component.orchestrateParts ? component.parts.Count : 1) + component.subManagers.Count;
            else 
                rootStepCount = Mathf.Max(component.orchestrateParts ? component.parts.Count : 1, component.subManagers.Count);
        }
        else if (component.orchestrateParts)
        {
            rootStepCount = component.parts.Count;
        }

        DrawFactorWithArrows("Explosion Factor", explosionFactor, rootStepCount);
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
        EditorGUILayout.PropertyField(onlyMoveImmediateChildren, new GUIContent("Only Move Immediate Children", "If enabled, only immediate children will be moved, ignoring deep hierarchies. Significant performance boost for complex models."));
        EditorGUILayout.PropertyField(autoCreateTargets, new GUIContent("Auto-Create Targets"));

        if ((ExplodedView.ExplosionMode)explosionMode.enumValueIndex == ExplodedView.ExplosionMode.Curved)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(curveStrength, new GUIContent("Path Smoothing", "0 = Linear path through points, 1 = Smooth Bezier curve."));
            if (GUILayout.Button("Reset to Default Curve Layout"))
            {
                if (EditorUtility.DisplayDialog("Refresh Curves", "This will reset all existing control points for Curved mode based on the new strength. Continue?", "Yes", "Cancel"))
                {
                    foreach (var t in targets)
                    {
                        ExplodedView ev = t as ExplodedView;
                        if (ev != null)
                        {
                            Undo.RecordObject(ev, "Refresh Curves");
                            // Clear existing control points to force re-initialization
                            foreach(var p in ev.parts) p.controlPoints.Clear();
                            ev.InitializeTargetMode();
                            EditorUtility.SetDirty(ev);
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Orchestration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(orchestrateSubManagers, new GUIContent("Orchestrate Sequence"));
        if (orchestrateSubManagers.boolValue)
        {
            EditorGUILayout.PropertyField(linkExplosionFactors, new GUIContent("Link to Main Explosion"));
            
            bool isLinked = linkExplosionFactors.boolValue;
            
            // If linked, show the explosion factor is driving it.
            // If NOT linked, we enable the slider (unless controlled by parent)
            
            bool disabled = isControlledByParent || isLinked;
            EditorGUI.BeginDisabledGroup(disabled);
            int orchStepCount = component.orchestrateSubManagers ? component.subManagers.Count : 0;
            DrawFactorWithArrows("Orchestration Factor", orchestrationFactor, orchStepCount);
            EditorGUI.EndDisabledGroup();
            
            if (isLinked)
            {
                EditorGUILayout.HelpBox("Orchestration is driven by the Main Explosion Factor.", MessageType.Info);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(separateMovementAndOrchestration, new GUIContent("Sequential Mode (Parts then Subs)"));
                EditorGUI.indentLevel--;
            }
            
            if (subManagersList != null)
            {
                subManagersList.DoLayoutList();
                EditorGUILayout.HelpBox("Drag to reorder. Top explodes first (0.0), bottom last (1.0).", MessageType.Info);
                if (GUILayout.Button("Reverse Sub-Managers List"))
                {
                    Undo.RecordObject(component, "Reverse Sub-Managers List");
                    component.ReverseSubManagers();
                    EditorUtility.SetDirty(component);
                }
            }
        }
        
        EditorGUILayout.PropertyField(orchestrateParts, new GUIContent("Orchestrate Parts"));
        if (orchestrateParts.boolValue && partsList != null)
        {
            partsList.DoLayoutList();
            EditorGUILayout.HelpBox("Parts Order: Top explodes first (0.0), bottom last (1.0). Controls local sequence.", MessageType.Info);
            if (GUILayout.Button("Reverse Parts List"))
            {
                Undo.RecordObject(component, "Reverse Parts List");
                component.ReverseParts();
                EditorUtility.SetDirty(component);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Parts list not available.", MessageType.Warning);
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Visual Fidelity & Debugging", EditorStyles.boldLabel);
        
        // Debug Overlays
        EditorGUILayout.LabelField("Debug Overlays", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(drawDebugLines, new GUIContent("Draw Path Lines"));
        EditorGUILayout.PropertyField(debugLineColor, GUIContent.none, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.PropertyField(showHeatmap, new GUIContent("Distance Heatmap"));
        EditorGUILayout.PropertyField(showPathLength, new GUIContent("Path Lengths"));
        EditorGUILayout.PropertyField(applyDebugToChildren, new GUIContent("Apply Globally"));

        EditorGUILayout.Space(5);
        
        // Annotations
        EditorGUILayout.LabelField("Annotation Overrides", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(showAnnotations, new GUIContent("Show Annotations"));
        EditorGUILayout.PropertyField(globalAnnotationScale, new GUIContent("Global Scale"));
        EditorGUILayout.PropertyField(globalAnnotationOffset, new GUIContent("Global Offset"));
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(annotationControlPoint, new GUIContent("Control Point", "A Transform in the scene that drives the Global Offset."));
        if (component.annotationControlPoint == null)
        {
            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                GameObject cp = new GameObject("AnnotationControl");
                cp.transform.SetParent(component.transform, false);
                cp.transform.localPosition = component.globalAnnotationOffset;
                
                Undo.RegisterCreatedObjectUndo(cp, "Create Annotation Control Point");
                Undo.RecordObject(component, "Assign Annotation Control Point");
                component.annotationControlPoint = cp.transform;
                EditorUtility.SetDirty(component);
            }
        }
        else
        {
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeTransform = component.annotationControlPoint;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Master Overrides", EditorStyles.miniBoldLabel);
        
        // 1. Line Overrides
        EditorGUILayout.PropertyField(overrideLineSettings, new GUIContent("Override All Lines"));
        if (overrideLineSettings.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(masterLineColor, new GUIContent("Color"));
            EditorGUILayout.PropertyField(masterLineWidth, new GUIContent("Width"));
            EditorGUI.indentLevel--;
        }

        // 2. Fade Overrides
        EditorGUILayout.PropertyField(overrideFadeSettings, new GUIContent("Override All Fading"));
        if (overrideFadeSettings.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(masterUseFadeIn, new GUIContent("Use Fade In"));
            EditorGUILayout.PropertyField(masterFadeInStart, new GUIContent("Start"));
            EditorGUILayout.PropertyField(masterFadeInEnd, new GUIContent("End"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(masterUseFadeOut, new GUIContent("Use Fade Out"));
            EditorGUILayout.PropertyField(masterFadeOutStart, new GUIContent("Start"));
            EditorGUILayout.PropertyField(masterFadeOutEnd, new GUIContent("End"));
            EditorGUI.indentLevel--;
        }

        // 3. Visual Overrides
        EditorGUILayout.PropertyField(overrideVisualSettings, new GUIContent("Override Visual Behavior"));
        if (overrideVisualSettings.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(masterLookAtCamera, new GUIContent("All Look At Camera"));
            EditorGUI.indentLevel--;
        }

        // 4. UI/Canvas Overrides
        EditorGUILayout.PropertyField(overrideUISettings, new GUIContent("Override All UI Styles"));
        if (overrideUISettings.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(masterBackgroundColor, new GUIContent("BG Color"));
            EditorGUILayout.PropertyField(masterTextColor, new GUIContent("Text Color"));
            EditorGUILayout.PropertyField(masterFontSize, new GUIContent("Font Size"));
            EditorGUILayout.PropertyField(masterBGSize, new GUIContent("BG Size"));
            EditorGUI.indentLevel--;
        }

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
                ExplodedView ev = t as ExplodedView;
                if (ev != null) PerformSetupWithWarning(ev);
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
                int subExplosionStepCount = sub.orchestrateParts ? sub.parts.Count : 0;
                float newFactor = DrawFactorWithArrows("Explosion", sub.explosionFactor, subExplosionStepCount);
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
                    int subOrchStepCount = sub.orchestrateSubManagers ? sub.subManagers.Count : 0;
                    newOrchestrationFactor = DrawFactorWithArrows("Seq Factor", sub.orchestrationFactor, subOrchStepCount);
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                }

                bool newOrchestrateParts = EditorGUILayout.Toggle("Orchestrator(Parts)", sub.orchestrateParts);
                
                // Motion Quality Controls for Sub-Managers
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                AnimationCurve newCurve = EditorGUILayout.CurveField("Local Global Curve", sub.globalMotionCurve);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(sub, "Change Main Curve");
                    sub.globalMotionCurve = newCurve;
                    EditorUtility.SetDirty(sub);
                }
                EditorGUI.indentLevel--;

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
                        Transform newStart = (Transform)EditorGUILayout.ObjectField("Startpoint", partData.startTransform, typeof(Transform), true);
                        Transform newTarget = (Transform)EditorGUILayout.ObjectField("Endpoint", partData.targetTransform, typeof(Transform), true);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(manager, "Change Curve Transforms");
                            partData.startTransform = newStart;
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

            // Motion Quality Controls (Show for all modes)
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            AnimationCurve newMotionCurve = EditorGUILayout.CurveField("Motion Curve", part.motionCurve);
            float newDelay = EditorGUILayout.FloatField("Delay", part.delay);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Change Part Motion");
                part.motionCurve = newMotionCurve;
                part.delay = newDelay;
                EditorUtility.SetDirty(manager);
            }
            EditorGUI.indentLevel--;

            if (isParentInTargetMode)
            {
                EditorGUI.BeginChangeCheck();
                Transform newStart = (Transform)EditorGUILayout.ObjectField("Startpoint", part.startTransform, typeof(Transform), true);
                Transform newTarget = (Transform)EditorGUILayout.ObjectField("Endpoint", part.targetTransform, typeof(Transform), true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(manager, "Change Curve Transforms");
                    part.startTransform = newStart;
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

            Vector3 startPos = (part.startTransform != null) ? part.startTransform.position : (part.transform.parent != null 
                ? part.transform.parent.TransformPoint(part.originalLocalPosition) 
                : part.transform.position);
            
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
                endPos = startPos + (part.targetTransform.position - startPos) * manager.sensitivity;
            }
            else if (manager.explosionMode == ExplodedView.ExplosionMode.Curved && part.targetTransform != null)
            {
                // Draw Bezier Curve with multi-point support
                List<Vector3> points = new List<Vector3>();
                points.Add(startPos);
                foreach (var cp in part.controlPoints) 
                {
                    if (cp != null) 
                    {
                        Vector3 cpPos = startPos + (cp.position - startPos) * manager.sensitivity;
                        points.Add(cpPos);
                    }
                }
                endPos = startPos + (part.targetTransform.position - startPos) * manager.sensitivity;
                points.Add(endPos);

                // Draw segments of the curve for high-fidelity visualization
                int segments = 20;
                Vector3 lastP = startPos;
                for (int i = 1; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    Vector3 bezP = GetBezierPointWorld(t, points);
                    Vector3 linP = GetLinearPointWorld(t, points);
                    Vector3 nextP = Vector3.Lerp(linP, bezP, manager.curveStrength);
                    
                    Handles.DrawLine(lastP, nextP);
                    lastP = nextP;
                }

                // NEW: Draw lines connecting the scaled control points (the "hull")
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

    private void DrawFactorWithArrows(string label, SerializedProperty prop, int stepCount = 0)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prop, new GUIContent(label));

        if (stepCount > 0)
        {
            int currentIndex = Mathf.RoundToInt(prop.floatValue * stepCount);
            EditorGUILayout.LabelField($"[{currentIndex}/{stepCount}]", EditorStyles.miniLabel, GUILayout.Width(45));
        }
        
        if (GUILayout.Button("<", GUILayout.Width(20)))
        {
            prop.floatValue = CalculateNextStep(prop.floatValue, stepCount, false);
        }
        if (GUILayout.Button(">", GUILayout.Width(20)))
        {
            prop.floatValue = CalculateNextStep(prop.floatValue, stepCount, true);
        }
        EditorGUILayout.EndHorizontal();
    }

    private float DrawFactorWithArrows(string label, float value, int stepCount = 0)
    {
        EditorGUILayout.BeginHorizontal();
        float newValue = EditorGUILayout.Slider(label, value, 0f, 1f);

        if (stepCount > 0)
        {
            int currentIndex = Mathf.RoundToInt(value * stepCount);
            EditorGUILayout.LabelField($"[{currentIndex}/{stepCount}]", EditorStyles.miniLabel, GUILayout.Width(45));
        }

        if (GUILayout.Button("<", GUILayout.Width(20)))
        {
            newValue = CalculateNextStep(value, stepCount, false);
            GUI.changed = true;
        }
        if (GUILayout.Button(">", GUILayout.Width(20)))
        {
            newValue = CalculateNextStep(value, stepCount, true);
            GUI.changed = true;
        }
        EditorGUILayout.EndHorizontal();
        return newValue;
    }

    private float CalculateNextStep(float current, int stepCount, bool increment)
    {
        if (stepCount <= 0) return Mathf.Clamp01(current + (increment ? 0.1f : -0.1f));

        float step = 1f / stepCount;
        if (increment)
        {
            // Jump to the next boundary, with a tiny epsilon to handle floating point errors
            int nextIdx = Mathf.FloorToInt(current / step + 0.001f) + 1;
            return Mathf.Clamp01(nextIdx * step);
        }
        else
        {
            // Jump to the previous boundary
            int prevIdx = Mathf.CeilToInt(current / step - 0.001f) - 1;
            return Mathf.Clamp01(prevIdx * step);
        }
    }

    private void PerformSetupWithWarning(ExplodedView ev)
    {
        if (!ev.onlyMoveImmediateChildren)
        {
            int totalChildren = GetRecursiveChildCount(ev.transform);
            if (totalChildren > 100)
            {
                if (!EditorUtility.DisplayDialog("Large Hierarchy Detected",
                    $"This object has {totalChildren} sub-objects. Setting up full explosion may take some time.\n\n" +
                    "Consider enabling 'Only Move Immediate Children' for a significant performance boost.",
                    "Setup Anyway", "Cancel"))
                {
                    return;
                }
            }
        }
        ev.SetupExplosion();
    }

    private int GetRecursiveChildCount(Transform parent)
    {
        int count = parent.childCount;
        foreach (Transform child in parent)
        {
            count += GetRecursiveChildCount(child);
            // Early exit if it's already massive to avoid deep scan delay itself
            if (count > 500) return count; 
        }
        return count;
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

    private Vector3 GetLinearPointWorld(float t, List<Vector3> points)
    {
        if (points == null || points.Count == 0) return Vector3.zero;
        if (points.Count == 1) return points[0];
        if (t <= 0) return points[0];
        if (t >= 1) return points[points.Count - 1];

        int segments = points.Count - 1;
        float scaledT = t * segments;
        int index = Mathf.FloorToInt(scaledT);
        index = Mathf.Clamp(index, 0, segments - 1);
        float localT = scaledT - index;

        return Vector3.Lerp(points[index], points[index + 1], localT);
    }
}
