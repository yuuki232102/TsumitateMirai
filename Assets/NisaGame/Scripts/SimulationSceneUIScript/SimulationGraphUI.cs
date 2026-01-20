using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationGraphUI : MonoBehaviour
{
    //========================================================
    //  Inspector
    //========================================================

    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;    // GraphContent

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;  // GraphPointPrefab
    [SerializeField] private RectTransform linePrefab;   // GraphLinePrefab

    [Header("基本設定")]
    [SerializeField] private int maxYear = 15;

    [Header("縦軸設定（基本は固定：0中心・±上限）")]
    [Tooltip("初期のY軸上限（±）。必要に応じて自動拡張される。")]
    [SerializeField] private float fixedAbsMax = 5000000f;

    [Header("縦軸の自動拡張（上限に近づいたら引き上げ）")]
    [Tooltip("abs(value) が fixedAbsMax * expandThreshold を超えたら拡張する。例：0.92")]
    [Range(0.5f, 0.99f)]
    [SerializeField] private float expandThreshold = 0.92f;

    [Tooltip("拡張時に fixedAbsMax を何倍するか。例：1.25")]
    [SerializeField] private float expandMultiplier = 1.25f;

    [Tooltip("自動拡張の上限（暴走防止）。例：50,000,000")]
    [SerializeField] private float maxAutoAbsMax = 50000000f;

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

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
    [SerializeField] private float hoverSnapMaxDistance = 40f;

    //========================================================
    //  Internal
    //========================================================

    private TMP_Text[] yAxisLabelsTMP;
    private Text[] yAxisLabelsUGUI;

    private TMP_Text[] xAxisLabelsTMP;
    private Text[] xAxisLabelsUGUI;

    private readonly List<int> years = new List<int>();                 // X：年
    private readonly List<int> assets = new List<int>();                // Y：資産
    private readonly List<string> yearEventLabels = new List<string>(); // 年の景気ラベル（最大2つ連結済み）

    private readonly List<Vector2> pointPositions = new List<Vector2>();

    private Canvas rootCanvas;
    private Camera uiCamera;

    //========================================================
    //  Unity
    //========================================================

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

    public void ResetGraph()
    {
        years.Clear();
        assets.Clear();
        yearEventLabels.Clear();
        pointPositions.Clear();

        ClearGraphVisuals();

        // Reset時点では fixedAbsMax の値は Inspector の値をそのまま使う前提
        float absMax = Mathf.Max(1f, fixedAbsMax);
        UpdateYAxisLabels(-absMax, absMax);
        UpdateXAxisLabels();

        if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
        if (hoverInfoText != null) hoverInfoText.text = "";
    }

    // 互換用：2引数
    public void AddPoint(int yearIndex, int asset)
    {
        AddPoint(yearIndex, asset, "平常");
    }

    // 景気ラベル付き（方式1）
    public void AddPoint(int yearIndex, int asset, string eventLabel)
    {
        years.Add(yearIndex);
        assets.Add(asset);
        yearEventLabels.Add(string.IsNullOrEmpty(eventLabel) ? "平常" : eventLabel);

        // ★ 追加される新データに応じて上限を必要なら拡張
        EnsureCapacityForValue(asset);

        RebuildGraph();
    }

    //========================================================
    //  Auto Expand
    //========================================================

    /// <summary>
    /// 指定値が上限に近づく/超える場合、fixedAbsMax を段階的に引き上げる。
    /// </summary>
    private void EnsureCapacityForValue(int value)
    {
        float absValue = Mathf.Abs((float)value);
        float absMax = Mathf.Max(1f, fixedAbsMax);

        // パラメータ安全化
        float threshold = Mathf.Clamp(expandThreshold, 0.5f, 0.99f);
        float mult = Mathf.Max(1.01f, expandMultiplier);
        float cap = Mathf.Max(absMax, maxAutoAbsMax);

        // 近づいていなければ何もしない
        if (absValue < absMax * threshold) return;

        // 必要なら複数回拡張（1回で足りないケース対策）
        int safety = 0;
        while (absValue >= absMax * threshold && absMax < cap && safety < 50)
        {
            absMax *= mult;
            safety++;
        }

        // 上限でクリップ
        absMax = Mathf.Min(absMax, cap);

        fixedAbsMax = absMax;
    }

    //========================================================
    //  Draw
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

    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();
        pointPositions.Clear();

        float absMax = Mathf.Max(1f, fixedAbsMax);
        float minAsset = -absMax;
        float maxAsset = absMax;

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

                float delta = assets[i] - assets[i - 1];

                // Prefab構造に強い（子にImageがあるケースも対応）
                var graphic = line.GetComponent<Graphic>();
                if (graphic == null) graphic = line.GetComponentInChildren<Graphic>();

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
