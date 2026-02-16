using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class ExplodedView : MonoBehaviour
{
    public enum ExplosionMode { Spherical, Target, Curved }
    public ExplosionMode explosionMode = ExplosionMode.Spherical;

    [Range(0f, 1f)]
    public float explosionFactor = 0f;
    [Range(0f, 1f)]
    public float orchestrationFactor = 0f;
    public float sensitivity = 1f;
    public bool autoCreateTargets = false;
    public Transform center;
    public bool useHierarchicalCenter = false;
    public bool useBoundsCenter = false;
    public bool autoGroupChildren = false;

    [System.Serializable]
    public class PartData
    {
        public Transform transform;
        public Vector3 originalLocalPosition;
        public Quaternion originalLocalRotation;
        public Vector3 originalLocalScale;
        public Vector3 direction; // Used for Spherical
        public Transform targetTransform; // Used for Target/Curved Mode (Endpoint)
        public List<Transform> controlPoints = new List<Transform>(); // New: Custom curve control
        public BoltUnscrew boltComponent; // New: Thread/Unscrew Animation logic
        public List<Annotation> annotations = new List<Annotation>(); // New: Support multiple labels per part
        
        [Header("Motion Quality")]
        public AnimationCurve motionCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public float delay = 0f;
    }

    [SerializeField] // Serialize to keep data between reloads/play mode
    public List<PartData> parts = new List<PartData>();

    public PartData GetPartData(Transform t)
    {
        return parts.Find(p => p.transform == t);
    }
    // ... (Lines 42-169 remain roughly same, skipping to AddPart)

    // In AddPart (conceptually, but using replace_file_content so I need precise target)
    // Actually, I will replace the PartData struct definition first.

    // ... (Update AddPart logic) ...

    // ... (Update Update logic) ...


    [SerializeField]
    public List<ExplodedView> subManagers = new List<ExplodedView>();

    public bool orchestrateSubManagers = false;
    public bool linkExplosionFactors = false;
    public bool orchestrateParts = false;
    public bool separateMovementAndOrchestration = false;

    [Header("Global Motion")]
    public AnimationCurve globalMotionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Debug Overlays")]
    public bool drawDebugLines = true;
    public Color debugLineColor = Color.yellow;
    public bool showHeatmap = false;
    public bool showPathLength = false;
    public bool applyDebugToChildren = true; // NEW: Propagate these settings down

    [Header("Global Annotation Settings")]
    public bool showAnnotations = true;
    public float globalAnnotationScale = 1.0f;
    public Vector3 globalAnnotationOffset = Vector3.zero;

    private void OnEnable()
    {
        // Only setup if we haven't already, or if the list is empty
        if (autoCreateTargets && (parts == null || parts.Count == 0 || subManagers == null))
        {
            SetupExplosion();
        }
    }

    private void OnValidate()
    {
        if (autoCreateTargets && (explosionMode == ExplosionMode.Target || explosionMode == ExplosionMode.Curved))
        {
            InitializeTargetMode();
        }

        // Safety: ensure default values for global settings if they seem uninitialized 
        // (This handles existing components in scenes picking up new fields)
        if (globalAnnotationScale == 0 && showAnnotations == false && globalAnnotationOffset == Vector3.zero)
        {
            showAnnotations = true;
            globalAnnotationScale = 1f;
        }

        if (parts != null)
        {
            foreach (var part in parts)
            {
                foreach (var anno in part.annotations)
                {
                    if (anno != null)
                    {
                        anno.globalVisibility = showAnnotations;
                        anno.globalScaleMultiplier = globalAnnotationScale;
                        anno.globalPositionOffset = globalAnnotationOffset;

                        // Force an update to reflect changes in inspector immediately
                        float factor = orchestrateParts ? 0 : explosionFactor;
                        anno.Animate(factor);
                    }
                }
            }
        }
    }

    [ContextMenu("Reset & Setup Explosion")]
    public void SetupExplosion()
    {
        if (autoGroupChildren)
        {
            AutoAttachSubManagers();
        }

        // --- Safe Setup Logic ---
        // Restore any existing parts to their home before re-scanning
        // This prevents capturing "already exploded" positions as the home state.
        foreach (var part in parts)
        {
            if (part != null && part.transform != null)
            {
                part.transform.localPosition = part.originalLocalPosition;
                part.transform.localRotation = part.originalLocalRotation;
                part.transform.localScale = part.originalLocalScale;
            }
        }
        explosionFactor = 0f;

        parts.Clear();
        subManagers.Clear();

        // 2. Recursively find "Significant Parts" to move
        // This stops at the first thing that is either a Renderer OR another manager.
        DiscoverParts(transform, center != null ? center.position : transform.position);

        // 3. Initialize Targets and Control Points if needed
        if (explosionMode == ExplosionMode.Target || explosionMode == ExplosionMode.Curved)
        {
            InitializeTargetMode();
        }
        
        RefreshAnnotations();

        Debug.Log($"ExplodedView ({gameObject.name}): Setup complete. Moving {parts.Count} parts locally. Master Control has {subManagers.Count} direct sub-managers.");
    }

    [ContextMenu("Refresh Annotations")]
    public void RefreshAnnotations()
    {
        if (parts == null) return;
        
        int count = 0;
        foreach (var part in parts)
        {
            if (part.transform != null)
            {
                part.annotations = new List<Annotation>(part.transform.GetComponentsInChildren<Annotation>(true));
                count += part.annotations.Count;
            }
        }
        
        // Also refresh sub-managers
        foreach (var sub in subManagers)
        {
            if (sub != null) sub.RefreshAnnotations();
        }

        Debug.Log($"ExplodedView ({gameObject.name}): Refreshed annotation links. Found {count} annotations.");
    }

    private void DiscoverParts(Transform current, Vector3 rootCenterPos)
    {
        foreach (Transform child in current)
        {
            bool isSignificant = false;

            // If child has another manager, it's a "significant part" (a group)
            ExplodedView childManager = child.GetComponent<ExplodedView>();
            if (childManager != null)
            {
                isSignificant = true;
                subManagers.Add(childManager);
                
                // Propagate mode to children
                childManager.explosionMode = this.explosionMode;
                childManager.useBoundsCenter = this.useBoundsCenter;
                childManager.SetupExplosion();
            }
            // Else if it has a renderer, it's a "significant part" (a leaf mesh)
            else if (child.GetComponent<Renderer>() != null)
            {
                isSignificant = true;
            }

            if (isSignificant)
            {
                AddPart(child, rootCenterPos);
            }
            else
            {
                DiscoverParts(child, rootCenterPos);
            }
        }
    }

    private void AddPart(Transform child, Vector3 rootCenterPos)
    {
        // Determine the center to explode from
        Vector3 centerForThisPart = rootCenterPos;
        if (useHierarchicalCenter && child.parent != null && child.parent != transform)
        {
            centerForThisPart = child.parent.position;
        }

        Vector3 worldDir;
        if (useBoundsCenter)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            Vector3 centerPos = renderer != null ? renderer.bounds.center : child.position;
            worldDir = (centerPos - centerForThisPart).normalized;
        }
        else
        {
            worldDir = (child.position - centerForThisPart).normalized;
        }

        if (worldDir == Vector3.zero) worldDir = Vector3.up;

        Vector3 localDir = child.parent != null ? child.parent.InverseTransformVector(worldDir) : worldDir;

        Transform targetT = null;
        if (explosionMode == ExplosionMode.Target || explosionMode == ExplosionMode.Curved)
        {
            targetT = SetupTargetObject(child, localDir);
        }

        // Check for special components
        BoltUnscrew bolt = child.GetComponent<BoltUnscrew>();
        if (bolt != null)
        {
            bolt.Init(child.localPosition, child.localRotation);
        }

        // Create and populate PartData
        PartData part = new PartData
        {
            transform = child,
            originalLocalPosition = child.localPosition,
            originalLocalRotation = child.localRotation,
            originalLocalScale = child.localScale,
            direction = localDir,
            targetTransform = targetT,
            boltComponent = bolt,
            annotations = new List<Annotation>(child.GetComponentsInChildren<Annotation>(true))
        };

        parts.Add(part);
    }

    public void InitializeTargetMode()
    {
        if (parts == null || parts.Count == 0) return;

        foreach (var part in parts)
        {
            if (part.targetTransform == null)
            {
                part.targetTransform = SetupTargetObject(part.transform, part.direction);
            }
            
            // Auto-populate control points for Curved mode if empty
            if (explosionMode == ExplosionMode.Curved && (part.controlPoints == null || part.controlPoints.Count == 0))
            {
                InitializeControlPoints(part);
            }
        }
    }

    private void InitializeControlPoints(PartData part)
    {
        if (part.targetTransform == null) return;
        
        Transform cpContainer = part.targetTransform.Find("ControlPoints");
        if (cpContainer == null)
        {
            GameObject go = new GameObject("ControlPoints");
            cpContainer = go.transform;
            cpContainer.SetParent(part.targetTransform, false);
            cpContainer.localPosition = Vector3.zero;
        }

        // Create 2 default control points for a Cubic Bezier arc
        part.controlPoints.Clear();
        for (int i = 1; i <= 2; i++)
        {
            float ratio = i * 0.05f; // Place them at 5% and 10% along the path
            string cpName = $"CP_{i}";
            Transform cp = cpContainer.Find(cpName);
            if (cp == null)
            {
                GameObject cpGo = new GameObject(cpName);
                cp = cpGo.transform;
                cp.SetParent(cpContainer, false);
                
                // Position along the path with a very subtle initial arc
                Vector3 start = part.originalLocalPosition;
                Vector3 end = part.targetTransform.localPosition;
                Vector3 linearMid = Vector3.Lerp(start, end, ratio);
                // Very subtle default arc offset (5% of sensitivity)
                cp.localPosition = part.targetTransform.InverseTransformPoint(linearMid + part.direction * (sensitivity * 0.05f));
            }
            part.controlPoints.Add(cp);
        }
    }

    public void ClearTargets()
    {
        Transform container = transform.Find("ExplosionTargets");
        if (container != null)
        {
            DestroyImmediate(container.gameObject);
        }

        foreach (var part in parts)
        {
            part.targetTransform = null;
            part.controlPoints.Clear();
        }
    }

    private Transform SetupTargetObject(Transform child, Vector3 localDir)
    {
        // Look for existing target container
        Transform container = transform.Find("ExplosionTargets");
        if (container == null)
        {
            GameObject containerObj = new GameObject("ExplosionTargets");
            containerObj.transform.SetParent(transform, false);
            container = containerObj.transform;
        }

        string targetName = $"Target_{child.name}";
        Transform targetT = container.Find(targetName);
        if (targetT == null)
        {
            GameObject targetObj = new GameObject(targetName);
            targetT = targetObj.transform;
            targetT.SetParent(container, false);
            
            // Initial position: Offset slightly along the direction so it's visible/useful
            targetT.localPosition = child.localPosition + localDir * sensitivity;
            targetT.localRotation = child.localRotation;
            targetT.localScale = child.localScale;
        }

        return targetT;
    }

    private void Update()
    {
        // Calculate Effective Factors based on modes
        float effectiveLocalFactor = explosionFactor;
        float effectiveOrchestrationFactor = orchestrationFactor;

        if (linkExplosionFactors)
        {
            if (separateMovementAndOrchestration)
            {
                // Main Sequential Mode: 
                // 0.0 - 0.5: Local Parts (Movement)
                // 0.5 - 1.0: Sub-Managers (Orchestration)
                effectiveLocalFactor = Mathf.InverseLerp(0f, 0.5f, explosionFactor);
                effectiveOrchestrationFactor = Mathf.InverseLerp(0.5f, 1f, explosionFactor);
            }
            else
            {
                // Simultaneous Linked Mode
                effectiveLocalFactor = explosionFactor;
                effectiveOrchestrationFactor = explosionFactor;
            }
        }
    
        // 1. Orchestrate Sub-Managers if enabled
        if (orchestrateSubManagers && subManagers != null && subManagers.Count > 0)
        {
            float step = 1f / subManagers.Count;
            for (int i = 0; i < subManagers.Count; i++)
            {
                var sub = subManagers[i];
                if (sub == null) continue;

                // Calculate local factor for this slice
                // e.g. 3 subs: [0-0.33], [0.33-0.66], [0.66-1.0]
                float start = i * step;
                float end = (i + 1) * step;
                
                // Map global factor to 0-1 for this sub-manager
                float val = Mathf.InverseLerp(start, end, effectiveOrchestrationFactor);
                
                if (sub.separateMovementAndOrchestration)
                {
                    // Split the time slice: First 50% for Movement, Second 50% for Orchestration
                    sub.explosionFactor = Mathf.InverseLerp(0f, 0.5f, val);
                    sub.orchestrationFactor = Mathf.InverseLerp(0.5f, 1f, val);
                }
                else
                {
                    // Simultaneous (Default)
                    sub.explosionFactor = val;
                    // IMPORTANT: Recursively drive the sub-manager's orchestration logic too!
                    sub.orchestrationFactor = val;
                }

                // Hierarchical Propagation of Visuals
                if (applyDebugToChildren)
                {
                    sub.applyDebugToChildren = true;
                    sub.drawDebugLines = drawDebugLines;
                    sub.debugLineColor = debugLineColor;
                    sub.showHeatmap = showHeatmap;
                    sub.showPathLength = showPathLength;
                    sub.showAnnotations = showAnnotations;
                    sub.globalAnnotationScale = globalAnnotationScale;
                    sub.globalAnnotationOffset = globalAnnotationOffset;
                }
            }
        }

        // 2. Handle own parts (Leaf Nodes)
        if (parts == null || parts.Count == 0) return;
        
        float partsStep = 1f / parts.Count;

        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part == null || part.transform == null) continue;

            // Determine effective factor for this part
            float effectivePartFactor = effectiveLocalFactor;
            if (orchestrateParts)
            {
                float start = i * partsStep;
                float end = (i + 1) * partsStep;
                effectivePartFactor = Mathf.InverseLerp(start, end, effectiveLocalFactor);
            }

            // Apply Motion Quality Controls
            float motionTime = effectivePartFactor;
            
            // 1. Apply Delay (Local)
            if (part.delay > 0)
            {
                motionTime = Mathf.InverseLerp(part.delay, 1f, motionTime);
            }

            // 2. Apply Per-Part Motion Curve
            motionTime = part.motionCurve.Evaluate(motionTime);

            // 3. Apply Global Motion Curve
            motionTime = globalMotionCurve.Evaluate(motionTime);
            
            // Priority: Special Components Override Modes
            if (part.boltComponent != null)
            {
                part.boltComponent.Animate(motionTime);
            }
            else if (explosionMode == ExplosionMode.Spherical)
            {
                Vector3 displacement = part.direction * (motionTime * sensitivity);
                part.transform.localPosition = part.originalLocalPosition + displacement;
            }
            else if (explosionMode == ExplosionMode.Target && part.targetTransform != null)
            {
                part.transform.localPosition = Vector3.Lerp(part.originalLocalPosition, part.targetTransform.localPosition, motionTime);
            }
            else if (explosionMode == ExplosionMode.Curved && part.targetTransform != null)
            {
                // Generalized Bezier calculation using control points
                List<Vector3> points = new List<Vector3>();
                points.Add(part.originalLocalPosition);
                foreach (var cp in part.controlPoints) if (cp != null) points.Add(transform.InverseTransformPoint(cp.position));
                points.Add(transform.InverseTransformPoint(part.targetTransform.position));

                part.transform.localPosition = GetBezierPoint(motionTime, points);
            }

            // Animate All Annotations if present
            if (part.annotations != null)
            {
                foreach (var anno in part.annotations)
                {
                    if (anno != null)
                    {
                        // Use current settings (which might have been pushed from parent)
                        anno.globalVisibility = showAnnotations;
                        anno.globalScaleMultiplier = globalAnnotationScale;
                        anno.globalPositionOffset = globalAnnotationOffset;
                        
                        anno.Animate(motionTime);
                    }
                }
            }
        }
    }

    private Vector3 GetBezierPoint(float t, List<Vector3> points)
    {
        if (points == null || points.Count < 2) return Vector3.zero;
        
        // De Casteljau's algorithm
        int n = points.Count;
        Vector3[] temp = new Vector3[n];
        for (int i = 0; i < n; i++) temp[i] = points[i];

        for (int j = 1; j < n; j++)
        {
            for (int i = 0; i < n - j; i++)
            {
                temp[i] = Vector3.Lerp(temp[i], temp[i + 1], t);
            }
        }
        return temp[0];
    }

    // Optional: Reset positions when disabled or destroyed to prevent "stuck" explosion
    private void OnDisable()
    {
        // ...
    }

    private void OnDestroy()
    {
        // If we are being removed in the editor, and not because of a scene close/playmode change
        if (!Application.isPlaying && this == null) 
        {
            Cleanup(false);
        }
    }

    public void Cleanup(bool removeComponent = false)
    {
        // 1. Reset all parts managed by THIS component
        foreach (var part in parts)
        {
            if (part != null && part.transform != null)
            {
                part.transform.localPosition = part.originalLocalPosition;
                part.transform.localRotation = part.originalLocalRotation;
                part.transform.localScale = part.originalLocalScale;
            }
        }

        // 2. Cleanup Targets
        Transform container = transform.Find("ExplosionTargets");
        if (container != null)
        {
            DestroyImmediate(container.gameObject);
        }

        // 3. Recursive Cleanup of Sub-Managers
        // Copy to list to avoid modification during iteration if we are destroying them
        List<ExplodedView> subs = new List<ExplodedView>(subManagers);
        foreach (var sub in subs)
        {
            if (sub != null)
            {
                sub.Cleanup(removeComponent);
            }
        }

        parts.Clear();
        subManagers.Clear();

        if (removeComponent)
        {
            // Use DelayCall to avoid "Destroying during OnDestroy" or similar issues if called from UI
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => {
                if (this != null) DestroyImmediate(this);
            };
            #else
            DestroyImmediate(this);
            #endif
        }
    }

    private void AutoAttachSubManagers()
    {
        // ... (existing code)
        foreach (Transform child in transform)
        {
            if (child.childCount > 0 && child.GetComponentInChildren<Renderer>() != null)
            {
                if (child.GetComponent<ExplodedView>() == null)
                {
                    ExplodedView newManager = child.gameObject.AddComponent<ExplodedView>();
                    newManager.explosionFactor = this.explosionFactor;
                    newManager.sensitivity = this.sensitivity;
                    newManager.useHierarchicalCenter = this.useHierarchicalCenter;
                    newManager.useBoundsCenter = this.useBoundsCenter;
                    newManager.autoGroupChildren = true;
                    newManager.SetupExplosion();
                    Debug.Log($"ExplodedView: Auto-added group manager to: {child.name}");
                }
            }
        }
    }
}
