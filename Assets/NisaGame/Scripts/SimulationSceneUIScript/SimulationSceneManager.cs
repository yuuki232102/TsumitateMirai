using UnityEngine;
using UnityEngine.UI;   // Toggle / Slider
using TMPro;            // TMP_Text

public class SimulationSceneManager : MonoBehaviour
{
    [Header("年数表示 UI")]
    [SerializeField] private TMP_Text yearText;              // 「0年目 / 15年」

    [Header("資産表示 UI")]
    [SerializeField] private TMP_Text currentAssetText;      // 「現在の資産」の数値部分

    [Header("積立額 UI")]
    [SerializeField] private TMP_Text monthlyAmountText;     // 「10,000円」のテキスト
    [SerializeField] private Slider monthlyAmountSlider;   // 毎月のつみたて額スライダー

    [Header("リスク選択 UI")]
    [SerializeField] private Toggle riskLowToggle;         // 低リスク
    [SerializeField] private Toggle riskMiddleToggle;      // 中リスク
    [SerializeField] private Toggle riskHighToggle;        // 高リスク
    [SerializeField] private TMP_Text riskLabelText;         // 「リスクタイプ：◯◯」

    [Header("年数設定")]
    [SerializeField] private int maxYear = 15;           // 最後は15年目
    [SerializeField] private int currentYear = 0;            // 0年目スタート

    [Header("積立額設定")]
    [SerializeField] private int monthlyAmount = 1000;    // 初期の毎月の積立額（円）
    [SerializeField] private int monthlyStep = 1000;    // ＋/− / スライダー刻み
    [SerializeField] private int minMonthlyAmount = 1000;    // 最低額
    [SerializeField] private int maxMonthlyAmount = 100000;  // 最大額

    [Header("資産状態（シミュレーション用）")]
    [SerializeField] private int assetAtStartOfYear = 0;     // 「今年の始め」の資産（前年までの結果）
    [SerializeField] private int currentAsset = 0;     // 「今年の設定で1年回した場合の今年末資産」
    [SerializeField] private int totalPrincipal = 0;     // これまで積み立てた元本の合計

    [Header("リスク設定")]
    // 0 = 低リスク, 1 = 中リスク, 2 = 高リスク
    [SerializeField] private int currentRiskType = 1;        // デフォルト中リスク

    [Header("リスク別期待リターン（年率）")]
    [SerializeField] private float lowRiskReturnRate = 0.02f; // 年率2%
    [SerializeField] private float middleRiskReturnRate = 0.04f; // 年率4%
    [SerializeField] private float highRiskReturnRate = 0.06f; // 年率6%


    [Header("グラフ UI")]
    [SerializeField] private SimulationGraphUI graphUI;



    // 内部フラグ
    private bool isUpdatingMonthlySlider = false;         // スライダー同期中フラグ
    private bool hasRiskCallbackInitialized = false;         // リスクトグル初回コールバックを無視する用

    //==================================================
    // 初期化
    //==================================================
    private void Start()
    {
        // 年数の初期化
        currentYear = Mathf.Clamp(currentYear, 0, maxYear);

        // monthlyStep が 0 以下だと困るので保険
        if (monthlyStep <= 0)
        {
            monthlyStep = 1000;
        }

        // 資産は毎回 0 からスタート（0年目のスタート時点）
        assetAtStartOfYear = 0;
        currentAsset = 0;
        totalPrincipal = 0;

        // 積立額を範囲内 & 刻みにスナップ
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        // スライダー初期設定
        if (monthlyAmountSlider != null)
        {
            monthlyAmountSlider.minValue = minMonthlyAmount;
            monthlyAmountSlider.maxValue = maxMonthlyAmount;
            monthlyAmountSlider.wholeNumbers = true;

            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;   // ここで 1000 にセット
            isUpdatingMonthlySlider = false;
        }

        // ★ グラフ初期化：0年目の点を打つ
        if (graphUI != null)
        {
            graphUI.ResetGraph();
            graphUI.AddPoint(currentYear, currentAsset);   // 0年目・現在の資産（最初は 1000円）
        }

        if (graphUI != null)
        {
            // 「今年の終わり」の点 → 年数は currentYear + 1
            graphUI.AddPoint(currentYear + 1, currentAsset);
        }


        // ラベル類の更新
        UpdateYearText();
        UpdateMonthlyAmountText();
        UpdateRiskLabel();
        UpdateRiskUIInteractable();

        // ★ 0年目の初期表示をスライダーの金額と揃える
        UpdateCurrentYearPreviewAsset();   // この中で currentAssetText も更新される
    }

