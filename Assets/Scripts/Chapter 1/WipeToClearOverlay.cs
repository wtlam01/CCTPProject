// control overwork事件入面嘅橙色overlay wipe效果，玩家需要用滑鼠或者手指
// 喺橙色overlay上面抹，抹到指定百分比之後overlay自動消失，觸發下一步
// This script handles the orange wipe overlay in the overwork sequence, where the player drags across
// the screen to clear it. Once enough is wiped away, the overlay hides itself and fires an event to continue.

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WipeToClearOverlay : MonoBehaviour
// this script handles the orange overlay that player wipes away with mouse drag, used in overwork sequence
{
    [Header("UI")]
    public RectTransform overlayRect;
    public Graphic overlayGraphic;
    public CanvasGroup overlayGroup;
    // the orange overlay UI references, graphic needed to apply the mask material

    [Header("Hint")]
    public OrangeWipeFingerHint overlayHint;
    // the M shape finger hint that shows player to wipe, hides after first drag

    [Header("Mask Texture")]
    public int texSize = 512;
    public int brushRadiusPx = 24;
    // texture resolution and brush size, higher texSize = more precise wipe but more memory

    [Range(0.05f, 0.99f)]
    public float clearToFinish = 0.80f;
    // how much needs to be wiped before it counts as done, 80% by default

    [Header("Output")]
    public bool disableOnFinish = true;
    // hide the overlay automatically when wipe is complete

    [Header("Cleared Ratio Calc")]
    public float ratioUpdateInterval = 0.15f;
    // only recalculate cleared percentage every 0.15 seconds, saves performance

    public event Action OnFinished;
    // fires when wipe is complete, SofaEmailController listens to this

    Texture2D maskTex;
    Color32[] pixels;
    Material runtimeMat;
    bool wipingEnabled = false;
    bool finished = false;
    // internal state, wipingEnabled controls if input is being checked

    bool hasHiddenHintAfterFirstDrag = false;
    // track if hint has been hidden after first drag

    float cachedClearedRatio = 0f;
    float lastRatioUpdateAt = -999f;
    // cache cleared ratio so we dont loop through all pixels every frame

    void Reset()
    {
        overlayRect = GetComponent<RectTransform>();
        overlayGraphic = GetComponent<Graphic>();
        overlayGroup = GetComponent<CanvasGroup>();
        // auto assign in editor
    }

    void Awake()
    {
        if (overlayRect == null) overlayRect = GetComponent<RectTransform>();
        if (overlayGraphic == null) overlayGraphic = GetComponent<Graphic>();
        if (overlayGroup == null) overlayGroup = GetComponent<CanvasGroup>();

        EnsureMaskTexture();
        EndWipeHide();
        // set up texture and start hidden
    }

    void EnsureMaskTexture()
    {
        if (maskTex != null) return;
        // only create texture once

        maskTex = new Texture2D(texSize, texSize, TextureFormat.R8, false, true);
        maskTex.wrapMode = TextureWrapMode.Clamp;
        maskTex.filterMode = FilterMode.Bilinear;
        // R8 format bc we only need one channel for the mask, saves memory

        pixels = new Color32[texSize * texSize];
        FillMask(255);
        ApplyMask();
        // fill fully covered at start

        if (overlayGraphic != null && overlayGraphic.material != null)
        {
            runtimeMat = new Material(overlayGraphic.material);
            overlayGraphic.material = runtimeMat;
        }
        else
        {
            runtimeMat = overlayGraphic != null ? overlayGraphic.material : null;
        }
        // create runtime copy of material so we dont modify the shared asset

        if (runtimeMat != null)
        {
            runtimeMat.SetTexture("_MaskTex", maskTex);
        }
        // pass the mask texture to the shader
    }

    void FillMask(byte value)
    {
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(value, value, value, 255);
        // fill every pixel with given value, 255 = fully covered, 0 = wiped
    }

    void ApplyMask()
    {
        maskTex.SetPixels32(pixels);
        maskTex.Apply(false, false);
        cachedClearedRatio = 0f;
        lastRatioUpdateAt = Time.unscaledTime;
        // push pixel changes to GPU and reset cache
    }

    // ===== Public API =====
    public void BeginWipe()
    {
        EnsureMaskTexture();
        FillMask(255);
        ApplyMask();
        // reset mask to fully covered before starting

        finished = false;
        wipingEnabled = true;
        hasHiddenHintAfterFirstDrag = false;

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
        // show the overlay and enable input

        if (overlayHint != null)
            overlayHint.ShowAndPlay();
        // show the M shape finger hint
    }

    public void EndWipeHide()
    {
        wipingEnabled = false;

        if (overlayHint != null)
            overlayHint.HideAndStop();
        // stop and hide hint first

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
        // hide overlay and stop blocking raycasts
    }

    public float ClearedRatio => GetClearedRatio();
    // shorthand property for cleared ratio

    public bool IsNearlyClean(float threshold)
    {
        return GetClearedRatio() >= threshold;
        // check if cleared enough, used externally to decide when to trigger next step
    }

    public float GetClearedRatio()
    {
        if (Time.unscaledTime - lastRatioUpdateAt < ratioUpdateInterval)
            return cachedClearedRatio;
        // return cached value if updated recently, avoids looping pixels every frame

        lastRatioUpdateAt = Time.unscaledTime;

        int cleared = 0;
        int total = pixels.Length;

        for (int i = 0; i < total; i++)
        {
            if (pixels[i].r <= 10) cleared++;
        }
        // count pixels with r value near 0, those are wiped

        cachedClearedRatio = (total > 0) ? (float)cleared / total : 0f;
        return cachedClearedRatio;
    }

    // ===== Input / Painting =====
    void Update()
    {
        if (!wipingEnabled) return;
        if (overlayRect == null || overlayGraphic == null) return;

        bool paintedThisFrame = false;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 screen = Mouse.current.position.ReadValue();
            paintedThisFrame = TryPaintAtScreenPos(screen) || paintedThisFrame;
        }
        // check mouse drag input

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            Vector2 screen = Touchscreen.current.primaryTouch.position.ReadValue();
            paintedThisFrame = TryPaintAtScreenPos(screen) || paintedThisFrame;
        }
        // also support touch input for mobile

        if (paintedThisFrame && !hasHiddenHintAfterFirstDrag)
        {
            hasHiddenHintAfterFirstDrag = true;

            if (overlayHint != null)
                overlayHint.HideAndStop();
        }
        // hide hint the moment player starts dragging

        if (!finished && GetClearedRatio() >= clearToFinish)
        {
            finished = true;
            wipingEnabled = false;
            EndWipeHide();
            OnFinished?.Invoke();
        }
        // check if wiped enough to finish, fire event when done
    }

    bool TryPaintAtScreenPos(Vector2 screenPos)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect, screenPos, null, out Vector2 local))
            return false;
        // convert screen position to local position within the overlay rect

        Rect r = overlayRect.rect;
        float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);

        if (u < 0f || u > 1f || v < 0f || v > 1f) return false;
        // out of bounds check, ignore if outside overlay area

        int x = Mathf.RoundToInt(u * (texSize - 1));
        int y = Mathf.RoundToInt(v * (texSize - 1));
        // convert normalised UV to pixel coordinates

        return PaintCircle(x, y, brushRadiusPx);
    }

    bool PaintCircle(int cx, int cy, int radius)
    {
        int r2 = radius * radius;

        int minX = Mathf.Max(0, cx - radius);
        int maxX = Mathf.Min(texSize - 1, cx + radius);
        int minY = Mathf.Max(0, cy - radius);
        int maxY = Mathf.Min(texSize - 1, cy + radius);
        // clamp brush bounds to texture edges

        bool changed = false;

        for (int y = minY; y <= maxY; y++)
        {
            int dy = y - cy;
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                if (dx * dx + dy * dy > r2) continue;
                // skip pixels outside the circle radius

                int idx = y * texSize + x;

                if (pixels[idx].r != 0)
                {
                    pixels[idx] = new Color32(0, 0, 0, 255);
                    changed = true;
                }
                // set pixel to 0 (wiped), only if not already wiped
            }
        }

        if (changed)
        {
            maskTex.SetPixels32(pixels);
            maskTex.Apply(false, false);
        }
        // only upload to GPU if something actually changed, saves performance

        return changed;
    }
}

// References: Texture2D painting technique
// https://docs.unity3d.com/ScriptReference/Texture2D.SetPixels32.html