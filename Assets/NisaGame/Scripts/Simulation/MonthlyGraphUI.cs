using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonthlyGraphUI : MonoBehaviour
{
    //========================================================
    //  インスペクタ設定
    //========================================================

    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab;
    [SerializeField] private RectTransform linePrefab;

    [Header("縦軸設定（0中心・±上限 値）")]
    // 0年目の標準レンジの絶対値（±defaultAbsMax を基準にして、
    // 実データが大きくなったらその都度スケールアップします）
    [SerializeField] private float defaultAbsMax = 800000f;
    [SerializeField] private float axisMargin = 50000f;   // データに上乗せする余白

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;
    [SerializeField] private float lineThickness = 3f;

    // ★追加：1ヶ月目の開始点だけにかけるY方向オフセット（ピクセル）
    [Header("開始点オフセット")]
    [SerializeField] private float startPointYOffset = 0f;

    // ★追加：ラインの色（上昇 / 減少）
    [Header("ライン色設定")]
    [SerializeField] private Color lineUpColor = new Color(0.4f, 0.8f, 0.2f, 1f); // 黄緑っぽい
    [SerializeField] private Color lineDownColor = new Color(0.9f, 0.2f, 0.2f, 1f); // 赤っぽい

    [Header("Y軸ラベルの親（3つ推奨）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    [Header("X軸ラベルの親")]
    [SerializeField] private RectTransform xAxisLabelsRoot;

    [Header("カーソル表示UI（任意）")]
    [SerializeField] private TMP_Text hoverInfoText;        // 例: 「7月 : 123,456円」
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

    // その年の 12ヶ月分の資産推移
    private readonly List<int> monthlyAssets = new List<int>();

    // 各点のローカル座標（graphRect 左下原点）
    private readonly List<Vector2> pointPositions = new List<Vector2>();

    // このグラフ描画で開始点オフセットを使うかどうか
    private bool useStartOffsetThisYear = false;

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

    /// <summary>
    /// 指定された年の月次資産推移をセット＆描画（開始点オフセットなし）
    /// </summary>
    public void SetMonthlyData(List<int> assetsForYear)
    {
        SetMonthlyData(assetsForYear, false);
    }

    /// <summary>
    /// 指定された年の月次資産推移をセット＆描画
    /// useStartOffset == true のとき、その年の 1ヶ月目だけ開始点オフセットを適用。
    /// </summary>
    public void SetMonthlyData(List<int> assetsForYear, bool useStartOffset)
    {
        monthlyAssets.Clear();
        useStartOffsetThisYear = useStartOffset;

        if (assetsForYear != null && assetsForYear.Count > 0)
        {
            monthlyAssets.AddRange(assetsForYear);
        }

        RebuildGraph();
    }

    //========================================================
    //  描画
    //========================================================

    /// <summary>グラフの子要素（点・線）を全削除（ホバーマーカーは残す）</summary>
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

    /// <summary>内部データからグラフを再構築</summary>
    private void RebuildGraph()
    {
        if (graphRect == null) return;

        ClearGraphVisuals();
        pointPositions.Clear();

        float minAsset;
        float maxAsset;

        //----------------------------------------------------
        // 1. Y軸レンジ計算（0 を中心にした ±上限）
        //----------------------------------------------------
        if (monthlyAssets.Count == 0)
        {
            // データが無いときはデフォルトレンジで軸だけ表示
            minAsset = -defaultAbsMax;
            maxAsset = defaultAbsMax;

            UpdateYAxisLabels(minAsset, maxAsset);
            UpdateXAxisLabels(12); // 12ヶ月分の目盛り

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

        // 絶対値で一番大きい値を基準に
        float absMaxData = Mathf.Max(Mathf.Abs(rawMin), Mathf.Abs(rawMax));

        // 余白を追加
        absMaxData += axisMargin;

        // デフォルト上限と比較して大きい方を採用
        float finalAbsMax = Mathf.Max(absMaxData, defaultAbsMax);

        // 最終的なレンジ（上: +finalAbsMax / 下: -finalAbsMax）
        minAsset = -finalAbsMax;
        maxAsset = finalAbsMax;

        UpdateYAxisLabels(minAsset, maxAsset);
        UpdateXAxisLabels(monthlyAssets.Count);

        //----------------------------------------------------
        // 2. 点と線の描画
        //----------------------------------------------------
        float width = graphRect.rect.width;
        float height = graphRect.rect.height;

        Vector2 previousPos = Vector2.zero;
        bool hasPrev = false;

        int count = monthlyAssets.Count;
        if (count <= 1) count = 2;   // 1点だけでも左端/右端を計算できるように

        for (int i = 0; i < monthlyAssets.Count; i++)
        {
            // X：左0〜右1 に正規化
            float tX = (float)i / (count - 1);
            float x = tX * width;

            // Y：下 minAsset〜上 maxAsset を 0〜1 に正規化
            float tY = Mathf.InverseLerp(minAsset, maxAsset, monthlyAssets[i]);
            float y = tY * height;

            // 1ヶ月目 かつ useStartOffsetThisYear のときだけ Y オフセットを加える
            if (i == 0 && useStartOffsetThisYear)
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

            // ---- 線（上昇 / 減少で色分け）----
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

                // ここで資産の増減を見て色を変える
                int prevIndex = i - 1;
                if (prevIndex >= 0 && prevIndex < monthlyAssets.Count)
                {
                    bool isUp = monthlyAssets[i] >= monthlyAssets[prevIndex];
                    var img = line.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = isUp ? lineUpColor : lineDownColor;
                    }
                }
            }

            previousPos = pos;
            hasPrev = true;
        }
    }

    //========================================================
    //  軸ラベル
    //========================================================

    /// <summary>Y軸ラベル（上:+上限 / 中央:0 / 下:-上限）</summary>
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
                if (i == 0) text = topText;          // 一番上
                else if (i == 1) text = middleText;  // 真ん中
                else text = bottomText;              // 一番下
            }
            else
            {
                // 3つ以外のときは保険として線形配置
                float t = (n == 1) ? 0f : (float)i / (n - 1);
                float v = Mathf.Lerp(-absMax, absMax, t);
                int vi = Mathf.RoundToInt(v);
                text = $"{vi.ToString("N0")}円";
            }

            if (i < nTMP && yAxisLabelsTMP[i] != null) yAxisLabelsTMP[i].text = text;
            if (i < nGUI && yAxisLabelsUGUI[i] != null) yAxisLabelsUGUI[i].text = text;
        }
    }

    /// <summary>X軸ラベル（月）</summary>
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

        int monthIndex = closestIndex;          // 0-based
        int month = monthIndex + 1;             // 表示用は 1月〜
        int asset = monthlyAssets[monthIndex];

        if (hoverInfoText != null)
        {
            hoverInfoText.text = $"{month}月 : {asset.ToString("N0")}円";
        }

        if (hoverMarker != null)
        {
            hoverMarker.gameObject.SetActive(true);
            hoverMarker.anchorMin = hoverMarker.anchorMax = new Vector2(0f, 0f);
            hoverMarker.anchoredPosition = pointPositions[closestIndex];
        }
    }
}
