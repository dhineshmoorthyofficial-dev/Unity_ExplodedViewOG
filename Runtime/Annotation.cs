using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
public class Annotation : MonoBehaviour
{
    [Header("Fade In Range")]
    public bool useFadeIn = true;
    [Range(0f, 1f)] public float fadeInStart = 0.2f;
    [Range(0f, 1f)] public float fadeInEnd = 0.4f;

    [Header("Fade Out Range")]
    public bool useFadeOut = true;
    [Range(0f, 1f)] public float fadeOutStart = 0.7f;
    [Range(0f, 1f)] public float fadeOutEnd = 0.9f;

    [Header("UI Settings")]
    public string labelText = "New Annotation";
    public Vector3 positionOffset = new Vector3(0, 0.2f, 0);
    public float canvasScale = 0.01f;
    public bool autoCreateUI = true;

    [Header("Line Path")]
    public Vector3 lineStartOffset = Vector3.zero;
    public System.Collections.Generic.List<Vector3> intermediatePoints = new System.Collections.Generic.List<Vector3>();
    public LineAnchor lineAnchor = LineAnchor.BottomLeft;
    public enum LineAnchor { Center, BottomLeft, BottomRight, TopLeft, TopRight, Left, Right, Top, Bottom }
    public bool showLine = true;
    public Color lineColor = Color.white;
    public float lineWidth = 0.01f;
    public Material lineMaterial;
    [Range(0f, 1f)] public float lineGrowth = 1.0f; 
    public bool lookAtCamera = true;

    [Header("Target Components")]
    public CanvasGroup canvasGroup;
    public SpriteRenderer spriteRenderer;
    public Renderer meshRenderer;
    public Image backgroundImage;
    public TextMeshProUGUI textMesh;
    public LineRenderer lineRenderer;
    
    [Header("Debug Info")]
    [Range(0f, 1f)] public float currentAlpha;
    
    // Internal state for global overrides from ExplodedView
    [HideInInspector] public float globalScaleMultiplier = 1f;
    [HideInInspector] public Vector3 globalPositionOffset = Vector3.zero;
    [HideInInspector] public bool globalVisibility = true;
    [HideInInspector] public bool treeVisibility = true; // Manual override from Model Tree
    [HideInInspector] public bool isolateVisibility = true; // Visibility due to isolation logic

    // --- Global Override Data (Pushed from ExplodedView) ---
    [HideInInspector] public bool useGlobalLineSettings = false;
    [HideInInspector] public Color globalLineColor = Color.white;
    [HideInInspector] public float globalLineWidth = 0.01f;

    [HideInInspector] public bool useGlobalFadeSettings = false;
    [HideInInspector] public bool globalUseFadeIn = true;
    [HideInInspector] public float globalFadeInStart = 0.2f;
    [HideInInspector] public float globalFadeInEnd = 0.4f;
    [HideInInspector] public bool globalUseFadeOut = true;
    [HideInInspector] public float globalFadeOutStart = 0.7f;
    [HideInInspector] public float globalFadeOutEnd = 0.9f;

    [HideInInspector] public bool useGlobalVisualSettings = false;
    [HideInInspector] public bool globalLookAtCamera = true;

    [HideInInspector] public bool useGlobalUISettings = false;
    [HideInInspector] public Color globalBackgroundColor = new Color(0, 0, 0, 0.5f);
    [HideInInspector] public Color globalTextColor = Color.white;
    [HideInInspector] public float globalFontSize = 36f;
    [HideInInspector] public Vector2 globalBGSize = new Vector2(250, 70);

    private void Reset()
    {
        if (autoCreateUI)
        {
            CreateDefaultUI();
        }
    }

    private void CreateDefaultUI()
    {
        // Use GameObject name if text is default or empty
        if (string.IsNullOrEmpty(labelText) || labelText == "New Annotation")
        {
            labelText = gameObject.name;
        }

        // Cleanup existing auto-generated UI if any
        Transform existing = transform.Find("AnnotationCanvas");
        if (existing != null) DestroyImmediate(existing.gameObject);

        // 1. Create Canvas
        GameObject canvasGO = new GameObject("AnnotationCanvas");
        canvasGO.transform.SetParent(this.transform, false);
        canvasGO.transform.localPosition = positionOffset;
        canvasGO.transform.localScale = Vector3.one * canvasScale;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        canvasGroup = canvasGO.AddComponent<CanvasGroup>();

        // 2. Create Background Image
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        backgroundImage = bgGO.AddComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black
        
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(250, 70);
        bgRect.anchoredPosition = Vector2.zero;

        // 3. Create Text (TMPro)
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(bgGO.transform, false);
        
        textMesh = textGO.AddComponent<TextMeshProUGUI>();
        textMesh.text = labelText;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 36;
        textMesh.color = Color.white;
        textMesh.enableWordWrapping = false;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // 4. Create Line Renderer
        GameObject lineGO = new GameObject("ConnectingLine");
        lineGO.transform.SetParent(this.transform, false);
        lineRenderer = lineGO.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        
        // Use a standard shader that supports vertex colors for gradients if no material is assigned
        if (lineMaterial == null)
        {
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null) lineShader = Shader.Find("UI/Default");
            lineMaterial = new Material(lineShader);
        }
        
