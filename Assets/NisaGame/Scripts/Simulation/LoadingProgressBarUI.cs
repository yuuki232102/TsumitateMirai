using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingProgressBarUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;

    [Header("Fill Tuning")]
    [Tooltip("0→1に到達するまでの秒数（演出用）")]
    [SerializeField] private float durationSeconds = 1.5f;

    [Tooltip("最後の伸びをゆっくりにして“それっぽく”する")]
    [SerializeField] private bool useSmoothStep = true;

    [Tooltip("100%到達後、少しだけ止める（完了感）")]
    [SerializeField] private float holdAtFullSeconds = 0.1f;

    private Coroutine running;

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        ResetToZero();
    }

    private void OnDisable()
    {
        // 非表示時に止めておく
        if (running != null)
        {
            StopCoroutine(running);
            running = null;
        }
        ResetToZero();
    }

    public void ResetToZero()
    {
        if (slider != null) slider.value = 0f;
    }

    /// <summary>
    /// 指定秒数で 0→1 まで進める（演出用）
    /// </summary>
    public void Play(float seconds)
    {
        durationSeconds = Mathf.Max(0.01f, seconds);

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(CoFill());
    }

    private IEnumerator CoFill()
    {
        ResetToZero();

        float t = 0f;
        while (t < durationSeconds)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / durationSeconds);
            if (useSmoothStep) p = Mathf.SmoothStep(0f, 1f, p);

            if (slider != null) slider.value = p;
            yield return null;
        }

        if (slider != null) slider.value = 1f;

        if (holdAtFullSeconds > 0f)
            yield return new WaitForSecondsRealtime(holdAtFullSeconds);

        running = null;
    }
}
