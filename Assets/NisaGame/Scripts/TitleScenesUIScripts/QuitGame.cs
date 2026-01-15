using UnityEngine;

public class QuitGame : MonoBehaviour
{
    // ボタンから呼ぶ
    public void Quit()
    {
        // ビルドしたアプリを終了
        Application.Quit();

#if UNITY_EDITOR
        // Unityエディタ上では再生停止
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
