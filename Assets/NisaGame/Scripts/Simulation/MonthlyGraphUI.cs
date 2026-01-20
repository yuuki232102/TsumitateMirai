using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonthlyGraphUI : MonoBehaviour
{
    //========================================================
    //  Inspector
    //========================================================

    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;
    [SerializeField] private RectTransform linePrefab;

    [Header("縦軸設定（0中心・±上限 値）")]
    [SerializeField] private float minAbsMax = 800000f;      // 最低でもこのレンジ
    [SerializeField] private float axisMargin = 50000f;      // デフォルト余白（年区分ONなら使われない）

    [Header("AxisMargin（年区分：3/6/9/12年ごと）")]
    [SerializeField] private bool useAxisMarginByYear = true;

    [Tooltip("1〜3年目のAxisMargin")]
    [SerializeField] private float axisMarginUpTo3 = 50000f;

    [Tooltip("4〜6年目のAxisMargin")]
    [SerializeField] private float axisMarginUpTo6 = 80000f;

    [Tooltip("7〜9年目のAxisMargin")]
    [SerializeField] private float axisMarginUpTo9 = 120000f;

    [Tooltip("10〜12年目のAxisMargin")]
    [SerializeField] private float axisMarginUpTo12 = 180000f;

    [Tooltip("13年目以降のAxisMargin")]
    [SerializeField] private float axisMarginOver12 = 250000f;

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

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

    //========================================================
    //  Internal
    //========================================================

    private TMP_Text[] yAxisLabelsTMP;
    private Text[] yAxisLabelsUGUI;

    private TMP_Text[] xAxisLabelsTMP;
    private Text[] xAxisLabelsUGUI;

    // 描画用データ（0月点あり：points[0]=start, points[1..12]=月末）
    private readonly List<int> plottedAssets = new List<int>();

    // 12ヶ月のイベント（Jan..Dec）。0月点には対応するイベントは無いので保持しない
    private readonly List<EconomicEventType> monthlyEvents = new List<EconomicEventType>();

    private readonly List<Vector2> pointPositions = new List<Vector2>();

    // 表示中の年（1〜）。未指定なら -1
    private int currentYearNumber = -1;

    private Canvas rootCanvas;
    private Camera uiCamera;

    private void Awake()
    {
        CacheAxisLabels();

        rootCanvas = (graphRect != null) ? graphRect.GetComponentInParent<Canvas>() : null;
        uiCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = rootCanvas.worldCamera;
        }
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

    //========================================================
    //  Public API
    //========================================================

    // 互換：従来呼び出し（0月点なし版）も残す（内部で0月点=最初の月末扱いにする）
    public void SetMonthlyData(List<int> assetsForYear, bool useStartOffset, List<EconomicEventType> eventsForYear)
    {
        // 旧仕様のまま呼ばれても壊れないように、
        // startAsset=assetsForYear[0] として 0月点風に扱う（ただし厳密ではない）
        int startAsset = 0;
        List<int> monthEnds = assetsForYear ?? new List<int>();
        if (monthEnds.Count > 0) startAsset = monthEnds[0];

        SetMonthlyDataWithStartPoint(startAsset, monthEnds, eventsForYear, -1);
    }

    // ★互換：yearNumberを渡さない呼び出し
    public void SetMonthlyDataWithStartPoint(int startAsset, List<int> monthEndAssets, List<EconomicEventType> eventsForYear)
    {
        SetMonthlyDataWithStartPoint(startAsset, monthEndAssets, eventsForYear, -1);
    }

    // ★本命：0月点＋年番号つき
    public void SetMonthlyDataWithStartPoint(int startAsset, List<int> monthEndAssets, List<EconomicEventType> eventsForYear, int yearNumber)
    {
        currentYearNumber = yearNumber;

        plottedAssets.Clear();
        monthlyEvents.Clear();

        // 0月点
        plottedAssets.Add(startAsset);

        // 1..12月末（最大12件想定）
        if (monthEndAssets != null && monthEndAssets.Count > 0)
        {
            plottedAssets.AddRange(monthEndAssets);
        }

        // イベントは12個（Jan..Dec）
        if (eventsForYear != null && eventsForYear.Count > 0)
        {
            int count = Mathf.Min(12, eventsForYear.Count);
            for (int i = 0; i < count; i++) monthlyEvents.Add(eventsForYear[i]);
            for (int i = monthlyEvents.Count; i < 12; i++) monthlyEvents.Add(EconomicEventType.None);
        }
        else
        {
            for (int i = 0; i < 12; i++) monthlyEvents.Add(EconomicEventType.None);
        }

        RebuildGraph();
    }

    //========================================================
    //  Graph
    //========================================================

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

    private float GetAxisMarginForCurrentYear()
    {
        if (!useAxisMarginByYear) return axisMargin;

        int y = currentYearNumber; // 1〜想定。未指定(-1)はデフォルトへ
        if (y <= 0) return axisMargin;

        if (y <= 3) return axisMarginUpTo3;
        if (y <= 6) return axisMarginUpTo6;
        if (y <= 9) return axisMarginUpTo9;
        if (y <= 12) return axisMarginUpTo12;
        return axisMarginOver12;
    }

    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();
        pointPositions.Clear();

        // データなし
        if (plottedAssets.Count == 0)
        {
            float absMaxEmpty = Mathf.Max(1f, minAbsMax);
            UpdateYAxisLabels(-absMaxEmpty, absMaxEmpty);
            UpdateXAxisLabels(13); // 0〜12月想定

            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        //====================================================
        // 1) Y軸：その年の最大/最小（±）の絶対値最大に、marginを足して対称に
        //====================================================
        float rawMin = plottedAssets[0];
        float rawMax = plottedAssets[0];

        for (int i = 1; i < plottedAssets.Count; i++)
        {
            if (plottedAssets[i] < rawMin) rawMin = plottedAssets[i];
            if (plottedAssets[i] > rawMax) rawMax = plottedAssets[i];
        }

        float absMaxData = Mathf.Max(Mathf.Abs(rawMin), Mathf.Abs(rawMax));
        absMaxData += GetAxisMarginForCurrentYear();

        float absMax = Mathf.Max(absMaxData, minAbsMax);
        float minAsset = -absMax;
        float maxAsset = +absMax;

        UpdateYAxisLabels(minAsset, maxAsset);
        UpdateXAxisLabels(plottedAssets.Count); // 13点なら 0..12月

        //====================================================
        // 2) サイズ
        //====================================================
        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        //====================================================
        // 3) イベント帯（12セグメント：0→1月, ... 11→12月）
        //====================================================
        DrawEventBands(width, height);

        //====================================================
        // 4) 点と線
        //====================================================
        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        int pointCount = Mathf.Max(2, plottedAssets.Count);

        for (int i = 0; i < plottedAssets.Count; i++)
        {
            float tX = (float)i / (pointCount - 1);
            float x = tX * width;

            float tY = Mathf.InverseLerp(minAsset, maxAsset, plottedAssets[i]);
            float y = tY * height;

            Vector2 pos = new Vector2(x, y);
            pointPositions.Add(pos);

            // 点
            if (pointPrefab != null)
            {
                RectTransform p = Instantiate(pointPrefab, graphRect);
                p.anchorMin = p.anchorMax = new Vector2(0f, 0f);
                p.anchoredPosition = pos;
                p.sizeDelta = new Vector2(pointSize, pointSize);
            }

            // 線
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

    private void DrawEventBands(float width, float height)
    {
        if (eventBandPrefab == null) return;
        if (plottedAssets.Count < 2) return;

        // 0月点ありなら通常 13点 → 12セグメント
        int segments = plottedAssets.Count - 1;
        if (segments <= 0) return;

        float segmentWidth = width / segments;

        // イベントは 12ヶ月（Jan..Dec）
        int drawCount = Mathf.Min(12, segments);

        for (int i = 0; i < drawCount; i++)
        {
            EconomicEventType ev = (i < monthlyEvents.Count) ? monthlyEvents[i] : EconomicEventType.None;
            if (ev == EconomicEventType.None) continue;

            float startX = segmentWidth * i;
            float centerX = startX + segmentWidth * 0.5f;

            Image band = Instantiate(eventBandPrefab, graphRect);
            RectTransform rt = band.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(segmentWidth, height);
            rt.anchoredPosition = new Vector2(centerX, height * 0.5f);

            band.color = GetEventBandColor(ev);
            band.transform.SetSiblingIndex(0); // 最背面
        }
    }

    //========================================================
    //  Axis labels
    //========================================================

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

    private void UpdateXAxisLabels(int pointCount)
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
        if (n == 0 || pointCount <= 0) return;

        // 0月点ありなら pointCount=13 → monthIndex 0..12
        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (float)i / (n - 1);
            int monthIndex = Mathf.Clamp(Mathf.RoundToInt(t * (pointCount - 1)), 0, pointCount - 1);
            string text = $"{monthIndex}月";

            if (i < nTMP && xAxisLabelsTMP[i] != null) xAxisLabelsTMP[i].text = text;
            if (i < nGUI && xAxisLabelsUGUI[i] != null) xAxisLabelsUGUI[i].text = text;
        }
    }

    //========================================================
    //  Hover
    //========================================================

    private void UpdateHover()
    {
        if (graphRect == null || pointPositions.Count == 0)
        {
            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                graphRect, Input.mousePosition, uiCamera, out Vector2 local))
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

        int monthIndex = closestIndex; // 0..12
        int asset = plottedAssets[monthIndex];

        // 0月はイベント無し、それ以外は monthIndex-1 を参照（1月→events[0]）
        EconomicEventType ev = EconomicEventType.None;
        if (monthIndex >= 1)
        {
            int eIdx = monthIndex - 1;
            if (eIdx >= 0 && eIdx < monthlyEvents.Count) ev = monthlyEvents[eIdx];
        }

        string evLabel = GetEventLabel(ev);

        if (hoverInfoText != null)
        {
            hoverInfoText.text =
                $"{monthIndex}月 : {asset.ToString("N0")}円\n" +
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
