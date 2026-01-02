using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;   // Legacy Text 用

public class SimulationGraphUI : MonoBehaviour
{
    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;   // GraphContent

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab; // 点（●）
    [SerializeField] private RectTransform linePrefab;  // 線（―）

    [Header("基本設定")]
    [SerializeField] private int maxYear = 15;          // 年数（0〜15年）

    [Header("縦軸設定（デフォルト）")]
    [SerializeField] private float defaultMinAsset = -30000f;  // 初期下限：-3万円
    [SerializeField] private float defaultMaxAsset = 70000f;  // 初期上限：+7万円

    [Header("縦軸マージン設定")]
    [SerializeField] private float negativeMargin = 5000f;   // 下側余白（マイナス側）
    [SerializeField] private float positiveMargin = 10000f;  // 上側余白（プラス側）

    [Header("描画設定")]
    [SerializeField] private float pointSize = 12f;      // 点のサイズ
    [SerializeField] private float lineThickness = 3f;       // 線の太さ

    [Header("Y軸ラベルの親（子に Text を並べる）")]
    [SerializeField] private RectTransform yAxisLabelsRoot;

    // 実際に使っている現在の上下限
    private float minAssetValue;
    private float maxAssetValue;

    // 描画用・データ用
    private readonly List<RectTransform> pointList = new List<RectTransform>();
    private readonly List<Vector2Int> dataPoints = new List<Vector2Int>();   // (year, asset)

    private void Awake()
    {
        if (graphRect == null)
        {
            graphRect = GetComponent<RectTransform>();
        }

        // 縦軸レンジ初期値
        minAssetValue = defaultMinAsset;
        maxAssetValue = defaultMaxAsset;

        UpdateYAxisLabels();
    }

    /// <summary> グラフとデータを全部リセット </summary>
    public void ResetGraph()
    {
        foreach (Transform child in graphRect)
        {
            Destroy(child.gameObject);
        }
        pointList.Clear();
        dataPoints.Clear();

        minAssetValue = defaultMinAsset;
        maxAssetValue = defaultMaxAsset;
        UpdateYAxisLabels();
    }

    /// <summary>
    /// 年(year)・資産(asset) の点を追加する。
    /// 必要に応じて上下限を更新し、全点を描き直す。
    /// </summary>
    public void AddPoint(int year, int asset)
    {
        dataPoints.Add(new Vector2Int(year, asset));

        RecalculateAxisRangeFromData();
        RedrawAllPoints();
    }

    /// <summary>
    /// データから min/max を求めて縦軸レンジを更新
    /// </summary>
    private void RecalculateAxisRangeFromData()
    {
        if (dataPoints.Count == 0)
        {
            minAssetValue = defaultMinAsset;
            maxAssetValue = defaultMaxAsset;
            return;
        }

        float minSeen = dataPoints[0].y;
        float maxSeen = dataPoints[0].y;

        for (int i = 1; i < dataPoints.Count; i++)
        {
            int v = dataPoints[i].y;
            if (v < minSeen) minSeen = v;
            if (v > maxSeen) maxSeen = v;
        }

        // --- 下限（マイナス側） ---
        // デフォルトは -3万円。もっと下がったら「一番低い値 - negativeMargin」まで下げる。
        float newMin = defaultMinAsset;
        if (minSeen < 0f)
        {
            newMin = minSeen - negativeMargin;
        }

        // --- 上限（プラス側） ---
        // デフォルトは +7万円。もっと上がったら「一番高い値 + positiveMargin」まで上げる。
        float newMax = defaultMaxAsset;
        if (maxSeen > 0f)
        {
            newMax = Mathf.Max(defaultMaxAsset, maxSeen + positiveMargin);
        }

        // レンジが極端に狭くならないように保険
        if (newMax - newMin < 10000f)
        {
            newMax = newMin + 10000f;
        }

        minAssetValue = newMin;
        maxAssetValue = newMax;
    }

