using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ModeSelectSceneManager : MonoBehaviour
{
    [Header("Mode Buttons")]
    [SerializeField] private Button normalButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button chaosButton;

    [Header("Action Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text selectedModeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text paramsText;

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string simulationSceneName = "SimulationScene";

    [Header("Default")]
    [SerializeField] private GameMode defaultMode = GameMode.Normal;

    private GameMode currentMode;

    private void Awake()
    {
        // ボタン配線
        if (normalButton != null) normalButton.onClick.AddListener(() => SelectMode(GameMode.Normal));
        if (hardButton != null) hardButton.onClick.AddListener(() => SelectMode(GameMode.Hard));
        if (chaosButton != null) chaosButton.onClick.AddListener(() => SelectMode(GameMode.Chaos));

        if (startButton != null) startButton.onClick.AddListener(OnClickStart);
        if (backButton != null) backButton.onClick.AddListener(OnClickBack);
    }

    private void Start()
    {
        SelectMode(defaultMode);
    }

    private void SelectMode(GameMode mode)
    {
        currentMode = mode;

        if (selectedModeText != null)
            selectedModeText.text = $"選択中：{GetModeLabel(mode)}";

        if (descriptionText != null)
            descriptionText.text = GetModeDescription(mode);

        if (paramsText != null)
            paramsText.text = GetModeParamsText(mode);
    }

    private void OnClickStart()
    {
        // ★ここが重要：GameModeStoreに保存
        var store = GameModeStore.Ensure();
        store.SetMode(currentMode);

        SceneManager.LoadScene(simulationSceneName);
    }

    private void OnClickBack()
    {
        SceneManager.LoadScene(titleSceneName);
    }

    private string GetModeLabel(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Normal: return "ノーマル";
            case GameMode.Hard: return "ハード";
            case GameMode.Chaos: return "カオス";
            default: return "ノーマル";
        }
    }

    private string GetModeDescription(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Normal:
                return "標準の難易度。安定した学習向けのバランス。";
            case GameMode.Hard:
                return "不景気・ショックの影響が強め。慎重な運用が必要。";
            case GameMode.Chaos:
                return "値動きが激しいカオスな相場。ハイリスク・ハイリターン。";
            default:
                return "";
        }
    }

    // 表示用（ここは文章でOK。後でModeConfigの値を表示しても良い）
    private string GetModeParamsText(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Normal:
                return "イベント補正：標準\nイベント確率：標準";
            case GameMode.Hard:
                return "イベント補正：難しめ\nイベント確率：不景気・ショック多め";
            case GameMode.Chaos:
                return "イベント補正：超変動\nイベント確率：波乱多め（2イベント出やすい）";
            default:
                return "";
        }
    }
}
