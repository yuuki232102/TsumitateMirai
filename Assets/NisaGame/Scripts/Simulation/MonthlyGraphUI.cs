using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MonthlyGraphUI : MonoBehaviour
{
    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;
    [SerializeField] private RectTransform linePrefab;

    [Header("縦軸設定（デフォルト）")]
    [SerializeField] private float defaultMinAsset = -30000f;
    [SerializeField] private float defaultMaxAsset = 70000f;
    [SerializeField] private float axisMargin = 5000f;

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    [Header("Y軸ラベルの親（子に Text を並べる）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    [SerializeField] private TMP_Text[] yAxisLabels;

    private readonly List<int> monthlyAssets = new List<int>();

    private void Awake()
    {
        CacheYAxisLabels();
    }

    private void CacheYAxisLabels()
    {
        if (yAxisLabelsRoot == null) return;
        yAxisLabels = yAxisLabelsRoot.GetComponentsInChildren<TMP_Text>();
    }

    //========================================================
    // 外部インターフェース
    //========================================================

    /// <summary>指定された年の「月次資産推移」をセット＆再描画</summary>
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
    // 内部処理
    //========================================================

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

        if (monthlyAssets.Count == 0)
        {
            UpdateYAxisLabels(defaultMinAsset, defaultMaxAsset);
            return;
        }

        float minAsset = monthlyAssets[0];
        float maxAsset = monthlyAssets[0];

        for (int i = 1; i < monthlyAssets.Count; i++)
        {
            if (monthlyAssets[i] < minAsset) minAsset = monthlyAssets[i];
            if (monthlyAssets[i] > maxAsset) maxAsset = monthlyAssets[i];
        }

        minAsset -= axisMargin;
        maxAsset += axisMargin;

        if (minAsset > 0) minAsset = 0;
        if (maxAsset < 0) maxAsset = 0;

        if (Mathf.Approximately(minAsset, maxAsset))
        {
            minAsset -= 1000f;
            maxAsset += 1000f;
        }

        UpdateYAxisLabels(minAsset, maxAsset);

        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        int count = monthlyAssets.Count;
        if (count == 1) count = 2; // 1点だけの時に 0除算防止

        for (int i = 0; i < monthlyAssets.Count; i++)
        {
            float tX = (float)i / (count - 1); // 0〜1 に正規化
            float x = tX * width;

            float tY = Mathf.InverseLerp(minAsset, maxAsset, monthlyAssets[i]);
            float y = tY * height;

            Vector2 pos = new Vector2(x, y);

            if (pointPrefab != null)
            {
                RectTransform p = Instantiate(pointPrefab, graphRect);
                p.anchorMin = p.anchorMax = new Vector2(0f, 0f);
                p.anchoredPosition = pos;
                p.sizeDelta = new Vector2(pointSize, pointSize);
            }

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
            float t = (float)i / (n - 1);
            float v = Mathf.Lerp(minAsset, maxAsset, t);
            int vi = Mathf.RoundToInt(v);

            yAxisLabels[i].text = $"{vi.ToString("N0")}円";
        }
    }
}
