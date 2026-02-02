using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ResultScene のUI制御
/// ・ResultDataStore からデータ取得
/// ・サマリー表示（最終資産/元本/損益）
/// ・年別グラフ（0..N）
/// ・月別グラフ（年選択スライダー：0..N）
/// ・トグル切替（年別/年内月別）
/// </summary>
public class ResultSceneManager : MonoBehaviour
{
    [Header("Summary UI")]
    [SerializeField] private TMP_Text finalAssetText;
    [SerializeField] private TMP_Text principalText;
    [SerializeField] private TMP_Text profitText;

    [Header("Graph Mode")]
    [SerializeField] private Toggle graphYearlyToggle;
    [SerializeField] private Toggle graphMonthlyToggle;
    [SerializeField] private GameObject yearlyGraphRoot;
    [SerializeField] private GameObject monthlyGraphRoot;

    [Header("Yearly Graph UI")]
    [SerializeField] private SimulationGraphUI yearlyGraphUI;

    [Header("Monthly Graph UI")]
    [SerializeField] private MonthlyGraphUI monthlyGraphUI;
    [SerializeField] private Slider yearSelectSlider;   // 0..N
    [SerializeField] private TMP_Text yearSelectLabel;  // “選択中：X年目…”

    [Header("Buttons (Optional)")]
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private Button retryButton;

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string simulationSceneName = "SimulationScene";

    [Header("Debug (Optional)")]
    [SerializeField] private bool debugLog = false;

    private bool isUpdatingToggles = false;
    private bool isUpdatingSlider = false;

    private ResultDataStore store;

    private void Start()
    {
        store = ResultDataStore.Instance;

        // データが無い場合はタイトルへ（安全策）
        if (store == null || !store.HasData)
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        // まずサマリー
        ApplySummary();

        // UIイベント登録（先に登録してOKだが、初期化時はNotify無しで値を入れる）
        if (graphYearlyToggle != null)
            graphYearlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(true, isOn));

