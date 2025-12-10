using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シミュレーション結果をシーン間で共有するためのデータ保持クラス。
/// ・ResultScene で表示する内容をここにまとめておく。
/// ・SimulationScene から値をセットし、ResultScene で読むだけにする設計。
/// 
/// DontDestroyOnLoad でシーンを跨いで保持される。
/// </summary>
public class ResultData : MonoBehaviour
{
    // ====== シングルトン ======

    public static ResultData Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ====== フィールド（Result用データ） ======

    [Header("Result Values (Read Only from other scripts)")]

    [SerializeField] private float finalAsset = 0f;          // 最終資産
    [SerializeField] private float principal = 0f;           // 元本（つみたて総額）
    [SerializeField] private List<float> yearlyAssets = new List<float>(); // 年末資産のリスト（1〜15年）
    [SerializeField] private int monthlyAmount = 0;          // 毎月のつみたて額
    [SerializeField] private RiskType riskType = RiskType.Medium; // 使用したリスクタイプ

    // ====== 公開プロパティ（外部からは読み取り専用） ======

    public float FinalAsset => finalAsset;
    public float Principal => principal;
    public IReadOnlyList<float> YearlyAssets => yearlyAssets;
    public int MonthlyAmount => monthlyAmount;
    public RiskType RiskType => riskType;

    // ====== 結果のセット用メソッド ======

    /// <summary>
    /// シミュレーション終了時に呼び出して、結果を保存する。
    /// SimulationManager と GameSettings から値を受け取る想定。
    /// </summary>
    public void SetResult(SimulationManager simulationManager, GameSettings gameSettings)
    {
        if (simulationManager == null)
        {
            Debug.LogError("ResultData.SetResult: simulationManager が null です。");
            return;
        }

        if (gameSettings == null)
        {
            Debug.LogError("ResultData.SetResult: gameSettings が null です。");
            return;
        }

        // 毎月額・リスクタイプは GameSettings から
        monthlyAmount = gameSettings.MonthlyAmount;
        riskType = gameSettings.RiskType;

        // 最終資産は SimulationManager から
        finalAsset = simulationManager.CurrentAsset;

        // 元本は「毎月額 × 12ヶ月 × 年数」
        principal = monthlyAmount * 12 * gameSettings.TotalYears;
        // ↑ 一応 MonthsPerYear が 12前提だが、将来変わっても整合性を保ちやすいように記述

        // 年末資産リストをコピー（ディープコピー）
        yearlyAssets.Clear();
        var src = simulationManager.YearlyAssets;
        if (src != null)
        {
            yearlyAssets.AddRange(src);
        }

        // デバッグ用ログ（不要なら消してOK）
        Debug.Log($"ResultData: SetResult 完了。finalAsset={finalAsset}, principal={principal}, years={yearlyAssets.Count}");
    }

    /// <summary>
    /// 結果をクリアしたい場合用。再シミュレーション前などに任意で使用。
    /// （必須ではない）
    /// </summary>
    public void Clear()
    {
        finalAsset = 0f;
        principal = 0f;
        yearlyAssets.Clear();
        monthlyAmount = 0;
        riskType = RiskType.Medium;
    }
}
