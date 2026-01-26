using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SimulationScene → ResultScene に結果を渡すためのデータ保管（DontDestroyOnLoad）
/// ・SimulationSceneManager から SetResultData(...) で保存
/// ・ResultSceneManager から Instance を通して参照
/// 
/// ★注意：このファイルには ResultData クラスを定義しない（重複エラー回避）
/// </summary>
public class ResultDataStore : MonoBehaviour
{
    public static ResultDataStore Instance { get; private set; }

    //========================
    // 保存データ（ResultScene側が読む）
    //========================
    public int FinalAsset { get; private set; }
    public int TotalPrincipal { get; private set; }

    // 0年目含む（0..N）
    public List<int> YearlyAssets0ToN { get; private set; } = new List<int>();
    public List<string> YearlyEventLabels0ToN { get; private set; } = new List<string>();

    // 1..N 年分（Count=N）
    public List<int> YearStartAssets1ToN { get; private set; } = new List<int>();
    public List<List<int>> MonthlyAssetsPerYear1ToN { get; private set; } = new List<List<int>>();
    public List<EconomicEventType[]> YearlyEvents1ToN { get; private set; } = new List<EconomicEventType[]>();

    public bool HasData =>
        YearlyAssets0ToN != null && YearlyAssets0ToN.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// ★SimulationSceneManager の呼び出し（名前付き引数）に合わせて
    /// 引数名を完全一致で定義している
    /// </summary>
    public void SetResultData(
        int finalAsset,
        int totalPrincipal,
        List<int> yearlyAssets_0ToN,
        List<string> yearlyEventLabels_0ToN,
        List<int> yearStartAssets_1ToN,
        List<List<int>> monthlyAssetsPerYear_1ToN,
        List<EconomicEventType[]> yearlyEvents_1ToN
    )
    {
        FinalAsset = finalAsset;
        TotalPrincipal = totalPrincipal;

        // 防御的コピー（参照共有で後から書き換わる事故を防ぐ）
        YearlyAssets0ToN = (yearlyAssets_0ToN != null) ? new List<int>(yearlyAssets_0ToN) : new List<int>();
        YearlyEventLabels0ToN = (yearlyEventLabels_0ToN != null) ? new List<string>(yearlyEventLabels_0ToN) : new List<string>();

        YearStartAssets1ToN = (yearStartAssets_1ToN != null) ? new List<int>(yearStartAssets_1ToN) : new List<int>();

        MonthlyAssetsPerYear1ToN = new List<List<int>>();
        if (monthlyAssetsPerYear_1ToN != null)
        {
            for (int i = 0; i < monthlyAssetsPerYear_1ToN.Count; i++)
            {
                var src = monthlyAssetsPerYear_1ToN[i];
                MonthlyAssetsPerYear1ToN.Add(src != null ? new List<int>(src) : new List<int>());
            }
        }

        YearlyEvents1ToN = new List<EconomicEventType[]>();
        if (yearlyEvents_1ToN != null)
        {
            for (int i = 0; i < yearlyEvents_1ToN.Count; i++)
            {
                var src = yearlyEvents_1ToN[i];
                if (src == null) { YearlyEvents1ToN.Add(null); continue; }

                var copy = new EconomicEventType[src.Length];
                System.Array.Copy(src, copy, src.Length);
                YearlyEvents1ToN.Add(copy);
            }
        }
    }

    public void Clear()
    {
        FinalAsset = 0;
        TotalPrincipal = 0;
        YearlyAssets0ToN = new List<int>();
        YearlyEventLabels0ToN = new List<string>();
        YearStartAssets1ToN = new List<int>();
        MonthlyAssetsPerYear1ToN = new List<List<int>>();
        YearlyEvents1ToN = new List<EconomicEventType[]>();
    }
}