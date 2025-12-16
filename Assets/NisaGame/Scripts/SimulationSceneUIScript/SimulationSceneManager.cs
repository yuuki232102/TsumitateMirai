using UnityEngine;
using UnityEngine.UI;  // Toggle / Slider 用
using TMPro;           // TMP_Text 用

public class SimulationSceneManager : MonoBehaviour
{
    [Header("年数表示 UI")]
    [SerializeField] private TMP_Text yearText;        // 「0年目 / 15年」

    [Header("積立額 UI")]
    [SerializeField] private TMP_Text monthlyAmountText;   // 「10,000円」の部分
    [SerializeField] private Slider monthlyAmountSlider; // ★ スライダーを追加

    [Header("リスク選択 UI")]
    [SerializeField] private Toggle riskLowToggle;       // 低リスク
    [SerializeField] private Toggle riskMiddleToggle;    // 中リスク
    [SerializeField] private Toggle riskHighToggle;      // 高リスク
    [SerializeField] private TMP_Text riskLabelText;       // 「リスク：◯◯」

    [Header("年数設定")]
    [SerializeField] private int maxYear = 15;   // 最後は 15 年目
    [SerializeField] private int currentYear = 0; // 0年目スタート

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 10000;   // 初期の毎月の積立額（円）
    [SerializeField] private int monthlyStep = 1000;    // ＋/− 1回で変える金額・スライダー刻み
    [SerializeField] private int minMonthlyAmount = 1000; // 最低額
    [SerializeField] private int maxMonthlyAmount = 100000; // 最大額

    [Header("リスク設定")]
    // 0 = 低リスク, 1 = 中リスク, 2 = 高リスク
    [SerializeField] private int currentRiskType = 1; // デフォルト中リスク

    // スライダー更新ループを防ぐためのフラグ
    private bool isUpdatingMonthlySlider = false;

    private void Start()
    {
        // 念のため 0〜maxYear に丸めておく
        currentYear = Mathf.Clamp(currentYear, 0, maxYear);

        // monthlyStep が 0 以下だと困るので保険
        if (monthlyStep <= 0)
        {
            monthlyStep = 1000;
        }

        // 積立額を範囲内 & 刻みにスナップ
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        // ★ スライダーの初期設定
        if (monthlyAmountSlider != null)
        {
            monthlyAmountSlider.minValue = minMonthlyAmount;
            monthlyAmountSlider.maxValue = maxMonthlyAmount;
            monthlyAmountSlider.wholeNumbers = true;

            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }

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

    // 「＋」ボタン用（使う場合）
    public void OnClickIncreaseMonthly()
    {
        monthlyAmount = Mathf.Min(monthlyAmount + monthlyStep, maxMonthlyAmount);
        // 刻み＆範囲に再度合わせる
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        UpdateMonthlyAmountText();

        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }
    }

    // 「−」ボタン用（使う場合）
    public void OnClickDecreaseMonthly()
    {
        monthlyAmount = Mathf.Max(monthlyAmount - monthlyStep, minMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        UpdateMonthlyAmountText();

        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }
    }

    // ★ スライダー用：OnValueChanged(float) から呼ぶ
    public void OnMonthlySliderChanged(float sliderValue)
    {
        if (isUpdatingMonthlySlider)
        {
            // コード側から value を変えたときは何もしない
            return;
        }

        // スライダーの値を刻みにスナップ
        int snapped = Mathf.RoundToInt(sliderValue / (float)monthlyStep) * monthlyStep;
        snapped = Mathf.Clamp(snapped, minMonthlyAmount, maxMonthlyAmount);

        monthlyAmount = snapped;
        UpdateMonthlyAmountText();

        // スナップ後の値をスライダーに反映（無限ループ防止フラグ付き）
        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }
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
