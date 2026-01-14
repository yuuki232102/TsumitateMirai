using UnityEngine;

public class ButtonSe : MonoBehaviour
{
    public AudioClip clickSe;
    [Range(0f, 1f)] public float volume = 1f;

    public void PlayClick()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySe(clickSe, volume);
    }
}
