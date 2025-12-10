using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 設定画面（SettingsScene）のUI制御担当。
/// ・毎月のつみたて額
/// ・リスクタイプ（低・中・高）
/// を GameSettings に反映する。
/// 
/// 想定UI：
/// ・Slider monthlyAmountSlider     … 毎月のつみたて額
/// ・TextMeshProUGUI monthlyAmountValueText … スライダーの値を「◯◯◯円」で表示
/// ・TMP_Dropdown riskDropdown      … 0:低リスク / 1:中リスク / 2:高リスク
/// ・（任意）戻るボタン → タイトル or 前の画面へ
/// </summary>
public class SettingsUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider monthlyAmountSlider;
    [SerializeField] private TextMeshProUGUI monthlyAmountValueText;
    [SerializeField] private TMP_Dropdown riskDropdown;

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene"; // 戻るときのシーン名（必要なら変更）

    private GameSettings settings;

    private bool isInitialized = false;

    private void Awake()
    {
        // GameSettings の取得
        settings = GameSettings.Instance;
        if (settings == null)
        {
            Debug.LogError("SettingsUIController: GameSettings.Instance がシーン内に存在しません。GameSettings オブジェクトを配置してください。");
        }
    }

    private void Start()
    {
        // UIイベント登録
        if (monthlyAmountSlider != null)
        {
            monthlyAmountSlider.onValueChanged.AddListener(OnMonthlyAmountSliderChanged);
        }

        if (riskDropdown != null)
        {
            riskDropdown.onValueChanged.AddListener(OnRiskDropdownChanged);
        }

        // GameSettings から初期値を反映
        ApplySettingsToUI();

        isInitialized = true;
    }

    /// <summary>
    /// GameSettings に保存されている値を UI に反映する。
    /// 設定画面を開いたときに1回実行。
    /// </summary>
    private void ApplySettingsToUI()
    {
        if (settings == null)
        {
            return;
        }

        // 毎月のつみたて額
        if (monthlyAmountSlider != null)
        {
            // スライダーの範囲外だった場合は範囲内に収める
            float min = monthlyAmountSlider.minValue;
            float max = monthlyAmountSlider.maxValue;
            int amount = settings.MonthlyAmount;

            if (amount < min) amount = Mathf.RoundToInt(min);
            if (amount > max) amount = Mathf.RoundToInt(max);

            monthlyAmountSlider.value = amount;
        }

        UpdateMonthlyAmountText(settings.MonthlyAmount);

        // リスクタイプ（Dropdown: 0=低,1=中,2=高）
        if (riskDropdown != null)
        {
            int index = RiskTypeToIndex(settings.RiskType);
            riskDropdown.value = index;
            riskDropdown.RefreshShownValue();
        }
    }

    /// <summary>
    /// スライダーの値が変化したときに呼ばれる。
    /// </summary>
    /// <param name="value">スライダーのfloat値</param>
    public void OnMonthlyAmountSliderChanged(float value)
    {
        if (!isInitialized || settings == null)
            return;

        int amount = Mathf.RoundToInt(value);
        settings.SetMonthlyAmount(amount);
        UpdateMonthlyAmountText(amount);
    }

    /// <summary>
    /// 毎月額表示テキストを更新する。
    /// </summary>
    private void UpdateMonthlyAmountText(int amount)
    {
        if (monthlyAmountValueText != null)
        {
            monthlyAmountValueText.text = $"{amount.ToString("N0")} 円 / 月";
        }
    }

    /// <summary>
    /// リスクタイプを選ぶDropdownの値が変化したときに呼ばれる。
    /// </summary>
    /// <param name="index">0:低,1:中,2:高</param>
    public void OnRiskDropdownChanged(int index)
    {
        if (!isInitialized || settings == null)
            return;

        settings.SetRiskTypeByIndex(index);
    }

    /// <summary>
    /// リスクタイプ → Dropdownインデックス変換。
    /// </summary>
    private int RiskTypeToIndex(RiskType risk)
    {
        switch (risk)
        {
            case RiskType.Low:
                return 0;
            case RiskType.Medium:
                return 1;
            case RiskType.High:
                return 2;
            default:
                return 1;
        }
    }

    /// <summary>
    /// 「タイトルにもどる」ボタンなどから呼ぶ想定。
    /// </summary>
    public void OnClickBackToTitle()
    {
        if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
        }
        else
        {
            Debug.LogWarning("SettingsUIController: titleSceneName が設定されていません。");
        }
    }
}

