using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAutoClickSe : MonoBehaviour
{
    [Header("SE")]
    [SerializeField] private AudioClip clickSe;
    [Range(0f, 1f)][SerializeField] private float volume = 1f;

    [Header("Options")]
    [SerializeField] private bool ignoreToggles = true;
    [SerializeField] private bool ignoreSliders = true;
    [SerializeField] private bool ignoreInputFields = true;

    void Update()
    {
        // 左クリック / タップ開始で判定（押しっぱなしで連打しない）
        if (!Input.GetMouseButtonDown(0)) return;

        // UI上をクリックしているか？
        if (EventSystem.current == null) return;
        if (!EventSystem.current.IsPointerOverGameObject()) return;

        // クリック対象を取得
        var go = EventSystem.current.currentSelectedGameObject;
        if (go == null) return;

        // Buttonだけ鳴らしたい（基本はこれでOK）
        var button = go.GetComponent<Button>();
        if (button == null) return;
        if (!button.interactable) return;

        // 除外（任意）
        if (ignoreToggles && go.GetComponent<Toggle>() != null) return;
        if (ignoreSliders && go.GetComponent<Slider>() != null) return;
        if (ignoreInputFields && (go.GetComponent<InputField>() != null)) return;

        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySe(clickSe, volume);
    }
}
