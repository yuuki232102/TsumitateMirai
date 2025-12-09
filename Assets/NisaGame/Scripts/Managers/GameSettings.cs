using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体で共有する設定値を管理するシングルトン。
/// ・毎月のつみたて額
/// ・リスクタイプ
/// ・総年数（基本は15年固定）
/// 
/// TitleScene / SettingsScene などで値を変更し、
/// SimulationScene や ResultScene から参照する想定。
/// 
/// ※ RiskType enum は SimulationManager.cs など、
///   既に定義している場所と重複しないように注意。
/// </summary>
public class GameSettings : MonoBehaviour
{
    // ====== シングルトン ======

    public static GameSettings Instance { get; private set; }

    private void Awake()
    {
        // シングルトン確立
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // シーンをまたいでも消さない
    }

    // ====== 設定値（インスペクタ／UIから設定） ======

    [Header("Simulation Settings")]

    [SerializeField]
    private int totalYears = 15;       // 基本は 15 年固定

    [SerializeField]
    private int monthlyAmount = 10000; // デフォルト毎月つみたて額

    [SerializeField]
    private RiskType riskType = RiskType.Medium; // デフォルトは中リスク

    // 将来、他にも設定を増やすならここに追加（例：初期資金など）

    // ====== 公開プロパティ（読み取り用） ======

    /// <summary>
    /// シミュレーションする年数（仕様では15年）。
    /// 基本的には外から変更しない。
    /// </summary>
    public int TotalYears => totalYears;

    /// <summary>
    /// 毎月のつみたて額。
    /// </summary>
    public int MonthlyAmount => monthlyAmount;

    /// <summary>
    /// 現在選択されているリスクタイプ（低・中・高）。
    /// </summary>
    public RiskType RiskType => riskType;

    // ====== セッター（UIから変更するとき用） ======

    /// <summary>
    /// 毎月のつみたて額を設定する。
    /// SettingsScene のスライダーなどから呼ぶ想定。
    /// </summary>
    public void SetMonthlyAmount(int amount)
    {
        // マイナスなどが入らないように最低値チェック
        if (amount < 0)
        {
            amount = 0;
        }

        monthlyAmount = amount;
        // 必要なら Debug.Log やイベント発火もここで
    }

    /// <summary>
    /// リスクタイプを設定する。
    /// ドロップダウンやボタン群から呼ぶ想定。
    /// </summary>
    public void SetRiskType(RiskType newRiskType)
    {
        riskType = newRiskType;
    }

    /// <summary>
    /// リスクタイプを int（0,1,2）から設定したい場合用。
    /// UIのDropdownなどと相性が良い。
    /// </summary>
    public void SetRiskTypeByIndex(int index)
    {
        switch (index)
        {
            case 0:
                riskType = RiskType.Low;
                break;
            case 1:
                riskType = RiskType.Medium;
                break;
            case 2:
                riskType = RiskType.High;
                break;
            default:
                riskType = RiskType.Medium;
                break;
        }
    }

    /// <summary>
    /// 設定を初期値に戻したいとき用。
    /// （必要なければ使わなくてもOK）
    /// </summary>
    public void ResetToDefault()
    {
        totalYears = 15;
        monthlyAmount = 10000;
        riskType = RiskType.Medium;
    }
}

