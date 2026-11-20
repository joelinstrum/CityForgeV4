using System;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class DistrictFarmer : MonoBehaviour
    {
        private PrototypeRegionView view;
        private Camera districtCamera;
        private Animator animator;
        private DistrictLotRecord lot;
        internal Func<Vector2, Vector2, bool> clearRoute;
        internal Func<Vector2, float> ground;
        private Vector2 home, target;
        private float wait = 1f;
        private int patch;
        private bool walking;
        private string animationState;
        private ActorSimulationCadence cadence;
        internal bool Walking => walking;
        internal void Initialize(PrototypeRegionView source, Camera camera, DistrictLotRecord record, Vector2 anchor)
        {
            view = source; districtCamera = camera; lot = record; home = anchor;
            clearRoute = source.PatrolClear; ground = source.FarmerGround;
            animator = GetComponentInChildren<Animator>();
            if (animator) { animator.applyRootMotion = false; animator.Rebind(); animator.Update(0); }
            Animate("Idle");
        }
        private void Update()
        {
            if (!view || view.DistrictLoading || view.IsSaving) return;
            float elapsed = cadence.Step(Mathf.Min(Time.deltaTime, .1f),
                ActorSimulationVisibility.NearCamera(districtCamera, transform.position), GetInstanceID());
            if (elapsed > 0) Tick(elapsed);
        }
        internal void Tick(float elapsed)
        {
            if (walking)
            {
                var point = new Vector2(transform.position.x, transform.position.z);
                var next = Vector2.MoveTowards(point, target, elapsed * .9f);
                if (!clearRoute(point, next)) { walking = false; wait = 1f; Animate("Idle"); return; }
                var delta = next - point;
                if (delta.sqrMagnitude > .000001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(new Vector3(delta.x, 0, delta.y)), elapsed * 180);
                transform.position = new Vector3(next.x, ground(next), next.y);
                if ((next - target).sqrMagnitude < .01f)
                { walking = false; wait = 7f; Animate("Hoe"); }
                return;
            }
            wait -= elapsed;
            if (wait > 0) return;
            var bounds = lot.definition.LandBounds;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int index = patch++ % 12;
                var local = home + new Vector2((index % 4 - 1.5f) * 2f, (index / 4 - 1) * 2.5f);
                local.x = Mathf.Clamp(local.x, bounds.xMin + .7f, bounds.xMax - .7f);
                local.y = Mathf.Clamp(local.y, bounds.yMin + .7f, bounds.yMax - .7f);
                var rotated = Quaternion.Euler(0, lot.FacingDegrees, 0) * new Vector3(local.x, 0, local.y);
                var candidate = lot.center + new Vector2(rotated.x, rotated.z);
                var from = new Vector2(transform.position.x, transform.position.z);
                if ((candidate - from).sqrMagnitude < .5f || !clearRoute(from, candidate)) continue;
                target = candidate; walking = true; Animate("Walk"); return;
            }
            wait = 3; Animate("Hoe");
        }
        private void Animate(string state)
        {
            if (!animator || !animator.runtimeAnimatorController || animator.layerCount == 0 || animationState == state) return;
            string path = animator.GetLayerName(0) + "." + state;
            if (!animator.HasState(0, Animator.StringToHash(path))) return;
            animationState = state; animator.CrossFadeInFixedTime(path, .15f);
        }
    }
    public sealed partial class PrototypeRegionView
    {
        internal float FarmerGround(Vector2 point) => LotHeight(point);
    }
}
