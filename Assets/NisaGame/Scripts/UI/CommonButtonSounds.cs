using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 全画面共通で使うボタンサウンド用マネージャ。
/// 
/// ・シーン内に1つ置いておけば、
///   CommonButtonSounds.Instance.PlayClick(); のようにどこからでも再生可能。
/// 
/// ・任意で Button と同じ GameObject に付ければ、
///   「ボタンを押したときに自動でクリック音を鳴らす」こともできる。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CommonButtonSounds : MonoBehaviour, IPointerEnterHandler
{
    // ====== シングルトン ======
    public static CommonButtonSounds Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip clickClip;   // クリック音
    [SerializeField] private AudioClip hoverClip;   // ホバー音（任意）

    [Header("Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1.0f;

    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.5f;

    [Header("Auto Hook (Optional)")]
    [SerializeField] private bool autoHookButtonOnThisObject = false;
    // true にすると、同じ GameObject の Button.onClick に自動でサウンド再生を登録する

    private AudioSource audioSource;
    private Button attachedButton;

    private void Awake()
    {
        // シングルトン確立（1つ目だけ残す）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // このオブジェクト自体が Button を持っていて、
        // autoHook が有効なら、自動でクリック音を登録
        if (autoHookButtonOnThisObject)
        {
            attachedButton = GetComponent<Button>();
            if (attachedButton != null)
            {
                attachedButton.onClick.AddListener(PlayClick);
            }
        }
    }

    // ====== 外部から呼ぶ用 API ======

    /// <summary>
    /// クリック音を再生する（ボタンの OnClick から直接呼んでもOK）。
    /// </summary>
    public void PlayClick()
    {
        if (clickClip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clickClip, clickVolume);
    }

    /// <summary>
    /// ホバー音を再生する。
    /// IPointerEnterHandler 経由でも呼ばれる。
    /// </summary>
    public void PlayHover()
    {
        if (hoverClip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(hoverClip, hoverVolume);
    }

    // ====== イベントインターフェース ======

    /// <summary>
    /// このコンポーネントがアタッチされたオブジェクト上に
    /// ポインタが乗ったときに呼ばれる。
    /// （EventSystem ＋ Raycast 対象のUIが必要）
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // ここでは「このオブジェクトに付いているときのみ」ホバー音を鳴らす。
        // 全ボタンで共通ホバー音を使いたい場合は、このコンポーネントを各ボタンに付ける。
        PlayHover();
    }
}
