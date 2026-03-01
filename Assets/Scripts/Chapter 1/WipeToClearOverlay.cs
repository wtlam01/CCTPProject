// WipeToClearOverlay.cs
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WipeToClearOverlay : MonoBehaviour
{
    [Header("UI")]
    public RectTransform overlayRect;   // OrangeOverlay RectTransform
    public Graphic overlayGraphic;      // OrangeOverlay Image
    public CanvasGroup overlayGroup;    // OrangeOverlay CanvasGroup

    [Header("Mask Texture")]
    public int texSize = 512;
    public int brushRadiusPx = 24;

    [Range(0.05f, 0.99f)]
    public float clearToFinish = 0.80f; // ✅ 80% default

    [Header("Output")]
    public bool disableOnFinish = true;

    [Header("Cleared Ratio Calc")]
    public float ratioUpdateInterval = 0.15f;

    public event Action OnFinished;

    // internal
    Texture2D maskTex;
    Color32[] pixels;
    Material runtimeMat;
    bool wipingEnabled = false;
    bool finished = false;

    // ratio cache
    float cachedClearedRatio = 0f;
    float lastRatioUpdateAt = -999f;

    void Reset()
    {
        overlayRect = GetComponent<RectTransform>();
        overlayGraphic = GetComponent<Graphic>();
        overlayGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (overlayRect == null) overlayRect = GetComponent<RectTransform>();
        if (overlayGraphic == null) overlayGraphic = GetComponent<Graphic>();
        if (overlayGroup == null) overlayGroup = GetComponent<CanvasGroup>();

        EnsureMaskTexture();
        EndWipeHide(); // start hidden & not blocking
    }

    void EnsureMaskTexture()
    {
        if (maskTex != null) return;

        maskTex = new Texture2D(texSize, texSize, TextureFormat.R8, false, true);
        maskTex.wrapMode = TextureWrapMode.Clamp;
        maskTex.filterMode = FilterMode.Bilinear;

        pixels = new Color32[texSize * texSize];
        FillMask(255); // 255 = fully covered
        ApplyMask();

        // runtime material instance (avoid editing asset)
        if (overlayGraphic != null && overlayGraphic.material != null)
        {
            runtimeMat = new Material(overlayGraphic.material);
            overlayGraphic.material = runtimeMat;
        }
        else
        {
            runtimeMat = overlayGraphic != null ? overlayGraphic.material : null;
        }

        if (runtimeMat != null)
        {
            // shader expects _MaskTex
            runtimeMat.SetTexture("_MaskTex", maskTex);
        }
    }

    void FillMask(byte value)
    {
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(value, value, value, 255);
    }

    void ApplyMask()
    {
        maskTex.SetPixels32(pixels);
        maskTex.Apply(false, false);
        cachedClearedRatio = 0f;
        lastRatioUpdateAt = Time.unscaledTime;
    }

    // ===== Public API =====
    public void BeginWipe()
    {
        EnsureMaskTexture();
        FillMask(255);
        ApplyMask();

        finished = false;
        wipingEnabled = true;

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 1f;
            overlayGroup.interactable = true;
            overlayGroup.blocksRaycasts = true;
            overlayGroup.gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void EndWipeHide()
    {
        wipingEnabled = false;

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;
            if (disableOnFinish) overlayGroup.gameObject.SetActive(false);
        }
        else
        {
            if (disableOnFinish) gameObject.SetActive(false);
        }
    }

    public float ClearedRatio => GetClearedRatio();

    public bool IsNearlyClean(float threshold)
    {
        return GetClearedRatio() >= threshold;
    }

    public float GetClearedRatio()
    {
        if (Time.unscaledTime - lastRatioUpdateAt < ratioUpdateInterval)
            return cachedClearedRatio;

        lastRatioUpdateAt = Time.unscaledTime;

        int cleared = 0;
        int total = pixels.Length;

        // treat <= 10 as erased
        for (int i = 0; i < total; i++)
        {
            if (pixels[i].r <= 10) cleared++;
        }

        cachedClearedRatio = (total > 0) ? (float)cleared / total : 0f;
        return cachedClearedRatio;
    }

    // ===== Input / Painting =====
    void Update()
    {
        if (!wipingEnabled) return;
        if (overlayRect == null || overlayGraphic == null) return;

        // mouse
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 screen = Mouse.current.position.ReadValue();
            TryPaintAtScreenPos(screen);
        }

        // touch
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 screen = Touchscreen.current.primaryTouch.position.ReadValue();
            TryPaintAtScreenPos(screen);
        }

        // ✅ auto-finish when nearly clean
        if (!finished && GetClearedRatio() >= clearToFinish)
        {
            finished = true;
            wipingEnabled = false;
            EndWipeHide();
            OnFinished?.Invoke();
        }
    }

    void TryPaintAtScreenPos(Vector2 screenPos)
    {
        // If your Canvas is Screen Space - Overlay, camera should be null.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect, screenPos, null, out Vector2 local))
            return;

        Rect r = overlayRect.rect;
        float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

        if (u < 0f || u > 1f || v < 0f || v > 1f) return;

        int x = Mathf.RoundToInt(u * (texSize - 1));
        int y = Mathf.RoundToInt(v * (texSize - 1));

        PaintCircle(x, y, brushRadiusPx);
    }

    void PaintCircle(int cx, int cy, int radius)
    {
        int r2 = radius * radius;

        int minX = Mathf.Max(0, cx - radius);
        int maxX = Mathf.Min(texSize - 1, cx + radius);
        int minY = Mathf.Max(0, cy - radius);
        int maxY = Mathf.Min(texSize - 1, cy + radius);

        bool changed = false;

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy * dy > r2) continue;

                int idx = y * texSize + x;

                // set to 0 => erased
                if (pixels[idx].r != 0)
                {
                    pixels[idx] = new Color32(0, 0, 0, 255);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            maskTex.SetPixels32(pixels);
            maskTex.Apply(false, false);
        }
    }
}