using UnityEngine;
using TMPro;

public class TMPDebugFinder : MonoBehaviour
{
    void Start()
    {
        var all = FindObjectsOfType<TextMeshProUGUI>(true);

        foreach (var t in all)
        {
            if (t == null) continue;

            // 1) font missing
            if (t.font == null)
                Debug.LogError($"[TMPDebug] MISSING FONT -> {GetPath(t.transform)}", t);

            // 2) rect NaN / Inf
            var rt = t.rectTransform;
            if (HasBad(rt.anchoredPosition) || HasBad(rt.sizeDelta) || HasBad((Vector2)rt.localScale))
                Debug.LogError($"[TMPDebug] BAD RECT (NaN/Inf) -> {GetPath(t.transform)}  pos={rt.anchoredPosition} size={rt.sizeDelta} scale={rt.localScale}", t);

            // 3) fontSize crazy (optional)
            if (t.fontSize > 5000f || float.IsNaN(t.fontSize) || float.IsInfinity(t.fontSize))
                Debug.LogError($"[TMPDebug] BAD FONTSIZE -> {GetPath(t.transform)} fontSize={t.fontSize}", t);
        }
    }

    static bool HasBad(Vector2 v) =>
        float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y);

    static string GetPath(Transform tr)
    {
        string p = tr.name;
        while (tr.parent != null)
        {
            tr = tr.parent;
            p = tr.name + "/" + p;
        }
        return p;
    }
}