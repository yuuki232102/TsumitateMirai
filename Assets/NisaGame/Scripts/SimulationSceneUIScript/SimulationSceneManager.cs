using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SimulationSceneManager : MonoBehaviour
{
    //========================
    //  UI 参照
    //========================
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

    [Header("景気予測メーター UI")]
    [SerializeField] private EconomicForecastMeterUI forecastMeterUI;

    //========================
    //  ロード画面（同一シーン内オーバーレイ：方式A）
    //========================
    [Header("ロード画面UI（方式A：同一シーン内オーバーレイ）")]
    [SerializeField] private LoadingScreenUI loadingScreenUI;

    [Tooltip("次の年へ押下時にロード画面を表示する秒数")]
    [SerializeField] private float nextYearLoadingSeconds = 1.5f;

    [Header("任意：次の年へボタン（連打防止）")]
    [SerializeField] private Button nextYearButton;

    private bool isBusy = false;

    //========================
    //  ResultScene 遷移
    //========================
    [Header("ResultScene 遷移")]
    [SerializeField] private string resultSceneName = "ResultScene";

    [Header("結果へボタン（15年到達で有効化）")]
    [SerializeField] private Button goResultButton;

    private bool isResultReady = false;

    //========================
    //  モード設定
    //========================
    [Header("Mode Settings (Optional)")]
    [SerializeField] private ModeConfig normalConfig;
    [SerializeField] private ModeConfig hardConfig;
    [SerializeField] private ModeConfig chaosConfig;

    //========================
    //  設定
    //========================
    [Header("年数設定")]
    [SerializeField] private int maxYear = 15;
    [SerializeField] private int currentYear = 0;

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 1000;
    [SerializeField] private int monthlyStep = 1000;
    [SerializeField] private int minMonthlyAmount = 1000;
    [SerializeField] private int maxMonthlyAmount = 33000;

    [Header("資産状態（シミュレーション用）")]
    [SerializeField] private int assetAtStartOfYear = 0; // 年初（0月点）
    [SerializeField] private int currentAsset = 0;
    [SerializeField] private int totalPrincipal = 0;

    [Header("リスク設定（確定値）")]
    [Tooltip("0:低 1:中 2:高（確定値）")]
    [SerializeField] private int currentRiskType = 1;

    [Header("リスク別期待リターン（年率）")]
    [SerializeField] private float lowRiskReturnRate = 0.02f;
    [SerializeField] private float middleRiskReturnRate = 0.04f;
    [SerializeField] private float highRiskReturnRate = 0.06f;

    [Header("景気イベント補正（月利に加算）")]
    [SerializeField] private float boomMonthlyDelta = 0.01f;
    [SerializeField] private float recessionMonthlyDelta = -0.01f;
    [SerializeField] private float shockMonthlyDelta = -0.05f;

    //========================
    //  ★追加：マイナスイベント低減（不景気・ショックのみ）
    //========================
    [Header("マイナスイベント低減（不景気・ショックのみ）")]
    [Tooltip("低リスク時のマイナスイベント低減率（例：0.20 = 20%低減）")]
    [Range(0f, 1f)]
    [SerializeField] private float negativeEventReductionLow = 0.20f;

    [Tooltip("中リスク時のマイナスイベント低減率（例：0.10 = 10%低減）")]
    [Range(0f, 1f)]
    [SerializeField] private float negativeEventReductionMiddle = 0.10f;

    [Tooltip("高リスク時のマイナスイベント低減率（例：0.00 = 低減なし）")]
    [Range(0f, 1f)]
    [SerializeField] private float negativeEventReductionHigh = 0.00f;

    [Header("イベント月のノイズ（平常は0固定）")]
    [SerializeField] private float lowRiskNoiseAmp = 0.01f;
    [SerializeField] private float middleRiskNoiseAmp = 0.02f;
    [SerializeField] private float highRiskNoiseAmp = 0.04f;

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
    //  モードで調整する「発生確率/重み」
    //========================
    [Header("Event Probability (Mode Adjustable)")]
    [SerializeField, Range(0f, 1f)] private float chanceAnyEvent = 1f;
    [SerializeField, Range(0f, 1f)] private float chanceSecondEvent = 0.2f;

    [Header("Event Type Weights (Mode Adjustable)")]
    [SerializeField] private float boomWeight = 0.50f;
    [SerializeField] private float recessionWeight = 0.38f;
    [SerializeField] private float shockWeight = 0.12f;

    //========================
    //  リスク変更制限＆ペナルティ（方式A）
    //========================
    [Header("方式A：リスク変更制限")]
    [SerializeField] private int maxRiskChanges = 4;

    [Header("方式A：変更確定後 1ヶ月ペナルティ（月利に加算）")]
    [SerializeField] private float penaltyLow = -0.001f;
    [SerializeField] private float penaltyMiddle = -0.002f;
    [SerializeField] private float penaltyHigh = -0.003f;

    //========================
    //  内部データ
    //========================
    private readonly List<int> yearEndAssets = new List<int>();                       // 年末資産（index0=1年目）
    private readonly List<List<int>> monthlyAssetsPerYear = new List<List<int>>();    // 各年12個（1月末〜12月末）
    private readonly List<EconomicEventType[]> yearlyEvents = new List<EconomicEventType[]>(); // 各年イベント12
    private readonly List<int> yearStartAssets = new List<int>();                     // 各年年初（0月点）
    private readonly List<string> yearEventLabels = new List<string>();               // 各年ラベル（index0=1年目）

    private bool isUpdatingMonthlySlider = false;
    private bool isUpdatingGraphToggles = false;
    private bool isUpdatingRiskToggles = false;

    private int selectedRiskType = 1; // 予約リスク
    private int riskChangeCount = 0;

    private int pendingPenaltyMonths = 0;
    private float pendingPenaltyRate = 0f;

    private void Start()
    {
        // ========================
        // ボタン配線（入れ忘れ対策）
        // ========================
        if (nextYearButton != null)
        {
            nextYearButton.onClick.RemoveListener(OnClickNextYear);
            nextYearButton.onClick.AddListener(OnClickNextYear);
        }
        if (goResultButton != null)
        {
            goResultButton.onClick.RemoveListener(OnClickGoResult);
            goResultButton.onClick.AddListener(OnClickGoResult);
        }

        // モード適用（まず最初に）
        ApplySelectedModeIfAny();

        // ロードUIは開始時に確実に非表示
        if (loadingScreenUI != null) loadingScreenUI.HideInstant();

        // 結果へボタンは最初は無効
        isResultReady = false;
        if (goResultButton != null) goResultButton.interactable = false;

        // 積立額・スライダー初期化
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

            monthlyAmountSlider.onValueChanged.RemoveListener(OnMonthlySliderChanged);
            monthlyAmountSlider.onValueChanged.AddListener(OnMonthlySliderChanged);
        }

        // リスクトグル（予約変更のみ）
        if (riskLowToggle != null)
        {
            riskLowToggle.onValueChanged.RemoveAllListeners();
            riskLowToggle.onValueChanged.AddListener(isOn => { if (isOn) SetSelectedRiskType(0); });
        }
        if (riskMiddleToggle != null)
        {
            riskMiddleToggle.onValueChanged.RemoveAllListeners();
            riskMiddleToggle.onValueChanged.AddListener(isOn => { if (isOn) SetSelectedRiskType(1); });
        }
        if (riskHighToggle != null)
        {
            riskHighToggle.onValueChanged.RemoveAllListeners();
            riskHighToggle.onValueChanged.AddListener(isOn => { if (isOn) SetSelectedRiskType(2); });
        }

        // グラフトグル
        if (graphYearlyToggle != null)
        {
            graphYearlyToggle.onValueChanged.RemoveAllListeners();
            graphYearlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(true, isOn));
        }
        if (graphMonthlyToggle != null)
        {
            graphMonthlyToggle.onValueChanged.RemoveAllListeners();
            graphMonthlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(false, isOn));
        }

        // 詳細年スライダー
        if (detailYearSlider != null)
        {
            detailYearSlider.minValue = 0;
            detailYearSlider.maxValue = 0;
            detailYearSlider.wholeNumbers = true;
            detailYearSlider.value = 0;

            detailYearSlider.onValueChanged.RemoveListener(OnDetailYearSliderChanged);
            detailYearSlider.onValueChanged.AddListener(OnDetailYearSliderChanged);
        }

        // 内部状態初期化
        currentYear = Mathf.Max(0, currentYear);
        assetAtStartOfYear = 0;
        currentAsset = 0;
        totalPrincipal = 0;

        yearEndAssets.Clear();
        monthlyAssetsPerYear.Clear();
        yearlyEvents.Clear();
        yearStartAssets.Clear();
        yearEventLabels.Clear();

        selectedRiskType = currentRiskType;
        riskChangeCount = 0;
        pendingPenaltyMonths = 0;
        pendingPenaltyRate = 0f;

        // グラフ初期化（0年目点）
        InitializeGraphs();

        // グラフモード初期値（年別オン）
        isUpdatingGraphToggles = true;
        if (graphYearlyToggle != null) graphYearlyToggle.isOn = true;
        if (graphMonthlyToggle != null) graphMonthlyToggle.isOn = false;
        isUpdatingGraphToggles = false;
        ApplyGraphMode(true);

        // 予測メーター初期化（ニュートラル）
        if (forecastMeterUI != null) forecastMeterUI.SetForecast(0f, 0f, false);

        // UI 初期表示
        RefreshAllUI();
    }

    private void OnDestroy()
    {
        // 二重登録事故回避（シーン再読込の保険）
        if (nextYearButton != null) nextYearButton.onClick.RemoveListener(OnClickNextYear);
        if (goResultButton != null) goResultButton.onClick.RemoveListener(OnClickGoResult);

        if (monthlyAmountSlider != null) monthlyAmountSlider.onValueChanged.RemoveListener(OnMonthlySliderChanged);
        if (detailYearSlider != null) detailYearSlider.onValueChanged.RemoveListener(OnDetailYearSliderChanged);
    }

    //========================
    //  結果へボタン（UIから呼ぶ）
    //========================
    public void OnClickGoResult()
    {
        if (!isResultReady) return;
        if (isBusy) return;
        SendResultAndGoToResultScene();
    }

    //========================
    //  UI 更新
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
        if (yearText != null) yearText.text = $"{currentYear}年目 / {maxYear}年";
    }

    private void UpdateCurrentAssetText()
    {
        if (currentAssetText != null) currentAssetText.text = $"{currentAsset:N0}円";
    }

    private void UpdateMonthlyAmountText()
    {
        if (monthlyAmountText != null) monthlyAmountText.text = $"{monthlyAmount:N0}円";
    }

    private void UpdateRiskUI()
    {
        if (isUpdatingRiskToggles) return;

        isUpdatingRiskToggles = true;
        if (riskLowToggle != null) riskLowToggle.isOn = (selectedRiskType == 0);
        if (riskMiddleToggle != null) riskMiddleToggle.isOn = (selectedRiskType == 1);
        if (riskHighToggle != null) riskHighToggle.isOn = (selectedRiskType == 2);
        isUpdatingRiskToggles = false;

        if (riskLabelText != null) riskLabelText.text = GetRiskLabel(selectedRiskType);
    }

    private void SetSelectedRiskType(int type)
    {
        if (isUpdatingRiskToggles) return;
        if (selectedRiskType == type) return;
        selectedRiskType = type;
        UpdateRiskUI();
    }

    //========================
    //  積立額スライダー
    //========================
    public void OnMonthlySliderChanged(float value)
    {
        if (isUpdatingMonthlySlider) return;

        int snapped = Mathf.RoundToInt(value / monthlyStep) * monthlyStep;
        snapped = Mathf.Clamp(snapped, minMonthlyAmount, maxMonthlyAmount);

        monthlyAmount = snapped;

        isUpdatingMonthlySlider = true;
        if (monthlyAmountSlider != null) monthlyAmountSlider.value = monthlyAmount;
        isUpdatingMonthlySlider = false;

        UpdateMonthlyAmountText();

        // 表示用（年初 + 今月積立分）
        currentAsset = assetAtStartOfYear + monthlyAmount;
        UpdateCurrentAssetText();
    }

    //========================
    //  次の年へ：ロード画面 → 年進行（コルーチン）
    //========================
    public void OnClickNextYear()
    {
        if (isBusy) return;
        if (currentYear >= maxYear) return;

        StartCoroutine(NextYearRoutine());
    }

    private IEnumerator NextYearRoutine()
    {
        isBusy = true;
        if (nextYearButton != null) nextYearButton.interactable = false;

        if (nextYearLoadingSeconds > 0f)
        {
            if (loadingScreenUI != null)
                yield return loadingScreenUI.CoShowForSeconds(nextYearLoadingSeconds);
            else
                yield return new WaitForSeconds(nextYearLoadingSeconds);
        }

        AdvanceOneYearCore();

        if (nextYearButton != null) nextYearButton.interactable = true;
        isBusy = false;
    }

    private void AdvanceOneYearCore()
    {
        if (currentYear >= maxYear) return;

        ConfirmRiskChangeIfNeeded();

        int startAsset = assetAtStartOfYear;
        EconomicEventType[] eventsThisYear = GenerateEventsForOneYear();
        UpdateForecastMeter(eventsThisYear);

        string yearEventLabel = BuildYearEventLabel(eventsThisYear);

        List<int> monthlyAssets = new List<int>(12);
        int asset = assetAtStartOfYear;

        for (int month = 0; month < 12; month++)
        {
            asset += monthlyAmount;
            totalPrincipal += monthlyAmount;

            float monthlyRate = GetMonthlyRate(eventsThisYear[month]);

            if (pendingPenaltyMonths > 0)
            {
                monthlyRate += pendingPenaltyRate;
                pendingPenaltyMonths--;
                if (pendingPenaltyMonths <= 0) pendingPenaltyRate = 0f;
            }

            asset = Mathf.RoundToInt(asset * (1f + monthlyRate));
            monthlyAssets.Add(asset);
        }

        assetAtStartOfYear = asset;
        currentAsset = asset;
        currentYear++;

        yearStartAssets.Add(startAsset);
        yearEndAssets.Add(asset);
        monthlyAssetsPerYear.Add(monthlyAssets);
        yearlyEvents.Add(eventsThisYear);
        yearEventLabels.Add(yearEventLabel);

        if (graphUI != null)
            graphUI.AddPoint(currentYear, currentAsset, yearEventLabel);

        UpdateMonthlyGraphToLatestYear();

        if (detailYearSlider != null)
        {
            detailYearSlider.maxValue = Mathf.Max(0, yearEndAssets.Count - 1);
            detailYearSlider.value = yearEndAssets.Count - 1;
        }

        UpdateDetailYearLabel();
        RefreshAllUI();

        AppendLog($"{currentYear}年目終了 : 資産 {currentAsset:N0}円（景気: {yearEventLabel}）");

        // 15年到達：結果へボタン有効化（自動遷移しない）
        if (!isResultReady && currentYear >= maxYear)
        {
            isResultReady = true;
            if (goResultButton != null) goResultButton.interactable = true;
            AppendLog("15年が終了しました。『結果へ』ボタンから結果画面へ進めます。");
        }
    }

    //========================
    //  ResultDataStore へ保存して ResultScene へ
    //========================
    private void SendResultAndGoToResultScene()
    {
        var store = EnsureResultDataStore();

        // 年別（0年目含む：0..maxYear）
        List<int> yearlyAssets0ToN = new List<int>(maxYear + 1);
        yearlyAssets0ToN.Add(0); // 0年目

        for (int y = 1; y <= maxYear; y++)
        {
            int idx = y - 1;
            int v = (idx >= 0 && idx < yearEndAssets.Count) ? yearEndAssets[idx] : yearlyAssets0ToN[y - 1];
            yearlyAssets0ToN.Add(v);
        }

        // 年別ラベル（0年目含む）
        List<string> yearlyLabels0ToN = new List<string>(maxYear + 1);
        yearlyLabels0ToN.Add("平常");
        for (int y = 1; y <= maxYear; y++)
        {
            int idx = y - 1;
            string label = (idx >= 0 && idx < yearEventLabels.Count) ? yearEventLabels[idx] : "平常";
            if (string.IsNullOrEmpty(label)) label = "平常";
            yearlyLabels0ToN.Add(label);
        }

        store.SetResultData(
            finalAsset: currentAsset,
            totalPrincipal: totalPrincipal,
            yearlyAssets_0ToN: yearlyAssets0ToN,
            yearlyEventLabels_0ToN: yearlyLabels0ToN,
            yearStartAssets_1ToN: yearStartAssets,
            monthlyAssetsPerYear_1ToN: monthlyAssetsPerYear,
            yearlyEvents_1ToN: yearlyEvents
        );

        SceneManager.LoadScene(resultSceneName);
    }

    private ResultDataStore EnsureResultDataStore()
    {
        if (ResultDataStore.Instance != null) return ResultDataStore.Instance;

        var found = FindObjectOfType<ResultDataStore>();
        if (found != null) return found;

        var go = new GameObject("ResultDataStore");
        return go.AddComponent<ResultDataStore>();
    }

    //========================
    //  リスク確定（方式A）
    //========================
    private void ConfirmRiskChangeIfNeeded()
    {
        if (selectedRiskType == currentRiskType) return;

        if (riskChangeCount >= maxRiskChanges)
        {
            selectedRiskType = currentRiskType;
            UpdateRiskUI();
            AppendLog($"リスク変更は最大{maxRiskChanges}回までです。変更は反映されませんでした。");
            return;
        }

        currentRiskType = selectedRiskType;
        riskChangeCount++;

        pendingPenaltyMonths = 1;
        pendingPenaltyRate = GetRiskChangePenaltyForConfirmedRisk(currentRiskType);

        AppendLog($"リスク変更確定：{GetRiskLabel(currentRiskType)}（{riskChangeCount}/{maxRiskChanges}） 次月ペナルティ {pendingPenaltyRate * 100f:F2}%");
    }

    private float GetRiskChangePenaltyForConfirmedRisk(int riskType)
    {
        switch (riskType)
        {
            case 0: return penaltyLow;
            case 1: return penaltyMiddle;
            case 2: return penaltyHigh;
            default: return penaltyMiddle;
        }
    }

    private string GetRiskLabel(int riskType)
    {
        switch (riskType)
        {
            case 0: return "低リスク";
            case 1: return "中リスク";
            case 2: return "高リスク";
            default: return "中リスク";
        }
    }

    //========================
    //  予測メーター
    //========================
    private void UpdateForecastMeter(EconomicEventType[] eventsThisYear)
    {
        if (forecastMeterUI == null) return;

        if (eventsThisYear == null || eventsThisYear.Length == 0)
        {
            forecastMeterUI.SetForecast(0f, 0f, false);
            return;
        }

        int boom = 0, rec = 0, shock = 0;
        for (int i = 0; i < eventsThisYear.Length; i++)
        {
            if (eventsThisYear[i] == EconomicEventType.Boom) boom++;
            else if (eventsThisYear[i] == EconomicEventType.Recession) rec++;
            else if (eventsThisYear[i] == EconomicEventType.Shock) shock++;
        }

        float score = (boom * 1f) + (rec * -1f) + (shock * -2f);
        float tendency = Mathf.Clamp(score / 12f, -1f, 1f);

        float density = Mathf.Clamp01((boom + rec + shock) / 12f);
        float shockBoost = (shock > 0) ? 0.15f : 0f;
        float confidence = Mathf.Clamp01(density + shockBoost);

        bool shockWarning = (shock > 0);
        forecastMeterUI.SetForecast(tendency, confidence, shockWarning);
    }

    //========================
    //  グラフ初期化
    //========================
    private void InitializeGraphs()
    {
        if (graphUI != null)
        {
            graphUI.ResetGraph();
            graphUI.AddPoint(0, assetAtStartOfYear, "平常");
        }

        if (monthlyGraphUI != null)
        {
            monthlyGraphUI.SetMonthlyData(null, false, null);
        }
    }

    //========================
    //  月別グラフ（直近年）
    //========================
    private void UpdateMonthlyGraphToLatestYear()
    {
        if (monthlyGraphUI == null) return;
        if (monthlyAssetsPerYear.Count <= 0) return;

        int idx = Mathf.Clamp(currentYear - 1, 0, monthlyAssetsPerYear.Count - 1);

        List<EconomicEventType> eventsList = new List<EconomicEventType>(12);
        if (idx >= 0 && idx < yearlyEvents.Count && yearlyEvents[idx] != null)
            eventsList.AddRange(yearlyEvents[idx]);

        while (eventsList.Count < 12) eventsList.Add(EconomicEventType.None);
        if (eventsList.Count > 12) eventsList.RemoveRange(12, eventsList.Count - 12);

        int startAsset = (idx >= 0 && idx < yearStartAssets.Count) ? yearStartAssets[idx] : 0;
        int yearNumber = idx + 1;

        monthlyGraphUI.SetMonthlyDataWithStartPoint(startAsset, monthlyAssetsPerYear[idx], eventsList, yearNumber);
    }

    //========================
    //  月利計算
    //========================
    private float GetMonthlyRate(EconomicEventType evType)
    {
        float yearly = middleRiskReturnRate;
        if (currentRiskType == 0) yearly = lowRiskReturnRate;
        else if (currentRiskType == 2) yearly = highRiskReturnRate;

        float baseMonthly = Mathf.Pow(1f + yearly, 1f / 12f) - 1f;

        float eventDelta = 0f;
        switch (evType)
        {
            case EconomicEventType.Boom: eventDelta = boomMonthlyDelta; break;
            case EconomicEventType.Recession: eventDelta = recessionMonthlyDelta; break;
            case EconomicEventType.Shock: eventDelta = shockMonthlyDelta; break;
            default: eventDelta = 0f; break;
        }

        //========================================================
        // ★追加：マイナスイベント低減（不景気・ショックのみ）
        // eventDelta がマイナスの時だけ軽減を掛ける
        // 例：-0.05 を 20%低減 -> -0.04
        //========================================================
        if (eventDelta < 0f)
        {
            float reduction = GetNegativeEventReductionByRisk(currentRiskType);
            reduction = Mathf.Clamp01(reduction);

            // マイナス量を小さくする（=値を0方向へ寄せる）
            eventDelta *= (1f - reduction);
        }

        float noise = 0f;
        if (evType != EconomicEventType.None)
        {
            float amp = middleRiskNoiseAmp;
            if (currentRiskType == 0) amp = lowRiskNoiseAmp;
            else if (currentRiskType == 2) amp = highRiskNoiseAmp;

            noise = Random.Range(-amp, amp);
        }

        return baseMonthly + eventDelta + noise;
    }

    private float GetNegativeEventReductionByRisk(int riskType)
    {
        switch (riskType)
        {
            case 0: return negativeEventReductionLow;     // 低リスク：20%
            case 1: return negativeEventReductionMiddle;  // 中リスク：10%
            case 2: return negativeEventReductionHigh;    // 高リスク：0%
            default: return 0f;
        }
    }

    //========================
    //  景気イベント生成（モードで確率/重みを調整）
    //========================
    private EconomicEventType[] GenerateEventsForOneYear()
    {
        EconomicEventType[] schedule = new EconomicEventType[12];
        for (int i = 0; i < 12; i++) schedule[i] = EconomicEventType.None;

        float any = Mathf.Clamp01(chanceAnyEvent);
        float second = Mathf.Clamp01(chanceSecondEvent);

        // そもそもイベントが起きるか（0イベントを許す）
        if (Random.value > any)
            return schedule;

        // 最低1つ
        PlaceRandomEvent(schedule);

        // 2つ目
        if (Random.value < second)
            PlaceRandomEvent(schedule);

        return schedule;
    }

    private void PlaceRandomEvent(EconomicEventType[] schedule)
    {
        EconomicEventType type = DrawEventType();
        int duration = GetEventDuration(type);
        if (duration <= 0) return;

        List<int> candidateStarts = new List<int>();
        for (int start = 0; start <= 12 - duration; start++)
        {
            bool canPlace = true;
            for (int m = start; m < start + duration; m++)
            {
                if (schedule[m] != EconomicEventType.None) { canPlace = false; break; }
            }
            if (canPlace) candidateStarts.Add(start);
        }

        if (candidateStarts.Count == 0) return;

        int chosenStart = candidateStarts[Random.Range(0, candidateStarts.Count)];
        for (int m = chosenStart; m < chosenStart + duration; m++) schedule[m] = type;
    }

    private EconomicEventType DrawEventType()
    {
        float wBoom = Mathf.Max(0f, boomWeight);
        float wRec = Mathf.Max(0f, recessionWeight);
        float wShock = Mathf.Max(0f, shockWeight);

        float sum = wBoom + wRec + wShock;
        if (sum <= 0f)
        {
            // 事故対策：全部0なら「不景気」に寄せる（ここは好みでBoomでもOK）
            return EconomicEventType.Recession;
        }

        float r = Random.value * sum;
        if (r < wBoom) return EconomicEventType.Boom;
        r -= wBoom;
        if (r < wRec) return EconomicEventType.Recession;
        return EconomicEventType.Shock;
    }

    private int GetEventDuration(EconomicEventType type)
    {
        switch (type)
        {
            case EconomicEventType.Recession: return Random.Range(3, 7);
            case EconomicEventType.Boom: return Random.Range(5, 9);
            case EconomicEventType.Shock: return Random.Range(1, 4);
            default: return 0;
        }
    }

    //========================
    //  年内イベント最大2つラベル化
    //========================
    private string BuildYearEventLabel(EconomicEventType[] eventsThisYear)
    {
        if (eventsThisYear == null || eventsThisYear.Length == 0) return "平常";

        List<EconomicEventType> found = new List<EconomicEventType>(2);
        EconomicEventType last = EconomicEventType.None;

        for (int i = 0; i < eventsThisYear.Length; i++)
        {
            EconomicEventType cur = eventsThisYear[i];
            if (cur != last && cur != EconomicEventType.None)
            {
                found.Add(cur);
                if (found.Count >= 2) break;
            }
            last = cur;
        }

        if (found.Count == 0) return "平常";
        if (found.Count == 1) return GetEventLabel(found[0]);
        return $"{GetEventLabel(found[0])} + {GetEventLabel(found[1])}";
    }

    private string GetEventLabel(EconomicEventType ev)
    {
        switch (ev)
        {
            case EconomicEventType.Boom: return "好景気";
            case EconomicEventType.Recession: return "不景気";
            case EconomicEventType.Shock: return "ショック";
            default: return "平常";
        }
    }

    //========================
    //  月別グラフ：年変更
    //========================
    private void OnDetailYearSliderChanged(float value)
    {
        int index = Mathf.RoundToInt(value);
        UpdateDetailYearLabel();

        if (index < 0 || index >= monthlyAssetsPerYear.Count) return;
        if (monthlyGraphUI == null) return;

        List<EconomicEventType> eventsList = new List<EconomicEventType>(12);
        if (index >= 0 && index < yearlyEvents.Count && yearlyEvents[index] != null)
            eventsList.AddRange(yearlyEvents[index]);

        while (eventsList.Count < 12) eventsList.Add(EconomicEventType.None);
        if (eventsList.Count > 12) eventsList.RemoveRange(12, eventsList.Count - 12);

        int startAsset = (index >= 0 && index < yearStartAssets.Count) ? yearStartAssets[index] : 0;
        int yearNumber = index + 1;

        monthlyGraphUI.SetMonthlyDataWithStartPoint(startAsset, monthlyAssetsPerYear[index], eventsList, yearNumber);
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
        if (!isOn)
        {
            if (graphYearlyToggle != null && graphMonthlyToggle != null &&
                !graphYearlyToggle.isOn && !graphMonthlyToggle.isOn)
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

        // ★追加：年別表示中は Slider_DetailYear を操作不可にする
        if (detailYearSlider != null)
            detailYearSlider.interactable = !showYearly;

        // （任意）年別中はラベルを隠すなら以下もON
        // if (detailYearLabel != null) detailYearLabel.gameObject.SetActive(!showYearly);
    }

    //========================
    //  ログ
    //========================
    private void AppendLog(string message)
    {
        if (logScrollView == null || logEntryPrefab == null) return;

        TMP_Text entry = Instantiate(logEntryPrefab, logScrollView.content);
        entry.text = message;

        Canvas.ForceUpdateCanvases();
        logScrollView.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// モードの選択用（GameModeStoreから読み、ModeConfigの値を安全に反映）
    /// </summary>
    private void ApplySelectedModeIfAny()
    {
        var store = GameModeStore.Ensure();
        var mode = store.SelectedMode;

        ModeConfig config = mode switch
        {
            GameMode.Hard => hardConfig,
            GameMode.Chaos => chaosConfig,
            _ => normalConfig
        };

        if (config == null) return;

        // イベント補正
        boomMonthlyDelta = config.boomMonthlyDelta;
        recessionMonthlyDelta = config.recessionMonthlyDelta;
        shockMonthlyDelta = config.shockMonthlyDelta;

        // ★確率（Clamp）
        chanceAnyEvent = Mathf.Clamp01(config.chanceAnyEvent);
        chanceSecondEvent = Mathf.Clamp01(config.chanceSecondEvent);

        // ★重み（0以上）
        boomWeight = Mathf.Max(0f, config.boomWeight);
        recessionWeight = Mathf.Max(0f, config.recessionWeight);
        shockWeight = Mathf.Max(0f, config.shockWeight);
    }
}
