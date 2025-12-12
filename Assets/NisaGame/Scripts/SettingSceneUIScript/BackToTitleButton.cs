using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsBackToTitle : MonoBehaviour
{
    [SerializeField] private string titleSceneName = "TitleScene";

    public void OnClickBackToTitle()
    {
        SceneManager.LoadScene(titleSceneName);
    }
}
