using UnityEngine;
using System.Collections;

public class ChapterIntro : MonoBehaviour
{
    public CanvasGroup introGroup;   // 黑背景 + 文字
    public CanvasGroup imageGroup;   // Sofa 圖片

    public float fadeDuration = 1.5f;
    public float holdTime = 1.5f;

    void Start()
    {
        imageGroup.alpha = 0f;
        StartCoroutine(FadeSequence());
    }

    IEnumerator FadeSequence()
    {
        // 等待文字停留
        yield return new WaitForSeconds(holdTime);

        // 淡出文字 + 黑背景
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            introGroup.alpha = 1f - (t / fadeDuration);
            yield return null;
        }

        introGroup.alpha = 0f;

        // 等 0.3 秒（可選）
        yield return new WaitForSeconds(0.3f);

        // 淡入圖片
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            imageGroup.alpha = t / fadeDuration;
            yield return null;
        }

        imageGroup.alpha = 1f;
    }
}