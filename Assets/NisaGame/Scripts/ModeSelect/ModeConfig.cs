using UnityEngine;

[CreateAssetMenu(menuName = "Game/ModeConfig")]
public class ModeConfig : ScriptableObject
{
    [Header("Event Monthly Deltas")]
    public float boomMonthlyDelta = 0.01f;
    public float recessionMonthlyDelta = -0.01f;
    public float shockMonthlyDelta = -0.05f;

    [Header("Event Occurrence")]
    [Range(0f, 1f)] public float chanceAnyEvent = 1f;       // その年にイベントが起きる確率（0イベントを許す場合に使う）
    [Range(0f, 1f)] public float chanceSecondEvent = 0.2f;  // 2つ目のイベントが追加で起きる確率

    [Header("Event Type Weights (relative)")]
    [Min(0f)] public float boomWeight = 0.50f;
    [Min(0f)] public float recessionWeight = 0.38f;
    [Min(0f)] public float shockWeight = 0.12f;
}
