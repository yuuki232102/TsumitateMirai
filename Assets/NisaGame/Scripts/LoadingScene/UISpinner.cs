using UnityEngine;

public class UISpinner : MonoBehaviour
{
    [SerializeField] private float speed = 180f; // 1秒あたりの回転角度（度）

    void Update()
    {
        // Z軸回転（UIはこれでOK）
        transform.Rotate(0f, 0f, -speed * Time.deltaTime);
    }
}