    //==================================================
    // 年を進める（「次の年へ ▶」ボタン）
    //==================================================
    public void OnClickNextYear()
    {
        if (currentYear >= maxYear)
        {
            Debug.Log("これ以上進めません（最終年です）");
            return;
        }

        // 現在の設定で「今年1年分」を確定
        SimulateOneYear();

        // 年を進める
        currentYear++;

        // 表示更新
        UpdateYearText();
        UpdateRiskUIInteractable();
    }

    private void UpdateYearText()
    {
        if (yearText != null)
        {
            yearText.text = $"{currentYear}年目 / {maxYear}年";
        }
    }

    //==================================================
    // 積立額（毎月）
    //==================================================
    private void UpdateMonthlyAmountText()
    {
        if (monthlyAmountText != null)
        {
            monthlyAmountText.text = $"{monthlyAmount.ToString("N0")}円";
        }
    }

    // 「＋」ボタン（使う場合）
    public void OnClickIncreaseMonthly()
    {
        monthlyAmount = Mathf.Min(monthlyAmount + monthlyStep, maxMonthlyAmount);
        monthlyAmount = Mathf.RoundToInt(monthlyAmount / (float)monthlyStep) * monthlyStep;
        monthlyAmount = Mathf.Clamp(monthlyAmount, minMonthlyAmount, maxMonthlyAmount);

        UpdateMonthlyAmountText();

        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }

