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

    [Header("縦軸設定（0中心・±上限）")]
    [Tooltip("基準となる上限（最低でもこの±を確保する）")]
    [SerializeField] private float minAbsMax = 800000f;

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

    [Tooltip("年番号未指定(-1)の時に使うAxisMargin")]
    [SerializeField] private float axisMarginDefault = 50000f;

    [Header("自動拡張（閾値到達で拡張）")]
    [SerializeField] private bool enableAutoExpand = true;

    [Tooltip("データが上限の何%に到達したら拡張するか（例：0.85=85%）")]
    [Range(0.50f, 0.98f)]
    [SerializeField] private float expandTriggerRatio = 0.85f;

    [Tooltip("上限をキリの良い単位に切り上げる（例：100000 なら 10万円刻み）")]
    [SerializeField] private float niceStep = 100000f;

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    [Header("ラインカラー設定（上昇/下落/平坦）")]
    [SerializeField] private Color riseLineColor = new Color(0.6f, 1f, 0.3f, 1f);
    [SerializeField] private Color fallLineColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color flatLineColor = Color.black;

    [Header("ライン色：イベントで上書き（任意）")]
    [SerializeField] private bool overrideLineColorByEvent = false;
    [SerializeField] private Color boomLineColor = new Color(0.45f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color recessionLineColor = new Color(0.2f, 0.55f, 0.95f, 1f);
    [SerializeField] private Color shockLineColor = new Color(0.95f, 0.25f, 0.25f, 1f);

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

    // 0月点あり：assets[0]=0月(年初), assets[1..12]=各月末
    private readonly List<int> plottedAssets = new List<int>();

    // 12ヶ月イベント（1月〜12月）。0月点には対応イベントなし
    private readonly List<EconomicEventType> monthlyEvents = new List<EconomicEventType>();

    // 各点のローカル座標（graphRect 左下原点）
    private readonly List<Vector2> pointPositions = new List<Vector2>();

    // 表示中の年（1〜）。未指定なら -1
    private int currentYearNumber = -1;

    // 「前回表示していた年」：年が変わったらスケールキャッシュをリセットするため
    private int lastYearNumber = int.MinValue;

    // 現在の上限（±）を保持（閾値拡張のため）
    private float currentAbsMax = -1f;

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
            yAxisLabelsTMP = yAxisLabelsRoot.GetComponentsInChildren<TMP_Text>(true);
            yAxisLabelsUGUI = yAxisLabelsRoot.GetComponentsInChildren<Text>(true);
        }

        if (xAxisLabelsRoot != null)
        {
            xAxisLabelsTMP = xAxisLabelsRoot.GetComponentsInChildren<TMP_Text>(true);
            xAxisLabelsUGUI = xAxisLabelsRoot.GetComponentsInChildren<Text>(true);
        }
    }

    //========================================================
    //  Public API
    //========================================================

    // 互換：旧呼び出しが残っていても壊れないように置いておく
    public void SetMonthlyData(List<int> assetsForYear, bool useStartOffset, List<EconomicEventType> eventsForYear)
    {
        // 旧仕様は0月点が無いので、startAsset=0 扱い
        SetMonthlyDataWithStartPoint(0, assetsForYear, eventsForYear, -1);
    }

    // yearNumberなし版（互換）
    public void SetMonthlyDataWithStartPoint(int startAsset, List<int> monthEndAssets, List<EconomicEventType> eventsForYear)
    {
        SetMonthlyDataWithStartPoint(startAsset, monthEndAssets, eventsForYear, -1);
    }

    // ★本命：0月点 + 年番号つき
    public void SetMonthlyDataWithStartPoint(int startAsset, List<int> monthEndAssets, List<EconomicEventType> eventsForYear, int yearNumber)
    {
        currentYearNumber = yearNumber;

        // ★年が変わったら上限キャッシュをリセット（ここが重要）
        if (currentYearNumber != lastYearNumber)
        {
            currentAbsMax = -1f;
            lastYearNumber = currentYearNumber;
        }

        plottedAssets.Clear();
        monthlyEvents.Clear();
        pointPositions.Clear();

        // 0月点
        plottedAssets.Add(startAsset);

        // 1..12月末（最大12個想定）
        if (monthEndAssets != null && monthEndAssets.Count > 0)
        {
            plottedAssets.AddRange(monthEndAssets);
        }

        // イベントは12個（1月〜12月）
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
        if (!useAxisMarginByYear) return axisMarginDefault;

        int y = currentYearNumber; // 1〜想定、未指定なら default
        if (y <= 0) return axisMarginDefault;

        if (y <= 3) return axisMarginUpTo3;
        if (y <= 6) return axisMarginUpTo6;
        if (y <= 9) return axisMarginUpTo9;
        if (y <= 12) return axisMarginUpTo12;
        return axisMarginOver12;
    }

    private float NiceCeil(float value)
    {
        float step = Mathf.Max(1f, niceStep);
        return Mathf.Ceil(value / step) * step;
    }

    private float ResolveAbsMaxByThreshold(float absMaxData)
    {
        float baseAbs = Mathf.Max(1f, minAbsMax);

        // 初期値
        if (currentAbsMax <= 0f) currentAbsMax = baseAbs;

        // まず最低保証
        currentAbsMax = Mathf.Max(currentAbsMax, baseAbs);

        if (!enableAutoExpand)
        {
            currentAbsMax = baseAbs;
            return currentAbsMax;
        }

        float trigger = Mathf.Clamp(expandTriggerRatio, 0.50f, 0.98f);

        if (absMaxData >= currentAbsMax * trigger)
        {
            float required = absMaxData / trigger;
            required = Mathf.Max(required, baseAbs);
            currentAbsMax = NiceCeil(required);
        }

        if (absMaxData > currentAbsMax)
        {
            currentAbsMax = NiceCeil(absMaxData);
        }

        return currentAbsMax;
    }

    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();
        pointPositions.Clear();

        if (plottedAssets.Count == 0)
        {
            float absMaxEmpty = Mathf.Max(1f, minAbsMax);
            currentAbsMax = absMaxEmpty;

            UpdateYAxisLabels(-absMaxEmpty, absMaxEmpty);
            UpdateXAxisLabels(13);

            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        //====================================================
        // 1) データ最大（絶対値）
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

        //====================================================
        // 2) 閾値ベースの自動拡張
        //====================================================
        float finalAbsMax = ResolveAbsMaxByThreshold(absMaxData);
        finalAbsMax = Mathf.Max(1f, finalAbsMax);

        float minAsset = -finalAbsMax;
        float maxAsset = +finalAbsMax;

        UpdateYAxisLabels(minAsset, maxAsset);
        UpdateXAxisLabels(plottedAssets.Count);

        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        //====================================================
        // 3) イベント帯（12セグメント）
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

                // 点は前面にしたいので最後に寄せる（帯より前、線より前）
                p.SetAsLastSibling();
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

                // ★色設定：プレハブ構造が子にGraphicを持っていても反映する
                Graphic graphic = line.GetComponent<Graphic>();
                if (graphic == null) graphic = line.GetComponentInChildren<Graphic>(true);

                if (graphic != null)
                {
                    // まず上昇/下落/平坦
                    int delta = plottedAssets[i] - plottedAssets[i - 1];
                    Color c;
                    if (delta > 0) c = riseLineColor;
                    else if (delta < 0) c = fallLineColor;
                    else c = flatLineColor;

                    // 任意：イベントで上書き（この線は i-1月→i月 の区間。イベントは「i月」の月イベントに合わせる）
                    // 0→1月 の線は 1月イベント（index0）でOK
                    if (overrideLineColorByEvent)
                    {
                        int eventIndex = i - 1; // 0..11
                        if (eventIndex >= 0 && eventIndex < monthlyEvents.Count)
                        {
                            var ev = monthlyEvents[eventIndex];
                            if (ev == EconomicEventType.Boom) c = boomLineColor;
                            else if (ev == EconomicEventType.Recession) c = recessionLineColor;
                            else if (ev == EconomicEventType.Shock) c = shockLineColor;
                        }
                    }

                    graphic.color = c;
                }

                // 線は帯より前、点より後にしたいので最後に寄せた後、点が上に来るよう点側で SetAsLastSibling している
                line.SetAsLastSibling();
            }

            previousPos = pos;
            hasPrev = true;
        }
    }

    private void DrawEventBands(float width, float height)
    {
        if (eventBandPrefab == null) return;
        if (plottedAssets.Count < 2) return;

        int segments = plottedAssets.Count - 1; // 13点→12セグメント
        if (segments <= 0) return;

        float segmentWidth = width / segments;
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

            // 背景なので最背面
            band.transform.SetSiblingIndex(0);
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

        EconomicEventType ev = EconomicEventType.None;
        if (monthIndex >= 1)
        {
            int eIdx = monthIndex - 1;
            if (eIdx >= 0 && eIdx < monthlyEvents.Count) ev = monthlyEvents[eIdx];
        }

        if (hoverInfoText != null)
        {
            hoverInfoText.text =
                $"{monthIndex}月 : {asset.ToString("N0")}円\n" +
                $"景気: {GetEventLabel(ev)}";
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
