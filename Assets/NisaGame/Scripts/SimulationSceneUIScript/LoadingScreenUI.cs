using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ロード画面UI（同一シーン内オーバーレイ）
/// ・Show/Hide（フェード optional）
/// ・ShowForSeconds は IEnumerator を返す（StartCoroutineは呼び出し側で）
/// ・任意でロード中BGMを再生/停止、他のAudioSourceを一時停止
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Header("Root（ロード画面全体）")]
    [SerializeField] private GameObject root;

    [Header("CanvasGroup（フェードに使用・任意）")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeSeconds = 0.15f;

    [Header("Optional Visual")]
    [SerializeField] private Graphic dimPanelGraphic;
    [Range(0f, 1f)]
    [SerializeField] private float dimAlpha = 0.6f;

    [Header("Loading BGM (Optional)")]
    [SerializeField] private AudioSource loadingBgmSource;
    [SerializeField] private AudioClip loadingBgmClip;
    [SerializeField] private bool loopLoadingBgm = true;

    [Header("Pause Other AudioSources While Loading (Optional)")]
    [SerializeField] private AudioSource[] pauseTheseSources;

    private bool initialized = false;

    private void Awake()
    {
        InitializeIfNeeded();
        HideInstant();
    }

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        initialized = true;

        if (root == null) root = gameObject;

        if (canvasGroup == null)
        {
            canvasGroup = root.GetComponent<CanvasGroup>();
        }

        // Dim panel alpha 適用（任意）
        if (dimPanelGraphic != null)
        {
            var c = dimPanelGraphic.color;
            c.a = dimAlpha;
            dimPanelGraphic.color = c;
        }
    }

    public void HideInstant()
    {
        InitializeIfNeeded();

        StopLoadingBgm();
        ResumePausedSources();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (root != null) root.SetActive(false);
    }

    public IEnumerator CoShow()
    {
        InitializeIfNeeded();

        if (root != null && !root.activeSelf) root.SetActive(true);

        ApplyDimAlpha();

        PauseSources();
        PlayLoadingBgm();

        if (!useFade || canvasGroup == null || fadeSeconds <= 0f)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            yield break;
        }

        yield return Fade(0f, 1f, fadeSeconds);

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public IEnumerator CoHide()
    {
        InitializeIfNeeded();

        if (root == null || !root.activeSelf)
        {
            StopLoadingBgm();
            ResumePausedSources();
            yield break;
        }

        if (!useFade || canvasGroup == null || fadeSeconds <= 0f)
        {
            StopLoadingBgm();
            ResumePausedSources();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            root.SetActive(false);
            yield break;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        yield return Fade(1f, 0f, fadeSeconds);

        StopLoadingBgm();
        ResumePausedSources();

        root.SetActive(false);
    }

    public IEnumerator CoShowForSeconds(float seconds)
    {
        yield return CoShow();

        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);

        yield return CoHide();
    }

    private IEnumerator Fade(float from, float to, float sec)
    {
        float t = 0f;
        canvasGroup.alpha = from;

        while (t < sec)
        {
            t += Time.unscaledDeltaTime; // UIロードは止まらない方が自然
            float a = Mathf.Lerp(from, to, Mathf.Clamp01(t / sec));
            canvasGroup.alpha = a;
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void ApplyDimAlpha()
    {
        if (dimPanelGraphic == null) return;
        var c = dimPanelGraphic.color;
        c.a = dimAlpha;
        dimPanelGraphic.color = c;
    }

    private const string KEY_SE_ON = "SeOn";

    private void PlayLoadingBgm()
    {
        // ✅ ここを「SEトグル」で制御する
        bool seOn = PlayerPrefs.GetInt(KEY_SE_ON, 1) == 1;
        if (!seOn) return;

        if (loadingBgmSource == null || loadingBgmClip == null) return;

        loadingBgmSource.clip = loadingBgmClip;
        loadingBgmSource.loop = loopLoadingBgm;
        if (!loadingBgmSource.isPlaying) loadingBgmSource.Play();
    }


    private void StopLoadingBgm()
    {
        if (loadingBgmSource == null) return;
        if (loadingBgmSource.isPlaying) loadingBgmSource.Stop();
    }

    private void PauseSources()
    {
        if (pauseTheseSources == null) return;
        for (int i = 0; i < pauseTheseSources.Length; i++)
        {
            var s = pauseTheseSources[i];
            if (s == null) continue;
            if (s.isPlaying) s.Pause();
        }
    }

    private void ResumePausedSources()
    {
        if (pauseTheseSources == null) return;
        for (int i = 0; i < pauseTheseSources.Length; i++)
        {
            var s = pauseTheseSources[i];
            if (s == null) continue;
            // Pauseしたものだけ戻す用途なら UnPause でOK
            s.UnPause();
        }
    }

    // ★ 外部（設定画面など）から呼ぶ用
    public void ForceStopLoadingSe()
    {
        // カチカチ音を止める（SE扱い）
        if (loadingBgmSource != null)
        {
            loadingBgmSource.Stop();
        }
    }

}
