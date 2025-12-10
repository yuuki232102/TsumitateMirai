using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// タイトル画面（TitleScene）のUI制御担当。
/// ・「はじめる」→ シミュレーション開始（SimulationScene）
/// ・「あそびかた」→ HowToPlayScene
/// ・「せってい」→ SettingsScene
/// 
/// ボタンの OnClick から各メソッドを呼び出す想定。
/// </summary>
public class TitleUIController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string simulationSceneName = "SimulationScene";
    [SerializeField] private string howToPlaySceneName = "HowToPlayScene";
    [SerializeField] private string settingsSceneName = "SettingsScene";

    private void Awake()
    {
        // GameSettings / ResultData がシーンに存在するか軽くチェック
        // （存在しなくても致命的ではないので警告のみ）
        if (GameSettings.Instance == null)
        {
            Debug.LogWarning("TitleUIController: GameSettings.Instance が見つかりません。TitleScene かどこかに GameSettings を配置することをおすすめします。");
        }

        if (ResultData.Instance == null)
        {
            Debug.LogWarning("TitleUIController: ResultData.Instance が見つかりません。TitleScene かどこかに ResultData を配置することをおすすめします。");
        }
    }

    /// <summary>
    /// 「はじめる」ボタンから呼ぶ。
    /// シミュレーションを新しく開始するため、前回の結果があればクリアしてから SimulationScene へ。
    /// </summary>
    public void OnClickStartGame()
    {
        // 前回の結果が残っている場合はクリアしておく（バグ・混入防止）
        if (ResultData.Instance != null)
        {
            ResultData.Instance.Clear();
        }

        if (!string.IsNullOrEmpty(simulationSceneName))
        {
            SceneManager.LoadScene(simulationSceneName);
        }
        else
        {
            Debug.LogWarning("TitleUIController: simulationSceneName が設定されていません。");
        }
    }

    /// <summary>
    /// 「あそびかた」ボタンから呼ぶ。
    /// HowToPlayScene へ遷移。
    /// </summary>
    public void OnClickHowToPlay()
    {
        if (!string.IsNullOrEmpty(howToPlaySceneName))
        {
            SceneManager.LoadScene(howToPlaySceneName);
        }
        else
        {
            Debug.LogWarning("TitleUIController: howToPlaySceneName が設定されていません。");
        }
    }

    /// <summary>
    /// 「せってい」ボタンから呼ぶ。
    /// SettingsScene へ遷移。
    /// </summary>
    public void OnClickSettings()
    {
        if (!string.IsNullOrEmpty(settingsSceneName))
        {
            SceneManager.LoadScene(settingsSceneName);
        }
        else
        {
            Debug.LogWarning("TitleUIController: settingsSceneName が設定されていません。");
        }
    }

    /// <summary>
    /// （任意）アプリ終了ボタン用。
    /// PCビルドでのみ有効。エディタでは再生停止。
    /// </summary>
    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
