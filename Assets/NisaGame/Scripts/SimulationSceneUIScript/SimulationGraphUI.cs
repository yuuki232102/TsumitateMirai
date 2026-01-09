using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   // ← 旧UI Text 用
using TMPro;           // ← TMP_Text 用

public class SimulationGraphUI : MonoBehaviour
{
    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;    // GraphContent

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;  // GraphPointPrefab
    [SerializeField] private RectTransform linePrefab;   // GraphLinePrefab

    [Header("基本設定")]
    [SerializeField] private int maxYear = 15;

    [Header("縦軸設定（デフォルト）")]
    [SerializeField] private float defaultMinAsset = -30000f;
    [SerializeField] private float defaultMaxAsset = 70000f;
    [SerializeField] private float axisMargin = 5000f;

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    [Header("Y軸ラベルの親（子に Text / TMP_Text を並べる）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    [Header("X軸ラベルの親（子に Text / TMP_Text を並べる）")]
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

        // デフォルト範囲でラベル初期化
        UpdateYAxisLabels(defaultMinAsset, defaultMaxAsset);
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

    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();
        pointPositions.Clear();

        if (years.Count == 0)
        {
            UpdateYAxisLabels(defaultMinAsset, defaultMaxAsset);
            UpdateXAxisLabels();
            return;
        }

        // ---- Y軸レンジ計算 ----
        float minAsset = assets[0];
        float maxAsset = assets[0];

        for (int i = 1; i < assets.Count; i++)
        {
            if (assets[i] < minAsset) minAsset = assets[i];
            if (assets[i] > maxAsset) maxAsset = assets[i];
        }

        minAsset -= axisMargin;
        maxAsset += axisMargin;

        // 0 を必ず範囲に含める
        if (minAsset > 0) minAsset = 0;
        if (maxAsset < 0) maxAsset = 0;

        if (Mathf.Approximately(minAsset, maxAsset))
        {
            minAsset -= 1000f;
            maxAsset += 1000f;
        }

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

            // Y：資産を 0〜1 に正規化
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

        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0f : (float)i / (n - 1);  // 下0〜上1
            float v = Mathf.Lerp(minAsset, maxAsset, t);
            int vi = Mathf.RoundToInt(v);
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