        // 積立額変更にあわせて今年の資産プレビューを更新
        UpdateCurrentYearPreviewAsset();
    }

    // 「−」ボタン（使う場合）
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

        // 積立額変更にあわせて今年の資産プレビューを更新
        UpdateCurrentYearPreviewAsset();
    }

    // スライダーの OnValueChanged(float) から呼ぶ
    public void OnMonthlySliderChanged(float sliderValue)
    {
        // スクリプト側から value をいじっている最中は無視
        if (isUpdatingMonthlySlider)
        {
            return;
        }

        // スライダーの値を刻みにスナップ
        int snapped = Mathf.RoundToInt(sliderValue / (float)monthlyStep) * monthlyStep;
        snapped = Mathf.Clamp(snapped, minMonthlyAmount, maxMonthlyAmount);

        monthlyAmount = snapped;
        UpdateMonthlyAmountText();

        // スナップ後の値をスライダーに反映（見た目も揃える）
        if (monthlyAmountSlider != null)
        {
            isUpdatingMonthlySlider = true;
            monthlyAmountSlider.value = monthlyAmount;
            isUpdatingMonthlySlider = false;
        }

        // 積立額変更にあわせて今年の資産プレビューを更新
        UpdateCurrentYearPreviewAsset();
    }

    public int GetMonthlyAmount() => monthlyAmount;

    //==================================================
    // 現在の資産表示
    //==================================================
    private void UpdateCurrentAssetText()
    {
        if (currentAssetText != null)
        {
            currentAssetText.text = $"{currentAsset.ToString("N0")}円";
        }
    }

    public int GetCurrentAsset() => currentAsset;
    public int GetTotalPrincipal() => totalPrincipal;
    public int GetCurrentYear() => currentYear;
    public int GetCurrentRiskType() => currentRiskType;

    //==================================================
    // リスクタイプ関連
    //==================================================

    // この年にリスク変更して良いか？（0 / 5 / 10 年目だけ）
    private bool CanChangeRiskThisYear()
    {
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
        if (!isOn) return;

        currentRiskType = 0;
        UpdateRiskLabel();

        // 起動直後の最初の1回はプレビュー更新をスキップ
        if (!hasRiskCallbackInitialized)
        {
            hasRiskCallbackInitialized = true;
            return;
        }

        UpdateCurrentYearPreviewAsset();
        Debug.Log("リスクタイプ：低リスク");
    }

    // 中リスク
    public void OnSelectRiskMiddle(bool isOn)
    {
        if (!isOn) return;

        currentRiskType = 1;
        UpdateRiskLabel();

        if (!hasRiskCallbackInitialized)
        {
            hasRiskCallbackInitialized = true;
            return;
        }

        UpdateCurrentYearPreviewAsset();
        Debug.Log("リスクタイプ：中リスク");
    }

    // 高リスク
    public void OnSelectRiskHigh(bool isOn)
    {
        if (!isOn) return;

        currentRiskType = 2;
        UpdateRiskLabel();

        if (!hasRiskCallbackInitialized)
        {
            hasRiskCallbackInitialized = true;
            return;
        }

        UpdateCurrentYearPreviewAsset();
        Debug.Log("リスクタイプ：高リスク");
    }

    private void UpdateRiskLabel()
    {
        if (riskLabelText == null) return;

        string label;
        switch (currentRiskType)
        {
            case 0: label = "低リスク"; break;
            case 1: label = "中リスク"; break;
            case 2: label = "高リスク"; break;
            default: label = "不明"; break;
        }

        riskLabelText.text = $"リスクタイプ：{label}";
    }

    // リスクタイプから年率を取得
    private float GetAnnualReturnRate()
    {
        switch (currentRiskType)
        {
            case 0: return lowRiskReturnRate;
            case 1: return middleRiskReturnRate;
            case 2: return highRiskReturnRate;
            default: return middleRiskReturnRate;
        }
    }

    //==================================================
    // 「今年1年分」の資産プレビュー（スライダー＆リスク変更時）
    //==================================================
    private void UpdateCurrentYearPreviewAsset()
    {
        // ★ 0年目だけは「スライダーの値＝毎月のつみたて額」をそのまま資産として扱う
        if (currentYear == 0)
        {
            currentAsset = monthlyAmount;     // 例：スライダーが 1,000円 なら 現在の資産も 1,000円
        }
        else
        {
            // 1年目以降は、これまでの資産＋今年の積立額を利回り付きで計算
            int yearlyContribution = monthlyAmount * 12;
            float rate = GetAnnualReturnRate();

            // 「今年の始めの資産 ＋ 今年の積立額」を年率 rate で1年間運用したと仮定
            float after = (assetAtStartOfYear + yearlyContribution) * (1f + rate);

            currentAsset = Mathf.RoundToInt(after);
        }

        UpdateCurrentAssetText();
    }

    //==================================================
    // 1年分のシミュレーション（確定処理・次の年へ進むとき）
    //==================================================
    private void SimulateOneYear()
    {
        // 最新の設定でプレビューを更新
        UpdateCurrentYearPreviewAsset();

        int yearlyContribution = monthlyAmount * 12;

        // 元本合計に今年の積立額を加算（確定）
        totalPrincipal += yearlyContribution;

        // 来年の「年初資産」は、今年末の資産になる
        assetAtStartOfYear = currentAsset;

        // 将来グラフ用のデータ追加などはここで行う想定
        // ★ グラフに「今年の終わり」の点を追加（年数は currentYear + 1）
        if (graphUI != null)
        {
            int newYear = currentYear + 1;
            graphUI.AddPoint(newYear, currentAsset);
        }
    }
}
