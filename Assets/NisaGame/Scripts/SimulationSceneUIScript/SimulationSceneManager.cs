using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationSceneManager : MonoBehaviour
{
    //========================
    //  内部用：景気イベント種別
    //========================
    

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
    [SerializeField] private int currentYear = 0;   // 0,1,2,...maxYear

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 1000;
    [SerializeField] private int monthlyStep = 1000;
    [SerializeField] private int minMonthlyAmount = 1000;
    [SerializeField] private int maxMonthlyAmount = 33000;

    [Header("資産状態（シミュレーション用）")]
    [SerializeField] private int assetAtStartOfYear = 0;        // その年の開始時点の資産
    [SerializeField] private int currentAsset = 0;              // 画面に出している現在の資産（プレビュー込み）
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

    // 各年の「年末時点の資産」
    private readonly List<int> yearEndAssets = new List<int>();

    // 各年ごとに、その年の「12ヶ月の資産推移」
    private readonly List<List<int>> monthlyAssetsPerYear = new List<List<int>>();

    // 将来 UI に出すかもしれない用に：各年の景気イベントスケジュールも保存しておく
    // （今は利回り計算でしか使っていないが、後で月別グラフに色帯を出したいときに使える）
    private readonly List<EconomicEventType[]> yearlyEvents = new List<EconomicEventType[]>();

    // スライダー更新中フラグ（無限ループ防止）
    private bool isUpdatingMonthlySlider = false;

    // グラフトグルの内部更新フラグ
    private bool isUpdatingGraphToggles = false;

    //========================
    //  Unity ライフサイクル
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
        // リスクトグル
        //--------------------------------
        if (riskLowToggle != null)
            riskLowToggle.onValueChanged.AddListener(isOn => { if (isOn) SetRiskType(0); });

        if (riskMiddleToggle != null)
            riskMiddleToggle.onValueChanged.AddListener(isOn => { if (isOn) SetRiskType(1); });

        if (riskHighToggle != null)
            riskHighToggle.onValueChanged.AddListener(isOn => { if (isOn) SetRiskType(2); });

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

        //--------------------------------
        // グラフ初期化（0年目・資産0円の点を 1 つ置く）
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
        // ※将来、リスク変更ペナルティを入れるならここでフラグ管理
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

        // プレビュー用：今年の開始資産 + 今月の積み立て 1 回分
        currentAsset = assetAtStartOfYear + monthlyAmount;
        UpdateCurrentAssetText();
    }

    //========================
    //  次の年へボタン
    //========================

    public void OnClickNextYear()
    {
        if (currentYear >= maxYear) return;

        // この年の景気イベントスケジュールを事前に決める（12ヶ月分）
        EconomicEventType[] eventsThisYear = GenerateEventsForOneYear();

        // この年の 12 ヶ月をシミュレート
        List<int> monthlyAssets = new List<int>();
        int asset = assetAtStartOfYear;

        for (int month = 0; month < 12; month++)
        {
            // まず積立
            asset += monthlyAmount;
            totalPrincipal += monthlyAmount;

            // この月の利回りを、イベント＋ランダム込みで計算
            float monthlyRate = GetMonthlyRate(eventsThisYear[month]);

            // 資産に利回りをかける
            float afterReturn = asset * (1f + monthlyRate);
            asset = Mathf.RoundToInt(afterReturn);

            // 月末資産として保存
            monthlyAssets.Add(asset);
        }

        // 年末資産を確定
        assetAtStartOfYear = asset;
        currentAsset = asset;
        currentYear++;

        // データ保存
        yearEndAssets.Add(asset);
        monthlyAssetsPerYear.Add(monthlyAssets);
        yearlyEvents.Add(eventsThisYear);

        // 年別グラフに 1 点追加
        if (graphUI != null)
        {
            graphUI.AddPoint(currentYear, currentAsset);
        }

        // 月別グラフ…直近の年を表示
        if (monthlyGraphUI != null)
        {
            int idx = Mathf.Clamp(currentYear - 1, 0, monthlyAssetsPerYear.Count - 1);

            // 0→1年目のグラフだけ開始点オフセットを有効にする
            bool useOffset = (idx == 0);
            monthlyGraphUI.SetMonthlyData(monthlyAssetsPerYear[idx], useOffset);
        }

        // 詳細年スライダー範囲更新
        if (detailYearSlider != null)
        {
            detailYearSlider.maxValue = Mathf.Max(0, yearEndAssets.Count - 1);
            detailYearSlider.value = yearEndAssets.Count - 1;
        }

        UpdateDetailYearLabel();
        RefreshAllUI();

        // ログ出力（任意）
        AppendLog($"{currentYear}年目終了 : 資産 {currentAsset.ToString("N0")}円");
    }

    //========================
    //  年別グラフ＆月別グラフの初期化
    //========================

    private void InitializeGraphs()
    {
        // 年別グラフ
        if (graphUI != null)
        {
            // 一旦リセット
            graphUI.ResetGraph();

            // 0年目の開始点（初期資産）を 1 点だけ登録
            graphUI.AddPoint(0, assetAtStartOfYear);
        }

        // 月別グラフ（表示だけクリア）
        if (monthlyGraphUI != null)
        {
            // 初期状態ではデータなし＆オフセットなし
            monthlyGraphUI.SetMonthlyData(null, false);
        }
    }

    //========================
    //  利率計算（イベント＋ランダムブレ込み）
    //========================

    /// <summary>
    /// 現在のリスクタイプとイベント種別に応じて、
    /// その月の利回り（例: 0.01 → +1%）を返す。
    /// </summary>
    private float GetMonthlyRate(EconomicEventType evType)
    {
        // 1. リスク別のベース年率
        float yearly = middleRiskReturnRate;
        if (currentRiskType == 0) yearly = lowRiskReturnRate;
        else if (currentRiskType == 2) yearly = highRiskReturnRate;

        // 年率 → 月率（複利ベース）
        float baseMonthly = Mathf.Pow(1f + yearly, 1f / 12f) - 1f;

        // 2. イベント補正
        float eventDelta = 0f;
        switch (evType)
        {
            case EconomicEventType.Boom:      // 好景気
                eventDelta = 0.01f;          // +1%
                break;

            case EconomicEventType.Recession: // 不景気
                eventDelta = -0.01f;         // -1%
                break;

            case EconomicEventType.Shock:     // ショック
                eventDelta = -0.05f;         // -5%
                break;

            case EconomicEventType.None:
            default:
                eventDelta = 0f;
                break;
        }

        // 3. リスク別ランダムブレ
        float noiseAmp;
        switch (currentRiskType)
        {
            case 0: // 低リスク
                noiseAmp = 0.01f;   // ±1%
                break;
            case 1: // 中リスク
                noiseAmp = 0.02f;   // ±2%
                break;
            case 2: // 高リスク
                noiseAmp = 0.04f;   // ±4%
                break;
            default:
                noiseAmp = 0.02f;
                break;
        }

        float noise = Random.Range(-noiseAmp, noiseAmp);

        // 4. 最終的な月利
        float monthlyRate = baseMonthly + eventDelta + noise;

        return monthlyRate;
    }

    //========================
    //  景気イベントスケジュール生成
    //========================

    /// <summary>
    /// その年の 12ヶ月分の景気イベントスケジュールを作る。
    /// - 少なくとも1つイベントを入れる
    /// - 20%の確率で2つ目のイベントも入れる
    /// - イベントの種類と期間は仕様どおり
    /// </summary>
    private EconomicEventType[] GenerateEventsForOneYear()
    {
        EconomicEventType[] schedule = new EconomicEventType[12];
        for (int i = 0; i < 12; i++)
        {
            schedule[i] = EconomicEventType.None;
        }

        // 必ず1つはイベントを入れる
        PlaceRandomEvent(schedule);

        // 低確率で2つ目のイベント
        float secondProb = 0.2f;  // 「低確率」イメージ
        if (Random.value < secondProb)
        {
            PlaceRandomEvent(schedule);
        }

        return schedule;
    }

    /// <summary>
    /// 空いている月の範囲にランダムなイベントを1つ配置する。
    /// 空きがない場合は何もしない。
    /// </summary>
    private void PlaceRandomEvent(EconomicEventType[] schedule)
    {
        // 1. イベント種別を確率で決める
        EconomicEventType type = DrawEventType();

        // 2. 種別ごとの期間
        int duration = GetEventDuration(type);

        // 3. 配置可能な開始月を探す（重ならない場所）
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
            if (canPlace)
            {
                candidateStarts.Add(start);
            }
        }

        if (candidateStarts.Count == 0)
        {
            // 置く場所がないので諦め
            return;
        }

        // 4. 候補の中からランダムに開始月を選ぶ
        int chosenIndex = Random.Range(0, candidateStarts.Count);
        int chosenStart = candidateStarts[chosenIndex];

        for (int m = chosenStart; m < chosenStart + duration; m++)
        {
            schedule[m] = type;
        }
    }

    /// <summary>
    /// イベント種別を確率（好景気50%, 不景気38%, ショック12%）で1つ抽選
    /// </summary>
    private EconomicEventType DrawEventType()
    {
        float r = Random.value; // 0〜1

        // 好景気 50%
        if (r < 0.50f) return EconomicEventType.Boom;

        // 不景気 38%（0.50〜0.88）
        if (r < 0.50f + 0.38f) return EconomicEventType.Recession;

        // 残り 12% はショック
        return EconomicEventType.Shock;
    }

    /// <summary>
    /// 種別ごとの期間（単位：ヶ月）
    /// 不景気: 3〜6ヶ月 / 好景気: 5〜8ヶ月 / ショック: 1〜3ヶ月
    /// </summary>
    private int GetEventDuration(EconomicEventType type)
    {
        switch (type)
        {
            case EconomicEventType.Recession:  // 3〜6ヶ月
                return Random.Range(3, 7);     // 上限は排他的なので 7 → 3〜6

            case EconomicEventType.Boom:       // 5〜8ヶ月
                return Random.Range(5, 9);     // 5〜8

            case EconomicEventType.Shock:      // 1〜3ヶ月
                return Random.Range(1, 4);     // 1〜3

            case EconomicEventType.None:
            default:
                return 0;
        }
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
            // インデックス0（1年目）のときだけ開始点オフセットを有効にする
            bool useOffset = (index == 0);
            monthlyGraphUI.SetMonthlyData(monthlyAssetsPerYear[index], useOffset);
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
        if (!isOn)
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