        if (graphMonthlyToggle != null)
            graphMonthlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(false, isOn));

        if (yearSelectSlider != null)
            yearSelectSlider.onValueChanged.AddListener(OnYearSliderChanged);

        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(() => SceneManager.LoadScene(titleSceneName));

        if (retryButton != null)
            retryButton.onClick.AddListener(() => SceneManager.LoadScene(simulationSceneName));

        // レイアウト確定後にグラフ描画（RectTransform/Canvas更新待ち）
        StartCoroutine(InitializeAfterOneFrame());
    }

    private IEnumerator InitializeAfterOneFrame()
    {
        // 1フレーム待つ（UIレイアウトが確定してから描く）
        yield return null;

        // スライダー初期化（最後の年を選択）
        SetupYearSlider_SelectLastYear();

        // 年別グラフ描画
        DrawYearlyGraph();

        // 月別グラフ描画（選択年）
        int selectedYear = GetSelectedYearSafe();
        UpdateYearLabel(selectedYear);
        DrawMonthlyGraphForYear(selectedYear);

        // 初期表示：年別
        isUpdatingToggles = true;
        if (graphYearlyToggle != null) graphYearlyToggle.SetIsOnWithoutNotify(true);
        if (graphMonthlyToggle != null) graphMonthlyToggle.SetIsOnWithoutNotify(false);
        isUpdatingToggles = false;
        ApplyGraphMode(showYearly: true);

        if (debugLog)
        {
            Debug.Log($"[Result] Init done. maxYear={GetMaxYearFromStore()} selectedYear={selectedYear} " +
                      $"monthlyCount={(store.MonthlyAssetsPerYear1ToN != null ? store.MonthlyAssetsPerYear1ToN.Count : 0)} " +
                      $"startCount={(store.YearStartAssets1ToN != null ? store.YearStartAssets1ToN.Count : 0)}");
        }
    }

    private void ApplySummary()
    {
        int finalAsset = store.FinalAsset;
        int principal = store.TotalPrincipal;
        int profit = finalAsset - principal;

        if (finalAssetText != null) finalAssetText.text = $"{finalAsset:N0}円";
        if (principalText != null) principalText.text = $"{principal:N0}円";
        if (profitText != null) profitText.text = $"{profit:N0}円";
    }

    //========================
    // Slider
    //========================
    private int GetMaxYearFromStore()
    {
        // YearlyAssets0ToN は 0..N なので Count-1 が最大年
        if (store.YearlyAssets0ToN == null) return 0;
        return Mathf.Max(0, store.YearlyAssets0ToN.Count - 1);
    }

    private void SetupYearSlider_SelectLastYear()
    {
        if (yearSelectSlider == null) return;

        int maxYear = GetMaxYearFromStore();

        isUpdatingSlider = true;

        yearSelectSlider.minValue = 0;
        yearSelectSlider.maxValue = maxYear;
        yearSelectSlider.wholeNumbers = true;

        // ★重要：Notify無しで値を入れる（初期化時のOnValueChanged暴発を防ぐ）
        yearSelectSlider.SetValueWithoutNotify(maxYear);

        isUpdatingSlider = false;

        // ラベルは明示更新
        UpdateYearLabel(maxYear);
    }

    private int GetSelectedYearSafe()
    {
        if (yearSelectSlider == null) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(yearSelectSlider.value), 0, GetMaxYearFromStore());
    }

    private void OnYearSliderChanged(float v)
    {
        if (isUpdatingSlider) return;

        int year = Mathf.Clamp(Mathf.RoundToInt(v), 0, GetMaxYearFromStore());
        UpdateYearLabel(year);

        // 月別表示中でなくても、データは更新しておく（切替時に即表示できる）
        DrawMonthlyGraphForYear(year);

        if (debugLog)
            Debug.Log($"[Result] Slider changed -> year={year}");
    }

    private void UpdateYearLabel(int year)
    {
        if (yearSelectLabel == null) return;

        if (year <= 0)
            yearSelectLabel.text = $"選択中：{year}年目（開始時）";
        else
            yearSelectLabel.text = $"選択中：{year}年目";
    }

    //========================
    // Draw Graphs
    //========================
    private void DrawYearlyGraph()
    {
        if (yearlyGraphUI == null) return;
        if (store.YearlyAssets0ToN == null || store.YearlyAssets0ToN.Count == 0) return;

        yearlyGraphUI.ResetGraph();

        for (int y = 0; y < store.YearlyAssets0ToN.Count; y++)
        {
            int asset = store.YearlyAssets0ToN[y];

            string label = "平常";
            if (store.YearlyEventLabels0ToN != null && y < store.YearlyEventLabels0ToN.Count)
            {
                var s = store.YearlyEventLabels0ToN[y];
                if (!string.IsNullOrEmpty(s)) label = s;
            }

            yearlyGraphUI.AddPoint(y, asset, label);
        }
    }

    private void DrawMonthlyGraphForYear(int year)
    {
        if (monthlyGraphUI == null) return;

        // 0年目：全部0（0月点 + 12ヶ月末）
        if (year <= 0)
        {
            var monthEnds12_zero = new List<int>(12);
            for (int i = 0; i < 12; i++) monthEnds12_zero.Add(0);

            var events12_zero = new List<EconomicEventType>(12);
            for (int i = 0; i < 12; i++) events12_zero.Add(EconomicEventType.None);

            monthlyGraphUI.SetMonthlyDataWithStartPoint(0, monthEnds12_zero, events12_zero, 0);
            return;
        }

        int idx = year - 1; // 1年目→index0

        // 年初資産
        int startAsset = 0;
        if (store.YearStartAssets1ToN != null && idx >= 0 && idx < store.YearStartAssets1ToN.Count)
            startAsset = store.YearStartAssets1ToN[idx];

        // 月末資産（12個） ※必ずコピーして扱う
        List<int> monthEnds12 = null;
        if (store.MonthlyAssetsPerYear1ToN != null && idx >= 0 && idx < store.MonthlyAssetsPerYear1ToN.Count)
        {
            var src = store.MonthlyAssetsPerYear1ToN[idx];
            if (src != null) monthEnds12 = new List<int>(src);
        }
        if (monthEnds12 == null) monthEnds12 = new List<int>(12);

        // 12に正規化
        while (monthEnds12.Count < 12)
        {
            int prev = (monthEnds12.Count > 0) ? monthEnds12[monthEnds12.Count - 1] : startAsset;
            monthEnds12.Add(prev);
        }
        if (monthEnds12.Count > 12) monthEnds12.RemoveRange(12, monthEnds12.Count - 12);

        // イベント12個 ※必ず List 化して12に正規化
        var events12 = new List<EconomicEventType>(12);
        EconomicEventType[] evArr = null;
        if (store.YearlyEvents1ToN != null && idx >= 0 && idx < store.YearlyEvents1ToN.Count)
            evArr = store.YearlyEvents1ToN[idx];

        if (evArr != null) events12.AddRange(evArr);

        while (events12.Count < 12) events12.Add(EconomicEventType.None);
        if (events12.Count > 12) events12.RemoveRange(12, events12.Count - 12);

        if (debugLog)
        {
            Debug.Log($"[Result] DrawMonthly year={year} idx={idx} start={startAsset} " +
                      $"m0={monthEnds12[0]} m11={monthEnds12[11]} evCount={events12.Count}");
        }

        monthlyGraphUI.SetMonthlyDataWithStartPoint(startAsset, monthEnds12, events12, year);
    }

    //========================
    // Toggle
    //========================
    private void OnGraphToggleChanged(bool yearlyToggle, bool isOn)
    {
        if (!isOn) return;
        if (isUpdatingToggles) return;

        bool showYearly = yearlyToggle;

        isUpdatingToggles = true;
        if (graphYearlyToggle != null) graphYearlyToggle.SetIsOnWithoutNotify(showYearly);
        if (graphMonthlyToggle != null) graphMonthlyToggle.SetIsOnWithoutNotify(!showYearly);
        isUpdatingToggles = false;

        ApplyGraphMode(showYearly);

        // 切り替えた瞬間に確実に描画しておく
        if (showYearly)
        {
            DrawYearlyGraph();
        }
        else
        {
            int year = GetSelectedYearSafe();
            UpdateYearLabel(year);
            DrawMonthlyGraphForYear(year);
        }
    }

    private void ApplyGraphMode(bool showYearly)
    {
        if (yearlyGraphRoot != null) yearlyGraphRoot.SetActive(showYearly);
        if (monthlyGraphRoot != null) monthlyGraphRoot.SetActive(!showYearly);

        // ★追加：月別グラフのときだけスライダー操作可能
        bool canUseYearSlider = !showYearly;
        if (yearSelectSlider != null) yearSelectSlider.interactable = canUseYearSlider;

        // （任意）ラベルも同様に見た目を変えたい場合
        if (yearSelectLabel != null)
        {
            yearSelectLabel.alpha = canUseYearSlider ? 1f : 0.5f; // TMP_Textならalphaいけます
        }
    }

}
