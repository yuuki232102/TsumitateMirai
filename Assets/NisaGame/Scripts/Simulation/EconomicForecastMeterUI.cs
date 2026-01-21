using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 景気予測メーターUI
/// - 傾向: -1..+1（または任意レンジ）を矢印で表現（↑ ↓ → ↗ ↘）
/// - 信頼度: 0..1
/// - ショック警戒（任意）
/// </summary>
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
    [Tooltip("例: 傾向: {0}   {0}には矢印が入る")]
    [SerializeField] private string tendencyFormat = "傾向: {0}";
    [Tooltip("例: 信頼度: {0:0}%")]
    [SerializeField] private string confidenceFormat = "信頼度: {0:0}%";

    [Header("矢印のしきい値（傾向値の絶対値）")]
    [Tooltip("この範囲内は横（→）。例: 0.10 なら -0.10〜+0.10 は →")]
    [Range(0f, 0.5f)]
    [SerializeField] private float flatThreshold = 0.10f;

    [Tooltip("斜め（↗/↘）になる境界。例: 0.35 なら 0.10〜0.35 は ↗、0.35以上は ↑")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float diagonalThreshold = 0.35f;

    [Header("スライダーのレンジ")]
    [Tooltip("傾向スライダーの最小値/最大値。通常は -1〜+1")]
    [SerializeField] private float tendencyMin = -1f;
    [SerializeField] private float tendencyMax = 1f;

    private void Reset()
    {
        // 追加した直後に事故りにくい初期値
        tendencyMin = -1f;
        tendencyMax = 1f;
        flatThreshold = 0.10f;
        diagonalThreshold = 0.35f;
    }

    /// <summary>
    /// 外部から呼ぶAPI（SimulationSceneManager から呼ぶ想定）
    /// tendency: -1..+1 推奨（レンジは inspector で変更可）
    /// confidence: 0..1
    /// shockWarning: trueなら警告表示
    /// </summary>
    public void SetForecast(float tendency, float confidence, bool shockWarning)
    {
        // --- tendency slider ---
        if (tendencySlider != null)
        {
            tendencySlider.minValue = tendencyMin;
            tendencySlider.maxValue = tendencyMax;
            tendencySlider.value = Mathf.Clamp(tendency, tendencyMin, tendencyMax);
        }

        // --- confidence slider ---
        if (confidenceSlider != null)
        {
            confidenceSlider.minValue = 0f;
            confidenceSlider.maxValue = 1f;
            confidenceSlider.value = Mathf.Clamp01(confidence);
        }

        // --- labels ---
        string arrow = ToArrow(tendency);

        if (tendencyLabel != null)
        {
            tendencyLabel.text = string.Format(tendencyFormat, arrow);
        }

        if (confidenceLabel != null)
        {
            confidenceLabel.text = string.Format(confidenceFormat, Mathf.Clamp01(confidence) * 100f);
        }

        // --- shock warning ---
        if (shockWarningRoot != null)
        {
            shockWarningRoot.SetActive(shockWarning);
        }

        if (shockWarningLabel != null)
        {
            shockWarningLabel.text = shockWarning ? "ショック警戒" : "";
        }
    }

    /// <summary>
    /// 傾向値から矢印へ変換
    ///  - flatThreshold以内: →（横）
    ///  - それ以上で diagonalThreshold未満: ↗ / ↘（斜め）
    ///  - diagonalThreshold以上: ↑ / ↓（上下）
    /// </summary>
    private string ToArrow(float tendency)
    {
        float v = tendency;

        // -1..+1に正規化したい場合：レンジが違っても大丈夫なように正規化
        float range = Mathf.Max(0.0001f, tendencyMax - tendencyMin);
        float normalized = (v - tendencyMin) / range;     // 0..1
        float centered = (normalized - 0.5f) * 2f;        // -1..+1

        float abs = Mathf.Abs(centered);
        float flat = Mathf.Clamp(flatThreshold, 0f, 0.5f);
        float diag = Mathf.Max(flat + 0.0001f, diagonalThreshold);

        if (abs <= flat) return "(→)";

        if (centered > 0f)
        {
            // 上方向
            if (abs < diag) return "(↗)";
            return "(↑)";
        }
        else
        {
            // 下方向
            if (abs < diag) return "(↘)";
            return "(↓)";
        }
    }
}
