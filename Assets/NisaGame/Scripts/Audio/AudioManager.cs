using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioSource bgmSource;

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
        bgmSource.mute = !bgmOn;
    }

    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void SetBgmOn(bool on)
    {
        bgmSource.mute = !on;
        PlayerPrefs.SetInt("BgmOn", on ? 1 : 0);
    }
}
