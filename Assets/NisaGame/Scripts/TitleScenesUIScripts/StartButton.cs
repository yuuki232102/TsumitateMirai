using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneManager : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string modeSelectSceneName = "ModeSelectScene";

    // ボタンの OnClick にこれを登録
    public void OnClickStart()
    {
        SceneManager.LoadScene(modeSelectSceneName);
    }
}
