using UnityEngine;
using UnityEngine.SceneManagement;  // シーン遷移に必要

public class HowToPlayButton : MonoBehaviour
{
    // 遷移先のシーン名をインスペクタから設定できるようにする
    [SerializeField] private string HowToPlaySceneName = "HowToPlayScene";

    // ボタンの OnClick で呼び出すメソッド
    public void OnClickHowToPlay()
    {
        SceneManager.LoadScene(HowToPlaySceneName);
    }
}
