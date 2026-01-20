using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 景気予測メーターUI（傾向-1..+1 と 信頼度0..1）
/// </summary>
public class EconomicForecastMeterUI : MonoBehaviour
{
    [Header("傾向メーター（-1..+1）")]
    [SerializeField] private Slider tendencySlider; // min=-1 max=+1 推奨
    [SerializeField] private TMP_Text tendencyLabel;

    [Header("信頼度（0..1）")]
    [SerializeField] private Slider confidenceSlider; // min=0 max=1 推奨
    [SerializeField] private TMP_Text confidenceLabel;

    [Header("ショック警戒（任意）")]
    [SerializeField] private GameObject shockWarningRoot; // アイコン等
    [SerializeField] private TMP_Text shockWarningLabel;

    [Header("表示フォーマット")]
    [SerializeField] private string confidenceFormat = "信頼度: {0:0}%";
    [SerializeField] private string tendencyFormat = "傾向: {0}";

    public void SetForecast(float tendencyMinus1ToPlus1, float confidence01, bool shockWarning, string label)
    {
        float t = Mathf.Clamp(tendencyMinus1ToPlus1, -1f, 1f);
        float c = Mathf.Clamp01(confidence01);

        if (tendencySlider != null) tendencySlider.value = t;
        if (confidenceSlider != null) confidenceSlider.value = c;

        if (tendencyLabel != null)
        {
            string textLabel = string.IsNullOrEmpty(label) ? "平常" : label;
            tendencyLabel.text = string.Format(tendencyFormat, textLabel);
        }

        if (confidenceLabel != null)
        {
            confidenceLabel.text = string.Format(confidenceFormat, c * 100f);
        }

        if (shockWarningRoot != null) shockWarningRoot.SetActive(shockWarning);
        if (shockWarningLabel != null) shockWarningLabel.text = shockWarning ? "ショック警戒" : "";
    }
}
