using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private enum EaglePhase { Circle, PerchApproach, Perched, SearchClimb, Search, StreetApproach, Reach, Carry, DropRun, Release, Splash }

        private readonly System.Collections.Generic.List<Transform> eagleLotTrees = new();
        private readonly System.Collections.Generic.List<(Bounds bounds, Matrix4x4 worldToLocal, float roof)> eagleBuildingBounds = new();
        private const float SidewalkForgiveness = .20f;
        private readonly System.Collections.Generic.List<SkinnedMeshRenderer> eagleForcedSkins = new();
        private Renderer[] eaglePassengerRenderers;
        private Transform eagle;
        private Animator eagleAnimator;
        private AnimatorCullingMode eaglePassengerCulling;
        private Transform eagleLeftGrip, eagleRightGrip;
        private int eagleDistrict = -1, eagleLotCount;
        private Vector2 eagleOrbitCenter;
        private EaglePhase eaglePhase;
        private Pedestrian eaglePassenger, eagleMissedPassenger;
        private int eaglePickupRetries;
        private const float EagleSpeedScale = .75f;
        private Vector3 eaglePassengerOrigin, eagleWater;
        private float eagleAngle, eagleTimer, eagleApproachLimit = 7f, eagleEventDelay;
        private bool eagleGliding, eagleCameraFollow;
        private float eaglePerchDelay;
        private Vector3 eaglePerch, eagleStreetEntry, eagleStreetHeading;
        private float eagleStreetHalfWidth, eagleGripBlend;
        private Vector3 eagleFlightVelocity, eagleDropHeading, eagleReleasePoint, eaglePassengerVelocity;
        private float eagleBank;
        private bool eagleReleased, eaglePassengerHidden;
        private const float EagleDropSpeed = 14f;
        private const float EagleGravity = 9.81f;
        internal Transform EagleCameraTarget => !eagleCameraFollow ? null :
            (eaglePhase == EaglePhase.Release && eagleTimer >= .18f) || eaglePhase == EaglePhase.Splash
                ? eaglePassenger?.Body : eagle;
        private LineRenderer eagleSplash;
        private readonly System.Collections.Generic.List<Renderer> eagleHiddenRenderers = new();

        private void TickDistrictEagle()
        {
            if (SelectedDistrict < 0 || SelectedDistrict != eagleDistrict || !AnimalsEnabled)
            {
                ResetDistrictEagle();
                eagleDistrict = SelectedDistrict;
            }
            if (SelectedDistrict < 0 || DistrictLoading || IsSaving || !AnimalsEnabled) return;
            if (districtLotRoots.Count == 0 || TimeOfDay == RegionTimeOfDay.Night)
            {
                if (eagle) ResetDistrictEagle();
                return;
            }

            if (eagleLotCount != districtLotRoots.Count)
            {
                eagleLotCount = districtLotRoots.Count;
                eagleOrbitCenter = Vector2.zero;
                foreach (var lot in districtLotRoots.Keys) eagleOrbitCenter += lot.center;
                eagleOrbitCenter /= eagleLotCount;
            }
            var center = eagleOrbitCenter;
            if (!eagle)
            {
                var prefab = Resources.Load<GameObject>("Wildlife/Eagle/Eagle");
                if (!prefab) return;
                eagle = Instantiate(prefab, transform).transform;
                eagle.name = "District bald eagle";
                eagleAnimator = eagle.GetComponentInChildren<Animator>();
                var joints = eagle.GetComponentsInChildren<Transform>();
                eagleLeftGrip = joints.FirstOrDefault(joint => joint.name == "Grip.L");
                eagleRightGrip = joints.FirstOrDefault(joint => joint.name == "Grip.R");
                if (!eagleAnimator || !eagleLeftGrip || !eagleRightGrip)
                {
                    ResetDistrictEagle();
                    return;
                }
                eagleAngle = Random.value * Mathf.PI * 2f;
                var orbitStart = center + new Vector2(Mathf.Cos(eagleAngle), Mathf.Sin(eagleAngle)) * 29f;
                eagle.position = new Vector3(orbitStart.x, LotHeight(orbitStart) + 19f, orbitStart.y);
                eagle.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(eagleAngle), 0, Mathf.Cos(eagleAngle)));
                eagleEventDelay = Random.Range(85f, 155f);
                eaglePhase = EaglePhase.Circle;
                eaglePerchDelay = Random.Range(35f, 65f);
                eagleGliding = false;
            }

            AdvanceEagle(center, Mathf.Min(Time.deltaTime, .08f));
        }

        private void AdvanceEagle(Vector2 center, float delta)
        {
            if ((eaglePhase is EaglePhase.StreetApproach or EaglePhase.Reach or EaglePhase.Carry or EaglePhase.DropRun or EaglePhase.Release or EaglePhase.Splash) && (eaglePassenger == null || !eaglePassenger.Body ||
                !eaglePassenger.Body.gameObject.activeInHierarchy))
            {
                ReturnEaglePassenger();
                FinishEagleEvent();
            }
            if ((eaglePhase is EaglePhase.Carry or EaglePhase.DropRun or EaglePhase.Release) &&
                (eaglePassenger is not { EagleCarried: true } || !HasVisibleEaglePassenger()))
            {
                ReturnEaglePassenger();
                FinishEagleEvent();
            }
            switch (eaglePhase)
            {
                case EaglePhase.Circle:
                    eagleAngle = (eagleAngle + delta * .39f) % (Mathf.PI * 2f);
                    var orbit = center + new Vector2(Mathf.Cos(eagleAngle), Mathf.Sin(eagleAngle)) * 29f;
                    FlyEagleTo(new Vector3(orbit.x, LotHeight(orbit) + 19f, orbit.y), delta, 19f);
                    bool glide = Mathf.Repeat(eagleAngle, 2.1f) > 1.12f;
                    if (glide != eagleGliding)
                    {
                        eagleGliding = glide;
                        EagleAnimation(glide ? "Glide" : "Flap");
                    }
                    eagleEventDelay -= delta;
                    if (eagleEventDelay <= 0f && StartEagleSearch()) break;
                    eaglePerchDelay -= delta;
                    if (eaglePerchDelay <= 0f) TryPerchEagle();
                    break;
                case EaglePhase.PerchApproach:
                    FlyEagleTo(eaglePerch, delta, 12f, slowForLanding: true);
                    if ((eagle.position - eaglePerch).sqrMagnitude > .08f) break;
                    eagle.position = eaglePerch;
                    eagleTimer = 0f;
                    eaglePhase = EaglePhase.Perched;
                    EagleAnimation("Perch");
                    break;
                case EaglePhase.Perched:
                    eagleTimer += delta;
                    if (eagleTimer >= 8f) FinishEagleEvent();
                    break;
                case EaglePhase.SearchClimb:
                    eagleAngle += delta * .39f;
                    var climb = EagleOrbitPoint(center, eagleAngle, 29f);
                    var climbPoint = new Vector3(climb.x, LotHeight(center) + 55f, climb.y);
                    FlyEagleTo(climbPoint, delta, 18f);
                    if (Mathf.Abs(eagle.position.y - climbPoint.y) > .25f) break;
                    eagleAngle = Mathf.Atan2(eagle.position.z - center.y, eagle.position.x - center.x);
                    eagleTimer = 0f;
                    eaglePhase = EaglePhase.Search;
                    EagleAnimation("Glide");
                    break;
                case EaglePhase.Search:
                    eagleTimer += delta;
                    eagleAngle += delta * (Mathf.PI * 1.5f / 6f);
                    var scan = EagleOrbitPoint(center, eagleAngle, 19f);
                    FlyEagleTo(new Vector3(scan.x, LotHeight(center) + 55f, scan.y), delta, 21f);
                    if (eagleTimer < 6f) break;
                    if (TryBeginEagleEvent(allowZoomCulled: eagleCameraFollow)) break;
                    if (eagleCameraFollow) ShowDistrictMessage("The eagle found no walker with suitable offshore water.");
                    FinishEagleEvent();
                    break;
                case EaglePhase.StreetApproach:
                    eagleTimer += delta;
                    FlyEagleTo(eagleStreetEntry, delta, 23f, slowForLanding: true);
                    if (Vector3.Distance(eagle.position, eagleStreetEntry) > 8f && eagleTimer < 12f) break;
                    eagleTimer = 0f;
                    eaglePhase = EaglePhase.Reach;
                    EagleAnimation("Reach");
                    break;
                case EaglePhase.Reach:
                    eagleTimer += delta;
                    var pedestrian = eaglePassenger.Body.position;
                    var gripTarget = pedestrian + Vector3.up * 1.62f;
                    float lead = Mathf.Clamp((Vector3.Distance(eagle.position, gripTarget) - 2f) / 18f, 0f, .65f);
                    var approach = gripTarget + eaglePassenger.Body.forward * (eaglePassenger.Speed * lead) -
                        (EagleGrip() - eagle.position);
                    if (eagleStreetHeading != Vector3.zero && !EaglePickupSidewalk(gripTarget))
                    {
                        var offset = approach - eagleStreetEntry;
                        var streetSide = Vector3.Cross(Vector3.up, eagleStreetHeading);
                        float crossTrack = Mathf.Clamp(Vector3.Dot(offset, streetSide),
                            -eagleStreetHalfWidth * .65f, eagleStreetHalfWidth * .65f);
                        approach = eagleStreetEntry + eagleStreetHeading * Vector3.Dot(offset, eagleStreetHeading) +
                            streetSide * crossTrack;
                        approach.y = gripTarget.y - (EagleGrip() - eagle.position).y;
                    }
                    FlyEagleTo(approach, delta, 23f, slowForLanding: true, pickupApproach: true);
                    if (CanReachEaglePedestrian(gripTarget) && TrySecureEaglePassenger(pedestrian))
                    {
                        SetEagleReleaseRun();
                        eaglePhase = EaglePhase.Carry;
                        EagleAnimation("Carry");
                    }
                    else if (eagleTimer > eagleApproachLimit)
                    {
                        eagleMissedPassenger = eaglePassenger;
                        eaglePassenger = null;
                        if (eaglePickupRetries++ < 2 && StartEagleSearch(command: eagleCameraFollow, retry: true))
                            break;
                        FinishEagleEvent();
                    }
                    break;
                case EaglePhase.Carry:
                    var setup = eagleReleasePoint - eagleDropHeading * 62f;
                    var terrainAhead = eagle.position + EagleHorizontalForward() * (delta * 18f);
                    float clearance = LotHeight(new Vector2(terrainAhead.x, terrainAhead.z)) + 3f;
                    FlyEagleTo(new Vector3(setup.x, Mathf.Max(setup.y, clearance), setup.z), delta, 18f,
                        slowForLanding: true);
                    eagleGripBlend = Mathf.MoveTowards(eagleGripBlend, 1f, delta / .22f);
                    HoldEaglePassenger();
                    if (Vector3.Distance(eagle.position, setup) <= 5f) eaglePhase = EaglePhase.DropRun;
                    break;
                case EaglePhase.DropRun:
                    float along = Vector3.Dot(eagle.position - eagleReleasePoint, eagleDropHeading);
                    FlyEagleTo(eagleReleasePoint + eagleDropHeading * (along + 15f), delta, EagleDropSpeed);
                    HoldEaglePassenger();
                    if (Vector3.Dot(eagleReleasePoint - eagle.position, eagleDropHeading) > .25f) break;
                    var predictedImpact = eaglePassenger.Body.position + eagleFlightVelocity *
                        (Mathf.Sqrt(2f * Mathf.Max(0f, eaglePassenger.Body.position.y - eagleWater.y) / EagleGravity) + .18f);
                    predictedImpact.y = eagleWater.y;
                    if ((predictedImpact - eagleWater).sqrMagnitude > 12f * 12f)
                    {
                        eaglePhase = EaglePhase.Carry;
                        break;
                    }
                    eaglePassengerVelocity = eagleFlightVelocity;
                    eagleReleased = false;
                    eaglePhase = EaglePhase.Release;
                    eagleTimer = 0f;
                    EagleAnimation("Release");
                    break;
                case EaglePhase.Release:
                    eagleTimer += delta;
                    FlyEagleTo(eagle.position + EagleHorizontalForward() * 20f + Vector3.up * (delta * 3f), delta, EagleDropSpeed);
                    if (eagleTimer < .18f)
                    {
                        HoldEaglePassenger();
                        break;
                    }
                    if (!eagleReleased)
                    {
                        eaglePassengerVelocity = new Vector3(eagleFlightVelocity.x, eaglePassengerVelocity.y, eagleFlightVelocity.z);
                        eagleReleased = true;
                    }
                    eaglePassengerVelocity.y -= EagleGravity * delta;
                    eaglePassenger.Body.position += eaglePassengerVelocity * delta;
                    if (eaglePassenger.Body.position.y > eagleWater.y) break;
                    var impact = eaglePassenger.Body.position;
                    eagleWater = new Vector3(impact.x, eagleWater.y, impact.z);
                    eaglePassenger.Body.position = eagleWater;
                    eaglePhase = EaglePhase.Splash;
                    eagleTimer = 0f;
                    EagleAnimation("Flap");
                    break;
                case EaglePhase.Splash:
                    eagleTimer += delta;
                    FlyEagleTo(eagle.position + EagleHorizontalForward() * 20f + Vector3.up * (delta * 2f), delta, 16f);
                    DrawEagleSplash(eagleTimer);
                    float depth = Mathf.Min(5f, eagleTimer * 5.5f);
                    eaglePassenger.Body.position = eagleWater - Vector3.up * depth;
                    if (eagleTimer >= .25f) eagleCameraFollow = false;
                    if (depth >= 2.2f && !eaglePassengerHidden)
                    {
                        foreach (var renderer in eaglePassenger.Body.GetComponentsInChildren<Renderer>())
                            if (renderer.enabled)
                            {
                                eagleHiddenRenderers.Add(renderer);
                                renderer.enabled = false;
                            }
                        eaglePassengerHidden = true;
                    }
                    if (eagleTimer < 1.6f) break;
                    ReturnEaglePassenger();
                    FinishEagleEvent();
                    break;
            }
        }


        private bool StartEagleSearch(bool command = false, bool retry = false)
        {
            eagleEventDelay = 16f;
            if (Waterless || !pedestrians.Any(candidate => IsEagleTarget(candidate, command))) return false;
            if (!retry)
            {
                eaglePickupRetries = 0;
                eagleMissedPassenger = null;
            }
            eaglePhase = EaglePhase.SearchClimb;
            eagleTimer = 0f;
            EagleAnimation("Flap");
            return true;
        }

        private static bool IsEagleTarget(Pedestrian candidate, bool allowZoomCulled) =>
            !candidate.Vehicle && !candidate.EagleCarried && candidate.Route is { Length: > 1 } &&
            candidate.Body && (allowZoomCulled ? candidate.Body.gameObject.activeSelf : candidate.Body.gameObject.activeInHierarchy);

        private bool TryBeginEagleEvent(bool allowZoomCulled = false)
        {
            eagleEventDelay = 16f;
            if (Waterless || pedestrians.Count == 0) return false;

            var water = DistrictLotWater();
            float startAngle = Random.value * Mathf.PI * 2f;
            var candidates = pedestrians
                .Where(candidate => IsEagleTarget(candidate, allowZoomCulled))
                .OrderBy(candidate => candidate == eagleMissedPassenger)
                .ThenBy(candidate => (candidate.Body.position - eagle.position).sqrMagnitude);
            foreach (var candidate in candidates)
            {
                var point = candidate.Body.position;
                for (float radius = 12f; radius <= 300f; radius += 12f)
                for (int i = 0; i < 16; i++)
                {
                    float angle = startAngle + i * (Mathf.PI * 2f / 16f);
                    var sample = new Vector2(point.x, point.z) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (!InActiveDistrict(sample)) continue;
                    float ground = LotHeight(sample);
                    if (!TryLotWater(sample, ground, water, out float level) || level - ground < 2f ||
                        !ClearOfShore(sample, water)) continue;
                    eagleWater = new Vector3(sample.x, level, sample.y);
                    eaglePassenger = candidate;
                    eagleEventDelay = Random.Range(125f, 220f);
                    bool hasStreet = TryGetEagleStreetEntry(point);
                    eaglePhase = hasStreet ? EaglePhase.StreetApproach : EaglePhase.Reach;
                    eagleTimer = 0f;
                    eagleApproachLimit = 9f + Vector3.Distance(point, eagle.position) / 15f;
                    EagleAnimation(hasStreet ? "Glide" : "Reach");
                    return true;
                }
            }
            return false;
        }

        private bool TryGetEagleStreetEntry(Vector3 pedestrian)
        {
            eagleStreetHeading = Vector3.zero;
            float best = 10f * 10f;
            var point = new Vector2(pedestrian.x, pedestrian.z);
            var eaglePoint = new Vector2(eagle.position.x, eagle.position.z);
            foreach (var road in districtRoadMeshes.Keys)
            {
                var line = RoadCenterline(road);
                for (int i = 0; i < line.Count - (road.closed ? 0 : 1); i++)
                {
                    var a = line[i];
                    var b = line[(i + 1) % line.Count];
                    var edge = b - a;
                    if (edge.sqrMagnitude < .01f) continue;
                    float t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
                    var center = a + edge * t;
                    float distance = (point - center).sqrMagnitude;
                    if (distance >= best) continue;
                    float before = t * edge.magnitude;
                    float after = (1f - t) * edge.magnitude;
                    if (Mathf.Max(before, after) < 9f) continue;
                    bool fromA = before >= 9f && (after < 9f || (a - eaglePoint).sqrMagnitude <= (b - eaglePoint).sqrMagnitude);
                    var heading = edge.normalized * (fromA ? 1f : -1f);
                    var entry = center - heading * Mathf.Min(24f, fromA ? before : after);
                    eagleStreetEntry = new Vector3(entry.x, LotHeight(entry) + 5.5f, entry.y);
                    eagleStreetHeading = new Vector3(heading.x, 0f, heading.y);
                    eagleStreetHalfWidth = road.width * .5f;
                    best = distance;
                }
            }
            return eagleStreetHeading != Vector3.zero;
        }

        private bool ClearOfShore(Vector2 point, WildlifeWaterAccess water)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                var sample = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 18f;
                if (!InActiveDistrict(sample)) return false;
                float ground = LotHeight(sample);
                if (!TryLotWater(sample, ground, water, out float level) || level - ground < .75f) return false;
            }
            return true;
        }

        private bool TryPerchEagle()
        {
            eaglePerchDelay = Random.Range(45f, 85f);
            if (!eagle || eaglePhase != EaglePhase.Circle) return false;
            float best = float.PositiveInfinity;
            foreach (var tree in placedTreeViews.Values)
                if (tree) ConsiderTree(tree.transform);
            foreach (var tree in eagleLotTrees)
                if (tree) ConsiderTree(tree);
            if (ApprovedDistrict && ApprovedDistrict.GetComponentInChildren<ApprovedDistrictForest>() is { } forest &&
                forest.TryClosestCanopy(eagle.position, out var canopy))
                ConsiderCanopy(canopy + Vector3.up * .24f);
            if (float.IsPositiveInfinity(best)) return false;
            eaglePhase = EaglePhase.PerchApproach;
            EagleAnimation("Glide");
            return true;

            void ConsiderTree(Transform tree)
            {
                var renderers = tree.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.y < 2f) return;
                ConsiderCanopy(new Vector3(bounds.center.x, bounds.max.y + .24f, bounds.center.z));
            }

            void ConsiderCanopy(Vector3 perch)
            {
                float distance = (perch - eagle.position).sqrMagnitude;
                if (distance >= best) return;
                best = distance;
                eaglePerch = perch;
            }
        }

        private Vector3 EagleGrip() => (eagleLeftGrip.position + eagleRightGrip.position) * .5f;

        private static Vector2 EagleOrbitPoint(Vector2 center, float angle, float radius) =>
            center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

        private Vector3 EagleHorizontalForward()
        {
            var direction = Vector3.ProjectOnPlane(eagle.forward, Vector3.up);
            return direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.forward;
        }

        private void SetEagleReleaseRun()
        {
            eagleDropHeading = Vector3.ProjectOnPlane(eagleWater - eagle.position, Vector3.up).normalized;
            if (eagleDropHeading == Vector3.zero) eagleDropHeading = EagleHorizontalForward();
            float flightTime = Mathf.Sqrt(2f * 98f / EagleGravity);
            eagleReleasePoint = eagleWater + Vector3.up * 100f - eagleDropHeading * (EagleDropSpeed * EagleSpeedScale * (flightTime + .18f));
        }

        private bool CanReachEaglePedestrian(Vector3 target)
        {
            var grip = EagleGrip();
            var difference = target - grip;
            bool close = difference.sqrMagnitude < Mathf.Pow(.95f * (1f + SidewalkForgiveness), 2f);
            if (!close && eagleStreetHeading != Vector3.zero)
            {
                var side = Vector3.Cross(Vector3.up, eagleStreetHeading);
                close = Mathf.Abs(Vector3.Dot(difference, eagleStreetHeading)) < 1.15f &&
                    Mathf.Abs(difference.y) < .95f &&
                    Mathf.Abs(Vector3.Dot(difference, side)) < Mathf.Clamp(eagleStreetHalfWidth + .9f, 2f, 3.1f);
            }
            if (!close) return false;

            // Sidewalks and road margins are authoritative open pickup space, including overhanging renderer bounds.
            if (EagleSidewalkSegment(grip, target)) return true;
            foreach (var (bounds, worldToLocal, _) in eagleBuildingBounds)
            {
                var localGrip = worldToLocal.MultiplyPoint3x4(grip);
                var localTarget = worldToLocal.MultiplyPoint3x4(target);
                var segment = localTarget - localGrip;
                if (bounds.Contains(localGrip) || bounds.Contains(localTarget) ||
                    (segment.sqrMagnitude > .0001f &&
                     bounds.IntersectRay(new Ray(localGrip, segment.normalized), out float entry) && entry <= segment.magnitude))
                    return false;
            }
            return true;
        }

        private bool TrySecureEaglePassenger(Vector3 origin)
        {
            eaglePassengerRenderers = eaglePassenger.Body.GetComponentsInChildren<Renderer>();
            if (!HasVisibleEaglePassenger()) return false;
            eaglePassengerOrigin = origin;
            eaglePassenger.EagleCarried = true;
            if (eaglePassenger.Animator)
            {
                eaglePassengerCulling = eaglePassenger.Animator.cullingMode;
                eaglePassenger.Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                SetPedestrianAnimation(eaglePassenger, false);
            }
            foreach (var renderer in eaglePassengerRenderers)
                if (renderer is SkinnedMeshRenderer skin && skin.enabled && !skin.updateWhenOffscreen)
                {
                    skin.updateWhenOffscreen = true;
                    eagleForcedSkins.Add(skin);
                }
            eagleGripBlend = 0f;
            return true;
        }

        private bool HasVisibleEaglePassenger()
        {
            if (eaglePassengerRenderers == null) return false;
            foreach (var renderer in eaglePassengerRenderers)
                if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy) return true;
            return false;
        }

        private void RegisterEagleObstacle(Transform building)
        {
            var renderers = building.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var worldToLocal = Matrix4x4.Rotate(Quaternion.Inverse(building.rotation)) *
                Matrix4x4.Translate(-building.position);
            foreach (var renderer in renderers)
            {
                var bounds = renderer.localBounds;
                var toBuilding = worldToLocal * renderer.localToWorldMatrix;
                var oriented = new Bounds(toBuilding.MultiplyPoint3x4(bounds.min), Vector3.zero);
                for (int corner = 1; corner < 8; corner++)
                    oriented.Encapsulate(toBuilding.MultiplyPoint3x4(new Vector3(
                        (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                        (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                        (corner & 4) == 0 ? bounds.min.z : bounds.max.z)));
                eagleBuildingBounds.Add((oriented, worldToLocal, renderer.bounds.max.y + 2.5f));
            }
        }

        private float EagleRoofAhead(Vector3 position, Vector3 direction, float distance, float step, float padding, out float roofAtStep)
        {
            float clearance = float.NegativeInfinity;
            roofAtStep = float.NegativeInfinity;
            foreach (var (bounds, worldToLocal, roof) in eagleBuildingBounds)
            {
                var building = bounds;
                building.Expand(new Vector3(padding * 2f, 0f, padding * 2f));
                var origin = worldToLocal.MultiplyPoint3x4(position);
                origin.y = building.center.y;
                var ray = new Ray(origin, worldToLocal.MultiplyVector(direction));
                if (!building.IntersectRay(ray, out float entry) || entry > distance) continue;
                clearance = Mathf.Max(clearance, roof);
                if (entry <= step) roofAtStep = Mathf.Max(roofAtStep, roof);
            }
            return clearance;
        }

        private void HoldEaglePassenger() =>
            eaglePassenger.Body.position = Vector3.Lerp(eaglePassengerOrigin,
                EagleGrip() - Vector3.up * 1.62f, eagleGripBlend);

        private void FlyEagleTo(Vector3 target, float delta, float speed, bool slowForLanding = false, bool pickupApproach = false)
        {
            if (delta <= 0f) return;
            speed *= EagleSpeedScale;
            var position = eagle.position;
            var horizontal = Vector3.ProjectOnPlane(target - position, Vector3.up);
            var forward = EagleHorizontalForward();
            float turn = horizontal.sqrMagnitude > .0025f
                ? Mathf.Clamp(Vector3.SignedAngle(forward, horizontal, Vector3.up), -72f * delta, 72f * delta)
                : 0f;
            forward = Quaternion.AngleAxis(turn, Vector3.up) * forward;
            eagleBank = Mathf.MoveTowards(eagleBank, -turn / delta * .28f, 95f * delta);
            eagle.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, eagleBank);
            float advance = speed * delta;
            if (slowForLanding)
                advance = Mathf.Min(horizontal.magnitude, Mathf.Min(speed,
                    Mathf.Max(pickupApproach ? .55f : 2.5f, horizontal.magnitude)) * delta);
            bool sidewalkPickup = pickupApproach && EagleSidewalkSegment(position, target);
            if (sidewalkPickup && horizontal.magnitude < 2.5f && horizontal.sqrMagnitude > .0001f)
                forward = horizontal.normalized;
            float lookAhead = pickupApproach ? Mathf.Min(2.5f, horizontal.magnitude) : speed * 1.4f;
            float roofAtStep = float.NegativeInfinity;
            float roofAhead = sidewalkPickup ? float.NegativeInfinity : EagleRoofAhead(position, forward, Mathf.Max(advance, lookAhead), advance,
                pickupApproach ? .55f * (1f - SidewalkForgiveness) : 1.6f, out roofAtStep);
            float nextHeight = Mathf.MoveTowards(position.y, Mathf.Max(target.y, roofAhead), speed * .8f * delta);
            // Hold at the edge until the wings and any passenger clear the roof.
            if (Mathf.Min(position.y, nextHeight) < roofAtStep) advance = 0f;
            var next = position + forward * advance;
            next.y = nextHeight;
            eagle.position = next;
            eagleFlightVelocity = (next - position) / delta;
        }

        private void EagleAnimation(string state) => eagleAnimator.CrossFadeInFixedTime(state, .15f, 0);

        private void DrawEagleSplash(float elapsed)
        {
            if (!eagleSplash)
            {
                eagleSplash = new GameObject("Eagle water splash").AddComponent<LineRenderer>();
                eagleSplash.transform.SetParent(transform, false);
                eagleSplash.loop = true;
                eagleSplash.positionCount = 24;
                eagleSplash.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                eagleSplash.sharedMaterial = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit"))
                    { color = new Color(.8f, .94f, 1f) });
            }
            eagleSplash.enabled = elapsed < 1.4f;
            float radius = .26f + elapsed * 1.55f;
            eagleSplash.widthMultiplier = Mathf.Max(.012f, .12f * (1f - elapsed / 1.4f));
            for (int i = 0; i < eagleSplash.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / eagleSplash.positionCount;
                eagleSplash.SetPosition(i, eagleWater + new Vector3(Mathf.Cos(a) * radius, .09f, Mathf.Sin(a) * radius));
            }
        }

        private void ReturnEaglePassenger()
        {
            if (eaglePassenger is { EagleCarried: true } && eaglePassenger.Body)
            {
                eaglePassenger.Body.position = eaglePassengerOrigin;
                eaglePassenger.EagleCarried = false;
                if (eaglePassenger.Animator)
                    eaglePassenger.Animator.cullingMode = eaglePassengerCulling;
            }
            foreach (var renderer in eagleHiddenRenderers)
                if (renderer) renderer.enabled = true;
            eagleHiddenRenderers.Clear();
            foreach (var skin in eagleForcedSkins)
                if (skin) skin.updateWhenOffscreen = false;
            eagleForcedSkins.Clear();
            eaglePassengerRenderers = null;
            eaglePassengerHidden = false;
            eaglePassenger = null;
        }

        private void FinishEagleEvent()
        {
            if (eagleSplash) eagleSplash.enabled = false;
            eaglePhase = EaglePhase.Circle;
            eagleGliding = false;
            eagleCameraFollow = false;
            if (eagleEventDelay <= 0f || eaglePassenger != null)
                eagleEventDelay = Random.Range(125f, 220f);
            EagleAnimation("Flap");
        }

        private void ResetDistrictEagle()
        {
            ReturnEaglePassenger();
            if (eagleSplash) eagleSplash.enabled = false;
            if (eagle) Destroy(eagle.gameObject);
            eagle = null;
            eagleCameraFollow = false;
            eagleLotCount = 0;
            eagleBank = 0f;
        }
    }
}
