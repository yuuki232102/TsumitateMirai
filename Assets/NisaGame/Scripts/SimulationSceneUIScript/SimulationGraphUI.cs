using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationGraphUI : MonoBehaviour
{
    [Header("グラフ範囲")]
    [SerializeField] private RectTransform graphRect;   // GraphContent

    [Header("プレハブ")]
    [SerializeField] private RectTransform pointPrefab; // 点（●）
    [SerializeField] private RectTransform linePrefab;  // 線（―）

    [Header("設定")]
    [SerializeField] private int maxYear = 15;       // 年数（0〜15年）
    [SerializeField] private float maxAssetValue = 200000f;  // 縦軸の最大値（自動で更新される）
    [SerializeField] private float pointSize = 12f;      // 点のサイズ
    [SerializeField] private float lineThickness = 3f;       // 線の太さ

    [Header("Y軸ラベル（下 → 上 の順）")]
    [SerializeField] private TMP_Text[] yAxisLabels;         // 0円〜最大値までのラベル

    // 実際の描画用
    private readonly List<RectTransform> pointList = new List<RectTransform>();
    // データ保持用（year, asset）
    private readonly List<Vector2Int> dataPoints = new List<Vector2Int>();

    private void Awake()
    {
        if (graphRect == null)
        {
            graphRect = GetComponent<RectTransform>();
        }

        UpdateYAxisLabels();
    }

    /// <summary> グラフとデータを全部消す </summary>
    public void ResetGraph()
    {
        foreach (Transform child in graphRect)
        {
            Destroy(child.gameObject);
        }
        pointList.Clear();
        dataPoints.Clear();

        // 縦軸ラベルをリセット
        UpdateYAxisLabels();
    }

    /// <summary> 外から最大資産値を指定したいとき用（使わなくてもOK） </summary>
    public void SetMaxAssetValue(float newMax)
    {
        maxAssetValue = Mathf.Max(1f, newMax);
        RedrawAllPoints();
    }

    /// <summary>
    /// 年(year)・資産(asset) の点を一つ追加する。
    /// 必要に応じて縦軸スケールを拡大し、全点を描き直す。
    /// </summary>
    public void AddPoint(int year, int asset)
    {
        // データとして保存
        dataPoints.Add(new Vector2Int(year, asset));

        // これまでで最大の資産を調べる
        float maxSeen = 0f;
        foreach (var p in dataPoints)
        {
            if (p.y > maxSeen) maxSeen = p.y;
        }

        // 最大値を少し余裕を持って更新（1.1倍）
        if (maxSeen > maxAssetValue)
        {
            maxAssetValue = maxSeen * 1.1f;
        }

        // 全点描き直し
        RedrawAllPoints();
    }

    /// <summary> dataPoints を元に点と線を全部描き直す </summary>
    private void RedrawAllPoints()
    {
        // 既存の描画オブジェクトを全削除
        foreach (Transform child in graphRect)
        {
            Destroy(child.gameObject);
        }
        pointList.Clear();

        // 縦軸ラベル更新
        UpdateYAxisLabels();

        // 点と線を順番に再生成
        for (int i = 0; i < dataPoints.Count; i++)
        {
            Vector2Int dp = dataPoints[i];

            float x01 = Mathf.Clamp01((float)dp.x / maxYear);
            float y01 = maxAssetValue > 0 ? Mathf.Clamp01(dp.y / maxAssetValue) : 0f;

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

    /// <summary> Y軸ラベルのテキストを maxAssetValue に合わせて更新する </summary>
    private void UpdateYAxisLabels()
    {
        if (yAxisLabels == null || yAxisLabels.Length == 0) return;

        int n = yAxisLabels.Length;
        if (n == 1)
        {
            yAxisLabels[0].text = $"{maxAssetValue:N0}円";
            return;
        }

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / (n - 1); // 0 = 一番下, 1 = 一番上
            int value = Mathf.RoundToInt(maxAssetValue * t);

            if (yAxisLabels[i] != null)
            {
                yAxisLabels[i].text = $"{value:N0}円";
            }
        }
    }
}
