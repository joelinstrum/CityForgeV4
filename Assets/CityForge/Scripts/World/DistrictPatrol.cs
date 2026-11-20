using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    [DefaultExecutionOrder(-40)]
    internal sealed class DistrictPatrol : MonoBehaviour
    {
        internal enum Activity { Patrolling, Responding, Escorting, Booking }
        internal Activity Stage { get; private set; }
        internal DistrictHarass Crime { get; private set; }
        internal Vector2 Point => new(transform.position.x, transform.position.z);
        internal Vector2 Home { get; private set; }
        internal bool CanRespond => isActiveAndEnabled && Stage == Activity.Patrolling;
        internal int Deliveries { get; private set; }
        internal int RoutePlans { get; private set; }
        private PrototypeRegionView view;
        private Camera districtCamera;
        private Animator animator;
        private PatrolJourney journey;
        private string animation;
        private int beat;
        private float nextPlan, bookingRemaining, responseRemaining, nextResponsePlan, escortRemaining, followerDistance;
        private PatrolStreetGraph beatGraph;
        private Vector2 lastCrimePoint;
        private ActorSimulationCadence cadence;
        internal const float WalkSpeed = 1.3f, RunSpeed = 3.6f, EscortGap = 1.3f;

        internal static bool HasStation(LotDefinition lot) => lot?.objects != null &&
            lot.objects.Exists(item => item.assetId == "v3-building-constable-station");
        internal void Initialize(PrototypeRegionView source, Camera camera, DistrictLotRecord home)
        {
            if (!source || !HasStation(home?.definition)) { enabled = false; return; }
            view = source; districtCamera = camera; Home = Point;
            animator = GetComponentInChildren<Animator>();
            if (animator) { animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            view.RegisterPatrol(this); Animate("Idle");
        }
        private void LateUpdate()
        {
            if (!view || view.DistrictLoading || view.IsSaving || !view.HarassCloseView) return;
            bool near = ActorSimulationVisibility.NearCamera(districtCamera, transform.position);
            float delta = cadence.Step(Mathf.Min(Time.deltaTime, .1f), near, GetInstanceID());
            if (delta > 0) Step(delta, Time.time);
        }
        internal void Step(float delta, float now)
        {
            if (!view || delta <= 0 || view.DistrictLoading || view.IsSaving || !view.HarassCloseView) return;
            view.DispatchPatrolCrimes(now);
            if (Stage != Activity.Patrolling && (!Crime || (!Crime.IncidentActive && !Crime.InCustody))) { Resume(now); }
            if (Stage == Activity.Responding)
            {
                if ((responseRemaining -= delta) <= 0) { Resume(now); return; }
                var point = new Vector2(Crime.transform.position.x, Crime.transform.position.z);
                if ((point - Point).sqrMagnitude <= 1.8f * 1.8f && view.PatrolClear(Point, point))
                {
                    if (now < nextResponsePlan) { Animate("Idle"); return; }
                    nextResponsePlan = now + 2;
                    BeginEscort(now);
                    return;
                }
                if (journey == null || !journey.State.Equals(new NavigationState(view)) ||
                    (now >= nextResponsePlan && (point - lastCrimePoint).sqrMagnitude >= 2.5f * 2.5f) || journey.Finished)
                {
                    if (now < nextResponsePlan) { Animate("Idle"); return; }
                    nextResponsePlan = now + 1;
                    if (!Plan(point)) { Animate("Idle"); return; }
                    lastCrimePoint = point;
                }
                Travel(RunSpeed, delta, "Run"); return;
            }
            if (Stage == Activity.Escorting)
            {
                if ((escortRemaining -= delta) <= 0) { Resume(now); return; }
                if (journey == null || !journey.State.Equals(new NavigationState(view)))
                {
                    if (now < nextPlan) return;
                    nextPlan = now + 2;
                    if (!PlanEscort()) { Animate("Idle"); Crime.CustodyIdle(); return; }
                }
                Travel(WalkSpeed, delta, "Walk");
                float follow = Mathf.Max(0, journey.Distance - EscortGap);
                followerDistance = Mathf.MoveTowards(followerDistance, follow, WalkSpeed * delta);
                Crime.CustodyMove(journey.At(followerDistance), delta);
                if (journey.Finished) Animate("Idle");
                if (journey.Finished && followerDistance >= Mathf.Max(0, journey.Length - EscortGap) - .05f)
                { Stage = Activity.Booking; bookingRemaining = 2; Animate("Idle"); Crime.CustodyIdle(); }
                return;
            }
            if (Stage == Activity.Booking)
            {
                if ((bookingRemaining -= delta) > 0) return;
                Deliveries++; var booked = Crime; Crime = null;
                booked.Book(this); Resume(now); return;
            }
            if (journey != null && !journey.State.Equals(new NavigationState(view))) journey = null;
            if (journey == null || journey.Finished)
            {
                Animate("Idle"); if (now < nextPlan) return;
                nextPlan = now + 3;
                var graph = view.PatrolGraph;
                if (!ReferenceEquals(graph, beatGraph))
                {
                    beatGraph = graph; float nearest = float.PositiveInfinity;
                    for (int i = 0; i < graph.Count; i++)
                    {
                        float distance = (graph.Point(i) - Home).sqrMagnitude;
                        if (distance < nearest) { nearest = distance; beat = i; }
                    }
                }
                for (int attempt = 0; attempt < Mathf.Min(12, graph.Count); attempt++)
                {
                    var target = graph.Point(beat++ % graph.Count);
                    if ((target - Point).sqrMagnitude < 9 || (target - Home).sqrMagnitude > 80 * 80) continue;
                    if (Plan(target)) break;
                }
            }
            if (journey != null && !journey.Finished) Travel(WalkSpeed, delta, "Walk");
        }
        internal void Respond(DistrictHarass crime, Vector2[] route, float now)
        {
            Crime = crime; Stage = Activity.Responding; journey = new PatrolJourney(route, new NavigationState(view));
            lastCrimePoint = new Vector2(crime.transform.position.x, crime.transform.position.z);
            nextResponsePlan = now + 1; responseRemaining = 120; Animate("Run");
        }
        private bool Plan(Vector2 target)
        {
            RoutePlans++;
            if (!view.PatrolGraph.TryRoute(Point, target, out var path)) { journey = null; return false; }
            journey = new PatrolJourney(path, new NavigationState(view)); return true;
        }
        private void BeginEscort(float now)
        {
            Crime.TakeCustody(this); Stage = Activity.Escorting; nextPlan = now + 2; escortRemaining = 180;
            if (!PlanEscort()) { Animate("Idle"); Crime.CustodyIdle(); }
        }
        private bool PlanEscort()
        {
            RoutePlans++;
            if (!Crime || !view.PatrolGraph.TryRoute(Point, Home, out var path)) { journey = null; return false; }
            var suspect = new Vector2(Crime.transform.position.x, Crime.transform.position.z);
            if (!view.PatrolClear(suspect, Point)) { journey = null; return false; }
            var combined = new List<Vector2> { suspect };
            combined.AddRange(path);
            followerDistance = 0;
            journey = new PatrolJourney(combined.ToArray(), new NavigationState(view)) { Distance = Vector2.Distance(suspect, Point) };
            return true;
        }
        private void Travel(float speed, float delta, string state)
        {
            if (journey == null || !journey.State.Equals(new NavigationState(view))) { Animate("Idle"); return; }
            journey.Distance = Mathf.Min(journey.Length, journey.Distance + speed * delta);
            Move(journey.At(journey.Distance)); Animate(state);
        }
        private void Move(Vector2 next)
        {
            var direction = next - Point;
            if (direction.sqrMagnitude > .000001f) transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
            transform.position = new Vector3(next.x, view.HarassGroundHeight(next), next.y);
        }
        private void Animate(string state)
        {
            if (animation == state || !animator || !animator.runtimeAnimatorController) return;
            int hash = Animator.StringToHash(animator.GetLayerName(0) + "." + state);
            if (!animator.HasState(0, hash)) return;
            animator.speed = 1; animator.Play(hash, 0, 0); animator.Update(0); animation = state;
        }
        private void Resume(float now)
        {
            if (Crime) Crime.Release(this, now);
            Crime = null; Stage = Activity.Patrolling; journey = null; nextPlan = now + 1; Animate("Idle");
        }
        private void OnEnable() { if (view) view.RegisterPatrol(this); }
        private void OnDisable()
        {
            if (Crime) Crime.Release(this, Time.time);
            Crime = null;
            if (view) view.UnregisterPatrol(this);
        }
    }
}
