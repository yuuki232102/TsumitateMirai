using UnityEngine;
using UnityEngine.UI;   // Toggle, Slider
using TMPro;            // TMP_Text

public class SimulationSceneManager : MonoBehaviour
{
    [Header("年数表示 UI")]
    [SerializeField] private TMP_Text yearText;              // 0年目 / 15年

    [Header("資産表示 UI")]
    [SerializeField] private TMP_Text currentAssetText;      // 「現在の資産」の数値部分

    [Header("積立額 UI")]
    [SerializeField] private TMP_Text monthlyAmountText;     // 「10,000円」
    [SerializeField] private Slider monthlyAmountSlider;   // 毎月のつみたて額スライダー

    [Header("リスク選択 UI")]
    [SerializeField] private Toggle riskLowToggle;         // 低リスク
    [SerializeField] private Toggle riskMiddleToggle;      // 中リスク
    [SerializeField] private Toggle riskHighToggle;        // 高リスク
    [SerializeField] private TMP_Text riskLabelText;         // 「リスクタイプ：◯◯」

    [Header("年数設定")]
    [SerializeField] private int maxYear = 15;           // 最後は15年目
    [SerializeField] private int currentYear = 0;            // 0年目スタート

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 1000;    // ★初期毎月つみたて額
    [SerializeField] private int monthlyStep = 1000;    // スライダー刻み
    [SerializeField] private int minMonthlyAmount = 1000;    // 最低額
    [SerializeField] private int maxMonthlyAmount = 100000;  // 最大額

    [Header("資産状態（シミュレーション用）")]
    [SerializeField] private int assetAtStartOfYear = 0;     // 今年の年初資産
    [SerializeField] private int currentAsset = 0;     // 表示用の「現在の資産」
    [SerializeField] private int totalPrincipal = 0;     // これまでの元本合計
    private int totalElapsedMonths = 0;                      // シミュレーション全体で経過した月数

    [Header("リスク設定")]
    // 0 = 低リスク, 1 = 中リスク, 2 = 高リスク
    [SerializeField] private int currentRiskType = 1;

    [Header("リスク別期待リターン（年率）")]
    [SerializeField] private float lowRiskReturnRate = 0.02f; // 年率2%
    [SerializeField] private float middleRiskReturnRate = 0.04f; // 年率4%
    [SerializeField] private float highRiskReturnRate = 0.06f; // 年率6%

    [Header("グラフ UI")]
    [SerializeField] private SimulationGraphUI graphUI;

    [Header("結果ログ UI")]
    [SerializeField] private SimulationLogUI logUI;

    // 内部フラグ
    private bool isUpdatingMonthlySlider = false;
    private bool hasRiskCallbackInitialized = false;

    //==================================================
    // 初期化
    //==================================================
    private void Start()
    {
        // 年数の初期化
        currentYear = Mathf.Clamp(currentYear, 0, maxYear);

        // monthlyStep が 0 以下だと困るので保険
        if (monthlyStep <= 0)
        {
            monthlyStep = 1000;
        }

        // ★ 資産系は毎回 0 からスタートさせる（0年目の初期資産は 0 円）
        assetAtStartOfYear = 0;
        currentAsset = 0;
        totalPrincipal = 0;
        totalElapsedMonths = 0;
        UpdateCurrentAssetText();     // 一旦「0円」と表示

        // 積立額を範囲内 & 刻みにスナップ
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        // スライダー初期化
        if (monthlyAmountSlider != null)
        {
            monthlyAmountSlider.minValue = minMonthlyAmount;
            monthlyAmountSlider.maxValue = maxMonthlyAmount;
            monthlyAmountSlider.wholeNumbers = true;

            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }

        // ラベル類の更新
        UpdateYearText();
        UpdateMonthlyAmountText();
        UpdateRiskLabel();
        UpdateRiskUIInteractable();

        // ★ 0年目の初期「現在の資産」は、スライダーの値と同じにしておく仕様
        currentAsset = monthlyAmount;
        UpdateCurrentAssetText();

        // グラフ初期化
        if (graphUI != null)
        {
            graphUI.ResetGraph();
            graphUI.AddPoint(currentYear, currentAsset);   // 0年目の点
        }

        // ログ初期化
        if (logUI != null)
        {
            logUI.ClearAll();
        }
    }