    /// <summary> dataPoints を元に点と線を全部描き直す </summary>
    private void RedrawAllPoints()
    {
        // 既存オブジェクト削除
        foreach (Transform child in graphRect)
        {
            Destroy(child.gameObject);
        }
        pointList.Clear();

        UpdateYAxisLabels();

        if (dataPoints.Count == 0) return;

        for (int i = 0; i < dataPoints.Count; i++)
        {
            Vector2Int dp = dataPoints[i];

            // X方向：0年〜maxYear を 0〜1 に正規化
            float x01 = maxYear > 0 ? Mathf.Clamp01((float)dp.x / maxYear) : 0f;

            // Y方向：min〜max を 0〜1 に正規化
            float y01 = 0f;
            if (maxAssetValue > minAssetValue + Mathf.Epsilon)
            {
                y01 = Mathf.InverseLerp(minAssetValue, maxAssetValue, dp.y);
            }

            Vector2 size = graphRect.rect.size;
            Vector2 localPos = new Vector2(
                -size.x * 0.5f + x01 * size.x,
                -size.y * 0.5f + y01 * size.y
            );

            // 点
            RectTransform point = Instantiate(pointPrefab, graphRect);
            point.gameObject.SetActive(true);
            point.anchorMin = point.anchorMax = new Vector2(0.5f, 0.5f);
            point.anchoredPosition = localPos;
            point.sizeDelta = new Vector2(pointSize, pointSize);

            // 線（前の点と結ぶ）
            if (pointList.Count > 0)
            {
                RectTransform prev = pointList[pointList.Count - 1];

                RectTransform line = Instantiate(linePrefab, graphRect);
                line.gameObject.SetActive(true);
                line.anchorMin = line.anchorMax = new Vector2(0.5f, 0.5f);

                Vector2 dir = point.anchoredPosition - prev.anchoredPosition;
                float length = dir.magnitude;

                line.sizeDelta = new Vector2(length, lineThickness);

                Vector2 middle = (prev.anchoredPosition + point.anchoredPosition) * 0.5f;
                line.anchoredPosition = middle;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                line.localRotation = Quaternion.Euler(0, 0, angle);
            }

            pointList.Add(point);
        }
    }

    /// <summary>
    /// 現在の min/max に合わせて Y軸ラベルのテキストを更新。
    /// 0 をまたいでいる場合は、0 に一番近いラベルを「0円」に固定。
    /// </summary>
    private void UpdateYAxisLabels()
    {
        if (yAxisLabelsRoot == null) return;

        // 親の子孫から Text を全部拾う（毎回取り直し）
        Text[] labels = yAxisLabelsRoot.GetComponentsInChildren<Text>(true);
        if (labels == null || labels.Length == 0) return;

        // 上にあるものから下にあるものへ並び替え
        System.Array.Sort(labels,
            (a, b) => b.rectTransform.position.y.CompareTo(a.rectTransform.position.y));
        // labels[0] = 一番上, labels[n-1] = 一番下

        int n = labels.Length;

        if (n == 1)
        {
            int v = Mathf.RoundToInt(maxAssetValue / 1000f) * 1000;
            labels[0].text = $"{v:N0}円";
            return;
        }

        // まずは min〜max を均等に割った値を作る
        float[] rawValues = new float[n];
        for (int i = 0; i < n; i++)
        {
            // i=0(一番上)→t=1, i=n-1(一番下)→t=0
            float t = 1f - (float)i / (n - 1);
            rawValues[i] = Mathf.Lerp(minAssetValue, maxAssetValue, t);
        }

        // 範囲がマイナス〜プラスをまたいでいるときだけ、
        // 0 に一番近いラベルを「きっちり 0」にスナップさせる
        if (minAssetValue < 0f && maxAssetValue > 0f)
        {
            int closestIndex = 0;
            float closestAbs = Mathf.Abs(rawValues[0]);

            for (int i = 1; i < n; i++)
            {
                float abs = Mathf.Abs(rawValues[i]);
                if (abs < closestAbs)
                {
                    closestAbs = abs;
                    closestIndex = i;
                }
            }

            rawValues[closestIndex] = 0f;   // ここで 0 に固定
        }

        // 1000円単位に丸めて表示
        for (int i = 0; i < n; i++)
        {
            int rounded = Mathf.RoundToInt(rawValues[i] / 1000f) * 1000;
            labels[i].text = $"{rounded:N0}円";
        }
    }
}
