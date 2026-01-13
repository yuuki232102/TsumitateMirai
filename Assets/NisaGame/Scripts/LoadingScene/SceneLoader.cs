using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン遷移を一元管理するクラス。
/// ・任意のシーン名でロード
/// ・（あれば）フェード用CanvasGroupを使ってフェードイン / フェードアウト
/// ・全シーン共通で使えるようにシングルトン＋DontDestroyOnLoad
/// 
/// 他のスクリプトからは：
/// SceneLoader.Instance.LoadScene("SimulationScene");
/// のように呼ぶ。
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Fade Settings (Optional)")]
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 画面全体を覆う黒（or白）のCanvasGroup
    [SerializeField] private float fadeDuration = 0.5f;   // フェード時間（秒）

    private bool isLoading = false;

    private void Awake()
    {
        // シングルトン確立
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // フェード用CanvasGroupの初期状態を整える
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;       // 最初は明るい状態
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// シーン名を指定してシーン遷移を行う（公開API）。
    /// フェードが設定されていればフェード付き、なければ即時ロード。
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("SceneLoader: すでにシーンロード中です。二重呼び出しを防止しました。");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneLoader: 無効なシーン名です。");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// シーン遷移のコルーチン本体。
    /// </summary>
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        isLoading = true;

        // 1. フェードアウト
        if (fadeCanvasGroup != null && fadeDuration > 0f)
        {
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
        }

        // 2. シーンを非同期ロード
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null)
        {
            Debug.LogError($"SceneLoader: シーン '{sceneName}' のロードに失敗しました。シーン名を確認してください。");
            isLoading = false;
            yield break;
        }

        // ロード完了まで待機
        while (!op.isDone)
        {
            yield return null;
        }

        // 3. フェードイン
        if (fadeCanvasGroup != null && fadeDuration > 0f)
        {
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
        }

        isLoading = false;
    }

    /// <summary>
    /// alpha を from → to に時間をかけて変化させる簡易フェード。
    /// </summary>
    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null || duration <= 0f)
        {
            yield break;
        }

        fadeCanvasGroup.blocksRaycasts = true; // フェード中は入力をブロック

        float time = 0f;
        fadeCanvasGroup.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = (to > 0.99f); // ほぼ真っ暗のときだけブロック
    }

    // ====== 便利メソッド（任意で使う用） ======

    [Header("Optional Scene Name Presets")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string simulationSceneName = "SimulationScene";
    [SerializeField] private string settingsSceneName = "SettingsScene";
    [SerializeField] private string howToPlaySceneName = "HowToPlayScene";
    [SerializeField] private string resultSceneName = "ResultScene";

    public void LoadTitleScene()
    {
        LoadScene(titleSceneName);
    }

    public void LoadSimulationScene()
    {
        LoadScene(simulationSceneName);
    }

    public void LoadSettingsScene()
    {
        LoadScene(settingsSceneName);
    }

    public void LoadHowToPlayScene()
    {
        LoadScene(howToPlaySceneName);
    }

    public void LoadResultScene()
    {
        LoadScene(resultSceneName);
    }
}
