using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationSceneManager : MonoBehaviour
{
    //========================
    //  UI 参照
    //========================

    [Header("年数表示 UI")]
    [SerializeField] private TMP_Text yearText;                 // Text_YearCounter

    [Header("資産表示 UI")]
    [SerializeField] private TMP_Text currentAssetText;         // Text_CurrentAssetValue

    [Header("積立額 UI")]
    [SerializeField] private TMP_Text monthlyAmountText;        // Text_MonthlyAmount
    [SerializeField] private Slider monthlyAmountSlider;        // Slider_MonthlyAmount

    [Header("リスク選択 UI")]
    [SerializeField] private Toggle riskLowToggle;
    [SerializeField] private Toggle riskMiddleToggle;
    [SerializeField] private Toggle riskHighToggle;
    [SerializeField] private TMP_Text riskLabelText;

    [Header("年数設定")]
    [SerializeField] private int maxYear = 15;
    [SerializeField] private int currentYear = 0;

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 1000;
    [SerializeField] private int monthlyStep = 1000;
    [SerializeField] private int minMonthlyAmount = 1000;
    [SerializeField] private int maxMonthlyAmount = 33000;

    [Header("資産状態（シミュレーション用）")]
    [SerializeField] private int assetAtStartOfYear = 0;        // その年の開始時点の資産
    [SerializeField] private int currentAsset = 0;              // 表示用 現在の資産（プレビュー込み）
    [SerializeField] private int totalPrincipal = 0;            // 積み立てた元本の合計

    [Header("リスク設定")]
    [SerializeField] private int currentRiskType = 1;           // 0:低 1:中 2:高

    [Header("リスク別期待リターン（年率）")]
    [SerializeField] private float lowRiskReturnRate = 0.02f;
    [SerializeField] private float middleRiskReturnRate = 0.04f;
    [SerializeField] private float highRiskReturnRate = 0.06f;

    [Header("長期グラフ UI（年ごとの期末資産）")]
    [SerializeField] private SimulationGraphUI graphUI;

    [Header("月別グラフ UI（1年の12カ月）")]
    [SerializeField] private MonthlyGraphUI monthlyGraphUI;
    [SerializeField] private Slider detailYearSlider;
    [SerializeField] private TMP_Text detailYearLabel;

    [Header("グラフ表示切り替え")]
    [SerializeField] private GameObject yearlyGraphRoot;
    [SerializeField] private GameObject monthlyGraphRoot;
    [SerializeField] private Toggle graphYearlyToggle;
    [SerializeField] private Toggle graphMonthlyToggle;

    [Header("結果ログ UI")]
    [SerializeField] private ScrollRect logScrollView;
    [SerializeField] private TMP_Text logEntryPrefab;

    //========================
    //  内部データ
    //========================

    // 各年の期末資産
    private readonly List<int> yearEndAssets = new List<int>();

    // 年ごとの「12ヶ月の資産推移」
    private readonly List<List<int>> monthlyAssetsPerYear = new List<List<int>>();

    // スライダー更新中フラグ（無限ループ防止）
    private bool isUpdatingMonthlySlider = false;

    // グラフトグルの内部更新フラグ
    private bool isUpdatingGraphToggles = false;

    //========================
    //  Unity ライフサイクル
    //========================

    private void Start()
    {
        // --- 積立額・スライダー初期化 ---
        if (monthlyStep <= 0) monthlyStep = 1000;

        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        if (monthlyAmountSlider != null)
        {
            monthlyAmountSlider.minValue = minMonthlyAmount;
            monthlyAmountSlider.maxValue = maxMonthlyAmount;
            monthlyAmountSlider.wholeNumbers = true;

            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;

            // インスペクターで既に設定していても二重に壊れる事はない
            monthlyAmountSlider.onValueChanged.AddListener(OnMonthlySliderChanged);
        }

        // --- リスクトグル ---
        if (riskLowToggle != null)
            riskLowToggle.onValueChanged.AddListener(isOn => { if (isOn) SetRiskType(0); });
        if (riskMiddleToggle != null)
            riskMiddleToggle.onValueChanged.AddListener(isOn => { if (isOn) SetRiskType(1); });
        if (riskHighToggle != null)
            riskHighToggle.onValueChanged.AddListener(isOn => { if (isOn) SetRiskType(2); });

        // --- グラフトグル ---
        if (graphYearlyToggle != null)
            graphYearlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(true, isOn));
        if (graphMonthlyToggle != null)
            graphMonthlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(false, isOn));

        // --- 詳細年スライダー ---
        if (detailYearSlider != null)
        {
            detailYearSlider.minValue = 0;
            detailYearSlider.maxValue = 0;
            detailYearSlider.wholeNumbers = true;
            detailYearSlider.value = 0;
            detailYearSlider.onValueChanged.AddListener(OnDetailYearSliderChanged);
        }

        // --- 初期状態の資産 ---
        currentYear = 0;
        assetAtStartOfYear = 0;
        currentAsset = 0;
        totalPrincipal = 0;

        yearEndAssets.Clear();
        monthlyAssetsPerYear.Clear();

        // --- グラフクリア ---
        if (graphUI != null) graphUI.ResetGraph();
        if (monthlyGraphUI != null) monthlyGraphUI.SetMonthlyData(null);

        // --- グラフモード初期値（年別オン） ---
        isUpdatingGraphToggles = true;
        if (graphYearlyToggle != null) graphYearlyToggle.isOn = true;
        if (graphMonthlyToggle != null) graphMonthlyToggle.isOn = false;
        isUpdatingGraphToggles = false;
        ApplyGraphMode(true);

        // --- UI の初期表示 ---
        RefreshAllUI();
    }

    //========================
    //  UI 更新系
    //========================

    private void RefreshAllUI()
    {
        UpdateYearText();
        UpdateCurrentAssetText();
        UpdateMonthlyAmountText();
        UpdateRiskUI();
    }

    private void UpdateYearText()
    {
        if (yearText != null)
        {
            yearText.text = $"{currentYear}年目 / {maxYear}年";
        }
    }

    private void UpdateCurrentAssetText()
    {
        if (currentAssetText != null)
        {
            currentAssetText.text = $"{currentAsset.ToString("N0")}円";
        }
    }

    private void UpdateMonthlyAmountText()
    {
        if (monthlyAmountText != null)
        {
            monthlyAmountText.text = $"{monthlyAmount.ToString("N0")}円";
        }
    }

    private void UpdateRiskUI()
    {
        if (riskLowToggle != null) riskLowToggle.isOn = (currentRiskType == 0);
        if (riskMiddleToggle != null) riskMiddleToggle.isOn = (currentRiskType == 1);
        if (riskHighToggle != null) riskHighToggle.isOn = (currentRiskType == 2);

        if (riskLabelText != null)
        {
            string label = "中リスク";
            if (currentRiskType == 0) label = "低リスク";
            else if (currentRiskType == 2) label = "高リスク";
            riskLabelText.text = label;
        }
    }

    //========================
    //  リスク変更
    //========================

    private void SetRiskType(int type)
    {
        if (currentRiskType == type) return;
        currentRiskType = type;
        UpdateRiskUI();
    }

    //========================
    //  スライダー変更（毎月のつみたて額）
    //========================

    public void OnMonthlySliderChanged(float value)
    {
        if (isUpdatingMonthlySlider) return;

        int snapped = Mathf.RoundToInt(value / monthlyStep) * monthlyStep;
        snapped = Mathf.Clamp(snapped, minMonthlyAmount, maxMonthlyAmount);

        monthlyAmount = snapped;

        // スライダーをスナップ値に戻す
        isUpdatingMonthlySlider = true;
        monthlyAmountSlider.value = monthlyAmount;
        isUpdatingMonthlySlider = false;

        // テキスト更新
        UpdateMonthlyAmountText();

        // ★ここが重要★
        // 「次の年へ」を押した結果の資産 assetAtStartOfYear に
        // 今年の積立額1回分だけ足した値をプレビューとして表示する
        currentAsset = assetAtStartOfYear + monthlyAmount;

        UpdateCurrentAssetText();
    }

    //========================
    //  次の年へボタン
    //========================

    public void OnClickNextYear()
    {
        if (currentYear >= maxYear) return;

        // この年の 12ヶ月をシミュレート
        List<int> monthlyAssets = new List<int>();
        float monthlyRate = GetMonthlyRate();

        int asset = assetAtStartOfYear;

        for (int month = 0; month < 12; month++)
        {
            asset += monthlyAmount;          // 積立
            totalPrincipal += monthlyAmount;

            float afterReturn = asset * (1f + monthlyRate);
            asset = Mathf.RoundToInt(afterReturn);

            monthlyAssets.Add(asset);
        }

        // 年末資産を確定
        assetAtStartOfYear = asset;
        currentAsset = asset;
        currentYear++;

        // データを保存
        yearEndAssets.Add(asset);
        monthlyAssetsPerYear.Add(monthlyAssets);

        // グラフ更新（年別）
        if (graphUI != null)
        {
            graphUI.ResetGraph();
            for (int i = 0; i < yearEndAssets.Count; i++)
            {
                graphUI.AddPoint(i + 1, yearEndAssets[i]); // 1年〜で表示
            }
        }

        // グラフ更新（月別）…直近の年を表示
        if (monthlyGraphUI != null)
        {
            int idx = Mathf.Clamp(currentYear - 1, 0, monthlyAssetsPerYear.Count - 1);
            monthlyGraphUI.SetMonthlyData(monthlyAssetsPerYear[idx]);
        }

        // 詳細年スライダー範囲を更新
        if (detailYearSlider != null)
        {
            detailYearSlider.maxValue = Mathf.Max(0, yearEndAssets.Count - 1);
            detailYearSlider.value = yearEndAssets.Count - 1;
        }

        UpdateDetailYearLabel();
        RefreshAllUI();

        // ログも追加（任意）
        AppendLog($"{currentYear}年目終了 : 資産 {currentAsset.ToString("N0")}円");
    }

    private float GetMonthlyRate()
    {
        float yearly = middleRiskReturnRate;

        if (currentRiskType == 0) yearly = lowRiskReturnRate;
        else if (currentRiskType == 2) yearly = highRiskReturnRate;

        // 年率 → 月率
        return Mathf.Pow(1f + yearly, 1f / 12f) - 1f;
    }

    //========================
    //  月別グラフの年変更
    //========================

    private void OnDetailYearSliderChanged(float value)
    {
        int index = Mathf.RoundToInt(value);
        UpdateDetailYearLabel();

        if (index < 0 || index >= monthlyAssetsPerYear.Count) return;

        if (monthlyGraphUI != null)
        {
            monthlyGraphUI.SetMonthlyData(monthlyAssetsPerYear[index]);
        }
    }

    private void UpdateDetailYearLabel()
    {
        if (detailYearLabel == null || detailYearSlider == null) return;

        int idx = Mathf.RoundToInt(detailYearSlider.value);
        detailYearLabel.text = $"{idx + 1}年目の月別グラフ";
    }

    //========================
    //  グラフ表示切り替え
    //========================

    private void OnGraphToggleChanged(bool yearlyToggle, bool isOn)
    {
        if (!isOn) // OFF にされた時の扱い
        {
            // どちらも OFF になるのを防ぐ
            if (!graphYearlyToggle.isOn && !graphMonthlyToggle.isOn)
            {
                isUpdatingGraphToggles = true;
                if (yearlyToggle) graphYearlyToggle.isOn = true;
                else graphMonthlyToggle.isOn = true;
                isUpdatingGraphToggles = false;
            }
            return;
        }

        if (isUpdatingGraphToggles) return;

        bool showYearly = yearlyToggle;

        isUpdatingGraphToggles = true;
        if (graphYearlyToggle != null) graphYearlyToggle.isOn = showYearly;
        if (graphMonthlyToggle != null) graphMonthlyToggle.isOn = !showYearly;
        isUpdatingGraphToggles = false;

        ApplyGraphMode(showYearly);
    }

    private void ApplyGraphMode(bool showYearly)
    {
        if (yearlyGraphRoot != null) yearlyGraphRoot.SetActive(showYearly);
        if (monthlyGraphRoot != null) monthlyGraphRoot.SetActive(!showYearly);
    }

    //========================
    //  ログ出力（任意）
    //========================

    private void AppendLog(string message)
    {
        if (logScrollView == null || logEntryPrefab == null) return;

        TMP_Text entry = Instantiate(logEntryPrefab, logScrollView.content);
        entry.text = message;

        Canvas.ForceUpdateCanvases();
        logScrollView.verticalNormalizedPosition = 0f;
    }
}
