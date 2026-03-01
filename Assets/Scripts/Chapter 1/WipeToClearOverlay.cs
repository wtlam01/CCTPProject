using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WipeToClearOverlay : MonoBehaviour
{
    [Header("UI")]
    public RectTransform overlayRect;   // OrangeOverlay RectTransform
    public Graphic overlayGraphic;      // OrangeOverlay Image
    public CanvasGroup overlayGroup;    // OrangeOverlay CanvasGroup (IMPORTANT: drag it!)

    [Header("Mask Texture")]
    public int texSize = 512;
    public int brushRadiusPx = 24;
    [Range(0.1f, 0.95f)] public float clearToFinish = 0.55f;

    [Header("Output")]
    public bool disableOnFinish = true;

    Texture2D maskTex;
    Color32[] pixels;
    Material runtimeMat;

    public bool Finished { get; private set; } = false;

    bool running = false;

    void Awake()
    {
        Finished = false;
        running = false;

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }
    }

    /// <summary>
    /// Call this when you want the wipe interaction to start.
    /// </summary>
    public void StartWipe(Color overlayColor)
    {
        Finished = false;
        running = true;

        // Show overlay (solid)
        if (overlayGroup != null)
        {
            overlayGroup.gameObject.SetActive(true);
            overlayGroup.alpha = 1f;
            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = true;
        }

        if (overlayGraphic != null)
            overlayGraphic.color = overlayColor;

        SetupMaskTextureAndMaterial();
        ResetMaskToSolid();
    }

    void SetupMaskTextureAndMaterial()
    {
        if (overlayGraphic == null) return;

        // Make a runtime copy of material so we don't permanently edit the asset
        if (runtimeMat == null)
        {
            var srcMat = overlayGraphic.material;
            runtimeMat = (srcMat != null) ? new Material(srcMat) : null;
            overlayGraphic.material = runtimeMat;
        }

        if (maskTex == null)
        {
            maskTex = new Texture2D(texSize, texSize, TextureFormat.R8, false, true);
            maskTex.wrapMode = TextureWrapMode.Clamp;
            maskTex.filterMode = FilterMode.Bilinear;

            pixels = new Color32[texSize * texSize];
        }

        // IMPORTANT: your shader must use _MaskTex
        if (runtimeMat != null)
            runtimeMat.SetTexture("_MaskTex", maskTex);
    }

    void ResetMaskToSolid()
    {
        if (maskTex == null || pixels == null) return;

        // 255 = keep overlay (not erased)
        var solid = new Color32(255, 255, 255, 255);
        for (int i = 0; i < pixels.Length; i++) pixels[i] = solid;

        maskTex.SetPixels32(pixels);
        maskTex.Apply(false, false);
    }

    void Update()
    {
        if (!running || Finished) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.isPressed) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        if (!ScreenToUV(screenPos, out Vector2 uv)) return;

        PaintErase(uv);
        if (GetClearedRatio() >= clearToFinish)
        {
            Finish();
        }
    }

    bool ScreenToUV(Vector2 screenPos, out Vector2 uv)
    {
        uv = Vector2.zero;
        if (overlayRect == null) return false;

        var cam = Camera.main; // Canvas is likely Screen Space - Overlay; this still works for RectTransformUtility
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, screenPos, cam, out Vector2 local))
            return false;

        Rect r = overlayRect.rect;
        float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;
        uv = new Vector2(u, v);
        return true;
    }

    void PaintErase(Vector2 uv)
    {
        if (maskTex == null || pixels == null) return;

        int cx = Mathf.RoundToInt(uv.x * (texSize - 1));
        int cy = Mathf.RoundToInt(uv.y * (texSize - 1));
        int r = brushRadiusPx;
        int r2 = r * r;

        int xMin = Mathf.Max(0, cx - r);
        int xMax = Mathf.Min(texSize - 1, cx + r);
        int yMin = Mathf.Max(0, cy - r);
        int yMax = Mathf.Min(texSize - 1, cy + r);

        for (int y = yMin; y <= yMax; y++)
        {
            int dy = y - cy;
            for (int x = xMin; x <= xMax; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy * dy > r2) continue;

                int idx = y * texSize + x;
                // 0 = erased (transparent)
                pixels[idx] = new Color32(0, 0, 0, 255);
            }
        }

        maskTex.SetPixels32(pixels);
        maskTex.Apply(false, false);
    }

    float GetClearedRatio()
    {
        if (pixels == null) return 0f;

        // count erased pixels (R == 0)
        int erased = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r == 0) erased++;
        }
        return (float)erased / pixels.Length;
    }

    void Finish()
    {
        Finished = true;
        running = false;

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;

            if (disableOnFinish)
                overlayGroup.gameObject.SetActive(false);
        }
    }
}