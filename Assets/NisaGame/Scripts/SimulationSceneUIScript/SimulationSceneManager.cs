using UnityEngine;
using UnityEngine.UI;  // Toggle 用
using TMPro;           // TMP_Text 用

public class SimulationSceneManager : MonoBehaviour
{
    [Header("年数表示 UI")]
    [SerializeField] private TMP_Text yearText;        // 「0年目 / 15年」

    [Header("積立額 UI")]
    [SerializeField] private TMP_Text monthlyAmountText; // 「10,000円」の部分

    [Header("リスク選択 UI")]
    [SerializeField] private Toggle riskLowToggle;     // 低リスク
    [SerializeField] private Toggle riskMiddleToggle;  // 中リスク
    [SerializeField] private Toggle riskHighToggle;    // 高リスク
    [SerializeField] private TMP_Text riskLabelText;   // 「リスク：◯◯」みたいに表示したい場合（不要なら空でOK）

    [Header("年数設定")]
    [SerializeField] private int maxYear = 15;   // 最後は 15 年目
    [SerializeField] private int currentYear = 0; // 0年目スタート

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 10000;  // 初期の毎月の積立額（円）
    [SerializeField] private int monthlyStep = 1000;   // ＋/− 1回で変える金額
    [SerializeField] private int minMonthlyAmount = 1000;   // 最低額
    [SerializeField] private int maxMonthlyAmount = 100000; // 最大額

    [Header("リスク設定")]
    // 0 = 低リスク, 1 = 中リスク, 2 = 高リスク という想定（必要に応じて変えてOK）
    [SerializeField] private int currentRiskType = 1; // デフォルト中リスクにしておくなど

    private void Start()
    {
        // 念のため 0〜maxYear に丸めておく
        currentYear = Mathf.Clamp(currentYear, 0, maxYear);

        // 初期表示更新
        UpdateYearText();
        UpdateMonthlyAmountText();
        UpdateRiskLabel();
        UpdateRiskUIInteractable();
    }

    // =========================
    // 年を進める（「次の年へ ▶」ボタン用）
    // =========================
    public void OnClickNextYear()
    {
        if (currentYear >= maxYear)
        {
            Debug.Log("これ以上進めません（最終年です）");
            return;
        }

        // ★ここで「今年1年分のシミュレーション計算」を行うようにしていく予定
        SimulateOneYear();

        // 年を進める
        currentYear++;

        // 表示更新
        UpdateYearText();
        UpdateRiskUIInteractable();
        // 積立額は毎年変更可能なので、特にロック解除/ロックは不要
    }

    private void UpdateYearText()
    {
        if (yearText != null)
        {
            yearText.text = $"{currentYear}年目 / {maxYear}年";
        }
    }

    // =========================
    // 積立額（毎月）
    // =========================

    private void UpdateMonthlyAmountText()
    {
        if (monthlyAmountText != null)
        {
            // 3桁区切り ＋ 「円」
            monthlyAmountText.text = $"{monthlyAmount.ToString("N0")}円";
        }
    }

    // 「＋」ボタン用
    public void OnClickIncreaseMonthly()
    {
        monthlyAmount = Mathf.Min(monthlyAmount + monthlyStep, maxMonthlyAmount);
        UpdateMonthlyAmountText();
    }

    // 「−」ボタン用
    public void OnClickDecreaseMonthly()
    {
        monthlyAmount = Mathf.Max(monthlyAmount - monthlyStep, minMonthlyAmount);
        UpdateMonthlyAmountText();
    }

    public int GetMonthlyAmount()
    {
        return monthlyAmount;
    }

    // =========================
    // リスクタイプ関連
    // =========================

    // この年にリスク変更して良いか？
    private bool CanChangeRiskThisYear()
    {
        // 0年目・5年目・10年目だけ変更可能
        return currentYear == 0 || currentYear == 5 || currentYear == 10;
    }

    // トグルの interactable を切り替える
    private void UpdateRiskUIInteractable()
    {
        bool canEdit = CanChangeRiskThisYear();

        if (riskLowToggle != null) riskLowToggle.interactable = canEdit;
        if (riskMiddleToggle != null) riskMiddleToggle.interactable = canEdit;
        if (riskHighToggle != null) riskHighToggle.interactable = canEdit;
    }

    // 低リスクトグルの OnValueChanged(bool) に繋ぐ
    public void OnSelectRiskLow(bool isOn)
    {
        if (!isOn) return; // OFF になったタイミングでは何もしない

        currentRiskType = 0;
        UpdateRiskLabel();
        Debug.Log("リスクタイプ：低リスク");
    }

    // 中リスク
    public void OnSelectRiskMiddle(bool isOn)
    {
        if (!isOn) return;
        currentRiskType = 1;
        UpdateRiskLabel();
        Debug.Log("リスクタイプ：中リスク");
    }

    // 高リスク
    public void OnSelectRiskHigh(bool isOn)
    {
        if (!isOn) return;
        currentRiskType = 2;
        UpdateRiskLabel();
        Debug.Log("リスクタイプ：高リスク");
    }

    private void UpdateRiskLabel()
    {
        if (riskLabelText == null) return;

        string label = "";
        switch (currentRiskType)
        {
            case 0: label = "低リスク"; break;
            case 1: label = "中リスク"; break;
            case 2: label = "高リスク"; break;
            default: label = "不明"; break;
        }

        riskLabelText.text = $"リスクタイプ：{label}";
    }

    public int GetCurrentRiskType()
    {
        return currentRiskType;
    }

    // =========================
    // 1年分のシミュレーション（中身は後で）
    // =========================

    private void SimulateOneYear()
    {
        // ここに
        // ・currentYear
        // ・monthlyAmount
        // ・currentRiskType
        // を使って、1年分の資産を増やす計算を書いていく予定。
        // 今はまだダミー（何もしない）でOK。
    }

    // 現在の年を知りたいとき用
    public int GetCurrentYear()
    {
        return currentYear;
    }
}
