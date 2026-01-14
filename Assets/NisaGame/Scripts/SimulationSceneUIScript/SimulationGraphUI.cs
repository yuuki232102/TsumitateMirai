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
    // 年数区間ごとの「絶対値デフォルト上限」
    [SerializeField] private float defaultAbsMaxSegment1 = 800000f; // 0〜5年
    [SerializeField] private float defaultAbsMaxSegment2 = 800000f; // 6〜10年
    [SerializeField] private float defaultAbsMaxSegment3 = 800000f; // 11〜15年
    [SerializeField] private float axisMargin = 50000f;             // 実データに上乗せする余白

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

    [Header("ホバー感度設定")]
    [SerializeField] private float hoverSnapMaxDistance = 40f; // この距離以内ならホバー有効

    //========================================================
    //  内部状態
    //========================================================

    // ラベル用（TMP / 旧 Text 両対応）
    private TMP_Text[] yAxisLabelsTMP;
    private Text[] yAxisLabelsUGUI;

    private TMP_Text[] xAxisLabelsTMP;
    private Text[] xAxisLabelsUGUI;

    // グラフデータ
    private readonly List<int> years = new List<int>();   // X：年
    private readonly List<int> assets = new List<int>();  // Y：資産

    // ホバー用：各点のローカル座標（GraphRect 左下原点）
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

    /// <summary>グラフをリセット（全消去）</summary>
    public void ResetGraph()
    {
        years.Clear();
        assets.Clear();
        pointPositions.Clear();

        ClearGraphVisuals();

        // 0年目のデフォルト範囲（±defaultAbsMaxSegment1）で軸だけ初期化
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

    //========================================================
    //  描画処理
    //========================================================

    /// <summary>グラフの子要素（点・線など）を全削除（ホバーマーカーは残す）</summary>
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

    /// <summary>現在の「最後の年」に応じたデフォルト上限（絶対値）を返す</summary>
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

    /// <summary>内部データからグラフを再構築</summary>
    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();
        pointPositions.Clear();

        float minAsset;
        float maxAsset;

        if (years.Count == 0)
        {
            // データ無し → デフォルト（0〜5年用）
            float absMax = defaultAbsMaxSegment1;
            minAsset = -absMax;
            maxAsset = absMax;

            UpdateYAxisLabels(minAsset, maxAsset);
            UpdateXAxisLabels();
            return;
        }

        //----------------------------------------------------
        // 1. 実データの最小・最大を取得
        //----------------------------------------------------
        float rawMin = assets[0];
        float rawMax = assets[0];

        for (int i = 1; i < assets.Count; i++)
        {
            if (assets[i] < rawMin) rawMin = assets[i];
            if (assets[i] > rawMax) rawMax = assets[i];
        }

        // 絶対値で一番大きい値（プラス・マイナス両方を考慮）
        float absMaxData = Mathf.Max(Mathf.Abs(rawMin), Mathf.Abs(rawMax));

        // 余白を追加
        absMaxData += axisMargin;

        //----------------------------------------------------
        // 2. 区間ごとのデフォルト上限と比較して、最終的な ±上限値を決定
        //----------------------------------------------------
        float absMaxDefault = GetDefaultAbsMaxForCurrentYears();
        float finalAbsMax = Mathf.Max(absMaxData, absMaxDefault);

        // 上は +finalAbsMax、下は −finalAbsMax（常に対称）
        minAsset = -finalAbsMax;
        maxAsset = finalAbsMax;

        //----------------------------------------------------
        // 3. 軸ラベル更新
        //----------------------------------------------------
        UpdateYAxisLabels(minAsset, maxAsset);
        UpdateXAxisLabels();

        //----------------------------------------------------
        // 4. 点と線を描画
        //----------------------------------------------------
        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        for (int i = 0; i < years.Count; i++)
        {
            // X：0〜maxYear をグラフ幅に正規化
            float tX = (maxYear > 0) ? (float)years[i] / maxYear : 0f;
            float x = tX * width;

            // Y：資産を 0〜1 に正規化（下が minAsset, 上が maxAsset）
            float tY = Mathf.InverseLerp(minAsset, maxAsset, assets[i]);
            float y = tY * height;

            Vector2 pos = new Vector2(x, y);
            pointPositions.Add(pos);

            // ---- 点 ----
            if (pointPrefab != null)
            {
                RectTransform p = Instantiate(pointPrefab, graphRect);
                p.anchorMin = p.anchorMax = new Vector2(0f, 0f); // 左下原点
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
            }

            previousPos = pos;
            hasPrev = true;
        }
    }

    //========================================================
    //  軸ラベル更新
    //========================================================

    /// <summary>Y軸ラベルを更新（下：-上限 / 中央：0 / 上：+上限）</summary>
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

        // 実際の上限値の絶対値
        float absMax = Mathf.Max(Mathf.Abs(minAsset), Mathf.Abs(maxAsset));
        int absInt = Mathf.RoundToInt(absMax);

        // 0, ±上限の文字列
        string topText = $"{absInt.ToString("N0")}円";
        string middleText = "0円";
        string bottomText = $"-{absInt.ToString("N0")}円";

        // ラベルは「上 → 中央 → 下」の順で並んでいる想定
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
                // 3つ無い場合は線形に振り分け（保険）
                float t = (n == 1) ? 0f : (float)i / (n - 1);
                float v = Mathf.Lerp(-absMax, absMax, t);
                int vi = Mathf.RoundToInt(v);
                text = $"{vi.ToString("N0")}円";
            }

            if (i < nTMP && yAxisLabelsTMP[i] != null) yAxisLabelsTMP[i].text = text;
            if (i < nGUI && yAxisLabelsUGUI[i] != null) yAxisLabelsUGUI[i].text = text;
        }
    }

    /// <summary>X軸ラベルを 0年〜maxYear年 で更新</summary>
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
            float t = (n == 1) ? 0f : (float)i / (n - 1);  // 左0〜右1
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

        // 画面上のマウス座標 → graphRect のローカル座標
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                graphRect, Input.mousePosition, null, out Vector2 local))
        {
            return;
        }

        Rect r = graphRect.rect;

        // pivot 中心のローカル座標 → 左下(0,0)基準の座標に変換
        Vector2 fromBL = local - new Vector2(r.xMin, r.yMin);
        float width = r.width;
        float height = r.height;

        // グラフ範囲外ならホバー非表示
        if (fromBL.x < 0 || fromBL.x > width || fromBL.y < 0 || fromBL.y > height)
        {
            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        //----------------------------------------------------
        // 一番近い点を距離で探す
        //----------------------------------------------------
        int closestIndex = -1;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < pointPositions.Count; i++)
        {
            Vector2 diff = pointPositions[i] - fromBL; // 同じローカル座標系
            float sqrDist = diff.sqrMagnitude;

            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closestIndex = i;
            }
        }

        // 線／点から遠すぎる場合はホバーしない
        if (closestIndex < 0 || closestSqrDist > hoverSnapMaxDistance * hoverSnapMaxDistance)
        {
            if (hoverMarker != null) hoverMarker.gameObject.SetActive(false);
            if (hoverInfoText != null) hoverInfoText.text = "";
            return;
        }

        int year = years[closestIndex];
        int asset = assets[closestIndex];

        if (hoverInfoText != null)
        {
            hoverInfoText.text = $"{year}年目 : {asset.ToString("N0")}円";
        }

        if (hoverMarker != null)
        {
            hoverMarker.gameObject.SetActive(true);
            hoverMarker.anchorMin = hoverMarker.anchorMax = new Vector2(0f, 0f);
            hoverMarker.anchoredPosition = pointPositions[closestIndex];
        }
    }
}
