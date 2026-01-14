using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonthlyGraphUI : MonoBehaviour
{
    //========================================================
    //  インスペクタ設定
    //========================================================

    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;
    [SerializeField] private RectTransform linePrefab;

    [Header("縦軸設定（0中心・±上限 値）")]
    // 0年目の標準レンジの絶対値（±defaultAbsMax を基準にして、
    // 実データが大きくなったらその都度スケールアップします）
    [SerializeField] private float defaultAbsMax = 800000f;
    [SerializeField] private float axisMargin = 50000f;   // データに上乗せする余白

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    [Header("Y軸ラベルの親（3つ推奨）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    [Header("X軸ラベルの親")]
    [SerializeField] private RectTransform xAxisLabelsRoot;

    //========================================================
    //  内部状態
    //========================================================

    private TMP_Text[] yAxisLabelsTMP;
    private Text[] yAxisLabelsUGUI;

    private TMP_Text[] xAxisLabelsTMP;
    private Text[] xAxisLabelsUGUI;

    // その年の 12ヶ月分の資産推移
    private readonly List<int> monthlyAssets = new List<int>();

    //========================================================
    //  Unity ライフサイクル
    //========================================================

    private void Awake()
    {
        CacheAxisLabels();
    }

    private void CacheAxisLabels()
    {
        if (yAxisLabelsRoot != null)
        {
            yAxisLabelsTMP = yAxisLabelsRoot.GetComponentsInChildren<TMP_Text>();
            yAxisLabelsUGUI = yAxisLabelsRoot.GetComponentsInChildren<Text>();
        }

        if (xAxisLabelsRoot != null)
        {
            xAxisLabelsTMP = xAxisLabelsRoot.GetComponentsInChildren<TMP_Text>();
            xAxisLabelsUGUI = xAxisLabelsRoot.GetComponentsInChildren<Text>();
        }
    }

    //========================================================
    //  外部インターフェース
    //========================================================

    /// <summary>指定された年の月次資産推移をセット＆描画</summary>
    public void SetMonthlyData(List<int> assetsForYear)
    {
        monthlyAssets.Clear();

        if (assetsForYear != null && assetsForYear.Count > 0)
        {
            monthlyAssets.AddRange(assetsForYear);
        }

        RebuildGraph();
    }

    //========================================================
    //  描画
    //========================================================

    /// <summary>グラフの子要素（点・線）を全削除</summary>
    private void ClearGraphVisuals()
    {
        if (graphRect == null) return;

        for (int i = graphRect.childCount - 1; i >= 0; i--)
        {
            Destroy(graphRect.GetChild(i).gameObject);
        }
    }

    /// <summary>内部データからグラフを再構築</summary>
    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();

        float minAsset;
        float maxAsset;

        //----------------------------------------------------
        // 1. Y軸レンジ計算（0 を中心にした ±上限）
        //----------------------------------------------------
        if (monthlyAssets.Count == 0)
        {
            // データが無いときはデフォルトレンジで軸だけ表示
            minAsset = -defaultAbsMax;
            maxAsset = defaultAbsMax;

            UpdateYAxisLabels(minAsset, maxAsset);
            UpdateXAxisLabels(12); // 12ヶ月分の目盛り
            return;
        }

        float rawMin = monthlyAssets[0];
        float rawMax = monthlyAssets[0];

        for (int i = 1; i < monthlyAssets.Count; i++)
        {
            if (monthlyAssets[i] < rawMin) rawMin = monthlyAssets[i];
            if (monthlyAssets[i] > rawMax) rawMax = monthlyAssets[i];
        }

        // 絶対値で一番大きい値を基準に
        float absMaxData = Mathf.Max(Mathf.Abs(rawMin), Mathf.Abs(rawMax));

        // 余白を追加
        absMaxData += axisMargin;

        // デフォルト上限と比較して大きい方を採用
        float finalAbsMax = Mathf.Max(absMaxData, defaultAbsMax);

        // 最終的なレンジ（上: +finalAbsMax / 下: -finalAbsMax）
        minAsset = -finalAbsMax;
        maxAsset = finalAbsMax;

        UpdateYAxisLabels(minAsset, maxAsset);
        UpdateXAxisLabels(monthlyAssets.Count);

        //----------------------------------------------------
        // 2. 点と線の描画
        //----------------------------------------------------
        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        int count = monthlyAssets.Count;
        if (count <= 1) count = 2;   // 1点だけでも左端/右端を計算できるように

        for (int i = 0; i < monthlyAssets.Count; i++)
        {
            // X：左0〜右1 に正規化
            float tX = (float)i / (count - 1);
            float x = tX * width;

            // Y：下 minAsset〜上 maxAsset を 0〜1 に正規化
            float tY = Mathf.InverseLerp(minAsset, maxAsset, monthlyAssets[i]);
            float y = tY * height;

            Vector2 pos = new Vector2(x, y);

            // ---- 点 ----
            if (pointPrefab != null)
            {
                RectTransform p = Instantiate(pointPrefab, graphRect);
                p.anchorMin = p.anchorMax = new Vector2(0f, 0f);
                p.anchoredPosition = pos;
                p.sizeDelta = new Vector2(pointSize, pointSize);
            }

            // ---- 線 ----
            if (hasPrev && linePrefab != null)
            {
                RectTransform line = Instantiate(linePrefab, graphRect);
                line.anchorMin = line.anchorMax = new Vector2(0f, 0f);

                Vector2 dir = pos - previousPos;
                float dist = dir.magnitude;

                line.sizeDelta = new Vector2(dist, lineThickness);
                line.anchoredPosition = previousPos + dir * 0.5f;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                line.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            previousPos = pos;
            hasPrev = true;
        }
    }

    //========================================================
    //  軸ラベル
    //========================================================

    /// <summary>Y軸ラベル（上:+上限 / 中央:0 / 下:-上限）</summary>
    private void UpdateYAxisLabels(float minAsset, float maxAsset)
    {
        if (yAxisLabelsRoot == null) return;

        if ((yAxisLabelsTMP == null || yAxisLabelsTMP.Length == 0) &&
            (yAxisLabelsUGUI == null || yAxisLabelsUGUI.Length == 0))
        {
            CacheAxisLabels();
        }

        int nTMP = (yAxisLabelsTMP != null) ? yAxisLabelsTMP.Length : 0;
        int nGUI = (yAxisLabelsUGUI != null) ? yAxisLabelsUGUI.Length : 0;
        int n = Mathf.Max(nTMP, nGUI);
        if (n == 0) return;

        float absMax = Mathf.Max(Mathf.Abs(minAsset), Mathf.Abs(maxAsset));
        int absInt = Mathf.RoundToInt(absMax);

        string topText = $"{absInt.ToString("N0")}円";
        string middleText = "0円";
        string bottomText = $"-{absInt.ToString("N0")}円";

        for (int i = 0; i < n; i++)
        {
            string text;
            if (n >= 3)
            {
                if (i == 0) text = topText;    // 一番上
                else if (i == 1) text = middleText; // 真ん中
                else text = bottomText; // 一番下
            }
            else
            {
                // 3つ以外のときは保険として線形配置
                float t = (n == 1) ? 0f : (float)i / (n - 1);
                float v = Mathf.Lerp(-absMax, absMax, t);
                int vi = Mathf.RoundToInt(v);
                text = $"{vi.ToString("N0")}円";
            }

            if (i < nTMP && yAxisLabelsTMP[i] != null) yAxisLabelsTMP[i].text = text;
            if (i < nGUI && yAxisLabelsUGUI[i] != null) yAxisLabelsUGUI[i].text = text;
        }
    }

    /// <summary>X軸ラベル（月）</summary>
    private void UpdateXAxisLabels(int monthCount)
    {
        if (xAxisLabelsRoot == null) return;

        if ((xAxisLabelsTMP == null || xAxisLabelsTMP.Length == 0) &&
            (xAxisLabelsUGUI == null || xAxisLabelsUGUI.Length == 0))
        {
            CacheAxisLabels();
        }

        int nTMP = (xAxisLabelsTMP != null) ? xAxisLabelsTMP.Length : 0;
        int nGUI = (xAxisLabelsUGUI != null) ? xAxisLabelsUGUI.Length : 0;
        int n = Mathf.Max(nTMP, nGUI);
        if (n == 0 || monthCount <= 0) return;

        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (float)i / (n - 1);
            int month = Mathf.Clamp(Mathf.RoundToInt(t * (monthCount - 1)) + 1, 1, monthCount);
            string text = $"{month}月";

            if (i < nTMP && xAxisLabelsTMP[i] != null) xAxisLabelsTMP[i].text = text;
            if (i < nGUI && xAxisLabelsUGUI[i] != null) xAxisLabelsUGUI[i].text = text;
        }
    }
}
