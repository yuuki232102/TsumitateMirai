using UnityEngine;
using TMPro;

public class MHLoadingTextCharBounce : MonoBehaviour
{
    [SerializeField] private TMP_Text tmp;

    [Header("Bounce")]
    [SerializeField] private float bounceHeight = 12f;   // 跳ねる高さ
    [SerializeField] private float bounceDuration = 0.25f;
    [SerializeField] private float interval = 0.08f;     // 次の文字までの間隔

    private TMP_TextInfo textInfo;
    private float timer;

    private void Awake()
    {
        if (tmp == null) tmp = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        timer = 0f;
        tmp.ForceMeshUpdate();
    }

    private void Update()
    {
        tmp.ForceMeshUpdate();
        textInfo = tmp.textInfo;

        int charCount = textInfo.characterCount;
        if (charCount == 0) return;

        timer += Time.unscaledDeltaTime;

        // どの文字が今跳ねるか
        float cycle = bounceDuration + interval;
        int activeIndex = Mathf.FloorToInt(timer / interval) % charCount;

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;

            float offset = 0f;

            // 今の文字だけ跳ねる
            if (i == activeIndex)
            {
                float t = (timer % cycle) / bounceDuration;
                if (t <= 1f)
                {
                    // easeOut → easeIn
                    float eased =
                        t < 0.5f
                        ? 2f * t * t
                        : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

                    offset = eased * bounceHeight;
                }
            }

            Vector3 up = new Vector3(0, offset, 0);
            vertices[vertIndex + 0] += up;
            vertices[vertIndex + 1] += up;
            vertices[vertIndex + 2] += up;
            vertices[vertIndex + 3] += up;
        }

        // メッシュ反映
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
