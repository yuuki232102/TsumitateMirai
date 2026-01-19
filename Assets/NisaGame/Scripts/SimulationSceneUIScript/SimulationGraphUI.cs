using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationGraphUI : MonoBehaviour
{
    //========================================================
    //  インスペクタ設定
    //========================================================

    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;    // GraphContent

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;  // GraphPointPrefab
    [SerializeField] private RectTransform linePrefab;   // GraphLinePrefab

    [Header("基本設定")]
    [SerializeField] private int maxYear = 15;

    [Header("縦軸設定（0中心・±上限 値）")]
    [SerializeField] private float defaultAbsMaxSegment1 = 800000f; // 0〜5年
    [SerializeField] private float defaultAbsMaxSegment2 = 800000f; // 6〜10年
    [SerializeField] private float defaultAbsMaxSegment3 = 800000f; // 11〜15年
    [SerializeField] private float axisMargin = 50000f;             // 実データに上乗せする余白

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    [Header("開始点オフセット")]
    [SerializeField] private float startPointYOffset = 0f;

    [Header("ラインカラー設定")]
    [SerializeField] private Color riseLineColor = new Color(0.6f, 1f, 0.3f, 1f); // 黄緑
    [SerializeField] private Color fallLineColor = new Color(1f, 0.4f, 0.4f, 1f); // 赤
    [SerializeField] private Color flatLineColor = Color.black;                    // 変化なし

    [Header("Y軸ラベルの親（3つ推奨）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    [Header("X軸ラベルの親")]
    [SerializeField] private RectTransform xAxisLabelsRoot;

    [Header("カーソル表示UI（任意）")]
    [SerializeField] private TMP_Text hoverInfoText;        // 例: 「7年目 : 671,709円」
    [SerializeField] private RectTransform hoverMarker;     // グラフ上の小さなマーカー

    [Header("ホバー感度設定")]
    [SerializeField] private float hoverSnapMaxDistance = 40f; // この距離以内ならホバー有効

    //========================================================
    //  内部状態
    //========================================================

    private TMP_Text[] yAxisLabelsTMP;
    private Text[] yAxisLabelsUGUI;

    private TMP_Text[] xAxisLabelsTMP;
    private Text[] xAxisLabelsUGUI;

    private readonly List<int> years = new List<int>();          // X：年
    private readonly List<int> assets = new List<int>();         // Y：資産
    private readonly List<string> yearEventLabels = new List<string>(); // 年の景気ラベル（最大2つ連結済み）

    private readonly List<Vector2> pointPositions = new List<Vector2>();

    //========================================================
    //  Unity ライフサイクル
    //========================================================

    private void Awake()
    {
        CacheAxisLabels();
    }

    private void Update()
    {
        UpdateHover();
    }

    //========================================================
    //  ラベルキャッシュ
    //========================================================

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

    public void ResetGraph()
    {
        years.Clear();
        assets.Clear();
        yearEventLabels.Clear();
        pointPositions.Clear();

        ClearGraphVisuals();

        float absMax = defaultAbsMaxSegment1;
        UpdateYAxisLabels(-absMax, absMax);
        UpdateXAxisLabels();

        if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
        if (hoverInfoText != null) hoverInfoText.text = "";
    }

    // 互換用（他から2引数で呼ばれても壊れないように残す）
    public void AddPoint(int yearIndex, int asset)
    {
        AddPoint(yearIndex, asset, "平常");
    }

    // 方式1：景気ラベル付き
    public void AddPoint(int yearIndex, int asset, string eventLabel)
    {
        years.Add(yearIndex);
        assets.Add(asset);
        yearEventLabels.Add(string.IsNullOrEmpty(eventLabel) ? "平常" : eventLabel);
        RebuildGraph();
    }

    //========================================================
    //  描画処理
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

    private float GetDefaultAbsMaxForCurrentYears()
    {
        int lastYear = 0;
        if (years.Count > 0)
        {
            lastYear = years[years.Count - 1];
        }

        if (lastYear <= 5) return defaultAbsMaxSegment1;
        if (lastYear <= 10) return defaultAbsMaxSegment2;
        return defaultAbsMaxSegment3;
    }

    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();
        pointPositions.Clear();

        float minAsset;
        float maxAsset;

        if (years.Count == 0)
        {
            float absMax = defaultAbsMaxSegment1;
            minAsset = -absMax;
            maxAsset = absMax;

            UpdateYAxisLabels(minAsset, maxAsset);
            UpdateXAxisLabels();
            return;
        }

        float rawMin = assets[0];
        float rawMax = assets[0];

        for (int i = 1; i < assets.Count; i++)
        {
            if (assets[i] < rawMin) rawMin = assets[i];
            if (assets[i] > rawMax) rawMax = assets[i];
        }

        float absMaxData = Mathf.Max(Mathf.Abs(rawMin), Mathf.Abs(rawMax));
        absMaxData += axisMargin;

        float absMaxDefault = GetDefaultAbsMaxForCurrentYears();
        float finalAbsMax = Mathf.Max(absMaxData, absMaxDefault);

        minAsset = -finalAbsMax;
        maxAsset = finalAbsMax;

        UpdateYAxisLabels(minAsset, maxAsset);
        UpdateXAxisLabels();

        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        for (int i = 0; i < years.Count; i++)
        {
            float tX = (maxYear > 0) ? (float)years[i] / maxYear : 0f;
            float x = tX * width;

            float tY = Mathf.InverseLerp(minAsset, maxAsset, assets[i]);
            float y = tY * height;

            if (years[i] == 0)
            {
                y += startPointYOffset;
            }

            Vector2 pos = new Vector2(x, y);
            pointPositions.Add(pos);

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

                // 上昇/下落に応じて色を変える
                float delta = assets[i] - assets[i - 1];
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

    //========================================================
    //  軸ラベル更新
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

    private void UpdateXAxisLabels()
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
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            float t = (n == 1) ? 0f : (float)i / (n - 1);
            int yearLabel = Mathf.RoundToInt(t * maxYear);
            string text = $"{yearLabel}年";

            if (i < nTMP && xAxisLabelsTMP[i] != null) xAxisLabelsTMP[i].text = text;
            if (i < nGUI && xAxisLabelsUGUI[i] != null) xAxisLabelsUGUI[i].text = text;
        }
    }

    //========================================================
    //  ホバー表示
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

        int year = years[closestIndex];
        int asset = assets[closestIndex];

        string label = "平常";
        if (closestIndex >= 0 && closestIndex < yearEventLabels.Count && !string.IsNullOrEmpty(yearEventLabels[closestIndex]))
        {
            label = yearEventLabels[closestIndex];
        }

        if (hoverInfoText != null)
        {
            hoverInfoText.text = $"{year}年目 : {asset.ToString("N0")}円\n景気: {label}";
        }

        if (hoverMarker != null)
        {
            hoverMarker.gameObject.SetActive(true);
            hoverMarker.anchorMin = hoverMarker.anchorMax = new Vector2(0f, 0f);
            hoverMarker.anchoredPosition = pointPositions[closestIndex];
        }
    }
}
