using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EconomicForecastMeterUI : MonoBehaviour
{
    [Header("傾向メーター（-1..+1）")]
    [SerializeField] private Slider tendencySlider;
    [SerializeField] private TMP_Text tendencyLabel;

    [Header("信頼度（0..1）")]
    [SerializeField] private Slider confidenceSlider;
    [SerializeField] private TMP_Text confidenceLabel;

    [Header("ショック警戒（任意）")]
    [SerializeField] private GameObject shockWarningRoot;
    [SerializeField] private TMP_Text shockWarningLabel;

    [Header("表示フォーマット")]
    [SerializeField] private string tendencyFormat = "傾向: {0}";
    [SerializeField] private string confidenceFormat = "信頼度: {0:0}%";

    [Header("矢印のしきい値（傾向値の絶対値）")]
    [Range(0f, 0.5f)]
    [SerializeField] private float flatThreshold = 0.10f;

    [Range(0.1f, 0.9f)]
    [SerializeField] private float diagonalThreshold = 0.35f;

    [Header("スライダーのレンジ")]
    [SerializeField] private float tendencyMin = -1f;
    [SerializeField] private float tendencyMax = 1f;

    [Header("UI操作可否")]
    [Tooltip("プレイヤーがスライダーを触れないようにする（表示専用）")]
    [SerializeField] private bool lockUserInteraction = true;

    private void Awake()
    {
        ApplyInteractable();
    }

    private void OnValidate()
    {
        // Inspector変更時にも反映（エディタ上で便利）
        ApplyInteractable();
    }

    private void ApplyInteractable()
    {
        if (!lockUserInteraction) return;

        if (tendencySlider != null) tendencySlider.interactable = false;
        if (confidenceSlider != null) confidenceSlider.interactable = false;
    }

    private void Reset()
    {
        tendencyMin = -1f;
        tendencyMax = 1f;
        flatThreshold = 0.10f;
        diagonalThreshold = 0.35f;
        lockUserInteraction = true;
    }

    public void SetForecast(float tendency, float confidence, bool shockWarning)
    {
        // 念のため毎回ロック（Prefab差し替え/再有効化対策）
        ApplyInteractable();

        if (tendencySlider != null)
        {
            tendencySlider.minValue = tendencyMin;
            tendencySlider.maxValue = tendencyMax;
            tendencySlider.value = Mathf.Clamp(tendency, tendencyMin, tendencyMax);
        }

        if (confidenceSlider != null)
        {
            confidenceSlider.minValue = 0f;
            confidenceSlider.maxValue = 1f;
            confidenceSlider.value = Mathf.Clamp01(confidence);
        }

        string arrow = ToArrow(tendency);

        if (tendencyLabel != null) tendencyLabel.text = string.Format(tendencyFormat, arrow);
        if (confidenceLabel != null) confidenceLabel.text = string.Format(confidenceFormat, Mathf.Clamp01(confidence) * 100f);

        if (shockWarningRoot != null) shockWarningRoot.SetActive(shockWarning);
        if (shockWarningLabel != null) shockWarningLabel.text = shockWarning ? "ショック警戒" : "";
    }

    private string ToArrow(float tendency)
    {
        float range = Mathf.Max(0.0001f, tendencyMax - tendencyMin);
        float normalized = (tendency - tendencyMin) / range; // 0..1
        float centered = (normalized - 0.5f) * 2f;           // -1..+1

        float abs = Mathf.Abs(centered);
        float flat = Mathf.Clamp(flatThreshold, 0f, 0.5f);
        float diag = Mathf.Max(flat + 0.0001f, diagonalThreshold);

        if (abs <= flat) return "(→)";

        if (centered > 0f)
        {
            if (abs < diag) return "(↗)";
            return "(↑)";
        }
        else
        {
            if (abs < diag) return "(↘)";
            return "(↓)";
        }
    }
}
