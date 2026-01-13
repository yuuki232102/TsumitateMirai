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

    [Header("縦軸設定（0中心・±上限 値）")]
    [SerializeField] private float defaultAbsMax = 800000f;
    [SerializeField] private float axisMargin = 50000f;

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    [Header("Y軸ラベルの親（3つ推奨）")]
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

        float minAsset, maxAsset;

        if (monthlyAssets.Count == 0)
        {
            minAsset = -defaultAbsMax;
            maxAsset = defaultAbsMax;
            UpdateYAxisLabels(minAsset, maxAsset);
            UpdateXAxisLabels(12);
            return;
        }

        float rawMin = monthlyAssets[0];
        float rawMax = monthlyAssets[0];

        for (int i = 1; i < monthlyAssets.Count; i++)
        {
            if (monthlyAssets[i] < rawMin) rawMin = monthlyAssets[i];
            if (monthlyAssets[i] > rawMax) rawMax = monthlyAssets[i];
        }

        float absMaxData = Mathf.Max(Mathf.Abs(rawMin), Mathf.Abs(rawMax));
        absMaxData += axisMargin;

        float finalAbsMax = Mathf.Max(absMaxData, defaultAbsMax);

        minAsset = -finalAbsMax;
        maxAsset = finalAbsMax;

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

        float absMax = Mathf.Max(Mathf.Abs(minAsset), Mathf.Abs(maxAsset));

        for (int i = 0; i < n; i++)
        {
            float value;
            if (n == 3)
            {
                if (i == 0) value = absMax;           // 上
                else if (i == 1) value = 0f;         // 中央
                else value = -absMax;                // 下
            }
            else
            {
                float t = n == 1 ? 0f : (float)i / (n - 1);
                value = Mathf.Lerp(-absMax, absMax, t);
            }

            int vi = Mathf.RoundToInt(value);
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
