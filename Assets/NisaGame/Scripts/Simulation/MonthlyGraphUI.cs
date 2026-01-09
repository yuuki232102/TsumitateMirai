using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    [Header("Y軸ラベルの親")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    [Header("X軸ラベルの親")]
    [SerializeField] private RectTransform xAxisLabelsRoot;

    private TMP_Text[] yAxisLabelsTMP;
    private Text[] yAxisLabelsUGUI;

    private TMP_Text[] xAxisLabelsTMP;
    private Text[] xAxisLabelsUGUI;

    private readonly List<int> monthlyAssets = new List<int>();

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
    // 外部インターフェース
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
    // 描画
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
            UpdateXAxisLabels(12);
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
        UpdateXAxisLabels(monthlyAssets.Count);

        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        int count = monthlyAssets.Count;
        if (count == 1) count = 2;

        for (int i = 0; i < monthlyAssets.Count; i++)
        {
            float tX = (float)i / (count - 1);
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

    //========================================================
    // 軸ラベル
    //========================================================

    private void UpdateYAxisLabels(float minAsset, float maxAsset)
    {
        if (yAxisLabelsRoot == null) return;

        if ((yAxisLabelsTMP == null || yAxisLabelsTMP.Length == 0) &&
            (yAxisLabelsUGUI == null || yAxisLabelsUGUI.Length == 0))
        {
            CacheAxisLabels();
        }

        int nTMP = yAxisLabelsTMP != null ? yAxisLabelsTMP.Length : 0;
        int nGUI = yAxisLabelsUGUI != null ? yAxisLabelsUGUI.Length : 0;
        int n = Mathf.Max(nTMP, nGUI);
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : (float)i / (n - 1);
            float v = Mathf.Lerp(minAsset, maxAsset, t);
            int vi = Mathf.RoundToInt(v);
            string text = $"{vi.ToString("N0")}円";

            if (i < nTMP && yAxisLabelsTMP[i] != null) yAxisLabelsTMP[i].text = text;
            if (i < nGUI && yAxisLabelsUGUI[i] != null) yAxisLabelsUGUI[i].text = text;
        }
    }

    private void UpdateXAxisLabels(int monthCount)
    {
        if (xAxisLabelsRoot == null) return;

        if ((xAxisLabelsTMP == null || xAxisLabelsTMP.Length == 0) &&
            (xAxisLabelsUGUI == null || xAxisLabelsUGUI.Length == 0))
        {
            CacheAxisLabels();
        }

        int nTMP = xAxisLabelsTMP != null ? xAxisLabelsTMP.Length : 0;
        int nGUI = xAxisLabelsUGUI != null ? xAxisLabelsUGUI.Length : 0;
        int n = Mathf.Max(nTMP, nGUI);
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : (float)i / (n - 1);
            int month = Mathf.Clamp(Mathf.RoundToInt(t * (monthCount - 1)) + 1, 1, monthCount);
            string text = $"{month}月";

            if (i < nTMP && xAxisLabelsTMP[i] != null) xAxisLabelsTMP[i].text = text;
            if (i < nGUI && xAxisLabelsUGUI[i] != null) xAxisLabelsUGUI[i].text = text;
        }
    }
}
