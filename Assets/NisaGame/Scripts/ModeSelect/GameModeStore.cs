using UnityEngine;

/// <summary>
/// モード選択結果をシーン間で保持する（DontDestroyOnLoad）
/// </summary>
public class GameModeStore : MonoBehaviour
{
    public static GameModeStore Instance { get; private set; }

    public GameMode SelectedMode { get; private set; } = GameMode.Normal;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetMode(GameMode mode)
    {
        SelectedMode = mode;
    }

    /// <summary>
    /// いなければ生成（Title/ModeSelect/Simulationどこから呼んでもOK）
    /// </summary>
    public static GameModeStore Ensure()
    {
        if (Instance != null) return Instance;

        var found = FindObjectOfType<GameModeStore>();
        if (found != null) return found;

        var go = new GameObject("GameModeStore");
        return go.AddComponent<GameModeStore>();
    }
}
