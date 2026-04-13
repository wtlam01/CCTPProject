// script 控制一個chapter開場動畫flow：
// 一開始顯示：
// 1. 黑背景 + 文字（introGroup）
// 2. 停留一段時間（holdTime）
// 3. 將文字 + 黑背景 淡出
// 4. 停一小段（0.3 秒）
// 5. 圖片（imageGroup）淡入
// This script handles the chapter intro sequence, fades out the title overlay then fades in the image.
// At the start:
// 1. Show black background + text (introGroup)
// 2. Hold for a short duration (holdTime)
// 3. Fade out the text + black background
// 4. Wait briefly (0.3 seconds)
// 5. Fade in the image (imageGroup)

using UnityEngine; //用 Unity 的功能 (import 工具箱 kind of) ,  Import Unity core functionalities
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

    void Start() //遊戲開始時，會自動做一次
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
            introGroup.alpha = 1f - (t / fadeDuration); // gradually reduce alpha from 1 to 0 over time (fade out)
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
            imageGroup.alpha = t / fadeDuration; // gradually increase alpha from 0 to 1 (fade in)
            yield return null;
        }
        imageGroup.alpha = 1f;
        // fade in image
    }
}