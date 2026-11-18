using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class DistrictHarass : MonoBehaviour
    {
        internal LotHarass behavior;
        internal Camera districtCamera;
        internal PrototypeRegionView view;
        internal PrototypeRegionView.Pedestrian target;
        internal bool TestMode;
        internal float TestEnds;
        internal int TestDistrict;
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
            if (behavior == null || !view) return;
            if (TestMode && (now >= TestEnds || view.SelectedDistrict != TestDistrict)) { Destroy(gameObject); return; }
            if (!TestMode && view.TimeOfDay != RegionTimeOfDay.Evening)
            {
                behavior.Stop(); target = null; hasSegment = false; pending = 0; nextSearch = 0;
                return;
            }
            // Visual street trouble pauses outside the close camera view; no offscreen targeting or movement probes.
            if (!visible || !gameObject.activeInHierarchy || !view.HarassCloseView) return;
            behavior.Tick(delta);
            pending += delta;
            if (pending < .1f) return;
            float elapsed = Mathf.Min(pending, .2f); pending = 0;
            if (!PrototypeRegionView.HarassTargetAvailable(target) || now >= chaseEnds ||
                (target.Body.position - transform.position).sqrMagnitude > 32f * 32f)
            {
                target = null; hasSegment = false;
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
                if (view.FreightClear(point, destination, .15f)) view.ScarePedestrian(target, point, now, TestMode);
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
            var next = Vector2.MoveTowards(point, segmentEnd, 1.2f * elapsed);
            transform.position = new Vector3(next.x, view.HarassGroundHeight(next), next.y);
        }
    }
}
