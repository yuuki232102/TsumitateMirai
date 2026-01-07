using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimulationGraphUI : MonoBehaviour
{
    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;     // GraphContent

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;   // GraphPointPrefab
    [SerializeField] private RectTransform linePrefab;    // GraphLinePrefab

    [Header("基本設定")]
    [SerializeField] private int maxYear = 15;

    [Header("縦軸設定（デフォルト）")]
    [SerializeField] private float defaultMinAsset = -30000f;
    [SerializeField] private float defaultMaxAsset = 70000f;
    [SerializeField] private float axisMargin = 5000f;

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    [Header("Y軸ラベルの親（子に Text を並べる）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    // 実際のラベル配列（親から自動取得）
    [SerializeField] private TMP_Text[] yAxisLabels;

    // 内部データ：年ごとの資産
    private readonly List<int> years = new List<int>();
    private readonly List<int> assets = new List<int>();

    private void Awake()
    {
        CacheYAxisLabels();
    }

    private void CacheYAxisLabels()
    {
        if (yAxisLabelsRoot == null) return;
        yAxisLabels = yAxisLabelsRoot.GetComponentsInChildren<TMP_Text>();
    }

    //================================================================
    // 外部インターフェース
    //================================================================

    /// <summary>グラフをリセット（全消去）</summary>
    public void ResetGraph()
    {
        years.Clear();
        assets.Clear();
        ClearGraphVisuals();
        UpdateYAxisLabels(defaultMinAsset, defaultMaxAsset);
    }

    /// <summary>年と資産を追加し、グラフを描き直す</summary>
    public void AddPoint(int yearIndex, int asset)
    {
        years.Add(yearIndex);
        assets.Add(asset);
        RebuildGraph();
    }

    //================================================================
    // 内部処理
    //================================================================

    private void ClearGraphVisuals()
    {
        if (graphRect == null) return;

        for (int i = graphRect.childCount - 1; i >= 0; i--)
        {
            Destroy(graphRect.GetChild(i).gameObject);
        }
    }

    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();

        if (years.Count == 0)
        {
            UpdateYAxisLabels(defaultMinAsset, defaultMaxAsset);
            return;
        }

        // -------------------------
        // Y軸レンジ計算
        // -------------------------
        float minAsset = assets[0];
        float maxAsset = assets[0];

        for (int i = 1; i < assets.Count; i++)
        {
            if (assets[i] < minAsset) minAsset = assets[i];
            if (assets[i] > maxAsset) maxAsset = assets[i];
        }

        // ±方向に少し余白を持たせる
        minAsset -= axisMargin;
        maxAsset += axisMargin;

        // 0 を必ず範囲に含める（プラスだけ／マイナスだけのときも見やすく）
        if (minAsset > 0) minAsset = 0;
        if (maxAsset < 0) maxAsset = 0;

        if (Mathf.Approximately(minAsset, maxAsset))
        {
            // 全部同じ値のときに線が潰れないように幅を作る
            minAsset -= 1000f;
            maxAsset += 1000f;
        }

        UpdateYAxisLabels(minAsset, maxAsset);

        // -------------------------
        // 点と線を描画
        // -------------------------
        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        for (int i = 0; i < years.Count; i++)
        {
            // X：年 (0 ~ maxYear)
            float tX = maxYear > 0 ? (float)years[i] / maxYear : 0f;
            float x = tX * width;

            // Y：資産を 0〜1 に正規化
            float tY = Mathf.InverseLerp(minAsset, maxAsset, assets[i]);
            float y = tY * height;

            Vector2 pos = new Vector2(x, y);

            // 点
            if (pointPrefab != null)
            {
                RectTransform p = Instantiate(pointPrefab, graphRect);
                p.anchorMin = p.anchorMax = new Vector2(0f, 0f);
                p.anchoredPosition = pos;
                p.sizeDelta = new Vector2(pointSize, pointSize);
            }

            // 線（前の点と現在の点を結ぶ）
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

    private void UpdateYAxisLabels(float minAsset, float maxAsset)
    {
        if (yAxisLabelsRoot == null) return;

        if (yAxisLabels == null || yAxisLabels.Length == 0)
        {
            CacheYAxisLabels();
        }

        if (yAxisLabels == null || yAxisLabels.Length == 0) return;

        int n = yAxisLabels.Length;

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / (n - 1);          // 下0 ～ 上1
            float v = Mathf.Lerp(minAsset, maxAsset, t);
            int vi = Mathf.RoundToInt(v);

            yAxisLabels[i].text = $"{vi.ToString("N0")}円";
        }
    }
}
