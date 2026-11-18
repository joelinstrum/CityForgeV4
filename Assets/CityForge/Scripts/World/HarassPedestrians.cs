using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal bool HarassCloseView => !pedestrianCamera || pedestrianCamera.ZoomLevel <= 2;
        internal int HarassCandidateChecks { get; private set; }
        internal Pedestrian ChooseHarassTarget(Vector2 point, ref uint random)
        {
            if (TimeOfDay != RegionTimeOfDay.Evening || pedestrians.Count == 0) return null;
            // Bounded random sampling rather than scanning all residents or sorting distances.
            for (int attempt = 0; attempt < 8; attempt++)
            {
                random = random * 1664525u + 1013904223u;
                var candidate = pedestrians[(int)(random % (uint)pedestrians.Count)];
                HarassCandidateChecks++;
                if (HarassTargetAvailable(candidate) &&
                    (point - new Vector2(candidate.Body.position.x, candidate.Body.position.z)).sqrMagnitude <= 24f * 24f)
                    return candidate;
            }
            return null;
        }

        internal static bool HarassTargetAvailable(Pedestrian target) => target != null && target.Body &&
            target.Body.gameObject.activeInHierarchy && !target.Vehicle && !target.EagleCarried && target.Route is { Length: > 1 };

        internal void ScarePedestrian(Pedestrian target, Vector2 source, float now)
        {
            if (TimeOfDay != RegionTimeOfDay.Evening || !HarassTargetAvailable(target)) return;
            var at = new Vector2(target.Body.position.x, target.Body.position.z);
            if ((at - source).sqrMagnitude > 3.5f * 3.5f) return;
            // Continue along the certified circuit, choosing the direction away from the harasser.
            if (Vector2.Dot(target.Route[target.Next] - at, at - source) < 0)
            {
                int previous = target.Next - target.Direction;
                if (target.Closed) previous = (previous + target.Route.Length) % target.Route.Length;
                if (previous >= 0 && previous < target.Route.Length)
                { target.Next = previous; target.Direction = -target.Direction; }
            }
            target.ScaredUntil = now + 3f;
        }

        internal float HarassGroundHeight(Vector2 point) => LotHeight(point) + .04f;
    }
}