    //==================================================
    // 年を進める（「次の年へ ▶」ボタン）
    //==================================================
    public void OnClickNextYear()
    {
        if (currentYear >= maxYear)
        {
            Debug.Log("これ以上進めません（最終年です）");
            return;
        }

        // 今の設定で「1年（12ヶ月）」分シミュレーションして、
        // 12ヶ月分の結果をログに積み上げる
        SimulateOneYear();

        // 年を進める
        currentYear++;

        // 表示更新
        UpdateYearText();
        UpdateRiskUIInteractable();
    }

    private void UpdateYearText()
    {
        if (yearText != null)
        {
            yearText.text = $"{currentYear}年目 / {maxYear}年";
        }
    }

    //==================================================
    // 積立額（毎月）
    //==================================================
    private void UpdateMonthlyAmountText()
    {
        if (monthlyAmountText != null)
        {
            monthlyAmountText.text = $"{monthlyAmount.ToString("N0")}円";
        }
    }

    public void OnClickIncreaseMonthly()
    {
        monthlyAmount = Mathf.Min(monthlyAmount + monthlyStep, maxMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        UpdateMonthlyAmountText();

        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }

        // プレビュー更新
        UpdateCurrentYearPreviewAsset();
    }

    public void OnClickDecreaseMonthly()
    {
        monthlyAmount = Mathf.Max(monthlyAmount - monthlyStep, minMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        UpdateMonthlyAmountText();

        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }

        // プレビュー更新
        UpdateCurrentYearPreviewAsset();
    }

    // スライダーから呼ぶ
    public void OnMonthlySliderChanged(float sliderValue)
    {
        if (isUpdatingMonthlySlider) return;

        int snapped = Mathf.RoundToInt(sliderValue / (float)monthlyStep) * monthlyStep;
        snapped = Mathf.Clamp(snapped, minMonthlyAmount, maxMonthlyAmount);

        monthlyAmount = snapped;
        UpdateMonthlyAmountText();

        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }

