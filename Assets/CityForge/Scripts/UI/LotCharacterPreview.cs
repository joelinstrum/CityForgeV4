using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.UI
{
    internal sealed class LotCharacterPreview
    {
        private sealed class Actor
        {
            public Transform view;
            public Animator animator;
            public LotCharacterScript settings;
            public Vector2 home;
            public float yaw, scale, radius, wait, walked, nextSearch;
            public int searchIndex;
            public string requestedAnimation;
            public string assetId, idle;
            public Actor partner;
            public bool talking;
            public readonly Queue<Vector2> route = new Queue<Vector2>();

            public Vector2 Point => new Vector2(view.localPosition.x, view.localPosition.z);

            public void Animate(string state)
            {
                if (requestedAnimation == state) return;
                requestedAnimation = state;
                if (!animator || !animator.runtimeAnimatorController || animator.layerCount == 0) return;
                string prefix = animator.GetLayerName(0) + ".";
                int hash = Animator.StringToHash(prefix + state);
                if (!animator.HasState(0, hash)) hash = Animator.StringToHash(prefix + idle);
                if (!animator.HasState(0, hash) || animator.GetCurrentAnimatorStateInfo(0).fullPathHash == hash) return;
                animator.Play(hash, 0, 0);
                animator.Update(0);
            }
        }

        private readonly Dictionary<string, Actor> actors = new Dictionary<string, Actor>();
        private readonly List<Actor> partners = new List<Actor>();
        private readonly HashSet<string> bound = new HashSet<string>();
        private readonly List<Rect> obstacles = new List<Rect>();
        private readonly List<(IReadOnlyList<Vector2> points, bool closed)> paths =
            new List<(IReadOnlyList<Vector2>, bool)>();
        private Vector2 halfSize;
        private float previewTime;
        private const float WalkSpeed = 1.15f;
        private const float PartnerSearchInterval = 10f;
        private const int PartnerCandidateLimit = 4;

        public void BeginRefresh(LotDefinition lot, LotAssetCatalog catalog)
        {
            bound.Clear();
            halfSize = lot.Size * .5f;
            obstacles.Clear();
            paths.Clear();
            foreach (var item in lot.objects)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset?.category == "Buildings" || asset?.category == "Fences")
                    obstacles.Add(LotRuntimeLoader.Footprint(item, asset));
            }
            foreach (var path in lot.paths)
            {
                IReadOnlyList<Vector2> points = path.points;
                if (path.curved)
                {
                    var rounded = new List<Vector2>();
                    LotPathSurface.RoundCorners(points, null, path.closed, path.width, rounded);
                    points = rounded;
                }
                paths.Add((points, path.closed));
            }
        }

        public void Bind(LotObjectDefinition item, LotAssetCatalogEntry asset, Transform view, bool paused)
        {
            bound.Add(item.id);
            if (!actors.TryGetValue(item.id, out var actor) || actor.assetId != item.assetId ||
                actor.home != item.position || actor.yaw != item.yaw || actor.scale != item.scale ||
                !actor.settings.Equals(item.characterScript))
            {
                actor = new Actor
                {
                    assetId = item.assetId, home = item.position, yaw = item.yaw, scale = item.scale,
                    radius = Mathf.Max(asset.footprint.x, asset.footprint.y) * item.scale * .5f,
                    settings = item.characterScript, idle = string.IsNullOrEmpty(asset.idleState) ? "Idle" : asset.idleState
                };
                actors[item.id] = actor;
            }
            else if (actor.view && Inside(actor, actor.Point))
            {
                view.localPosition = actor.view.localPosition;
                view.localRotation = actor.view.localRotation;
            }
            actor.view = view;
            actor.animator = view.GetComponentInChildren<Animator>();
            actor.requestedAnimation = null;
            if (!ClearRoute(actor))
            {
                actor.route.Clear();
                actor.wait = 0;
            }
            if (!paused && actor.settings.configured)
                actor.Animate(actor.talking ? "FoldArms" : actor.idle);
        }

        private bool ClearRoute(Actor actor)
        {
            var position = actor.Point;
            foreach (var next in actor.route)
            {
                if (!Clear(actor, position, next)) return false;
                position = next;
            }
            return true;
        }

        public void EndRefresh()
        {
            var removed = new List<string>();
            partners.Clear();
            foreach (var pair in actors)
            {
                if (bound.Contains(pair.Key)) partners.Add(pair.Value);
                else removed.Add(pair.Key);
            }
            foreach (var id in removed)
            {
                actors[id].view = null;
                actors.Remove(id);
            }
        }

        public void Restart(string id)
        {
            if (!actors.TryGetValue(id, out var actor)) return;
            actor.route.Clear();
            actor.wait = 0;
            actor.talking = false;
        }

        public void Tick(float delta, string dragged)
        {
            if (delta <= 0) return;
            previewTime += delta;
            foreach (var pair in actors)
            {
                var actor = pair.Value;
                if (!actor.settings.configured) continue;
                if (pair.Key == dragged || actor.settings.idlePercent == 100)
                {
                    actor.Animate(actor.idle);
                    continue;
                }

                var point = actor.Point;
                while (actor.route.Count > 0 && (actor.route.Peek() - point).sqrMagnitude < .000001f)
                    actor.route.Dequeue();
                if (actor.route.Count == 0)
                {
                    actor.Animate(actor.talking ? "FoldArms" : actor.idle);
                    actor.wait -= delta;
                    if (actor.wait > 0) continue;
                    actor.talking = false;
                    actor.partner = FindPartner(actor);
                    if (!Plan(actor))
                    {
                        actor.wait = 1.5f;
                        continue;
                    }
                    actor.walked = 0;
                }

                var next = actor.route.Peek();
                var position = Vector2.MoveTowards(point, next, WalkSpeed * delta);
                if (position.Equals(point))
                {
                    actor.Animate(actor.idle);
                    continue;
                }

                Face(actor, next - point);
                actor.view.localPosition = new Vector3(position.x, 0, position.y);
                actor.walked += Vector2.Distance(point, position) / WalkSpeed;
                if (position != next)
                {
                    actor.Animate("Walk");
                    continue;
                }
                actor.route.Dequeue();
                if (actor.route.Count != 0)
                {
                    actor.Animate("Walk");
                    continue;
                }

                actor.wait = actor.walked * actor.settings.idlePercent / (100 - actor.settings.idlePercent);
                if (actor.partner?.view && Vector2.Distance(position, actor.partner.Point) < 2f)
                {
                    Face(actor, actor.partner.Point - position);
                    actor.talking = Random.Range(0, 100) < actor.settings.walkTalkPercent;
                    actor.wait = Mathf.Max(actor.wait, actor.talking ? 2.5f : .6f);
                }
                actor.Animate(actor.talking ? "FoldArms" : actor.idle);
            }
        }

        private Actor FindPartner(Actor actor)
        {
            if (!actor.settings.interactWithNpcs || partners.Count < 2 || previewTime < actor.nextSearch)
                return null;
            actor.nextSearch = previewTime + PartnerSearchInterval;
            if (Random.value >= .75f) return null;

            Actor closest = null;
            float distanceSquared = 14f * 14f;
            var point = actor.Point;
            int count = Mathf.Min(partners.Count, PartnerCandidateLimit);
            for (int i = 0; i < count; i++)
            {
                actor.searchIndex %= partners.Count;
                var other = partners[actor.searchIndex++];
                if (other == actor || !other.view) continue;
                float next = (point - other.Point).sqrMagnitude;
                if (next < 1f || next >= distanceSquared) continue;
                if (!actor.settings.stayOnPaths)
                {
                    var approach = Approach(actor, other);
                    if ((approach - point).sqrMagnitude < .01f || !Clear(actor, point, approach)) continue;
                }
                distanceSquared = next;
                closest = other;
            }
            return closest;
        }

        private bool Plan(Actor actor)
        {
            var point = actor.Point;
            if (actor.settings.stayOnPaths) return PlanOnPath(actor, point);
            if (actor.partner != null)
            {
                actor.route.Enqueue(Approach(actor, actor.partner));
                return true;
            }
            for (int i = 0; i < 12; i++)
            {
                var target = point + Random.insideUnitCircle.normalized * Random.Range(2.5f, 5f);
                if (!Clear(actor, point, target)) continue;
                actor.route.Enqueue(target);
                return true;
            }
            return false;
        }

        private bool PlanOnPath(Actor actor, Vector2 point)
        {
            IReadOnlyList<Vector2> path = null;
            Vector2 entry = default;
            int segment = 0;
            bool loop = false;
            float nearest = float.PositiveInfinity;
            foreach (var candidate in paths)
            {
                int count = candidate.closed ? candidate.points.Count : candidate.points.Count - 1;
                for (int i = 0; i < count; i++)
                {
                    var a = candidate.points[i];
                    var b = candidate.points[(i + 1) % candidate.points.Count];
                    var direction = b - a;
                    var projected = a + direction * Mathf.Clamp01(Vector2.Dot(point - a, direction) /
                        Mathf.Max(.0001f, direction.sqrMagnitude));
                    float distance = (point - projected).sqrMagnitude;
                    if (distance >= nearest || !Clear(actor, point, projected)) continue;
                    path = candidate.points;
                    loop = candidate.closed;
                    entry = projected;
                    segment = i;
                    nearest = distance;
                }
            }
            if (path == null) return false;

            int forward = (segment + 1) % path.Count;
            bool goForward = actor.partner != null
                ? (path[forward] - actor.partner.Point).sqrMagnitude < (path[segment] - actor.partner.Point).sqrMagnitude
                : Random.value < .5f;
            float budget = Random.Range(3f, 6f);
            if ((entry - point).sqrMagnitude > .0025f) actor.route.Enqueue(entry);
            var last = entry;
            for (int i = 0, index = goForward ? forward : segment; i < path.Count; i++)
            {
                if (index < 0 || index >= path.Count)
                {
                    if (!loop) break;
                    index = (index + path.Count) % path.Count;
                }
                var next = Vector2.MoveTowards(last, path[index], budget);
                float distance = Vector2.Distance(last, next);
                if (distance > .001f)
                {
                    if (!Clear(actor, last, next)) break;
                    actor.route.Enqueue(next);
                    budget -= distance;
                    last = next;
                    if (budget < .01f || actor.partner?.view && Vector2.Distance(last, actor.partner.Point) < 1.5f)
                        break;
                }
                index += goForward ? 1 : -1;
            }
            return actor.route.Count > 0;
        }

        private bool Inside(Actor actor, Vector2 point) =>
            Mathf.Abs(point.x) <= halfSize.x - actor.radius && Mathf.Abs(point.y) <= halfSize.y - actor.radius;

        private bool Clear(Actor actor, Vector2 from, Vector2 to)
        {
            if (!Inside(actor, to)) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .35f));
            foreach (var obstacle in obstacles)
            {
                var bounds = new Rect(obstacle.min - Vector2.one * actor.radius,
                    obstacle.size + Vector2.one * actor.radius * 2f);
                bool leaving = bounds.Contains(from);
                for (int i = 1; i <= steps; i++)
                {
                    bool inside = bounds.Contains(Vector2.Lerp(from, to, (float)i / steps));
                    if (inside && (!leaving || i == steps)) return false;
                    if (!inside) leaving = false;
                }
            }
            return true;
        }

        private static Vector2 Approach(Actor actor, Actor partner) =>
            partner.Point + (actor.Point - partner.Point).normalized * (actor.radius + partner.radius + .45f);

        private static void Face(Actor actor, Vector2 direction)
        {
            if (direction.sqrMagnitude > .00001f)
                actor.view.localRotation = Quaternion.Euler(0, Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 0);
        }
    }
}
