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

    [Header("開始点オフセット")]
    [SerializeField] private float startPointYOffset = 0f;

    [Header("Y軸ラベルの親（3つ推奨）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    [Header("X軸ラベルの親")]
    [SerializeField] private RectTransform xAxisLabelsRoot;

    [Header("カーソル表示UI（任意）")]
    [SerializeField] private TMP_Text hoverInfoText;
    [SerializeField] private RectTransform hoverMarker;

    [Header("ホバー感度設定")]
    [SerializeField] private float hoverSnapMaxDistance = 40f;

    [Header("景気イベント帯（背景）")]
    [SerializeField] private Image eventBandPrefab;
    [SerializeField] private Color boomBandColor = new Color(0.8f, 1.0f, 0.6f, 0.35f);
    [SerializeField] private Color recessionBandColor = new Color(0.6f, 0.8f, 1.0f, 0.35f);
    [SerializeField] private Color shockBandColor = new Color(1.0f, 0.6f, 0.6f, 0.35f);



    [Header("ラインカラー設定")]
    [SerializeField] private Color riseLineColor = new Color(0.6f, 1f, 0.3f, 1f); // 黄緑
    [SerializeField] private Color fallLineColor = new Color(1f, 0.4f, 0.4f, 1f); // 赤
    [SerializeField] private Color flatLineColor = Color.black;                    // 変化なし


    private TMP_Text[] yAxisLabelsTMP;
    private Text[] yAxisLabelsUGUI;

    private TMP_Text[] xAxisLabelsTMP;
    private Text[] xAxisLabelsUGUI;

    private readonly List<int> monthlyAssets = new List<int>();
    private readonly List<EconomicEventType> monthlyEvents = new List<EconomicEventType>();
    private readonly List<Vector2> pointPositions = new List<Vector2>();

    private bool useStartOffsetThisYear = false;

    private void Awake()
    {
        CacheAxisLabels();
    }

    private void Update()
    {
        UpdateHover();
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

    public void SetMonthlyData(List<int> assetsForYear)
    {
        SetMonthlyData(assetsForYear, false, null);
    }

    public void SetMonthlyData(List<int> assetsForYear, bool useStartOffset)
    {
        SetMonthlyData(assetsForYear, useStartOffset, null);
    }

    public void SetMonthlyData(List<int> assetsForYear, bool useStartOffset, List<EconomicEventType> eventsForYear)
    {
        monthlyAssets.Clear();
        monthlyEvents.Clear();
        useStartOffsetThisYear = useStartOffset;

        if (assetsForYear != null && assetsForYear.Count > 0)
        {
            monthlyAssets.AddRange(assetsForYear);
        }

        if (eventsForYear != null && eventsForYear.Count > 0)
        {
            int count = Mathf.Min(eventsForYear.Count, monthlyAssets.Count);
            for (int i = 0; i < count; i++)
            {
                monthlyEvents.Add(eventsForYear[i]);
            }
            for (int i = monthlyEvents.Count; i < monthlyAssets.Count; i++)
            {
                monthlyEvents.Add(EconomicEventType.None);
            }
        }
        else
        {
            for (int i = 0; i < monthlyAssets.Count; i++)
            {
                monthlyEvents.Add(EconomicEventType.None);
            }
        }

        RebuildGraph();
    }

    private void ClearGraphVisuals()
    {
        if (graphRect == null) return;

        for (int i = graphRect.childCount - 1; i >= 0; i--)
        {
            var child = graphRect.GetChild(i);
            if (hoverMarker != null && child == hoverMarker) continue;
            Destroy(child.gameObject);
        }
    }

    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();
        pointPositions.Clear();

        float minAsset;
        float maxAsset;

        if (monthlyAssets.Count == 0)
        {
            minAsset = -defaultAbsMax;
            maxAsset = defaultAbsMax;

            UpdateYAxisLabels(minAsset, maxAsset);
            UpdateXAxisLabels(12);

            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
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

        // 背景帯
        DrawEventBands(width, height);

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        int count = monthlyAssets.Count;
        if (count <= 1) count = 2;

        for (int i = 0; i < monthlyAssets.Count; i++)
        {
            float tX = (float)i / (count - 1);
            float x = tX * width;

            float tY = Mathf.InverseLerp(minAsset, maxAsset, monthlyAssets[i]);
            float y = tY * height;

            if (i == 0 && useStartOffsetThisYear)
            {
                y += startPointYOffset;
            }

            Vector2 pos = new Vector2(x, y);
            pointPositions.Add(pos);

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

                // ★上昇/下落に応じて色を変える（年別と同じ）
                float delta = monthlyAssets[i] - monthlyAssets[i - 1];
                var graphic = line.GetComponent<Graphic>();
                if (graphic != null)
                {
                    if (delta > 0f) graphic.color = riseLineColor;
                    else if (delta < 0f) graphic.color = fallLineColor;
                    else graphic.color = flatLineColor;
                }

            }

            previousPos = pos;
            hasPrev = true;
        }
    }

    private void DrawEventBands(float width, float height)
    {
        if (eventBandPrefab == null) return;
        if (monthlyAssets.Count <= 0) return;

        int count = monthlyAssets.Count;

        // ★月数で等分（count分割）に変更：最後の帯が右に飛ばない
        float segmentWidth = width / count;

        for (int i = 0; i < count; i++)
        {
            EconomicEventType ev =
                (i >= 0 && i < monthlyEvents.Count) ? monthlyEvents[i] : EconomicEventType.None;

            if (ev == EconomicEventType.None) continue;

            float startX = segmentWidth * i;
            float centerX = startX + segmentWidth * 0.5f;

            Image band = Instantiate(eventBandPrefab, graphRect);
            RectTransform rt = band.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(segmentWidth, height);
            rt.anchoredPosition = new Vector2(centerX, height * 0.5f);

            band.color = GetEventBandColor(ev);
            band.transform.SetSiblingIndex(0);
        }
    }

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
                if (i == 0) text = topText;
                else if (i == 1) text = middleText;
                else text = bottomText;
            }
            else
            {
                float t = (n == 1) ? 0f : (float)i / (n - 1);
                float v = Mathf.Lerp(-absMax, absMax, t);
                int vi = Mathf.RoundToInt(v);
                text = $"{vi.ToString("N0")}円";
            }

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

    private void UpdateHover()
    {
        if (graphRect == null || pointPositions.Count == 0)
        {
            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                graphRect, Input.mousePosition, null, out Vector2 local))
        {
            return;
        }

        Rect r = graphRect.rect;
        Vector2 fromBL = local - new Vector2(r.xMin, r.yMin);
        float width = r.width;
        float height = r.height;

        if (fromBL.x < 0 || fromBL.x > width || fromBL.y < 0 || fromBL.y > height)
        {
            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        int closestIndex = -1;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < pointPositions.Count; i++)
        {
            Vector2 diff = pointPositions[i] - fromBL;
            float sqrDist = diff.sqrMagnitude;

            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closestIndex = i;
            }
        }

        if (closestIndex < 0 || closestSqrDist > hoverSnapMaxDistance * hoverSnapMaxDistance)
        {
            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        int monthIndex = closestIndex;
        int month = monthIndex + 1;
        int asset = monthlyAssets[monthIndex];

        EconomicEventType ev = EconomicEventType.None;
        if (monthIndex >= 0 && monthIndex < monthlyEvents.Count)
        {
            ev = monthlyEvents[monthIndex];
        }
        string evLabel = GetEventLabel(ev);

        if (hoverInfoText != null)
        {
            hoverInfoText.text =
                $"{month}月 : {asset.ToString("N0")}円\n" +
                $"景気: {evLabel}";
        }

        if (hoverMarker != null)
        {
            hoverMarker.gameObject.SetActive(true);
            hoverMarker.anchorMin = hoverMarker.anchorMax = new Vector2(0f, 0f);
            hoverMarker.anchoredPosition = pointPositions[closestIndex];
        }
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

    private Color GetEventBandColor(EconomicEventType ev)
    {
        switch (ev)
        {
            case EconomicEventType.Boom: return boomBandColor;
            case EconomicEventType.Recession: return recessionBandColor;
            case EconomicEventType.Shock: return shockBandColor;
            default: return Color.clear;
        }
    }
}
