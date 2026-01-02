using UnityEngine;
using UnityEngine.UI;

public class SimulationLogUI : MonoBehaviour
{
    [Header("スクロールビューの Content")]
    [SerializeField] private RectTransform content;      // ResultScrollView/Viewport/Content

    [Header("ログ1行のプレハブ")]
    [SerializeField] private GameObject logEntryPrefab;  // LogEntryPrefab

    /// <summary>すべてのログを消す</summary>
    public void ClearAll()
    {
        if (content == null) return;

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 1ヶ月分の結果をログに追加する
    /// </summary>
    public void AddMonthlyRecord(
        int totalMonthIndex,
        int yearIndex,
        int monthInYear,
        int asset,
        int monthlyAmount,
        float annualRate
    )
    {
        if (content == null || logEntryPrefab == null) return;

        GameObject go = Instantiate(logEntryPrefab, content);
        go.SetActive(true);

        // 子に Text が付いている前提
        Text txt = go.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = string.Format(
                "{0:00}ヶ月目（{1}年目 {2}ヶ月目）：資産 {3:N0}円　[毎月 {4:N0}円 / 年率 {5:P1}]",
                totalMonthIndex,
                yearIndex,
                monthInYear,
                asset,
                monthlyAmount,
                annualRate
            );
        }
    }
}