        lineRenderer.material = lineMaterial;
        
        Debug.Log($"Annotation ({gameObject.name}): Auto-generated World Space Canvas with TMPro label, Background and Line.");
    }

    private void OnEnable()
    {
        RefreshLinks();
        if (!Application.isPlaying)
        {
            ApplyAlpha(0f);
        }
    }

    private void OnValidate()
    {
        RefreshLinks();
        UpdateUIProperties();
    }

    private void Update()
    {
        bool actualLookAtCamera = useGlobalVisualSettings ? globalLookAtCamera : lookAtCamera;
        if (actualLookAtCamera && canvasGroup != null)
        {
            Camera cam = Camera.main;
            if (cam == null && Camera.allCamerasCount > 0) cam = Camera.allCameras[0];
            
            if (cam != null)
            {
                canvasGroup.transform.LookAt(canvasGroup.transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
            }
        }

        // Only update line automatically if we're not being driven by Animate()
        // but it doesn't hurt to keep it for editor preview.
        UpdateLine();
    }

    private void UpdateLine()
    {
        if (lineRenderer == null) return;
        
        bool isVisible = showLine && globalVisibility && treeVisibility && isolateVisibility && currentAlpha > 0;
        lineRenderer.enabled = isVisible;

        if (isVisible)
        {
            // Gather all path points in world space
            System.Collections.Generic.List<Vector3> pathPoints = new System.Collections.Generic.List<Vector3>();
            
            // 1. Start Point (with offset)
            pathPoints.Add(transform.TransformPoint(lineStartOffset));

            // 2. Intermediate Points
            foreach (var pt in intermediatePoints)
            {
                pathPoints.Add(transform.TransformPoint(pt));
            }

            // 3. End Point (Label Anchor)
            pathPoints.Add(GetTargetAnchorPosition());

            // Calculate active segments based on lineGrowth
            // Growth is sequential: 0.0 means only start point, 1.0 means all points.
            int segments = pathPoints.Count - 1;
            float totalGrowth = lineGrowth * segments;

            System.Collections.Generic.List<Vector3> activePoints = new System.Collections.Generic.List<Vector3>();
            activePoints.Add(pathPoints[0]);

            for (int i = 0; i < segments; i++)
            {
                float segmentFactor = Mathf.Clamp01(totalGrowth - i);
                if (segmentFactor > 0)
                {
                    Vector3 p = Vector3.Lerp(pathPoints[i], pathPoints[i + 1], segmentFactor);
                    activePoints.Add(p);
                    if (segmentFactor < 1.0f) break; // Optimization: stop if segment is partially grown
                }
                else break;
            }

            lineRenderer.positionCount = activePoints.Count;
            lineRenderer.SetPositions(activePoints.ToArray());
            
            lineRenderer.startWidth = useGlobalLineSettings ? globalLineWidth : lineWidth;
            lineRenderer.endWidth = useGlobalLineSettings ? globalLineWidth : lineWidth;
            
            UpdateLineGradient(currentAlpha);
        }
    }

    private Vector3 GetTargetAnchorPosition()
    {
        if (backgroundImage == null) return canvasGroup.transform.position;

        Vector3[] corners = new Vector3[4];
        backgroundImage.rectTransform.GetWorldCorners(corners);

        // UI World Corners: 0: BottomLeft, 1: TopLeft, 2: TopRight, 3: BottomRight
        switch (lineAnchor)
        {
            case LineAnchor.BottomLeft: return corners[0];
            case LineAnchor.TopLeft: return corners[1];
            case LineAnchor.TopRight: return corners[2];
            case LineAnchor.BottomRight: return corners[3];
            case LineAnchor.Left: return (corners[0] + corners[1]) * 0.5f;   // Midpoint of Left edge
            case LineAnchor.Top: return (corners[1] + corners[2]) * 0.5f;    // Midpoint of Top edge
            case LineAnchor.Right: return (corners[2] + corners[3]) * 0.5f;  // Midpoint of Right edge
            case LineAnchor.Bottom: return (corners[3] + corners[0]) * 0.5f; // Midpoint of Bottom edge
            case LineAnchor.Center:
            default: return (corners[0] + corners[2]) * 0.5f;
        }
    }

    private void UpdateLineGradient(float alpha)
    {
        if (lineRenderer == null) return;

        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        Color actualLineColor = useGlobalLineSettings ? globalLineColor : lineColor;
        colorKeys[0] = new GradientColorKey(actualLineColor, 0.0f);
        colorKeys[1] = new GradientColorKey(actualLineColor, 1.0f);

        // Fade at tips: 0 at start, 1 at 20%, 1 at 80%, 0 at end
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        alphaKeys[0] = new GradientAlphaKey(0.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(alpha, 0.2f);
        alphaKeys[2] = new GradientAlphaKey(alpha, 0.8f);
        alphaKeys[3] = new GradientAlphaKey(0.0f, 1.0f);

        gradient.SetKeys(colorKeys, alphaKeys);
        lineRenderer.colorGradient = gradient;
    }

    public void RefreshLinks()
    {
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (meshRenderer == null) meshRenderer = GetComponentInChildren<Renderer>();
        if (backgroundImage == null && canvasGroup != null) backgroundImage = canvasGroup.GetComponentInChildren<Image>();
        if (textMesh == null && canvasGroup != null) textMesh = canvasGroup.GetComponentInChildren<TextMeshProUGUI>();
        if (lineRenderer == null) lineRenderer = GetComponentInChildren<LineRenderer>();
    }

    private void UpdateUIProperties()
    {
        if (canvasGroup != null)
        {
            // Sync active state with global and tree and isolate visibility overrides
            bool shouldBeActive = globalVisibility && treeVisibility && isolateVisibility;
            if (canvasGroup.gameObject.activeSelf != shouldBeActive)
            {
                canvasGroup.gameObject.SetActive(shouldBeActive);
            }

            if (shouldBeActive)
            {
                canvasGroup.transform.localPosition = positionOffset + globalPositionOffset;
                canvasGroup.transform.localScale = Vector3.one * (canvasScale * globalScaleMultiplier);
                
                if (textMesh != null)
                {
                    textMesh.text = labelText;
                    if (useGlobalUISettings)
                    {
                        textMesh.color = globalTextColor;
                        textMesh.fontSize = globalFontSize;
                    }
                }

                if (backgroundImage != null && useGlobalUISettings)
                {
                    backgroundImage.color = globalBackgroundColor;
                    backgroundImage.rectTransform.sizeDelta = globalBGSize;
                }
            }
        }

        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true; // Ensure world space is used for multi-point logic
            if (lineMaterial != null && lineRenderer.sharedMaterial != lineMaterial)
            {
                lineRenderer.sharedMaterial = lineMaterial;
            }
            
            lineRenderer.startWidth = useGlobalLineSettings ? globalLineWidth : lineWidth;
            lineRenderer.endWidth = useGlobalLineSettings ? globalLineWidth : lineWidth;
            lineRenderer.startColor = useGlobalLineSettings ? globalLineColor : lineColor;
            lineRenderer.endColor = useGlobalLineSettings ? globalLineColor : lineColor;
        }
    }

    public void SetTreeVisibility(bool visible)
    {
        treeVisibility = visible;
        UpdateUIProperties();
        UpdateLine();
    }

    public void SetIsolateVisibility(bool visible)
    {
        isolateVisibility = visible;
        UpdateUIProperties();
        UpdateLine();
    }

    public void Animate(float factor)
    {
        if (canvasGroup == null) RefreshLinks();
        
        UpdateUIProperties(); 
        currentAlpha = CalculateAlpha(factor);
        ApplyAlpha(currentAlpha);
        UpdateLine(); // Force line update with new alpha
    }

    private float CalculateAlpha(float factor)
    {
        // Handle Fade In
        float fadeInAlpha = 1f;
        bool actualUseFadeIn = useGlobalFadeSettings ? globalUseFadeIn : useFadeIn;
        float actualFadeInStart = useGlobalFadeSettings ? globalFadeInStart : fadeInStart;
        float actualFadeInEnd = useGlobalFadeSettings ? globalFadeInEnd : fadeInEnd;

        if (actualUseFadeIn)
        {
            if (actualFadeInStart < actualFadeInEnd)
                fadeInAlpha = Mathf.InverseLerp(actualFadeInStart, actualFadeInEnd, factor);
            else if (factor >= actualFadeInStart)
                fadeInAlpha = 1f;
            else
                fadeInAlpha = 0f;
        }
        
        // Handle Fade Out
        float fadeOutAlpha = 1f;
        bool actualUseFadeOut = useGlobalFadeSettings ? globalUseFadeOut : useFadeOut;
        float actualFadeOutStart = useGlobalFadeSettings ? globalFadeOutStart : fadeOutStart;
        float actualFadeOutEnd = useGlobalFadeSettings ? globalFadeOutEnd : fadeOutEnd;

        if (actualUseFadeOut)
        {
            if (actualFadeOutStart < actualFadeOutEnd)
                fadeOutAlpha = 1f - Mathf.InverseLerp(actualFadeOutStart, actualFadeOutEnd, factor);
            else if (factor >= actualFadeOutStart)
                fadeOutAlpha = 0f;
            else
                fadeOutAlpha = 1f;
        }
        
        return Mathf.Clamp01(Mathf.Min(fadeInAlpha, fadeOutAlpha));
    }

    private void ApplyAlpha(float alpha)
    {
        if (!treeVisibility || !isolateVisibility) alpha = 0;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        lineGrowth = alpha; // Sync line growth with fade

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }

        if (meshRenderer != null)
        {
            foreach (Material mat in meshRenderer.sharedMaterials)
            {
                if (mat == null) continue;
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }

        if (lineRenderer != null)
        {
            UpdateLineGradient(alpha);
        }
    }
}
