using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// つみたてみらい用メインシミュレーション管理クラス。
/// ・1ターン＝1年
/// ・内部では 12ヶ月分をループして計算
/// ・全15年（totalYears）まで進める
/// 
/// UI側（SimulationUIController など）は、
/// このクラスの公開プロパティを読むだけで表示を更新できる想定。
/// </summary>

public class SimulationManager : MonoBehaviour
{
    // ====== 設定値（インスペクタ or GameSettings から読み込む） ======

    [Header("Simulation Config")]
    [SerializeField] private int totalYears = 15;   // 仕様：15年固定
    [SerializeField] private int monthsPerYear = 12;

    // GameSettings から受け取る想定
    private int monthlyAmount;       // 毎月のつみたて額
    private RiskType riskType;       // 低・中・高リスク

    // ====== 状態 ======

    [Header("Runtime State (ReadOnly in Inspector)")]
    [SerializeField] private int currentYear = 0;           // 今「何年目」まで計算済みか（0〜totalYears）
    [SerializeField] private float currentAsset = 0f;       // 現在の資産（最新年の年末資産）
    [SerializeField] private float lastYearStartAsset = 0f; // その年の開始時資産
    [SerializeField] private float lastYearEndAsset = 0f;   // その年の終了時資産
    [SerializeField] private float lastYearChangePercent = 0f; // その年の変動率（％）

    // 年末資産の履歴（グラフ用）
    private readonly List<float> yearlyAssets = new List<float>();

    // ====== プロパティ（UIや他スクリプト用） ======

    public int CurrentYear => currentYear;               // 0年のときはまだ未シミュレーション
    public int TotalYears => totalYears;
    public int MonthsPerYear => monthsPerYear;
    public float CurrentAsset => currentAsset;
    public float LastYearStartAsset => lastYearStartAsset;
    public float LastYearEndAsset => lastYearEndAsset;
    public float LastYearChangePercent => lastYearChangePercent;
    public int MonthlyAmount => monthlyAmount;
    public RiskType CurrentRiskType => riskType;
    public IReadOnlyList<float> YearlyAssets => yearlyAssets;

    // UIが「今年の処理が終わった」タイミングを知りたい場合用のイベント
    public System.Action<int> OnYearSimulated;        // 引数：currentYear
    public System.Action OnSimulationFinished;        // 15年分終了時

    // ====== 外部から呼ぶ初期化 ======

    /// <summary>
    /// GameSettings などから値を読み取って新しいシミュレーションを開始する。
    /// （タイトル→設定→シミュレーション開始時に1回呼ぶ想定）
    /// </summary>
    public void StartNewSimulation()
    {
        // GameSettings から値を取得
        if (GameSettings.Instance != null)
        {
            monthlyAmount = GameSettings.Instance.MonthlyAmount;
            riskType = GameSettings.Instance.RiskType;
            totalYears = GameSettings.Instance.TotalYears; // 基本15
        }
        else
        {
            // 念のため、GameSettings が見つからなかったとき用のフォールバック
            if (monthlyAmount <= 0)
            {
                monthlyAmount = 10000;
            }
            riskType = RiskType.Medium;
            totalYears = 15;
            Debug.LogWarning("GameSettings.Instance が見つからなかったため、仮のデフォルト値を使用しました。");
        }

        currentYear = 0;
        currentAsset = 0f;
        lastYearStartAsset = 0f;
        lastYearEndAsset = 0f;
        lastYearChangePercent = 0f;

        yearlyAssets.Clear();
        yearlyAssets.Add(currentAsset);
    }

    // ====== 1年分だけ進める（ボタンから呼ぶ） ======

    /// <summary>
    /// 「次の年へ ▶」ボタンで呼び出す想定。
    /// 内部で 12ヶ月分の積立＆値動きを行い、1年分進める。
    /// </summary>
    public void SimulateOneYear()
    {
        // これ以上進めない
        if (currentYear >= totalYears)
        {
            Debug.LogWarning("Already reached max years.");
            return;
        }

        // 今年の開始時点の資産を記録
        lastYearStartAsset = currentAsset;

        // 12ヶ月ぶん処理
        for (int i = 0; i < monthsPerYear; i++)
        {
            // 毎月積立
            currentAsset += monthlyAmount;

            // 月次変動（％）取得（例：+1.5% → 1.5f）
            float monthlyChangePercent = GetMonthlyChangePercent(riskType);

            // 資産に反映
            currentAsset *= 1f + (monthlyChangePercent / 100f);
        }

        // 年末資産を記録
        lastYearEndAsset = currentAsset;

        // 年間変動率を計算（％）
        // 1年目だけ start が 0 なので、ゼロ割防止のため特別対応
        if (lastYearStartAsset > 0f)
        {
            lastYearChangePercent = (lastYearEndAsset - lastYearStartAsset) / lastYearStartAsset * 100f;
        }
        else
        {
            // 1年目は「今年増えたけど、開始時が0なので％は参考値」
            // → とりあえず 0 としておいてもOK。必要なら別ロジックに変更可。
            lastYearChangePercent = 0f;
        }

        // 年数を1つ進める（1年目完了 → currentYear = 1）
        currentYear++;

        // 年末資産を履歴に追加（グラフ用）
        yearlyAssets.Add(currentAsset);

        // UI側へ通知
        OnYearSimulated?.Invoke(currentYear);

        if (currentYear >= totalYears)
        {
            OnSimulationFinished?.Invoke();
        }
    }

    // ====== リスク別・月次変動ロジック（仕様書どおり） ======

    /// <summary>
    /// リスクタイプに応じた「月次のランダム変動率（％）」を返す。
    /// 仕様書の範囲：
    /// 低リスク： -1% ～ +2%
    /// 中リスク： -3% ～ +4%
    /// 高リスク： -6% ～ +8%
    /// </summary>
    private float GetMonthlyChangePercent(RiskType type)
    {
        switch (type)
        {
            case RiskType.Low:
                return Random.Range(-1f, 2f);
            case RiskType.Medium:
                return Random.Range(-3f, 4f);
            case RiskType.High:
                return Random.Range(-6f, 8f);
            default:
                return 0f;
        }
    }
}

/// <summary>
/// リスクタイプの列挙。将来 GameSettings と共有しても良い。
/// </summary>
public enum RiskType
{
    Low,
    Medium,
    High
}
