using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// シミュレーション画面（STEP1 & STEP2）のUI制御担当。
/// ・SimulationManager が計算した結果を画面に反映する
/// ・「次の年へ▶」「結果を見る▶」ボタンの挙動を制御する
/// ・STEP1（1年目開始画面）と STEP2（2〜15年目結果画面）の切り替えも担当
/// 
/// このスクリプトは SimulationScene の Canvas にアタッチして使う想定。
/// </summary>
public class SimulationUIController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private SimulationManager simulationManager;
    [SerializeField] private GraphRenderer graphRenderer; // 折れ線グラフ描画用（別スクリプト予定）

    [Header("Panels")]
    [SerializeField] private GameObject step1Panel;   // 1年目開始表示用パネル
    [SerializeField] private GameObject step2Panel;   // 2〜15年目結果表示用パネル

    [Header("Common UI")]
    [SerializeField] private TextMeshProUGUI yearCounterText; // "◯年目 / 15年"
    [SerializeField] private Button nextYearButton;
    [SerializeField] private TextMeshProUGUI nextYearButtonLabel; // ボタン上のテキスト

    [Header("STEP1 UI (初年度開始)")]
    [SerializeField] private TextMeshProUGUI step1CurrentAssetText;   // "0円"
    [SerializeField] private TextMeshProUGUI step1MonthlyAmountText;  // "まいつき◯円"
    [SerializeField] private TextMeshProUGUI step1RiskTypeText;       // "中リスク" 等
    [SerializeField] private TextMeshProUGUI step1CharacterMessage;   // 「ここからスタートだよ！」

    [Header("STEP2 UI (年次結果表示)")]
    [SerializeField] private TextMeshProUGUI thisYearChangeText;      // "＋6.8％ ↑"
    [SerializeField] private TextMeshProUGUI currentAssetText;        // "412,300円"
    [SerializeField] private TextMeshProUGUI annualContributionText;  // "120,000円"
    [SerializeField] private TextMeshProUGUI step2CharacterMessage;   // 年次コメント

    private bool isInitialized = false;

    private void Awake()
    {
        if (simulationManager == null)
        {
            simulationManager = FindObjectOfType<SimulationManager>();
        }
    }

    private void Start()
    {
        // SimulationManagerの初期化（GameSettings完成後はそちらに委譲）
        simulationManager.StartNewSimulation();

        // イベント登録（任意：使わなくてもOKだが、今後の拡張のために用意）
        simulationManager.OnYearSimulated += HandleYearSimulated;
        simulationManager.OnSimulationFinished += HandleSimulationFinished;

        // STEP1 からスタート
        ShowStep1();
        UpdateYearCounter(1); // 表示上は「1年目 / 15年」

        // ボタンにクリックイベントを登録
        nextYearButton.onClick.RemoveAllListeners();
        nextYearButton.onClick.AddListener(OnClickNextYear);

        isInitialized = true;
    }

    /// <summary>
    /// STEP1（初年度開始画面）を表示する。
    /// 計算前の状態：資産0円、設定の確認のみ。
    /// </summary>
    private void ShowStep1()
    {
        if (step1Panel != null) step1Panel.SetActive(true);
        if (step2Panel != null) step2Panel.SetActive(false);

        // 表示用のテキスト更新
        if (step1CurrentAssetText != null)
        {
            step1CurrentAssetText.text = "0円";
        }

        if (step1MonthlyAmountText != null)
        {
            step1MonthlyAmountText.text = $"まいつき {simulationManager.MonthlyAmount.ToString("N0")}円";
        }

        if (step1RiskTypeText != null)
        {
            step1RiskTypeText.text = GetRiskTypeLabel(simulationManager.CurrentRiskType);
        }

        if (step1CharacterMessage != null)
        {
            step1CharacterMessage.text = "ここから つみたてスタートだよ！";
        }

        // ボタンラベルは常に「次の年へ▶」から開始
        SetNextYearButtonToNextMode();
    }

    /// <summary>
    /// STEP2（2〜15年目の年次結果画面）を表示する。
    /// 1年目の計算が終わったタイミングで STEP1 → STEP2 に切り替える。
    /// </summary>
    private void ShowStep2()
    {
        if (step1Panel != null) step1Panel.SetActive(false);
        if (step2Panel != null) step2Panel.SetActive(true);
    }

    /// <summary>
    /// 「次の年へ▶ / 結果を見る▶」ボタンが押されたときの処理。
    /// </summary>
    private void OnClickNextYear()
    {
        if (!isInitialized) return;

        // まだシミュレーションが完了していない場合 → 1年分シミュレーション
        if (simulationManager.CurrentYear < simulationManager.TotalYears)
        {
            // 1年目の場合は STEP1 → STEP2 への切り替えのきっかけにもなる
            bool wasFirstYearBefore = (simulationManager.CurrentYear == 0);

            simulationManager.SimulateOneYear(); // ここで OnYearSimulated が呼ばれる

            if (wasFirstYearBefore)
            {
                // STEP1 の画面から結果表示用のSTEP2へ
                ShowStep2();
            }

            // UIを最新の年に合わせて更新
            UpdateYearCounter(simulationManager.CurrentYear);
            UpdateStep2UI();

            // 残り年数を見てボタンモードを切り替え
            if (simulationManager.CurrentYear >= simulationManager.TotalYears)
            {
                SetNextYearButtonToResultMode();
            }
        }
        else
        {
            // すでに全15年分のシミュレーションが完了している → 結果画面へ
            LoadResultScene();
        }
    }

    /// <summary>
    /// SimulationManager.OnYearSimulated のコールバック。
    /// ここで追加演出などを入れる余地がある。
    /// </summary>
    private void HandleYearSimulated(int year)
    {
        // 今は特に何もしない。
        // 必要ならここでアニメーション開始などを行う。
    }

    /// <summary>
    /// SimulationManager.OnSimulationFinished のコールバック。
    /// 15年分終了時に呼ばれる。
    /// </summary>
    private void HandleSimulationFinished()
    {
        // ボタン表記を「結果を見る▶」に切り替える。
        SetNextYearButtonToResultMode();
    }

    /// <summary>
    /// 年表示の更新（例：「3年目 / 15年」）
    /// </summary>
    private void UpdateYearCounter(int year)
    {
        if (yearCounterText != null)
        {
            yearCounterText.text = $"{year}年目 / {simulationManager.TotalYears}年";
        }
    }

    /// <summary>
    /// STEP2のUI（変動率・資産・年間積立額・キャラコメント・グラフ）の更新。
    /// </summary>
    private void UpdateStep2UI()
    {
        // 今年の変動率表示
        if (thisYearChangeText != null)
        {
            float change = simulationManager.LastYearChangePercent;
            string arrow = "→";
            Color color = Color.gray;

            if (change > 0.01f)
            {
                arrow = "↑";
                color = Color.green;
            }
            else if (change < -0.01f)
            {
                arrow = "↓";
                color = Color.red;
            }

            thisYearChangeText.text = $"{change:+0.0;-0.0;0.0}％ {arrow}";
            thisYearChangeText.color = color;
        }

        // 現在の資産
        if (currentAssetText != null)
        {
            currentAssetText.text = $"{simulationManager.CurrentAsset.ToString("N0")}円";
        }

        // 年間のつみたて額
        if (annualContributionText != null)
        {
            int annual = simulationManager.MonthlyAmount * simulationManager.MonthsPerYear;
            annualContributionText.text = $"{annual.ToString("N0")}円";
        }

        // キャラコメント
        if (step2CharacterMessage != null)
        {
            step2CharacterMessage.text = GetYearComment(simulationManager.LastYearChangePercent,
                                                        simulationManager.CurrentYear,
                                                        simulationManager.TotalYears);
        }

        // グラフ更新
        if (graphRenderer != null)
        {
            graphRenderer.UpdateGraph(simulationManager.YearlyAssets);
        }
    }

    /// <summary>
    /// リスクタイプのラベル文字列。
    /// </summary>
    private string GetRiskTypeLabel(RiskType riskType)
    {
        switch (riskType)
        {
            case RiskType.Low:
                return "低リスク";
            case RiskType.Medium:
                return "中リスク";
            case RiskType.High:
                return "高リスク";
            default:
                return "不明なリスク";
        }
    }

    /// <summary>
    /// 年次結果に応じて簡単なキャラコメントを返す。
    /// （必要に応じて後で細かく調整可能）
    /// </summary>
    private string GetYearComment(float changePercent, int year, int totalYears)
    {
        // 最終年かどうか
        bool isLastYear = (year >= totalYears);

        if (changePercent > 5f)
        {
            return isLastYear
                ? "最後の年に大きくふえたね！おつかれさま！"
                : "今年は大きくふえたね！この調子でつみたてしていこう！";
        }
        else if (changePercent > 0.1f)
        {
            return "すこしずつだけど、ちゃんとふえているよ。長く続けるのがコツだよ！";
        }
        else if (changePercent > -0.1f)
        {
            return "今年はあまり動かなかったみたい。こんな年もあるよ。";
        }
        else if (changePercent > -5f)
        {
            return "すこしさがっちゃったけど、長い目で見ていこう！つみたては続けることが大事だよ。";
        }
        else
        {
            return "大きくさがった年だったね…。でも、リスクを知ることも大事な学びだよ。";
        }
    }

    /// <summary>
    /// 「次の年へ▶」モードにボタン表示を変更。
    /// </summary>
    private void SetNextYearButtonToNextMode()
    {
        if (nextYearButtonLabel != null)
        {
            nextYearButtonLabel.text = "次の年へ ▶";
        }
    }

    /// <summary>
    /// 「結果を見る▶」モードにボタン表示を変更。
    /// </summary>
    private void SetNextYearButtonToResultMode()
    {
        if (nextYearButtonLabel != null)
        {
            nextYearButtonLabel.text = "結果を見る ▶";
        }
    }

    /// <summary>
    /// 結果画面（ResultScene）へ遷移。
    /// シーン名はプロジェクト側で揃えること。
    /// </summary>
    private void LoadResultScene()
    {
        // シーン名はプロジェクトで実際に付けた名前に合わせて変更すること。
        SceneManager.LoadScene("ResultScene");
    }
}

