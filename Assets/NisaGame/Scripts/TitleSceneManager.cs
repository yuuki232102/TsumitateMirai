using UnityEngine;
using UnityEngine.SceneManagement;  // ←これを忘れない！

public class TitleSceneManager : MonoBehaviour
{
    // インスペクターで設定できるようにしておく
    [SerializeField] private string simulationSceneName = "SimulationScene";
    // ↑ 実際のシーン名が "Simulation" なら、その名前を入れてね

    // Button_Start から呼び出す関数
    public void OnClickStart()
    {
        // シミュレーション（STEP1/STEP2の画面）へ遷移
        SceneManager.LoadScene(simulationSceneName);
    }
}
