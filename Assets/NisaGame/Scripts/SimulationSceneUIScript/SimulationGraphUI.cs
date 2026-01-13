using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationGraphUI : MonoBehaviour
{
    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;    // GraphContent

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;  // GraphPointPrefab
    [SerializeField] private RectTransform linePrefab;   // GraphLinePrefab

    [Header("基本設定")]
    [SerializeField] private int maxYear = 15;

    [Header("縦軸設定（0中心・±上限 値）")]
    // 区間ごとの「絶対値デフォルト上限」
    [SerializeField] private float defaultAbsMaxSegment1 = 800000f; // 0〜5年
    [SerializeField] private float defaultAbsMaxSegment2 = 800000f; // 6〜10年
    [SerializeField] private float defaultAbsMaxSegment3 = 800000f; // 11〜15年
    [SerializeField] private float axisMargin = 50000f;             // 余白

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    [Header("Y軸ラベルの親（3つ推奨）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    [Header("X軸ラベルの親")]
    [SerializeField] private RectTransform xAxisLabelsRoot;

    [Header("カーソル表示UI（任意）")]
    [SerializeField] private TMP_Text hoverInfoText;        // 例: 「7年目 : 671,709円」
    [SerializeField] private RectTransform hoverMarker;     // グラフ上の小さなマーカー

    // ---- 内部用ラベル配列（TMP と旧 Text 両対応） ----
    private TMP_Text[] yAxisLabelsTMP;
    private Text[] yAxisLabelsUGUI;

    private TMP_Text[] xAxisLabelsTMP;
    private Text[] xAxisLabelsUGUI;

    // ---- グラフデータ ----
    private readonly List<int> years = new List<int>();
    private readonly List<int> assets = new List<int>();

    // ホバー用：各点のローカル座標（左下 0,0 原点）
    private readonly List<Vector2> pointPositions = new List<Vector2>();

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

    //================================================================
    // 外部インターフェース
    //================================================================

    /// <summary>グラフをリセット（全消去）</summary>
    public void ResetGraph()
    {
        years.Clear();
        assets.Clear();
        pointPositions.Clear();

        ClearGraphVisuals();

        // デフォルト範囲でラベル初期化（0〜±defaultAbsMaxSegment1）
        float absMax = defaultAbsMaxSegment1;
        UpdateYAxisLabels(-absMax, absMax);
        UpdateXAxisLabels();

        // ホバー表示リセット
        if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
        if (hoverInfoText != null) hoverInfoText.text = "";
    }

    /// <summary>年と資産を追加し、グラフを描き直す</summary>
    public void AddPoint(int yearIndex, int asset)
    {
        years.Add(yearIndex);
        assets.Add(asset);
        RebuildGraph();
    }

    //================================================================
    // 描画処理
    //================================================================

    private void ClearGraphVisuals()
    {
        if (graphRect == null) return;

        for (int i = graphRect.childCount - 1; i >= 0; i--)
        {
            var child = graphRect.GetChild(i);
            // hoverMarker を GraphContent の子にしている場合は消さない
            if (hoverMarker != null && child == hoverMarker) continue;
            Destroy(child.gameObject);
        }
    }

    private float GetDefaultAbsMaxForCurrentYears()
    {
        // 0年目しかない時は 0〜5年扱い
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

        float minAsset, maxAsset;

        if (years.Count == 0)
        {
            // データなし → デフォルト
            float absMax = defaultAbsMaxSegment1;
            minAsset = -absMax;
            maxAsset = absMax;
            UpdateYAxisLabels(minAsset, maxAsset);
            UpdateXAxisLabels();
            return;
        }

        // ---- Y軸レンジ計算 ----
        float rawMin = assets[0];
        float rawMax = assets[0];

        for (int i = 1; i < assets.Count; i++)
        {
            if (assets[i] < rawMin) rawMin = assets[i];
            if (assets[i] > rawMax) rawMax = assets[i];
        }

        // 実データから絶対値最大を取得
        float absMaxData = Mathf.Max(Mathf.Abs(rawMin), Mathf.Abs(rawMax));

        // 余白を足す
        absMaxData += axisMargin;

        // 区間ごとのデフォルト上限と比較
        float absMaxDefault = GetDefaultAbsMaxForCurrentYears();
        float finalAbsMax = Mathf.Max(absMaxData, absMaxDefault);

        // ±対称レンジ
        minAsset = -finalAbsMax;
        maxAsset = finalAbsMax;

        UpdateYAxisLabels(minAsset, maxAsset);
        UpdateXAxisLabels();

        // ---- 点と線を描画 ----
        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        for (int i = 0; i < years.Count; i++)
        {
            // X：0〜maxYear をグラフ幅に正規化
            float tX = maxYear > 0 ? (float)years[i] / maxYear : 0f;
            float x = tX * width;

            // Y：資産を 0〜1 に正規化（下が minAsset, 上が maxAsset）
            float tY = Mathf.InverseLerp(minAsset, maxAsset, assets[i]);
            float y = tY * height;

            Vector2 pos = new Vector2(x, y);
            pointPositions.Add(pos);

            // 点
            if (pointPrefab != null)
            {
                RectTransform p = Instantiate(pointPrefab, graphRect);
                p.anchorMin = p.anchorMax = new Vector2(0f, 0f); // 左下原点
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

    //================================================================
    // 軸ラベル更新
    //================================================================

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

        // 対称レンジの絶対値
        float absMax = Mathf.Max(Mathf.Abs(minAsset), Mathf.Abs(maxAsset));

        // 3 ラベル想定：上 = +absMax, 中 = 0, 下 = -absMax
        for (int i = 0; i < n; i++)
        {
            float value;
            if (n == 3)
            {
                if (i == 0) value = absMax;           // 一番上
                else if (i == 1) value = 0f;         // 真ん中
                else value = -absMax;                // 一番下
            }
            else
            {
                // 一応その他の個数にも対応：線形に振る
                float t = n == 1 ? 0f : (float)i / (n - 1);
                value = Mathf.Lerp(-absMax, absMax, t);
            }

            int vi = Mathf.RoundToInt(value);
            string text = $"{vi.ToString("N0")}円";

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

        int nTMP = xAxisLabelsTMP != null ? xAxisLabelsTMP.Length : 0;
        int nGUI = xAxisLabelsUGUI != null ? xAxisLabelsUGUI.Length : 0;
        int n = Mathf.Max(nTMP, nGUI);
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : (float)i / (n - 1);  // 左0〜右1
            int yearLabel = Mathf.RoundToInt(t * maxYear);
            string text = $"{yearLabel}年";

            if (i < nTMP && xAxisLabelsTMP[i] != null) xAxisLabelsTMP[i].text = text;
            if (i < nGUI && xAxisLabelsUGUI[i] != null) xAxisLabelsUGUI[i].text = text;
        }
    }

    //================================================================
    // ホバー表示
    //================================================================

    private void Update()
    {
        UpdateHover();
    }

    private void UpdateHover()
    {
        if (graphRect == null || pointPositions.Count == 0)
        {
            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        // マウス座標を GraphRect ローカル座標に変換
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                graphRect, Input.mousePosition, null, out local))
        {
            return;
        }

        Rect r = graphRect.rect;
        // pivot 中心座標 → 左下原点座標に変換
        Vector2 fromBL = local - new Vector2(r.xMin, r.yMin);
        float width = r.width;
        float height = r.height;

        if (fromBL.x < 0 || fromBL.x > width || fromBL.y < 0 || fromBL.y > height)
        {
            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        // X位置から最も近いデータ点を求める
        float tX = Mathf.Clamp01(fromBL.x / width);
        int index = Mathf.Clamp(
            Mathf.RoundToInt(tX * (pointPositions.Count - 1)),
            0, pointPositions.Count - 1);

        int year = years[index];
        int asset = assets[index];

        if (hoverInfoText != null)
        {
            hoverInfoText.text = $"{year}年目 : {asset.ToString("N0")}円";
        }

        if (hoverMarker != null)
        {
            hoverMarker.gameObject.SetActive(true);
            hoverMarker.anchorMin = hoverMarker.anchorMax = new Vector2(0f, 0f);
            hoverMarker.anchoredPosition = pointPositions[index];
        }
    }
}
