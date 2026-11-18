using System;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class DistrictLumberjack : MonoBehaviour
    {
        private enum Activity { Idle, Walking, Chopping, Falling, Returning }
        private MeadowDistrictLandscape land;
        private Camera districtCamera;
        private DistrictLotRecord camp;
        private WildlifeWaterAccess water;
        private Animator animator;
        private Vector2 home, target, approach;
        private EstablishedRoute establishedRoute;
        private Predicate<Vector2> reachable;
        private System.Func<Vector2, Vector2, bool> movementClear;
        private bool Reachable(Vector2 tree) => land.TimberRouteClear(home, Approach(tree)) && ClearRoute(home, Approach(tree));
        private Quaternion homeRotation;
        private Activity activity;
        private bool claimed;
        private float wait;
        private string animationState;
        private ActorSimulationCadence cadence;
        internal Vector2 CaptureCenter => camp != null ? camp.center : home;
        private Vector2 Point => new(transform.position.x, transform.position.z);

        internal void Initialize(MeadowDistrictLandscape landscape, Camera camera, DistrictLotRecord source = null)
        {
            land = landscape;
            camp = source;
            districtCamera = camera;
            water = new WildlifeWaterAccess(land);
            reachable = Reachable;
            movementClear = (from, to) => land.TimberRouteClear(from, to) && ClearRoute(from, to);
            home = Point;
            homeRotation = transform.rotation;
            animator = GetComponentInChildren<Animator>();
            if (!HasAnimation("Chop") || !HasAnimation("Walk")) { enabled = false; return; }
            wait = 1f + (GetInstanceID() & 7) * .2f;
        }

        private bool HasAnimation(string state) => animator && animator.runtimeAnimatorController && animator.layerCount > 0 &&
            animator.HasState(0, Animator.StringToHash(animator.GetLayerName(0) + "." + state));

        private void Animate(string state)
        {
            if (animationState == state || !HasAnimation(state)) return;
            animationState = state;
            animator.CrossFadeInFixedTime(animator.GetLayerName(0) + "." + state, .15f);
        }

        private bool ClearRoute(Vector2 from, Vector2 to)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .5f));
            float previous = land.Height(from);
            for (int i = 0; i <= steps; i++)
            {
                var point = Vector2.Lerp(from, to, (float)i / steps);
                float height = land.Height(point);
                if (!water.Dry(point, .4f) || height < land.View.SeaLevel + .1f ||
                    Mathf.Abs(height - previous) > .4f || land.View.LumberjackObstacle(point)) return false;
                previous = height;
            }
            return true;
        }

        private Vector2 Approach(Vector2 tree) => tree + (home - tree).normalized * 1.2f;

        private void Update()
        {
            if (!land || land.IsRegionOverview || land.View.DistrictLoading || land.View.IsSaving) return;
            float delta = cadence.Step(Mathf.Min(Time.deltaTime, .1f), ActorSimulationVisibility.NearCamera(districtCamera, transform.position), GetInstanceID());
            if (delta <= 0) return;
            if (claimed && land.View.IsForestRemoved(target)) ReturnHome();
            switch (activity)
            {
                case Activity.Idle:
                    Animate("Idle");
                    wait -= delta;
                    if (wait > 0) break;
                    wait = 5f;
                    if (!land.TryClaimTimber(this, home, reachable, out target)) break;
                    claimed = true;
                    approach = Approach(target);
                    establishedRoute = new EstablishedRoute(new[] { home, approach }, new NavigationState(land.View));
                    activity = Activity.Walking;
                    break;
                case Activity.Walking:
                    if (!establishedRoute.CanTravel(0, Point, Vector2.MoveTowards(Point, approach, 1.15f * delta), new NavigationState(land.View), movementClear)) { ReturnHome(); break; }
                    if (!Move(approach, delta)) break;
                    Face(target);
                    activity = Activity.Chopping;
                    wait = 6f;
                    Animate("Chop");
                    break;
                case Activity.Chopping:
                    Animate("Chop");
                    wait -= delta;
                    if (wait > 0) break;
                    if (!land.FellClaimedTimber(this, target, Point)) { ReturnHome(); break; }
                    if (camp != null) camp.storedTimber = Mathf.Min(LotFreightValidation.StockLimit, camp.storedTimber + 1);
                    claimed = false;
                    activity = Activity.Falling;
                    wait = 2.5f;
                    Animate("Idle");
                    break;
                case Activity.Falling:
                    wait -= delta;
                    if (wait <= 0) ReturnHome();
                    break;
                case Activity.Returning:
                    if (establishedRoute != null && !establishedRoute.CanTravel(0, Point, Vector2.MoveTowards(Point, home, 1.15f * delta), new NavigationState(land.View), movementClear)) { Animate("Idle"); break; }
                    if (!Move(home, delta)) break;
                    transform.rotation = homeRotation;
                    activity = Activity.Idle;
                    wait = 3f;
                    Animate("Idle");
                    break;
            }
        }

        private bool Move(Vector2 destination, float delta)
        {
            Animate("Walk");
            Face(destination);
            var p = Vector2.MoveTowards(Point, destination, 1.15f * delta);
            transform.position = new Vector3(p.x, land.Height(p), p.y);
            return (p - destination).sqrMagnitude < .0001f;
        }

        private void Face(Vector2 destination)
        {
            var direction = destination - Point;
            if (direction.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
        }

        private void ReturnHome()
        {
            if (claimed && land) land.ReleaseTimber(this, target);
            claimed = false;
            activity = Activity.Returning;
        }

        private void OnDestroy() { if (land) land.ForgetTimberSearch(this); }

        private void OnDisable()
        {
            ReturnHome();
            Animate("Idle");
        }
    }
}
