using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource bgmSource;

    [Tooltip("通常SE（ボタン等）: PlayOneShot 用")]
    public AudioSource seSource;

    [Tooltip("ループSE（ローディング等）: ループ専用")]
    public AudioSource seLoopSource;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplyPrefsToAudio();
    }

    private void ApplyPrefsToAudio()
    {
        bool bgmOn = PlayerPrefs.GetInt("BgmOn", 1) == 1;
        bool seOn = PlayerPrefs.GetInt("SeOn", 1) == 1;

        if (bgmSource != null) bgmSource.mute = !bgmOn;

        if (seSource != null) seSource.mute = !seOn;
        if (seLoopSource != null) seLoopSource.mute = !seOn;

        // 念のため：SE OFFならループ停止
        if (!seOn) StopLoadingSe();
    }

    // ===== BGM =====
    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void SetBgmOn(bool on)
    {
        if (bgmSource != null) bgmSource.mute = !on;
        PlayerPrefs.SetInt("BgmOn", on ? 1 : 0);
    }

    // ===== SE (OneShot) =====
    public void PlaySe(AudioClip clip, float volumeScale = 1f)
    {
        if (seSource == null || clip == null) return;

        bool seOn = PlayerPrefs.GetInt("SeOn", 1) == 1;
        if (!seOn) return;

        seSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    // ===== Loading SE (Loop) =====
    public void PlayLoadingSe(AudioClip clip, float volume = 1f)
    {
        if (seLoopSource == null || clip == null) return;

        bool seOn = PlayerPrefs.GetInt("SeOn", 1) == 1;
        if (!seOn) return;

        // 同じのが鳴ってたら何もしない
        if (seLoopSource.isPlaying && seLoopSource.clip == clip) return;

        seLoopSource.clip = clip;
        seLoopSource.loop = true;
        seLoopSource.volume = Mathf.Clamp01(volume);
        seLoopSource.Play();
    }

    public void StopLoadingSe()
    {
        if (seLoopSource == null) return;

        if (seLoopSource.isPlaying) seLoopSource.Stop();
        seLoopSource.clip = null;
        seLoopSource.loop = false;
    }

    public void SetSeOn(bool on)
    {
        if (seSource != null) seSource.mute = !on;
        if (seLoopSource != null) seLoopSource.mute = !on;

        PlayerPrefs.SetInt("SeOn", on ? 1 : 0);

        if (!on) StopLoadingSe();
    }
}
