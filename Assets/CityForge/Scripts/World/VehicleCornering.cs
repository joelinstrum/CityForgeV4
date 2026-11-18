using UnityEngine;

namespace CityForge.World
{
    internal static class VehicleCornering
    {
        internal static float Speed(Vector2[] route, Vector2 position, int next, int direction, bool closed,
            float cruise, float current, float delta)
        {
            if (route == null || route.Length < 2) return cruise;
            float distance = Vector2.Distance(position, route[next]);
            var incoming = (route[next] - position).normalized;
            if (incoming.sqrMagnitude < .001f) incoming = (route[next] - route[Index(next - direction, route.Length)]).normalized;
            float turn = 0, firstTurn = float.PositiveInfinity;
            int at = next;
            for (int i = 0; i < route.Length && distance < 7f; i++)
            {
                int following = at + direction;
                if (!closed && (following < 0 || following >= route.Length)) break;
                following = Index(following, route.Length);
                var edge = route[following] - route[at];
                if (edge.sqrMagnitude > .0001f)
                {
                    float angle = Mathf.Abs(Vector2.SignedAngle(incoming, edge));
                    if (angle > 2f && float.IsPositiveInfinity(firstTurn)) firstTurn = distance;
                    turn += angle; incoming = edge.normalized; distance += edge.magnitude;
                }
                at = following;
            }
            float desired = cruise;
            if (turn > 8f)
            {
                float corner = Mathf.Lerp(cruise, Mathf.Min(.85f, cruise), Mathf.InverseLerp(8f, 85f, turn));
                desired = Mathf.Min(cruise, Mathf.Sqrt(corner * corner + 2f * .9f * Mathf.Max(0, firstTurn - .6f)));
            }
            return Mathf.MoveTowards(current, desired, (desired < current ? 1.5f : .65f) * delta);
        }

        internal static Vector2 Heading(Vector2[] route, Vector2 position, int next, int direction, bool closed)
        {
            var start = position;
            float remaining = .8f;
            for (int i = 0; i < route.Length; i++)
            {
                var target = route[next];
                float distance = Vector2.Distance(position, target);
                if (distance >= remaining) return (Vector2.MoveTowards(position, target, remaining) - start).normalized;
                remaining -= distance; position = target;
                next += direction;
                if (!closed && (next < 0 || next >= route.Length)) break;
                next = Index(next, route.Length);
            }
            return (position - start).normalized;
        }
        private static int Index(int value, int count) => (value % count + count) % count;
    }
}
