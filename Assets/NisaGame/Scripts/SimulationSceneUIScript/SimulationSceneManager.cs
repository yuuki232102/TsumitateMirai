using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationSceneManager : MonoBehaviour
{
    //==============================
    // UI 参照
    //==============================
    [Header("年数表示 UI")]
    [SerializeField] private TMP_Text yearText;

    [Header("資産表示 UI")]
    [SerializeField] private TMP_Text currentAssetText;

    [Header("積立額 UI")]
    [SerializeField] private TMP_Text monthlyAmountText;
    [SerializeField] private Slider monthlyAmountSlider;

    [Header("リスク選択 UI")]
    [SerializeField] private Toggle riskLowToggle;
    [SerializeField] private Toggle riskMiddleToggle;
    [SerializeField] private Toggle riskHighToggle;
    [SerializeField] private TMP_Text riskLabelText;

    [Header("年数設定")]
    [SerializeField] private int maxYear = 15;
    [SerializeField] private int currentYear = 0;    // 0年目スタート

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 1000;
    [SerializeField] private int monthlyStep = 1000;
    [SerializeField] private int minMonthlyAmount = 1000;
    [SerializeField] private int maxMonthlyAmount = 100000;

    [Header("資産状態（シミュレーション用）")]
    [SerializeField] private int assetAtStartOfYear = 0;
    [SerializeField] private int currentAsset = 0;
    [SerializeField] private int totalPrincipal = 0;
    private int totalElapsedMonths = 0;

    [Header("リスク設定")]
    [SerializeField] private int currentRiskType = 1; // 0:低,1:中,2:高

    [Header("リスク別期待リターン（年率）")]
    [SerializeField] private float lowRiskReturnRate = 0.02f;
    [SerializeField] private float middleRiskReturnRate = 0.04f;
    [SerializeField] private float highRiskReturnRate = 0.06f;

    [Header("長期グラフ UI（年ごとの期末資産）")]
    [SerializeField] private SimulationGraphUI graphUI;

    [Header("月別グラフ UI（1年の12ヶ月）")]
    [SerializeField] private MonthlyGraphUI detailGraphUI;
    [SerializeField] private Slider detailYearSlider;
    [SerializeField] private TMP_Text detailYearLabel;

    [Header("グラフ表示切り替え")]
    [SerializeField] private GameObject yearlyGraphRoot;
    [SerializeField] private GameObject monthlyGraphRoot;
    [SerializeField] private Toggle graphYearlyToggle;
    [SerializeField] private Toggle graphMonthlyToggle;

    [Header("結果ログ UI")]
    [SerializeField] private SimulationLogUI logUI;

    // 内部状態
    private bool isUpdatingMonthlySlider = false;
    private bool hasRiskCallbackInitialized = false;

    // [yearIndex][monthIndex] = asset
    private readonly List<List<int>> monthlyAssetHistory = new List<List<int>>();

    //==================================================
    // Awake / Start
    //==================================================
    private void Awake()
    {
        // 念のためステップが0以下にならないように
        if (monthlyStep <= 0)
            monthlyStep = 1000;

        // スライダー範囲の初期設定（アタッチされてさえいればここで必ず設定）
        if (monthlyAmountSlider != null)
        {
            monthlyAmountSlider.minValue = minMonthlyAmount;
            monthlyAmountSlider.maxValue = maxMonthlyAmount;
            monthlyAmountSlider.wholeNumbers = true;
        }
    }

    private void Start()
    {
        currentYear = Mathf.Clamp(currentYear, 0, maxYear);

        // 積立額を範囲内 & 刻みにスナップ
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        // 0年目の時点での資産は「現在の毎月つみたて額」として見せる
        assetAtStartOfYear = 0;
        totalPrincipal = 0;
        totalElapsedMonths = 0;
        currentAsset = monthlyAmount;

        // スライダー位置を反映
        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }

        // 表示更新
        UpdateYearText();
        UpdateMonthlyAmountText();
        UpdateCurrentAssetText();
        UpdateRiskLabel();
        UpdateRiskUIInteractable();

        // 長期グラフ初期化（0年目の点）
        if (graphUI != null)
        {
            graphUI.ResetGraph();
            graphUI.AddPoint(currentYear, currentAsset);
        }

        // ログ初期化
        if (logUI != null)
        {
            logUI.ClearAll();
        }

        // 月別グラフ用スライダー
        if (detailYearSlider != null)
        {
            detailYearSlider.minValue = 0;
            detailYearSlider.maxValue = maxYear;
            detailYearSlider.wholeNumbers = true;
            detailYearSlider.value = 0;
        }

        // 月別グラフ初期表示（まだデータ無し）
        UpdateDetailGraphForYear(0);

        // グラフ表示モード反映
        UpdateGraphViewMode();
    }

    //==================================================
    // 「次の年へ」ボタン
    //==================================================
    public void OnClickNextYear()
    {
        if (currentYear >= maxYear)
        {
            Debug.Log("これ以上進めません（最終年です）");
            return;
        }

        SimulateOneYear();

        currentYear++;
        UpdateYearText();
        UpdateRiskUIInteractable();
    }

    private void UpdateYearText()
    {
        if (yearText != null)
            yearText.text = $"{currentYear}年目 / {maxYear}年";
    }

    //==================================================
    // 毎月のつみたて額
    //==================================================
    private void UpdateMonthlyAmountText()
    {
        if (monthlyAmountText != null)
            monthlyAmountText.text = $"{monthlyAmount.ToString("N0")}円";
    }

    public void OnClickIncreaseMonthly()
    {
        monthlyAmount = Mathf.Min(monthlyAmount + monthlyStep, maxMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        UpdateMonthlyAmountText();
        SyncMonthlySlider();
        UpdateCurrentYearPreviewAsset();
    }

    public void OnClickDecreaseMonthly()
    {
        monthlyAmount = Mathf.Max(monthlyAmount - monthlyStep, minMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        UpdateMonthlyAmountText();
        SyncMonthlySlider();
        UpdateCurrentYearPreviewAsset();
    }

    /// <summary>
    /// スライダーの OnValueChanged に接続
    /// </summary>
    /// <summary>
    /// 毎月のつみたて額スライダーの変更コールバック
    /// （引数の sliderValue は Unity の都合で 0 になることがあるので、
    ///  実際の値は monthlyAmountSlider.value から直接読むようにする）
    /// </summary>
    public void OnMonthlySliderChanged(float _)
    {
        if (monthlyAmountSlider == null)
        {
            Debug.LogWarning("[MonthlySlider] monthlyAmountSlider が設定されていません");
            return;
        }

        // ★必ずスライダー本体から値を読む
        float sliderValue = monthlyAmountSlider.value;
        Debug.Log($"[MonthlySlider] arg= {_} / slider.value = {sliderValue}");

        if (isUpdatingMonthlySlider) return;

        // 金額にスナップ
        int snapped = Mathf.RoundToInt(sliderValue / (float)monthlyStep) * monthlyStep;
        snapped = Mathf.Clamp(snapped, minMonthlyAmount, maxMonthlyAmount);

        monthlyAmount = snapped;

        // 表示を更新
        UpdateMonthlyAmountText();
        // （slider.value は既に正しいので、ここで Sync しても OK／しなくてもほぼ同じ）
        SyncMonthlySlider();
        UpdateCurrentYearPreviewAsset();
    }



    private void SyncMonthlySlider()
    {
        if (monthlyAmountSlider == null) return;

        isUpdatingMonthlySlider = true;
        monthlyAmountSlider.value = monthlyAmount;
        isUpdatingMonthlySlider = false;
    }

    public int GetMonthlyAmount() => monthlyAmount;

    //==================================================
    // 資産表示
    //==================================================
    private void UpdateCurrentAssetText()
    {
        if (currentAssetText != null)
            currentAssetText.text = $"{currentAsset.ToString("N0")}円";
    }

    public int GetCurrentAsset() => currentAsset;
    public int GetTotalPrincipal() => totalPrincipal;
    public int GetCurrentYear() => currentYear;
    public int GetCurrentRiskType() => currentRiskType;

    //==================================================
    // リスク関連
    //==================================================
    private bool CanChangeRiskThisYear()
    {
        // 0・5・10年目だけ変更可能
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
    // プレビュー用：この年の設定で1年回したらいくらか
    //==================================================
    private void UpdateCurrentYearPreviewAsset()
    {
        if (currentYear == 0)
        {
            // 0年目だけは「現在の毎月つみたて額」をそのまま見せる
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
    // 本番シミュレーション（次の年へ）
    //==================================================
    private void SimulateOneYear()
    {
        int startAsset = assetAtStartOfYear;
        int monthly = monthlyAmount;
        float rate = GetAnnualReturnRate();

        int endAsset = SimulateOneYearWithMonthlyLog(currentYear, startAsset, monthly, rate);

        currentAsset = endAsset;
        UpdateCurrentAssetText();

        int yearlyContribution = monthly * 12;
        totalPrincipal += yearlyContribution;

        assetAtStartOfYear = currentAsset;

        if (graphUI != null)
        {
            int newYear = currentYear + 1;
            graphUI.AddPoint(newYear, currentAsset);
        }

        UpdateDetailGraphForYear(currentYear);

        if (detailYearSlider != null)
            detailYearSlider.value = currentYear;
    }

    private int SimulateOneYearWithMonthlyLog(
        int yearIndex,
        int startAsset,
        int monthly,
        float annualRate
    )
    {
        float asset = startAsset;
        float monthlyRate = annualRate / 12f;

        List<int> yearMonthlyList = new List<int>();

        for (int month = 1; month <= 12; month++)
        {
            asset += monthly;
            asset *= (1f + monthlyRate);

            int assetInt = Mathf.RoundToInt(asset);
            totalElapsedMonths++;
            yearMonthlyList.Add(assetInt);

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

        // 年別の月次履歴として保存
        if (yearIndex >= 0)
        {
            while (monthlyAssetHistory.Count <= yearIndex)
                monthlyAssetHistory.Add(new List<int>());

            monthlyAssetHistory[yearIndex] = yearMonthlyList;
        }

        return Mathf.RoundToInt(asset);
    }

    //==================================================
    // 月別グラフ表示
    //==================================================
    public void OnDetailYearSliderChanged(float sliderValue)
    {
        int yearIndex = Mathf.RoundToInt(sliderValue);
        UpdateDetailGraphForYear(yearIndex);
    }

    private void UpdateDetailGraphForYear(int yearIndex)
    {
        if (detailGraphUI == null) return;

        List<int> data = null;
        if (yearIndex >= 0 && yearIndex < monthlyAssetHistory.Count)
            data = monthlyAssetHistory[yearIndex];

        detailGraphUI.SetMonthlyData(data);

        if (detailYearLabel != null)
            detailYearLabel.text = $"{yearIndex}年目の月別推移";
    }

    //==================================================
    // グラフ表示モード切り替え
    //==================================================
    private void UpdateGraphViewMode()
    {
        bool showMonthly = (graphMonthlyToggle != null && graphMonthlyToggle.isOn);
        Debug.Log($"[GraphViewMode] showMonthly = {showMonthly}");

        if (yearlyGraphRoot != null)
            yearlyGraphRoot.SetActive(!showMonthly);

        if (monthlyGraphRoot != null)
            monthlyGraphRoot.SetActive(showMonthly);

        if (detailYearSlider != null)
            detailYearSlider.gameObject.SetActive(showMonthly);

        if (detailYearLabel != null)
            detailYearLabel.gameObject.SetActive(showMonthly);
    }


    public void OnSelectGraphYearly(bool _)
    {
        // 今のトグルの状態をログに出しておく（デバッグ用）
        bool yearly = graphYearlyToggle != null && graphYearlyToggle.isOn;
        bool monthly = graphMonthlyToggle != null && graphMonthlyToggle.isOn;
        Debug.Log($"[GraphToggle] Yearly toggled -> yearly={yearly}, monthly={monthly}");

        // 引数の true/false に関係なく毎回モード更新
        UpdateGraphViewMode();
    }

    public void OnSelectGraphMonthly(bool _)
    {
        bool yearly = graphYearlyToggle != null && graphYearlyToggle.isOn;
        bool monthly = graphMonthlyToggle != null && graphMonthlyToggle.isOn;
        Debug.Log($"[GraphToggle] Monthly toggled -> yearly={yearly}, monthly={monthly}");

        UpdateGraphViewMode();
    }


}
