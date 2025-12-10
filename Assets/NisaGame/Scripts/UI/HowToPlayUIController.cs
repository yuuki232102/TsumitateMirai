using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// あそびかた画面（HowToPlayScene）のUI制御担当。
/// ・STEP1 / STEP2 / STEP3 パネルの切り替え
/// ・「ゲームをはじめる ▶」で SimulationScene へ遷移
/// ・「タイトルにもどる」で TitleScene へ遷移
/// 
/// ボタンの OnClick に各メソッドを割り当てて使う。
/// </summary>
public class HowToPlayUIController : MonoBehaviour
{
    [Header("Step Panels")]
    [SerializeField] private GameObject step1Panel;
    [SerializeField] private GameObject step2Panel;
    [SerializeField] private GameObject step3Panel;

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string simulationSceneName = "SimulationScene";

    // 現在表示中のステップ（1〜3）
    private int currentStep = 1;

    private void Start()
    {
        // 最初はSTEP1を表示
        ShowStep(1);
    }

    /// <summary>
    /// 任意のステップ番号のパネル表示を切り替える。
    /// </summary>
    /// <param name="step">1〜3</param>
    private void ShowStep(int step)
    {
        currentStep = Mathf.Clamp(step, 1, 3);

        if (step1Panel != null) step1Panel.SetActive(currentStep == 1);
        if (step2Panel != null) step2Panel.SetActive(currentStep == 2);
        if (step3Panel != null) step3Panel.SetActive(currentStep == 3);
    }

    // ====== ボタン用メソッド ======

    /// <summary>
    /// STEP1 → STEP2、STEP2 → STEP3 へ進むボタンから呼ぶ。
    /// </summary>
    public void OnClickNextStep()
    {
        int next = currentStep + 1;
        if (next > 3) next = 3;
        ShowStep(next);
    }

    /// <summary>
    /// STEP3 → STEP2、STEP2 → STEP1 へ戻るボタンから呼ぶ。
    /// </summary>
    public void OnClickPrevStep()
    {
        int prev = currentStep - 1;
        if (prev < 1) prev = 1;
        ShowStep(prev);
    }

    /// <summary>
    /// 「ゲームをはじめる ▶」ボタンから呼ぶ。
    /// SimulationScene へ遷移。
    /// </summary>
    public void OnClickStartGame()
    {
        if (!string.IsNullOrEmpty(simulationSceneName))
        {
            SceneManager.LoadScene(simulationSceneName);
        }
        else
        {
            Debug.LogWarning("HowToPlayUIController: simulationSceneName が設定されていません。");
        }
    }

    /// <summary>
    /// 「タイトルにもどる」ボタンから呼ぶ。
    /// </summary>
    public void OnClickBackToTitle()
    {
        if (!string.IsNullOrEmpty(titleSceneName))
        {
            SceneManager.LoadScene(titleSceneName);
        }
        else
        {
            Debug.LogWarning("HowToPlayUIController: titleSceneName が設定されていません。");
        }
    }
}
