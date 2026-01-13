using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneRouter : MonoBehaviour
{
    public static string nextSceneName;

    public static void LoadScene(string sceneName)
    {
        Debug.Log("[Router] LoadScene called. target = " + sceneName);
        nextSceneName = sceneName;

        Debug.Log("[Router] Now load LoadingScene");
        SceneManager.LoadScene("LoadingScene");
    }
}
