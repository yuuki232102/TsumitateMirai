using UnityEngine;
using TMPro;

public class MHLoadingTextAnim : MonoBehaviour
{
    [SerializeField] private TMP_Text tmp;
    [SerializeField] private float dotsSpeed = 0.35f; // 点が変わる間隔
    [SerializeField] private int maxDots = 3;

    private string baseText;
    private float timer;
    private int dotCount;

    private void Awake()
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
        baseText = tmp.text;
    }

    private void OnEnable()
    {
        timer = 0f;
        dotCount = 0;
        tmp.text = baseText;
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer >= dotsSpeed)
        {
            timer = 0f;
            dotCount = (dotCount + 1) % (maxDots + 1);
            tmp.text = baseText + new string('.', dotCount);
        }
    }
}
