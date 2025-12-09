using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 年ごとの資産推移を、Canvas 内の RectTransform 上に折れ線グラフとして描画するクラス。
/// ・SimulationManager.YearlyAssets をもとに表示
/// ・SimulationUIController から UpdateGraph() を呼び出して使う
/// 
/// 必要なもの：
/// ・graphContainer : グラフを描画するエリア（RectTransform）
/// ・pointPrefab    : 1点を表す小さなUI(Image)プレハブ
/// ・linePrefab     : 点と点を結ぶ細長いUI(Image)プレハブ
/// 
/// どちらのPrefabも、Canvas上の普通のImageでOK。
/// </summary>
public class GraphRenderer : MonoBehaviour
{
    [Header("Graph Area")]
    [SerializeField] private RectTransform graphContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private GameObject linePrefab;

    [Header("Padding")]
    [SerializeField] private float xPadding = 30f;
    [SerializeField] private float yPadding = 30f;

    // 生成したポイント・ラインを管理（更新時に削除するため）
    private readonly List<GameObject> pointObjects = new List<GameObject>();
    private readonly List<GameObject> lineObjects = new List<GameObject>();

    /// <summary>
    /// 外部から呼び出してグラフを更新する。
    /// SimulationUIController から YearlyAssets を渡す想定。
    /// </summary>
    public void UpdateGraph(IReadOnlyList<float> values)
    {
        if (graphContainer == null)
        {
            Debug.LogWarning("GraphRenderer: graphContainer が設定されていません。");
            return;
        }

        // 既存のポイント・ラインを削除
        ClearGraph();

        if (values == null || values.Count == 0)
        {
            // データが無ければ何も描画しない
            return;
        }

        if (pointPrefab == null || linePrefab == null)
        {
            Debug.LogWarning("GraphRenderer: pointPrefab または linePrefab が設定されていません。");
            return;
        }

        int pointCount = values.Count;

        // グラフエリアのサイズ取得
        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;

        float usableWidth = Mathf.Max(0f, width - xPadding * 2f);
        float usableHeight = Mathf.Max(0f, height - yPadding * 2f);

        // 最小値・最大値を取得（スケーリング用）
        float minValue = values[0];
        float maxValue = values[0];

        for (int i = 1; i < pointCount; i++)
        {
            float v = values[i];
            if (v < minValue) minValue = v;
            if (v > maxValue) maxValue = v;
        }

        // 値が全部同じだったときにゼロ割りしないための調整
        if (Mathf.Approximately(maxValue, minValue))
        {
            // 全て同じ値の場合、範囲を少しだけ持たせる
            maxValue = minValue + 1f;
        }

        // ポイント生成 & ライン生成
        Vector2? lastPointPos = null;

        for (int i = 0; i < pointCount; i++)
        {
            float value = values[i];

            // X座標：0〜(pointCount-1)を等間隔に割り当て
            float t = (pointCount == 1) ? 0.5f : (float)i / (pointCount - 1);
            float xPos = xPadding + usableWidth * t;

            // Y座標：min〜max を 0〜usableHeight にマッピング
            float normalized = (value - minValue) / (maxValue - minValue);
            float yPos = yPadding + usableHeight * normalized;

            Vector2 anchoredPos = new Vector2(xPos, yPos);

            // ポイントを生成
            RectTransform pointRect = CreatePoint(anchoredPos);

            // ラインを生成（前のポイントと結ぶ）
            if (lastPointPos.HasValue)
            {
                RectTransform lineRect = CreateLine(lastPointPos.Value, anchoredPos);
            }

            lastPointPos = anchoredPos;
        }
    }

    /// <summary>
    /// グラフエリア内にポイント（丸や四角）を1つ生成する。
    /// </summary>
    private RectTransform CreatePoint(Vector2 anchoredPosition)
    {
        GameObject obj = Instantiate(pointPrefab, graphContainer);
        RectTransform rect = obj.GetComponent<RectTransform>();

        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);

        pointObjects.Add(obj);
        return rect;
    }

    /// <summary>
    /// 2点を結ぶライン（細長い矩形）を生成する。
    /// </summary>
    private RectTransform CreateLine(Vector2 start, Vector2 end)
    {
        GameObject obj = Instantiate(linePrefab, graphContainer);
        RectTransform rect = obj.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.localScale = Vector3.one;

        Vector2 dir = (end - start).normalized;
        float distance = Vector2.Distance(start, end);

        // ラインの中心座標（startとendの中間）
        Vector2 middle = (start + end) * 0.5f;

        rect.anchoredPosition = middle;
        // 横方向に伸びる前提で、X方向を長さにする（linePrefabは横長前提）
        rect.sizeDelta = new Vector2(distance, rect.sizeDelta.y);

        // 回転角度を設定
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.localEulerAngles = new Vector3(0, 0, angle);

        lineObjects.Add(obj);
        return rect;
    }

    /// <summary>
    /// 既に描画されているポイント・ラインを全削除。
    /// </summary>
    private void ClearGraph()
    {
        foreach (var obj in pointObjects)
        {
            if (obj != null) Destroy(obj);
        }
        pointObjects.Clear();

        foreach (var obj in lineObjects)
        {
            if (obj != null) Destroy(obj);
        }
        lineObjects.Clear();
    }
}
