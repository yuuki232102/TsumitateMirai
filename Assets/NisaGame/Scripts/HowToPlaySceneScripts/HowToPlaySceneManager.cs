using UnityEngine;
using UnityEngine.SceneManagement;

public class HowToPlaySceneManager : MonoBehaviour
{
    // ==============================
    // Scene Name 定義（整合性担保）
    // ==============================
    private const string TITLE_SCENE = "TitleScene";
    private const string SIMULATION_SCENE = "SimulationScene";

    // ==============================
    // STEP 定義（安全な状態管理）
    // ==============================
    private enum HowToStep
    {
        Step1 = 1,
        Step2 = 2,
        Step3 = 3
    }

    // ==============================
    // UI References
    // ==============================
    [Header("Step Panels")]
    [SerializeField] private GameObject step1Panel;
    [SerializeField] private GameObject step2Panel;
    [SerializeField] private GameObject step3Panel;

    private HowToStep currentStep;

    // ==============================
    // LifeCycle
    // ==============================
    private void Start()
    {
        ShowStep(HowToStep.Step1);
    }

    // ==============================
    // STEP 表示制御
    // ==============================
    private void ShowStep(HowToStep step)
    {
        currentStep = step;

        step1Panel.SetActive(step == HowToStep.Step1);
        step2Panel.SetActive(step == HowToStep.Step2);
        step3Panel.SetActive(step == HowToStep.Step3);
    }

    // ==============================
    // Button Events（STEP切替）
    // ==============================
    public void OnClickNextStep()
    {
        if (currentStep == HowToStep.Step1)
        {
            ShowStep(HowToStep.Step2);
        }
        else if (currentStep == HowToStep.Step2)
        {
            ShowStep(HowToStep.Step3);
        }
    }

    public void OnClickBackStep()
    {
        if (currentStep == HowToStep.Step3)
        {
            ShowStep(HowToStep.Step2);
        }
        else if (currentStep == HowToStep.Step2)
        {
            ShowStep(HowToStep.Step1);
        }
    }

    // ==============================
    // Scene 遷移
    // ==============================
    /// <summary>
    /// タイトル画面へ戻る
    /// </summary>
    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(TITLE_SCENE);
    }

    /// <summary>
    /// ゲーム開始（SimulationScene）
    /// </summary>
    public void OnClickStartGame()
    {
        SceneManager.LoadScene(SIMULATION_SCENE);
    }
}
