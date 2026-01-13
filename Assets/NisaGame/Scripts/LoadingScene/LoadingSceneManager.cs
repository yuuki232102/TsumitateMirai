using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingSceneManager : MonoBehaviour
{
    [SerializeField] private Slider progressSlider;

    [Header("Optional")]
    [SerializeField] private float minDisplayTime = 1.2f; // ロード画面を最低◯秒は表示

    private void Start()
    {
        Debug.Log("[LoadingScene] LoadingSceneManager Start");
        StartCoroutine(LoadAsync());
    }

    private IEnumerator LoadAsync()
    {
        float startTime = Time.time;

        var target = LoadingSceneRouter.nextSceneName;
        Debug.Log("[LoadingScene] Target = " + target);

        if (string.IsNullOrEmpty(target))
        {
            // 保険：何も指定されてなければタイトルに戻す
            target = "TitleScene";
            Debug.Log("[LoadingScene] Target is empty. Fallback to TitleScene");
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(target);
        if (op == null)
        {
            Debug.LogError("[LoadingScene] LoadSceneAsync failed. target = " + target);
            yield break;
        }

        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            // progressは0〜0.9で止まるので補正
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (progressSlider != null)
                progressSlider.value = progress;

            // 90%到達＝ロード完了直前
            if (op.progress >= 0.9f)
            {
                // 最低表示時間を満たすまで待つ（速すぎて見えない対策）
                float elapsed = Time.time - startTime;
                float remain = minDisplayTime - elapsed;
                if (remain > 0f)
                    yield return new WaitForSeconds(remain);

                Debug.Log("[LoadingScene] Activate scene now: " + target);
                op.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
