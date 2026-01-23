using UnityEngine;
using UnityEngine.SceneManagement;

public class HowToPlaySceneManager : MonoBehaviour
{
    // ==============================
    // Scene Name 定義
    // ==============================
    private const string TITLE_SCENE = "TitleScene";
    private const string SIMULATION_SCENE = "SimulationScene";

    // ==============================
    // STEP 定義（Step5まで）
    // ==============================
    private enum HowToStep
    {
        Step1 = 1,
        Step2 = 2,
        Step3 = 3,
        Step4 = 4,
        Step5 = 5
    }

    // ==============================
    // UI References
    // ==============================
    [Header("Step Panels")]
    [SerializeField] private GameObject step1Panel;
    [SerializeField] private GameObject step2Panel;
    [SerializeField] private GameObject step3Panel;
    [SerializeField] private GameObject step4Panel;
    [SerializeField] private GameObject step5Panel;

    private HowToStep currentStep;

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
        step4Panel.SetActive(step == HowToStep.Step4);
        step5Panel.SetActive(step == HowToStep.Step5);
    }

    // ==============================
    // Button Events（STEP切替）
    // ==============================
    public void OnClickNextStep()
    {
        if (currentStep < HowToStep.Step5)
        {
            ShowStep(currentStep + 1);
        }
        else
        {
            // Step5で「次へ」を押したらゲーム開始にするならこれ
            OnClickStartGame();
        }
    }

    public void OnClickBackStep()
    {
        if (currentStep > HowToStep.Step1)
        {
            ShowStep(currentStep - 1);
        }
    }

    // ==============================
    // Scene 遷移
    // ==============================
    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(TITLE_SCENE);
    }

    public void OnClickStartGame()
    {
        SceneManager.LoadScene(SIMULATION_SCENE);
    }
}
