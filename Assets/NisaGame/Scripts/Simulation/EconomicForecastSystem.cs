using UnityEngine;

/// <summary>
/// 次年イベント(真実)から「予測値」と「信頼度」を作るロジック専用（UI非依存）
/// </summary>
public static class EconomicForecastSystem
{
    public struct ForecastResult
    {
        /// <summary>-1..+1（-1:不景気寄り、+1:好景気寄り）</summary>
        public float tendency;

        /// <summary>0..1</summary>
        public float confidence01;

        /// <summary>ショック警戒</summary>
        public bool shockWarning;

        /// <summary>表示ラベル</summary>
        public string label;
    }

    /// <summary>
    /// 次年スケジュールから傾向を計算し、信頼度に応じてノイズを混ぜて「予測」にする。
    /// </summary>
    public static ForecastResult MakeForecast(
        EconomicEventType[] nextYearSchedule,
        float confidence01,
        float forecastAccuracyWeight,
        float tendencyNoiseAtLowConfidence = 0.6f,
        float shockWarnThreshold = 0.18f
    )
    {
        confidence01 = Mathf.Clamp01(confidence01);
        forecastAccuracyWeight = Mathf.Clamp01(forecastAccuracyWeight);

        // 1) 真の傾向（好:+1 / 不:-1 / ショック:-2）を月イベントから作る
        float trueTendency = 0f;
        int effectiveCount = 0;
        int shockCount = 0;

        if (nextYearSchedule != null && nextYearSchedule.Length > 0)
        {
            for (int i = 0; i < nextYearSchedule.Length; i++)
            {
                var ev = nextYearSchedule[i];
                if (ev == EconomicEventType.None) continue;

                effectiveCount++;

                if (ev == EconomicEventType.Boom) trueTendency += 1f;
                else if (ev == EconomicEventType.Recession) trueTendency -= 1f;
                else if (ev == EconomicEventType.Shock)
                {
                    trueTendency -= 2f;
                    shockCount++;
                }
            }
        }

        if (effectiveCount > 0) trueTendency /= effectiveCount;
        trueTendency = Mathf.Clamp(trueTendency, -1f, 1f);

        // 2) ショック警戒
        float shockRatio = (nextYearSchedule == null || nextYearSchedule.Length == 0)
            ? 0f
            : (float)shockCount / nextYearSchedule.Length;

        bool shockWarn = shockRatio >= shockWarnThreshold;

        // 3) 予測ノイズ：信頼度が低いほど大きい
        float noiseAmp = Mathf.Lerp(tendencyNoiseAtLowConfidence, 0f, confidence01);
        float noise = Random.Range(-noiseAmp, noiseAmp);

        // 4) 「真実寄り」への寄せ方（65%採用などをweightで調整）
        float truthWeight = Mathf.Lerp(0.25f, 1f, confidence01) * forecastAccuracyWeight;

        float predicted = Mathf.Lerp(noise, trueTendency, truthWeight);
        predicted = Mathf.Clamp(predicted, -1f, 1f);

        // 5) 表示ラベル
        string label = "平常";
        if (predicted >= 0.35f) label = "好景気寄り";
        else if (predicted <= -0.35f) label = "不景気寄り";

        if (shockWarn) label += "（ショック警戒）";

        return new ForecastResult
        {
            tendency = predicted,
            confidence01 = confidence01,
            shockWarning = shockWarn,
            label = label
        };
    }
}
