// this script係控制章節開場動畫，先顯示文字同黑背景
// 停留一段時間後淡出，然後淡入圖片。
// This script handles the chapter intro sequence, fades out the title overlay then fades in the image.

using UnityEngine;
using System.Collections;


public class ChapterIntro : MonoBehaviour
{
    public CanvasGroup introGroup;
    // black background and text canvas group

    public CanvasGroup imageGroup;
    // image canvas group

    public float fadeDuration = 1.5f;
    public float holdTime = 1.5f;
    // how long the text stays before starting to fade out

    void Start()
    {
        imageGroup.alpha = 0f;
        StartCoroutine(FadeSequence());
        // image starts hidden, then begin the sequence
    }

    IEnumerator FadeSequence()
    {
        yield return new WaitForSeconds(holdTime);
        // wait for text hold time

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            introGroup.alpha = 1f - (t / fadeDuration);
            yield return null;
        }
        introGroup.alpha = 0f;
        // fade out text and black background

        yield return new WaitForSeconds(0.3f);
        // short pause before fading in image

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            imageGroup.alpha = t / fadeDuration;
            yield return null;
        }
        imageGroup.alpha = 1f;
        // fade in image
    }
}