        // プレビュー更新
        UpdateCurrentYearPreviewAsset();
    }

    public int GetMonthlyAmount() => monthlyAmount;

    //==================================================
    // 現在の資産表示
    //==================================================
    private void UpdateCurrentAssetText()
    {
        if (currentAssetText != null)
        {
            currentAssetText.text = $"{currentAsset.ToString("N0")}円";
        }
    }

    public int GetCurrentAsset() => currentAsset;
    public int GetTotalPrincipal() => totalPrincipal;
    public int GetCurrentYear() => currentYear;
    public int GetCurrentRiskType() => currentRiskType;

    //==================================================
    // リスクタイプ関連
    //==================================================
    private bool CanChangeRiskThisYear()
    {
        // 0年目・5年目・10年目だけ変更可能
        return currentYear == 0 || currentYear == 5 || currentYear == 10;
    }

    private void UpdateRiskUIInteractable()
    {
        bool canEdit = CanChangeRiskThisYear();

        if (riskLowToggle != null) riskLowToggle.interactable = canEdit;
        if (riskMiddleToggle != null) riskMiddleToggle.interactable = canEdit;
        if (riskHighToggle != null) riskHighToggle.interactable = canEdit;
    }

    public void OnSelectRiskLow(bool isOn)
    {
        if (!isOn) return;

        currentRiskType = 0;
        UpdateRiskLabel();

        if (!hasRiskCallbackInitialized)
        {
            hasRiskCallbackInitialized = true;
            return;
        }

        UpdateCurrentYearPreviewAsset();
        Debug.Log("リスクタイプ：低リスク");
    }

    public void OnSelectRiskMiddle(bool isOn)
    {
        if (!isOn) return;

        currentRiskType = 1;
        UpdateRiskLabel();

        if (!hasRiskCallbackInitialized)
        {
            hasRiskCallbackInitialized = true;
            return;
        }

        UpdateCurrentYearPreviewAsset();
        Debug.Log("リスクタイプ：中リスク");
    }

    public void OnSelectRiskHigh(bool isOn)
    {
        if (!isOn) return;

        currentRiskType = 2;
        UpdateRiskLabel();

        if (!hasRiskCallbackInitialized)
        {
            hasRiskCallbackInitialized = true;
            return;
        }

        UpdateCurrentYearPreviewAsset();
        Debug.Log("リスクタイプ：高リスク");
    }

    private void UpdateRiskLabel()
    {
        if (riskLabelText == null) return;

        string label;
        switch (currentRiskType)
        {
            case 0: label = "低リスク"; break;
            case 1: label = "中リスク"; break;
            case 2: label = "高リスク"; break;
            default: label = "不明"; break;
        }

        riskLabelText.text = $"リスクタイプ：{label}";
    }

    private float GetAnnualReturnRate()
    {
        switch (currentRiskType)
        {
            case 0: return lowRiskReturnRate;
            case 1: return middleRiskReturnRate;
            case 2: return highRiskReturnRate;
            default: return middleRiskReturnRate;
        }
    }

    //==================================================
    // プレビュー用：この年の設定で1年回したらいくらになりそうか
    //==================================================
    private void UpdateCurrentYearPreviewAsset()
    {
        if (currentYear == 0)
        {
            // 0年目だけは、あくまで「今の毎月のつみたて額」を見せる仕様のままにする
            currentAsset = monthlyAmount;
        }
        else
        {
            float rate = GetAnnualReturnRate();
            int simulated = SimulateYearValue(assetAtStartOfYear, monthlyAmount, rate);
            currentAsset = simulated;
        }

        UpdateCurrentAssetText();
    }

    // 実際の資産更新はしない、プレビュー専用の1年シミュレーション
    private int SimulateYearValue(int startAsset, int monthly, float annualRate)
    {
        float asset = startAsset;
        float monthlyRate = annualRate / 12f;

        for (int i = 0; i < 12; i++)
        {
            asset += monthly;
            asset *= (1f + monthlyRate);
        }

        return Mathf.RoundToInt(asset);
    }

    //==================================================
    // 本番：次の年へ押下時の1年分シミュレーション（12ヶ月＋ログ＋グラフ）
    //==================================================
    private void SimulateOneYear()
    {
        int startAsset = assetAtStartOfYear;
        int monthly = monthlyAmount;
        float rate = GetAnnualReturnRate();

        // 12ヶ月分を回しながら、毎月ログを追加
        int endAsset = SimulateOneYearWithMonthlyLog(currentYear, startAsset, monthly, rate);

        currentAsset = endAsset;
        UpdateCurrentAssetText();

        int yearlyContribution = monthly * 12;
        totalPrincipal += yearlyContribution;

        // 来年の年初資産
        assetAtStartOfYear = currentAsset;

        // グラフ用・年末の点を追加
        if (graphUI != null)
        {
            int newYear = currentYear + 1;
            graphUI.AddPoint(newYear, currentAsset);
        }
    }

    /// <summary>
    /// 実際の資産を更新しつつ、12ヶ月ぶんを計算し、月ごとにログを追加する。
    /// </summary>
    private int SimulateOneYearWithMonthlyLog(
        int yearIndex,
        int startAsset,
        int monthly,
        float annualRate
    )
    {
        float asset = startAsset;
        float monthlyRate = annualRate / 12f;

        for (int month = 1; month <= 12; month++)
        {
            asset += monthly;                 // 今月の積立
            asset *= (1f + monthlyRate);     // 利回り適用

            int assetInt = Mathf.RoundToInt(asset);
            totalElapsedMonths++;

            // ログに1行追加
            if (logUI != null)
            {
                logUI.AddMonthlyRecord(
                    totalElapsedMonths,
                    yearIndex,
                    month,
                    assetInt,
                    monthly,
                    annualRate
                );
            }
        }

        return Mathf.RoundToInt(asset);
    }
}
