using UnityEngine;

namespace CityForge.Lots
{
    // Central defaults for script schedules; equal endpoints mean all day.
    public readonly struct CharacterScriptHours
    {
        public float StartHour { get; }
        public float EndHour { get; }
        public CharacterScriptHours(float startHour, float endHour)
        { StartHour = Mathf.Repeat(startHour, 24f); EndHour = Mathf.Repeat(endHour, 24f); }
        public bool Contains(float hour)
        {
            if (!float.IsFinite(hour)) return false;
            hour = Mathf.Repeat(hour, 24f);
            if (StartHour == EndHour) return true;
            return StartHour < EndHour ? hour >= StartHour && hour < EndHour
                : hour >= StartHour || hour < EndHour;
        }
        public static CharacterScriptHours For(LotScriptedBehavior behavior) =>
            behavior == LotScriptedBehavior.ChildrenAtPlay ? new CharacterScriptHours(9f, 15f) : default;
    }
}
