using UnityEngine;

public class ScreenInitializer : MonoBehaviour
{
    void Start()
    {
        // 1920x1080 フルスクリーン
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);

        // デフォルトはフルスクリーンONとして保存
        if (!PlayerPrefs.HasKey("FullScreen"))
        {
            PlayerPrefs.SetInt("FullScreen", 1);
            PlayerPrefs.Save();
        }
    }
}
