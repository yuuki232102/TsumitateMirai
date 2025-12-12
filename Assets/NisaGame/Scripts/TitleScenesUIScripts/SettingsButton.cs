using UnityEngine;
using UnityEngine.SceneManagement;  // シーン遷移に必要

public class SettingsButton : MonoBehaviour
{
    // 遷移先のシーン名をインスペクタから設定できるようにする
    [SerializeField] private string SettingsSceneName = "SettingsScene";

    // ボタンの OnClick で呼び出すメソッド
    public void OnClickSettings()
    {
        SceneManager.LoadScene(SettingsSceneName);
    }
}
