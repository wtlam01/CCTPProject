using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class DoorButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprites")]
    public Sprite doorClosedSprite;
    public Sprite doorOpenSprite;

    [Header("UI References")]
    public Image doorImage;
    public GameObject emailGroup;
    public GameObject videoRawImage;

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public string sofaVideoURL = "https://w33lam.panel.uwe.ac.uk/CCTPVideo/3OnSofa.mp4";

    [Header("IMPORTANT: disable the main flow script on VideoController")]
    public MonoBehaviour flowScriptToDisable; // <- 拖 SegmentedVideoSwipe_New... 入嚟

    Coroutine playCo;

    void Awake()
    {
        if (doorImage == null) doorImage = GetComponent<Image>();
        if (doorClosedSprite != null) doorImage.sprite = doorClosedSprite;
    }

public void OnDoorClicked()
{
    if (flowScriptToDisable != null)
        flowScriptToDisable.enabled = false;

    if (emailGroup != null)
        emailGroup.SetActive(false);

    if (videoRawImage != null)
        videoRawImage.SetActive(true);

    if (playCo != null) StopCoroutine(playCo);
    playCo = StartCoroutine(PrepareAndPlayLoop());

    // ✅ 用呢啲代替 SetActive(false)
    var btn = GetComponent<Button>();
    if (btn != null) btn.interactable = false;

    var cg = GetComponent<CanvasGroup>();
    if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    cg.alpha = 0f;
    cg.blocksRaycasts = false;
    cg.interactable = false;
}

IEnumerator PrepareAndPlayLoop()
{
    if (videoPlayer == null) yield break;

    videoPlayer.errorReceived += (vp, msg) => Debug.LogError("VideoPlayer error: " + msg);

    videoPlayer.Stop();
    videoPlayer.url = sofaVideoURL;
    videoPlayer.isLooping = true;

    videoPlayer.Prepare();

    float t = 0f;
    while (!videoPlayer.isPrepared && t < 5f)
    {
        t += Time.deltaTime;
        yield return null;
    }

    Debug.Log("Prepared? " + videoPlayer.isPrepared + " url=" + videoPlayer.url);

    if (!videoPlayer.isPrepared) yield break;

    videoPlayer.Play();
}

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (doorOpenSprite != null) doorImage.sprite = doorOpenSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (doorClosedSprite != null) doorImage.sprite = doorClosedSprite;
    }
}