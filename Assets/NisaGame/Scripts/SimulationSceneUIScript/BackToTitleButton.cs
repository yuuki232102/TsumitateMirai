using UnityEngine;
using UnityEngine.SceneManagement;  // シーン遷移に必要

public class BackToTitleButton : MonoBehaviour
{
    // 戻り先のタイトルシーン名（インスペクタから変えられるようにしておく）
    [SerializeField] private string titleSceneName = "TitleScene";

    // ボタンの OnClick で呼び出すメソッド
    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
