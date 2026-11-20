using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    [DefaultExecutionOrder(-50)]
    internal sealed class DistrictHarass : MonoBehaviour
    {
        internal LotHarass behavior;
        internal Camera districtCamera;
        internal PrototypeRegionView view;
        internal PrototypeRegionView.Pedestrian target;
        internal bool TestMode;
        internal float TestEnds;
        internal int TestDistrict;
        internal const float ChaseSpeed = 2.4f;
        internal bool IncidentActive { get; private set; }
        internal bool InCustody { get; private set; }
        internal bool Booked { get; private set; }
        internal DistrictPatrol RespondingOfficer { get; private set; }
        private Vector3 authoredHome;
        private bool homeCaptured;
        private Renderer[] custodyRenderers;
        private bool[] enabledBeforeBooking;
        private Animator custodyAnimator;
        private string custodyAnimation;
        private AnimatorCullingMode originalCulling;

        private uint random;
        private float nextSearch, chaseEnds, nextScare, pending;
        private Vector2 segmentEnd;
        private bool hasSegment;
        private NavigationState segmentState;
        internal int MovementProbes { get; private set; }
        private Vector2 Point => new(transform.position.x, transform.position.z);

        private void LateUpdate() => Step(Mathf.Min(Time.deltaTime, .1f), Time.time,
            ActorSimulationVisibility.NearCamera(districtCamera, transform.position));

        internal void Step(float delta, float now, bool visible)
        {
            if (behavior == null || !view || view.IsSaving || view.DistrictLoading) return;
            CaptureHome();
            if (TestMode && (now >= TestEnds || view.SelectedDistrict != TestDistrict))
            {
                if (view.HarassTest == this) view.EndHarassTest(showMessage: false);
                else Destroy(gameObject);
                return;
            }
            if (Booked)
            {
                if (view.TimeOfDay is RegionTimeOfDay.Morning or RegionTimeOfDay.Noon or RegionTimeOfDay.Afternoon)
                {
                    Booked = false; transform.position = authoredHome;
                    for (int i = 0; i < custodyRenderers.Length; i++) if (custodyRenderers[i]) custodyRenderers[i].enabled = enabledBeforeBooking[i];
                }
                return;
            }
            if (InCustody) return;
            if (!TestMode && view.TimeOfDay != RegionTimeOfDay.Evening && !RespondingOfficer)
            {
                EndIncident(); behavior.Stop(); target = null; hasSegment = false; pending = 0; nextSearch = 0;
                return;
            }
            // Visual street trouble pauses outside the close camera view; no offscreen targeting or movement probes.
            if (!visible || !gameObject.activeInHierarchy || !view.HarassCloseView) return;
            behavior.Tick(delta);
            pending += delta;
            if (pending < .1f && (!hasSegment || (segmentEnd - Point).sqrMagnitude >= .0001f))
            { Advance(delta); return; }
            pending = 0;
            if (!PrototypeRegionView.HarassTargetAvailable(target) || now >= chaseEnds ||
                (target.Body.position - transform.position).sqrMagnitude > 32f * 32f)
            {
                target = null; hasSegment = false;
                if (!RespondingOfficer) EndIncident();
                if (now < nextSearch) return;
                if (random == 0) random = (uint)GetInstanceID();
                target = view.ChooseHarassTarget(Point, ref random, TestMode);
                nextSearch = now + 4f;
                chaseEnds = now + 12f;
                if (target == null) return;
            }
            var point = Point;
            var destination = new Vector2(target.Body.position.x, target.Body.position.z);
            var direction = destination - point;
            if (direction.sqrMagnitude > .01f)
                behavior.Face(Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)));
            if (direction.sqrMagnitude <= 3.5f * 3.5f && now >= nextScare)
            {
                nextScare = now + 1.5f;
                MovementProbes++;
                if (view.FreightClear(point, destination, .15f))
                {
                    view.ScarePedestrian(target, point, now, TestMode);
                    if (!IncidentActive) { IncidentActive = true; view.ReportHarass(this); }
                }
            }
            if (direction.sqrMagnitude <= 2f * 2f) { hasSegment = false; return; }
            var state = new NavigationState(view);
            if (!state.Equals(segmentState)) hasSegment = false;
            if (!hasSegment || (segmentEnd - point).sqrMagnitude < .0001f)
            {
                segmentEnd = Vector2.MoveTowards(point, destination, Mathf.Min(.5f, direction.magnitude - 2f));
                MovementProbes++;
                if (!view.FreightClear(point, segmentEnd, .2f))
                { target = null; hasSegment = false; nextSearch = now + 4f; return; }
                segmentState = state; hasSegment = true;
            }
            Advance(delta);
        }

        private void CaptureHome() { if (!homeCaptured) { authoredHome = transform.position; homeCaptured = true; } }
        private void EndIncident() { IncidentActive = false; if (view) view.ForgetHarass(this); }
        internal bool Reserve(DistrictPatrol officer, float now)
        {
            if (!officer || RespondingOfficer || !IncidentActive || InCustody || Booked || !isActiveAndEnabled) return false;
            CaptureHome(); RespondingOfficer = officer;
            if (TestMode) TestEnds = Mathf.Max(TestEnds, now + 180);
            return true;
        }
        internal void TakeCustody(DistrictPatrol officer)
        {
            if (RespondingOfficer != officer) return;
            InCustody = true; EndIncident(); behavior.Stop(); hasSegment = false; pending = 0;
            if (target != null) { target.ScaredUntil = target.HarassTestUntil = 0; if (target.Animator) target.Animator.speed = 1; }
            target = null;
            custodyAnimator = GetComponentInChildren<Animator>();
            if (custodyAnimator) { originalCulling = custodyAnimator.cullingMode; custodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
            CustodyIdle();
        }
        internal void CustodyMove(Vector2 next, float delta)
        {
            var direction = next - Point;
            if (direction.sqrMagnitude > .000001f)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));
                AnimateCustody("Walk");
            }
            else AnimateCustody("Idle");
            transform.position = new Vector3(next.x, view.HarassGroundHeight(next), next.y);
        }
        internal void CustodyIdle() => AnimateCustody("Idle");
        private void AnimateCustody(string state)
        {
            if (custodyAnimation == state || !custodyAnimator || !custodyAnimator.isActiveAndEnabled || !custodyAnimator.runtimeAnimatorController) return;
            int hash = Animator.StringToHash(custodyAnimator.GetLayerName(0) + "." + state);
            if (!custodyAnimator.HasState(0, hash)) return;
            custodyAnimator.speed = 1; custodyAnimator.Play(hash, 0, 0); custodyAnimator.Update(0); custodyAnimation = state;
        }
        internal void Release(DistrictPatrol officer, float now)
        {
            if (RespondingOfficer != officer) return;
            RespondingOfficer = null; InCustody = false; EndIncident(); hasSegment = false;
            nextSearch = now + 8; target = null; behavior.Stop();
            CustodyIdle();
            if (custodyAnimator) custodyAnimator.cullingMode = originalCulling;
            custodyAnimation = null;
        }
        internal void Book(DistrictPatrol officer)
        {
            if (RespondingOfficer != officer || !InCustody) return;
            Release(officer, Time.time);
            if (TestMode && view.HarassTest == this) { view.EndHarassTest(false); return; }
            Booked = true; custodyRenderers ??= GetComponentsInChildren<Renderer>(true);
            enabledBeforeBooking = new bool[custodyRenderers.Length];
            for (int i = 0; i < custodyRenderers.Length; i++)
            {
                enabledBeforeBooking[i] = custodyRenderers[i] && custodyRenderers[i].enabled;
                if (custodyRenderers[i]) custodyRenderers[i].enabled = false;
            }
        }
        private void OnDisable()
        {
            if (RespondingOfficer) Release(RespondingOfficer, Time.time);
            EndIncident(); InCustody = false; RespondingOfficer = null;
            if (gameObject.activeInHierarchy) behavior?.Stop();
        }

        private void Advance(float delta)
        {
            if (!hasSegment) return;
            // Cheap per-frame interpolation over a certified segment. Geometry is not probed here.
            if (!segmentState.Equals(new NavigationState(view))) { hasSegment = false; return; }
            var next = Vector2.MoveTowards(Point, segmentEnd, ChaseSpeed * delta);
            transform.position = new Vector3(next.x, view.HarassGroundHeight(next), next.y);
        }
    }
}
