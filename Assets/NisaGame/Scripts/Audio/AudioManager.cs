using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource seSource;

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
        bool bgmOn = PlayerPrefs.GetInt("BgmOn", 1) == 1;
        bool seOn = PlayerPrefs.GetInt("SeOn", 1) == 1;

        if (bgmSource != null) bgmSource.mute = !bgmOn;
        if (seSource != null) seSource.mute = !seOn;
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

    // ===== SE =====
    public void PlaySe(AudioClip clip, float volumeScale = 1f)
    {
      if (seSource == null || clip == null) return;
    
      bool seOn = PlayerPrefs.GetInt("SeOn", 1) == 1;
      if (!seOn) return;
    
        seSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void SetSeOn(bool on)
    {
        if (seSource != null) seSource.mute = !on;
        PlayerPrefs.SetInt("SeOn", on ? 1 : 0);
    }
}
