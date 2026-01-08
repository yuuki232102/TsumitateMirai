using UnityEngine;
using UnityEngine.SceneManagement;

public class NisaIntroSceneManager : MonoBehaviour
{
    // 既存コードは省略

    // 🔽 タイトル画面へ戻る
    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
