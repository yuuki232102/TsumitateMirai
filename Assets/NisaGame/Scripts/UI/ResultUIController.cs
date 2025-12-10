using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 結果画面（ResultScene）のUI制御担当。
/// ・ResultData に保存されたシミュレーション結果を表示する。
/// ・グラフ描画、損益計算、キャラコメント表示、ボタン3種の挙動を管理する。
/// </summary>
public class ResultUIController : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI finalAssetText;      // 最終資産
    [SerializeField] private TextMeshProUGUI principalText;       // 元本（つみたて合計）
    [SerializeField] private TextMeshProUGUI profitText;          // 損益（＋◯円 / −◯円）
    [SerializeField] private TextMeshProUGUI profitPercentText;   // 損益率（＋◯％ / −◯％）
    [SerializeField] private TextMeshProUGUI characterMessageText;// キャラコメント

    [Header("Graph")]
    [SerializeField] private GraphRenderer graphRenderer;         // 折れ線グラフ描画用

    [Header("Scene Names")]
    [SerializeField] private string simulationSceneName = "SimulationScene";
    [SerializeField] private string settingsSceneName = "SettingsScene";
    [SerializeField] private string titleSceneName = "TitleScene";

    private ResultData result;
    private GameSettings gameSettings;

    private void Awake()
    {
        result = ResultData.Instance;
        gameSettings = GameSettings.Instance;

        if (result == null)
        {
            Debug.LogError("ResultUIController: ResultData.Instance が見つかりません。ResultData をどこかのシーンに配置してください。");
        }

        if (gameSettings == null)
        {
            Debug.LogWarning("ResultUIController: GameSettings.Instance が見つかりません。年数や設定の取得に制限が出ます。");
        }
    }

    private void Start()
    {
        if (result == null)
        {
            return;
        }

        // 1. 元本・損益を計算
        int totalYears = (gameSettings != null) ? gameSettings.TotalYears : GuessYearsFromResult();
        int monthlyAmount = result.MonthlyAmount;

        // 元本：毎月額 × 12ヶ月 × 年数
        float principal = monthlyAmount * 12 * totalYears;

        // 最終資産
        float finalAsset = result.FinalAsset;

        // 損益と損益率
        float profit = finalAsset - principal;
        float profitPercent = (principal > 0f) ? (profit / principal * 100f) : 0f;

        // 2. テキスト更新
        UpdateTexts(finalAsset, principal, profit, profitPercent);

        // 3. グラフ更新
        if (graphRenderer != null && result.YearlyAssets != null)
        {
            graphRenderer.UpdateGraph(result.YearlyAssets);
        }
        else
        {
            Debug.LogWarning("ResultUIController: graphRenderer または YearlyAssets が設定されていません。");
        }

        // 4. キャラコメント
        if (characterMessageText != null)
        {
            characterMessageText.text = GetFinalComment(profit, profitPercent, totalYears);
        }
    }

    /// <summary>
    /// 各テキストUIを更新する。
    /// </summary>
    private void UpdateTexts(float finalAsset, float principal, float profit, float profitPercent)
    {
        // 最終資産
        if (finalAssetText != null)
        {
            finalAssetText.text = $"{finalAsset.ToString("N0")} 円";
        }

        // 元本
        if (principalText != null)
        {
            principalText.text = $"{principal.ToString("N0")} 円";
        }

        // 損益
        if (profitText != null)
        {
            string signFormat = profit >= 0f ? "+#,0" : "#,0";
            profitText.text = $"{profit.ToString(signFormat)} 円";

            // 色分け
            if (profit > 0.01f)
            {
                profitText.color = Color.green;
            }
            else if (profit < -0.01f)
            {
                profitText.color = Color.red;
            }
            else
            {
                profitText.color = Color.gray;
            }
        }

        // 損益率
        if (profitPercentText != null)
        {
            profitPercentText.text = $"{profitPercent:+0.0;-0.0;0.0} ％";

            if (profitPercent > 0.01f)
            {
                profitPercentText.color = Color.green;
            }
            else if (profitPercent < -0.01f)
            {
                profitPercentText.color = Color.red;
            }
            else
            {
                profitPercentText.color = Color.gray;
            }
        }
    }

    /// <summary>
    /// totalYears を ResultData から推定する（GameSettings がない場合の保険）。
    /// YearlyAssets が「0年目の点も含んでいる」可能性を考慮して -1 している。
    /// </summary>
    private int GuessYearsFromResult()
    {
        if (result == null || result.YearlyAssets == null)
            return 15; // 最悪の保険：仕様どおり15年

        int count = result.YearlyAssets.Count;
        if (count <= 1)
            return 15;

        // SimulationManager で「初期0点＋年末15点」を入れている場合は 16件になるので -1
        return Mathf.Max(1, count - 1);
    }

    /// <summary>
    /// 最終結果に応じたキャラコメントを返す。
    /// </summary>
    private string GetFinalComment(float profit, float profitPercent, int years)
    {
        // 利益率と年数を見てざっくりコメント分岐
        if (profitPercent > 50f)
        {
            return $"15年間、おつかれさま！\n大きくふやすことができたね。長くつみたてる力がよくわかる結果だよ。";
        }
        else if (profitPercent > 10f)
        {
            return $"15年間、おつかれさま！\nコツコツつみたてることで、しっかりと資産が育ったね。";
        }
        else if (profitPercent > 0.1f)
        {
            return $"15年間、おつかれさま！\nゆっくりだけど、ちゃんとふえているね。長期投資らしい結果だよ。";
        }
        else if (profitPercent > -5f)
        {
            return $"15年間、おつかれさま！\nほとんど横ばいだったけど、こういう結果になることもあるんだ。リスクや期間を変えて試してみよう。";
        }
        else
        {
            return $"15年間、おつかれさま！\n今回はマイナスになってしまったけれど、リスクの大きさや期間の大事さを学べたはずだよ。";
        }
    }

    // ====== ボタン用メソッド ======

    /// <summary>
    /// 「同じ条件でもう一度」ボタン。
    /// → GameSettingsはそのまま、SimulationScene を再ロード。
    /// </summary>
    public void OnClickRetrySame()
    {
        if (!string.IsNullOrEmpty(simulationSceneName))
        {
            SceneManager.LoadScene(simulationSceneName);
        }
        else
        {
            Debug.LogWarning("ResultUIController: simulationSceneName が設定されていません。");
        }
    }

    /// <summary>
    /// 「条件を変えてやりなおす」ボタン。
    /// → SettingsScene へ。
    /// </summary>
    public void OnClickRetryWithNewSetting()
    {
        if (!string.IsNullOrEmpty(settingsSceneName))
        {
            SceneManager.LoadScene(settingsSceneName);
        }
        else
        {
            Debug.LogWarning("ResultUIController: settingsSceneName が設定されていません。");
        }
    }

    /// <summary>
    /// 「タイトルにもどる」ボタン。
    /// </summary>
    public void OnClickBackToTitle()
    {
        if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
        }
        else
        {
            Debug.LogWarning("ResultUIController: titleSceneName が設定されていません。");
        }
    }
}
