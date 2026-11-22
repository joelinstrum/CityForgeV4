using System;
using System.Linq;
using UnityEngine;
using CityForge.World;

namespace CityForge.Lots
{
    // A time budget, rather than a random 50/50 choice, guarantees at least half idle.
    public sealed class ChildrenAtPlay : MonoBehaviour
    {
        private Animator animator;
        private PrototypeRegionView region;
        private (string state, float duration)[] actions;
        private System.Random random;
        private float remaining, lastAction;
        private bool idle = true;
        private GameObject[] visuals;
        private bool[] visualActive;
        public bool ScheduledVisible { get; private set; } = true;
        public bool Manual { get; set; }
        public double IdleSeconds { get; private set; }
        public double ActiveSeconds { get; private set; }
        public string CurrentState { get; private set; } = "Idle";
        public static bool IsChild(string id) => id != null && id.StartsWith("v3-child-", StringComparison.Ordinal);
        public static string StateName(string clip) => string.Concat(clip.Split('|')[^1].Split('_', ' ')
            .Where(word => word.Length > 0).Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));

        internal static ChildrenAtPlay Attach(GameObject root)
        {
            var play = root.GetComponent<ChildrenAtPlay>() ?? root.AddComponent<ChildrenAtPlay>();
            play.Initialize();
            if (play.region) play.SetClockHour(play.region.CurrentClockHour);
            return play;
        }

        private void Initialize()
        {
            if (random != null) return;
            random = new System.Random(GetInstanceID());
            region = GetComponentInParent<PrototypeRegionView>();
            visuals = transform.Cast<Transform>().Select(child => child.gameObject).ToArray();
            visualActive = visuals.Select(child => child.activeSelf).ToArray();
            animator = GetComponentInChildren<Animator>();
            if (animator && animator.runtimeAnimatorController)
            {
                animator.applyRootMotion = false;
                actions = animator.runtimeAnimatorController.animationClips
                    .Where(clip => !clip.name.StartsWith("__") && StateName(clip.name) != "Idle")
                    .Select(clip => (state: StateName(clip.name), duration: Mathf.Clamp(clip.length, 1f, 8f)))
                    .Where(action => animator.HasState(0, Animator.StringToHash("Base Layer." + action.state)))
                    .GroupBy(action => action.state).Select(group => group.First()).ToArray();
            }
            actions ??= Array.Empty<(string, float)>();
            remaining = Range(6f, 10f);
            Animate("Idle");
        }

        public void SetClockHour(float hour)
        {
            Initialize();
            bool visible = CharacterScriptHours.For(LotScriptedBehavior.ChildrenAtPlay).Contains(hour);
            if (ScheduledVisible == visible) return;
            ScheduledVisible = visible;
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i]) visuals[i].SetActive(visible && visualActive[i]);
        }

        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        private void Update()
        {
            if (!Manual && region) SetClockHour(region.CurrentClockHour);
            if (Manual || !ScheduledVisible || (region && (region.DistrictLoading || region.IsSaving))) return;
            Tick(Mathf.Min(Time.deltaTime, .1f));
        }

        public void Tick(float seconds)
        {
            Initialize();
            if (!ScheduledVisible || !float.IsFinite(seconds) || seconds <= 0f) return;
            while (seconds > .000001f)
            {
                float step = Mathf.Min(remaining, seconds);
                if (idle) IdleSeconds += step; else ActiveSeconds += step;
                remaining -= step; seconds -= step;
                if (remaining > .000001f) continue;
                if (idle && actions.Length > 0 && IdleSeconds - ActiveSeconds >= 1f)
                {
                    var action = actions[random.Next(actions.Length)];
                    lastAction = Mathf.Min(action.duration, (float)(IdleSeconds - ActiveSeconds) * .95f);
                    remaining = lastAction;
                    idle = false;
                    Animate(action.state);
                }
                else
                {
                    idle = true;
                    // Include blend time in the idle budget, even for short jumping clips.
                    remaining = Range(lastAction + .35f, lastAction + 4f);
                    Animate("Idle");
                }
            }
        }

        private void Animate(string state)
        {
            CurrentState = state;
            if (!animator || !animator.runtimeAnimatorController || animator.layerCount == 0) return;
            int hash = Animator.StringToHash("Base Layer." + state);
            if (animator.HasState(0, hash)) animator.CrossFadeInFixedTime(hash, .15f, 0);
        }
    }
}
