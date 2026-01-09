using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroButton : MonoBehaviour
{
    // ▶ NISA説明画面へ
    public void OnClickIntro()
    {
        SceneManager.LoadScene("NisaIntroScene");
    }
}
