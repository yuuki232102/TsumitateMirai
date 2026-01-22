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

    private bool isUpdatingToggles = false;
    private bool isUpdatingSlider = false;

    private ResultDataStore store;

    private void Start()
    {
        store = ResultDataStore.Instance;
        if (store == null || !store.HasData)
        {
            // データが無い場合はタイトルへ（安全策）
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        // サマリー
        ApplySummary();

        // トグル
        if (graphYearlyToggle != null)
            graphYearlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(true, isOn));

        if (graphMonthlyToggle != null)
            graphMonthlyToggle.onValueChanged.AddListener(isOn => OnGraphToggleChanged(false, isOn));

        // 年選択スライダー（0..N）
        SetupYearSlider();

        // グラフ描画
        DrawYearlyGraph();
        DrawMonthlyGraphForYear(GetSelectedYear());

        // 初期表示：年別
        isUpdatingToggles = true;
        if (graphYearlyToggle != null) graphYearlyToggle.isOn = true;
        if (graphMonthlyToggle != null) graphMonthlyToggle.isOn = false;
        isUpdatingToggles = false;
        ApplyGraphMode(true);

        // ボタン
        if (backToTitleButton != null)
            backToTitleButton.onClick.AddListener(() => SceneManager.LoadScene(titleSceneName));

        if (retryButton != null)
            retryButton.onClick.AddListener(() => SceneManager.LoadScene(simulationSceneName));
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

    private void SetupYearSlider()
    {
        if (yearSelectSlider == null) return;

        int maxYear = Mathf.Max(0, (store.YearlyAssets0ToN != null ? store.YearlyAssets0ToN.Count - 1 : 0));

        yearSelectSlider.minValue = 0;
        yearSelectSlider.maxValue = maxYear;
        yearSelectSlider.wholeNumbers = true;

        isUpdatingSlider = true;
        yearSelectSlider.value = maxYear; // デフォルトは最終年
        isUpdatingSlider = false;

        yearSelectSlider.onValueChanged.AddListener(OnYearSliderChanged);

        UpdateYearLabel((int)yearSelectSlider.value);
    }

    private int GetSelectedYear()
    {
        if (yearSelectSlider == null) return 0;
        return Mathf.RoundToInt(yearSelectSlider.value);
    }

    private void OnYearSliderChanged(float v)
    {
        if (isUpdatingSlider) return;

        int year = Mathf.RoundToInt(v);
        UpdateYearLabel(year);
        DrawMonthlyGraphForYear(year);
    }

    private void UpdateYearLabel(int year)
    {
        if (yearSelectLabel == null) return;
        yearSelectLabel.text = $"選択中：{year}年目の月別グラフ";
    }

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
                if (!string.IsNullOrEmpty(store.YearlyEventLabels0ToN[y]))
                    label = store.YearlyEventLabels0ToN[y];
            }

            // 0年目も描く
            yearlyGraphUI.AddPoint(y, asset, label);
        }
    }

    private void DrawMonthlyGraphForYear(int year)
    {
        if (monthlyGraphUI == null) return;

        // 0年目：全部0で表示（0月点 + 12ヶ月末）
        if (year <= 0)
        {
            var zeros12 = new List<int>(12);
            for (int i = 0; i < 12; i++) zeros12.Add(0);

            var events12_zero = new List<EconomicEventType>(12);
            for (int i = 0; i < 12; i++) events12_zero.Add(EconomicEventType.None);

            monthlyGraphUI.SetMonthlyDataWithStartPoint(0, zeros12, events12_zero, 0);
            return;
        }

        int idx = year - 1; // 1年目→index0

        int startAsset = (store.YearStartAssets1ToN != null && idx >= 0 && idx < store.YearStartAssets1ToN.Count)
            ? store.YearStartAssets1ToN[idx]
            : 0;

        List<int> list12 = (store.MonthlyAssetsPerYear1ToN != null && idx >= 0 && idx < store.MonthlyAssetsPerYear1ToN.Count)
            ? store.MonthlyAssetsPerYear1ToN[idx]
            : null;

        // 保険：nullなら0埋め
        if (list12 == null) list12 = new List<int>(12);
        while (list12.Count < 12) list12.Add(list12.Count > 0 ? list12[list12.Count - 1] : startAsset);
        if (list12.Count > 12) list12.RemoveRange(12, list12.Count - 12);

        // イベント12個
        var events12 = new List<EconomicEventType>(12);
        EconomicEventType[] evArr =
            (store.YearlyEvents1ToN != null && idx >= 0 && idx < store.YearlyEvents1ToN.Count)
                ? store.YearlyEvents1ToN[idx]
                : null;

        if (evArr != null) events12.AddRange(evArr);
        while (events12.Count < 12) events12.Add(EconomicEventType.None);
        if (events12.Count > 12) events12.RemoveRange(12, events12.Count - 12);

        monthlyGraphUI.SetMonthlyDataWithStartPoint(startAsset, list12, events12, year);
    }


    //========================
    // グラフ表示切り替え
    //========================
    private void OnGraphToggleChanged(bool yearlyToggle, bool isOn)
    {
        if (!isOn) return;
        if (isUpdatingToggles) return;

        bool showYearly = yearlyToggle;

        isUpdatingToggles = true;
        if (graphYearlyToggle != null) graphYearlyToggle.isOn = showYearly;
        if (graphMonthlyToggle != null) graphMonthlyToggle.isOn = !showYearly;
        isUpdatingToggles = false;

        ApplyGraphMode(showYearly);
    }

    private void ApplyGraphMode(bool showYearly)
    {
        if (yearlyGraphRoot != null) yearlyGraphRoot.SetActive(showYearly);
        if (monthlyGraphRoot != null) monthlyGraphRoot.SetActive(!showYearly);
    }
}
