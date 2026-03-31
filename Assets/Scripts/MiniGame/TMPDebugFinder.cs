using UnityEngine;
using TMPro;
// TMPro to find and inspect all TextMeshProUGUI components in scene

public class TMPDebugFinder : MonoBehaviour
// debug helper script, scans all TMP text objects and logs errors if anything looks broken
// this is only for finding bugs, not needed in final build
{
    void Start()
    {
        var all = FindObjectsOfType<TextMeshProUGUI>(true);
        // find every TMP text in scene including inactive ones

        foreach (var t in all)
        {
            if (t == null) continue;

            if (t.font == null)
                Debug.LogError($"[TMPDebug] MISSING FONT -> {GetPath(t.transform)}", t);
            // 字體missingis the common pronlem，會導致text唔顯示

            var rt = t.rectTransform;
            if (HasBad(rt.anchoredPosition) || HasBad(rt.sizeDelta) || HasBad((Vector2)rt.localScale))
                Debug.LogError($"[TMPDebug] BAD RECT (NaN/Inf) -> {GetPath(t.transform)}  pos={rt.anchoredPosition} size={rt.sizeDelta} scale={rt.localScale}", t);
            // NaN or Infinity in position or size usually means something went wrong with layout calculations

            if (t.fontSize > 5000f || float.IsNaN(t.fontSize) || float.IsInfinity(t.fontSize))
                Debug.LogError($"[TMPDebug] BAD FONTSIZE -> {GetPath(t.transform)} fontSize={t.fontSize}", t);
            // font size over 5000 is probably a mistake, NaN/Inf definitely is
        }
    }

    static bool HasBad(Vector2 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y);
    // helper to check if a Vector2 has any invalid values

    static string GetPath(Transform tr)
    {
        string p = tr.name;
        while (tr.parent != null)
        {
            tr = tr.parent;
            p = tr.name + "/" + p;
        }
        return p;
        // builds full hierarchy path like Canvas/Panel/Text so easier to find in scene
    }
}