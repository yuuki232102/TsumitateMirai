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

    [Header("年数設定")]
    [SerializeField] private int maxYear = 15;
    [SerializeField] private int currentYear = 0;

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 1000;
    [SerializeField] private int monthlyStep = 1000;
    [SerializeField] private int minMonthlyAmount = 1000;
    [SerializeField] private int maxMonthlyAmount = 33000;

    [Header("資産状態（シミュレーション用）")]
    [SerializeField] private int assetAtStartOfYear = 0;
    [SerializeField] private int currentAsset = 0;
    [SerializeField] private int totalPrincipal = 0;

    [Header("リスク設定（確定値）")]
    [SerializeField] private int currentRiskType = 1;   // 0:低 1:中 2:高（確定値）

    [Header("リスク別期待リターン（年率）")]
    [SerializeField] private float lowRiskReturnRate = 0.02f;
    [SerializeField] private float middleRiskReturnRate = 0.04f;
    [SerializeField] private float highRiskReturnRate = 0.06f;

    [Header("イベント補正値（インスペクタで調整）")]
    [SerializeField] private float boomDeltaMonthly = 0.02f;       // 好景気：+2%（例）
    [SerializeField] private float recessionDeltaMonthly = -0.02f; // 不景気：-2%
    [SerializeField] private float shockDeltaMonthly = -0.08f;     // ショック：-8%

    [Header("イベント時ランダムブレ（リスク別）")]
    [SerializeField] private float lowRiskNoiseAmp = 0.01f;    // ±1%
    [SerializeField] private float middleRiskNoiseAmp = 0.02f; // ±2%
    [SerializeField] private float highRiskNoiseAmp = 0.04f;   // ±4%

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
    //  予測メーター設定
    //========================

    [Header("予測の見せ方（バランス 65% など）")]
    [Range(0f, 1f)]
    [SerializeField] private float forecastAccuracyWeight = 0.65f;

    [Header("信頼度（0..1）成長")]
    [Range(0f, 1f)]
    [SerializeField] private float forecastConfidenceStart = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] private float forecastConfidenceMax = 0.90f;
    [SerializeField] private float forecastConfidenceGainPerYear = 0.03f;

    [Header("ショック警戒判定（割合）")]
    [Range(0f, 1f)]
    [SerializeField] private float forecastShockWarnThreshold = 0.18f;

    //========================
    //  内部データ
    //========================

    // 各年の年末資産（1年目〜n年目の年末が index0〜）
    private readonly List<int> yearEndAssets = new List<int>();

    // 各年ごとの月末資産（各Listは12個：1月末〜12月末）
    private readonly List<List<int>> monthlyAssetsPerYear = new List<List<int>>();

    // 各年ごとの月イベント（12個）
    private readonly List<EconomicEventType[]> yearlyEvents = new List<EconomicEventType[]>();

    // 各年の「年初資産（0月点）」を保存（indexは上の2つと同じ）
    private readonly List<int> yearStartAssets = new List<int>();

    private bool isUpdatingMonthlySlider = false;
    private bool isUpdatingGraphToggles = false;

    // リスクUIの内部更新フラグ（トグルの無限ループ防止）
    private bool isUpdatingRiskToggles = false;

    //========================
    //  方式A用：リスク確定のための状態
    //========================

    // UIで選ばれている「予約」リスク（トグル操作で変わる）
    private int selectedRiskType = 1;

    // 切替回数（確定した回数のみカウント）
    private int riskChangeCount = 0;
    private const int MaxRiskChanges = 4;

    // 次月ペナルティ（確定した次の1ヶ月だけ）
    private bool pendingRiskPenalty = false;
    private float pendingPenaltyRate = 0f;

    //========================
    //  次年イベント（真実）を保持：予測UIの元データ
    //========================
    private EconomicEventType[] nextYearEvents;

    //========================
    //  Unity
    //========================

    private void Start()
    {
        //--------------------------------
        // 積立額・スライダー初期化
        //--------------------------------
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

            monthlyAmountSlider.onValueChanged.AddListener(OnMonthlySliderChanged);
        }

        //--------------------------------
        // リスクトグル（予約変更のみ）
        //--------------------------------
        if (riskLowToggle != null)
            riskLowToggle.onValueChanged.AddListener(isOn => { if (isOn) SetSelectedRiskType(0); });

        if (riskMiddleToggle != null)
            riskMiddleToggle.onValueChanged.AddListener(isOn => { if (isOn) SetSelectedRiskType(1); });

        if (riskHighToggle != null)
            riskHighToggle.onValueChanged.AddListener(isOn => { if (isOn) SetSelectedRiskType(2); });

        //--------------------------------
        // グラフトグル
        //--------------------------------
        if (graphYearlyToggle != null)
            graphYearlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(true, isOn));

        if (graphMonthlyToggle != null)
            graphMonthlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(false, isOn));

        //--------------------------------
        // 詳細年スライダー
        //--------------------------------
        if (detailYearSlider != null)
        {
            detailYearSlider.minValue = 0;
            detailYearSlider.maxValue = 0;
            detailYearSlider.wholeNumbers = true;
            detailYearSlider.value = 0;
            detailYearSlider.onValueChanged.AddListener(OnDetailYearSliderChanged);
        }

        //--------------------------------
        // シミュレーション内部状態の初期化
        //--------------------------------
        currentYear = 0;
        assetAtStartOfYear = 0;
        currentAsset = 0;
        totalPrincipal = 0;

        yearEndAssets.Clear();
        monthlyAssetsPerYear.Clear();
        yearlyEvents.Clear();
        yearStartAssets.Clear();

        // 方式A：予約値の初期化
        selectedRiskType = currentRiskType;
        riskChangeCount = 0;
        pendingRiskPenalty = false;
        pendingPenaltyRate = 0f;

        //--------------------------------
        // 次年イベントを先に作って保持（予測UIの真実）
        //--------------------------------
        nextYearEvents = GenerateEventsForOneYear();

        //--------------------------------
        // グラフ初期化（0年目点）
        //--------------------------------
        InitializeGraphs();

        //--------------------------------
        // グラフモード初期値（年別オン）
        //--------------------------------
        isUpdatingGraphToggles = true;
        if (graphYearlyToggle != null) graphYearlyToggle.isOn = true;
        if (graphMonthlyToggle != null) graphMonthlyToggle.isOn = false;
        isUpdatingGraphToggles = false;
        ApplyGraphMode(true);

        //--------------------------------
        // UI の初期表示
        //--------------------------------
        RefreshAllUI();

        //--------------------------------
        // 予測メーター初期表示（次の年＝1年目の予測）
        //--------------------------------
        UpdateForecastMeterUI();
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

    /// <summary>
    /// リスクUIは「予約(selectedRiskType)」を表示する。
    /// （確定(currentRiskType)は次の年へ押下で更新される）
    /// </summary>
    private void UpdateRiskUI()
    {
        if (isUpdatingRiskToggles) return;

        isUpdatingRiskToggles = true;

        if (riskLowToggle != null) riskLowToggle.isOn = (selectedRiskType == 0);
        if (riskMiddleToggle != null) riskMiddleToggle.isOn = (selectedRiskType == 1);
        if (riskHighToggle != null) riskHighToggle.isOn = (selectedRiskType == 2);

        isUpdatingRiskToggles = false;

        if (riskLabelText != null)
        {
            riskLabelText.text = GetRiskLabel(selectedRiskType);
        }
    }

    //========================
    //  リスク変更（予約）
    //========================

    private void SetSelectedRiskType(int type)
    {
        if (isUpdatingRiskToggles) return;
        if (selectedRiskType == type) return;

        selectedRiskType = type;
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

        isUpdatingMonthlySlider = true;
        monthlyAmountSlider.value = monthlyAmount;
        isUpdatingMonthlySlider = false;

        UpdateMonthlyAmountText();

        // プレビュー：年初資産 + 1回積立分
        currentAsset = assetAtStartOfYear + monthlyAmount;
        UpdateCurrentAssetText();
    }

    //========================
    //  次の年へ（方式A：ここでリスク確定）
    //========================

    public void OnClickNextYear()
    {
        if (currentYear >= maxYear) return;

        // 0) リスク切替をここで確定（方式A）
        ConfirmRiskChangeIfNeeded();

        // ★その年の開始資産（0月点用）
        int startAsset = assetAtStartOfYear;

        // 1) 今年のイベントは「次年として保持していた真実」を使用（予測→確定）
        EconomicEventType[] eventsThisYear = nextYearEvents != null ? nextYearEvents : GenerateEventsForOneYear();

        // 2) 年別グラフに表示する景気ラベル（最大2つ）
        string yearEventLabel = BuildYearEventLabel(eventsThisYear);

        // 3) この年の12ヶ月をシミュレート
        List<int> monthlyAssets = new List<int>(12);
        int asset = assetAtStartOfYear;

        for (int month = 0; month < 12; month++)
        {
            // 積立
            asset += monthlyAmount;
            totalPrincipal += monthlyAmount;

            // 月利
            float monthlyRate = GetMonthlyRate(eventsThisYear[month]);

            // 次月ペナルティ（確定した次の1ヶ月だけ）
            if (pendingRiskPenalty)
            {
                monthlyRate += pendingPenaltyRate;
                pendingRiskPenalty = false;
                pendingPenaltyRate = 0f;
            }

            asset = Mathf.RoundToInt(asset * (1f + monthlyRate));
            monthlyAssets.Add(asset);
        }

        // 4) 年末確定
        assetAtStartOfYear = asset;
        currentAsset = asset;
        currentYear++;

        // 5) 保存（indexを揃える）
        yearStartAssets.Add(startAsset);
        yearEndAssets.Add(asset);
        monthlyAssetsPerYear.Add(monthlyAssets);
        yearlyEvents.Add(eventsThisYear);

        // 6) 年別グラフに点追加（方式1：ラベル込み）
        if (graphUI != null)
        {
            graphUI.AddPoint(currentYear, currentAsset, yearEventLabel);
        }

        // 7) 月別グラフ：直近年を表示（0月点あり）
        UpdateMonthlyGraphToLatestYear();

        // 8) 詳細年スライダー更新
        if (detailYearSlider != null)
        {
            detailYearSlider.maxValue = Mathf.Max(0, yearEndAssets.Count - 1);
            detailYearSlider.value = yearEndAssets.Count - 1;
        }

        UpdateDetailYearLabel();
        RefreshAllUI();

        AppendLog($"{currentYear}年目終了 : 資産 {currentAsset.ToString("N0")}円（景気: {yearEventLabel}）");

        // 9) 次年イベントを新しく生成して保持（次の予測の真実）
        nextYearEvents = GenerateEventsForOneYear();

        // 10) 予測メーター更新（次の年＝currentYear+1 の予測）
        UpdateForecastMeterUI();
    }

    /// <summary>
    /// 方式A：次の年へ押下時に、予約リスク(selectedRiskType)を確定(currentRiskType)へ反映する。
    /// 切替上限（4回）を超える場合は確定させず、予約を確定値に戻す。
    /// 確定できた場合は「次の1ヶ月ペナルティ」を予約する。
    /// </summary>
    private void ConfirmRiskChangeIfNeeded()
    {
        if (selectedRiskType == currentRiskType) return;

        if (riskChangeCount >= MaxRiskChanges)
        {
            selectedRiskType = currentRiskType;
            UpdateRiskUI();
            AppendLog($"リスク変更は最大{MaxRiskChanges}回までです。変更は反映されませんでした。");
            return;
        }

        // 確定
        currentRiskType = selectedRiskType;
        riskChangeCount++;

        // 次月ペナルティ予約（確定した次の1ヶ月）
        pendingRiskPenalty = true;
        pendingPenaltyRate = GetRiskChangePenaltyForConfirmedRisk(currentRiskType);

        AppendLog($"リスク変更確定：{GetRiskLabel(currentRiskType)}（{riskChangeCount}/{MaxRiskChanges}） 次月ペナルティ {pendingPenaltyRate * 100f:F2}%");
    }

    private float GetRiskChangePenaltyForConfirmedRisk(int riskType)
    {
        // 仕様：低 -0.10% / 中 -0.20% / 高 -0.30%
        switch (riskType)
        {
            case 0: return -0.001f;
            case 1: return -0.002f;
            case 2: return -0.003f;
            default: return -0.002f;
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
    //  予測メーター更新
    //========================

    private void UpdateForecastMeterUI()
    {
        if (forecastMeterUI == null) return;

        // 信頼度は年が進むほど上がる（0年目→start、15年目付近→maxへ）
        float c = forecastConfidenceStart + currentYear * forecastConfidenceGainPerYear;
        c = Mathf.Clamp(c, 0f, forecastConfidenceMax);

        // 次の年が存在しないなら「予測なし」表示に近いものにする
        if (currentYear >= maxYear)
        {
            forecastMeterUI.SetForecast(0f, c, false, "これ以上予測なし");
            return;
        }

        var result = EconomicForecastSystem.MakeForecast(
            nextYearEvents,
            c,
            forecastAccuracyWeight,
            tendencyNoiseAtLowConfidence: 0.6f,
            shockWarnThreshold: forecastShockWarnThreshold
        );

        forecastMeterUI.SetForecast(result.tendency, result.confidence01, result.shockWarning, result.label);
    }

    //========================
    //  年別＆月別グラフ初期化
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
            // 初期はデータなし表示
            monthlyGraphUI.SetMonthlyDataWithStartPoint(0, null, null, -1);
        }
    }

    //========================
    //  月別グラフ表示更新（直近年 / 0月点あり）
    //========================

    private void UpdateMonthlyGraphToLatestYear()
    {
        if (monthlyGraphUI == null) return;
        if (monthlyAssetsPerYear.Count <= 0) return;

        int idx = Mathf.Clamp(currentYear - 1, 0, monthlyAssetsPerYear.Count - 1);
        int yearNumber = idx + 1;

        List<EconomicEventType> eventsList = new List<EconomicEventType>(12);
        if (idx >= 0 && idx < yearlyEvents.Count && yearlyEvents[idx] != null)
        {
            eventsList.AddRange(yearlyEvents[idx]);
        }
        else
        {
            for (int i = 0; i < 12; i++) eventsList.Add(EconomicEventType.None);
        }

        int startAsset = (idx >= 0 && idx < yearStartAssets.Count) ? yearStartAssets[idx] : 0;

        monthlyGraphUI.SetMonthlyDataWithStartPoint(startAsset, monthlyAssetsPerYear[idx], eventsList, yearNumber);
    }

    //========================
    //  月利計算（イベント＋ランダムブレ）
    //  ※平常時ノイズ0%
    //========================

    private float GetMonthlyRate(EconomicEventType evType)
    {
        // リスク別ベース年率
        float yearly = middleRiskReturnRate;
        if (currentRiskType == 0) yearly = lowRiskReturnRate;
        else if (currentRiskType == 2) yearly = highRiskReturnRate;

        // 年率→月率（複利ベース）
        float baseMonthly = Mathf.Pow(1f + yearly, 1f / 12f) - 1f;

        // イベント補正（インスペクタ調整可）
        float eventDelta = 0f;
        switch (evType)
        {
            case EconomicEventType.Boom: eventDelta = boomDeltaMonthly; break;
            case EconomicEventType.Recession: eventDelta = recessionDeltaMonthly; break;
            case EconomicEventType.Shock: eventDelta = shockDeltaMonthly; break;
            default: eventDelta = 0f; break;
        }

        // ★平常時はブレ0
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

    //========================
    //  景気イベントスケジュール生成
    //========================

    private EconomicEventType[] GenerateEventsForOneYear()
    {
        EconomicEventType[] schedule = new EconomicEventType[12];
        for (int i = 0; i < 12; i++) schedule[i] = EconomicEventType.None;

        // 必ず1つイベント
        PlaceRandomEvent(schedule);

        // 低確率で2つ目
        float secondProb = 0.2f;
        if (Random.value < secondProb)
        {
            PlaceRandomEvent(schedule);
        }

        return schedule;
    }

    private void PlaceRandomEvent(EconomicEventType[] schedule)
    {
        EconomicEventType type = DrawEventType();
        int duration = GetEventDuration(type);

        List<int> candidateStarts = new List<int>();

        for (int start = 0; start <= 12 - duration; start++)
        {
            bool canPlace = true;
            for (int m = start; m < start + duration; m++)
            {
                if (schedule[m] != EconomicEventType.None)
                {
                    canPlace = false;
                    break;
                }
            }
            if (canPlace) candidateStarts.Add(start);
        }

        if (candidateStarts.Count == 0) return;

        int chosenStart = candidateStarts[Random.Range(0, candidateStarts.Count)];
        for (int m = chosenStart; m < chosenStart + duration; m++)
        {
            schedule[m] = type;
        }
    }

    private EconomicEventType DrawEventType()
    {
        float r = Random.value;
        if (r < 0.50f) return EconomicEventType.Boom;
        if (r < 0.50f + 0.38f) return EconomicEventType.Recession;
        return EconomicEventType.Shock;
    }

    private int GetEventDuration(EconomicEventType type)
    {
        switch (type)
        {
            case EconomicEventType.Recession: return Random.Range(3, 7); // 3〜6
            case EconomicEventType.Boom: return Random.Range(5, 9);      // 5〜8
            case EconomicEventType.Shock: return Random.Range(1, 4);     // 1〜3
            default: return 0;
        }
    }

    //========================
    //  年内イベント最大2つをラベル化（例：好景気 + 不景気）
    //========================

    private string BuildYearEventLabel(EconomicEventType[] eventsThisYear)
    {
        if (eventsThisYear == null || eventsThisYear.Length == 0) return "平常";

        List<EconomicEventType> found = new List<EconomicEventType>(2);
        EconomicEventType last = EconomicEventType.None;

        for (int i = 0; i < eventsThisYear.Length; i++)
        {
            EconomicEventType cur = eventsThisYear[i];

            // イベント開始（None→イベント or 種別変化）を拾う
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
    //  月別グラフの年変更（0月点あり）
    //========================

    private void OnDetailYearSliderChanged(float value)
    {
        int index = Mathf.RoundToInt(value);
        UpdateDetailYearLabel();

        if (index < 0 || index >= monthlyAssetsPerYear.Count) return;
        if (monthlyGraphUI == null) return;

        int yearNumber = index + 1;

        List<EconomicEventType> eventsList = new List<EconomicEventType>(12);
        if (index >= 0 && index < yearlyEvents.Count && yearlyEvents[index] != null)
        {
            eventsList.AddRange(yearlyEvents[index]);
        }
        else
        {
            for (int i = 0; i < 12; i++) eventsList.Add(EconomicEventType.None);
        }

        int startAsset = (index >= 0 && index < yearStartAssets.Count) ? yearStartAssets[index] : 0;

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
