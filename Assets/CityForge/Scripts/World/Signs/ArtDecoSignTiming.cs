using System;

namespace CityForge.AssetPreparation
{
    public static class ArtDecoSignTiming
    {
        public static float KeyedIntensity(float elapsedSeconds, float cycleSeconds,
            float[] keyTimes, float[] keyValues)
        {
            if (keyTimes == null || keyValues == null || keyTimes.Length == 0 ||
                keyTimes.Length != keyValues.Length || cycleSeconds <= 0f) return 1f;
            float phase = elapsedSeconds % cycleSeconds;
            if (phase < 0f) phase += cycleSeconds;
            float value = keyValues[0];
            for (int i = 1; i < keyTimes.Length && phase >= keyTimes[i]; i++) value = keyValues[i];
            return Math.Max(0f, value);
        }

        public static bool IsBlinkOn(float elapsedSeconds, float onSeconds = 1f, float offSeconds = 1f)
        {
            float on = Math.Max(.01f, onSeconds);
            float period = on + Math.Max(.01f, offSeconds);
            float phase = elapsedSeconds % period;
            if (phase < 0f) phase += period;
            return phase < on;
        }

        public static int LitLetterCount(float elapsedSeconds, int letterCount,
            float stepSeconds, float fullHoldSeconds, float darkResetSeconds,
            bool animateLetters = true)
        {
            if (letterCount <= 0) return 0;
            if (!animateLetters) return letterCount;
            float step = Math.Max(.01f, stepSeconds);
            float fullEnd = (letterCount - 1) * step + Math.Max(0f, fullHoldSeconds);
            float period = fullEnd + Math.Max(.01f, darkResetSeconds);
            float time = elapsedSeconds % period;
            if (time < 0f) time += period;
            if (time >= fullEnd) return 0;
            return Math.Min(letterCount, 1 + (int)Math.Floor(time / step));
        }
    }
}
