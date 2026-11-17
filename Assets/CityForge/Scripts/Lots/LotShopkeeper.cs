using System;
using System.Linq;
using UnityEngine;
using CityForge.World;

namespace CityForge.Lots
{
    internal sealed class LotShopkeeper
    {
        private const float InsideWait = 8f;
        private const float LookDuration = 5f;
        private const float WalkSpeed = 1.05f;

        private enum Stage { Inside, Leaving, Looking, Returning }

        private readonly Transform view;
        private readonly Animator animator;
        private readonly Renderer[] renderers;
        private readonly Func<Vector2, float> height;
        private readonly Vector2 door, outside, forward;
        private readonly string idle;
        private readonly Transform head;
        private readonly Quaternion headRest;
        private readonly (Transform upper, Transform forearm, Transform hand, float side)[] arms;
        private Stage stage;
        private float timer;
        private string animation;
        private bool visible = true;

        internal LotShopkeeper(Transform view, LotAssetCatalogEntry asset, Vector2 door, Vector2 forward,
            Func<Vector2, float> height, bool preview = false)
        {
            this.view = view;
            this.door = door;
            this.forward = forward;
            this.height = height;
            outside = door + forward * 1.25f;
            idle = string.IsNullOrEmpty(asset.idleState) ? "Idle" : asset.idleState;
            animator = view.GetComponentInChildren<Animator>();
            renderers = view.GetComponentsInChildren<Renderer>().Where(renderer => renderer.enabled).ToArray();
            var bones = view.GetComponentsInChildren<Transform>();
            head = Find("Head", "Head");
            if (head) headRest = head.localRotation;
            arms = new[]
            {
                (Find("LeftArm", "UpperArm.L"), Find("LeftForeArm", "Forearm.L"), Find("LeftHand", "Hand.L"), 1f),
                (Find("RightArm", "UpperArm.R"), Find("RightForeArm", "Forearm.R"), Find("RightHand", "Hand.R"), -1f)
            };
            timer = preview ? .55f : 2f + UnityEngine.Random.value * 5f;
            if (!preview)
            {
                Place(door, forward);
                Show(false);
            }

            Transform Find(string mixamo, string alternate) => bones.FirstOrDefault(bone =>
                bone.name == mixamo || bone.name.EndsWith(":" + mixamo, StringComparison.Ordinal) || bone.name == alternate);
        }

        internal static bool TryEntrance(LotDefinition lot, LotAssetCatalog catalog, Vector2 near,
            LotObjectDefinition previewBuilding, out Vector2 door, out Vector2 forward)
        {
            var bestDoor = Vector2.zero;
            var bestForward = Vector2.zero;
            float best = float.PositiveInfinity;
            var layout = lot.zoningLayout;
            if (layout.Configured && layout.hasEntrance)
                Consider(layout.entrance, LotGroundOverlay.QuarterTurn(Vector2.down,
                    (int)layout.frontage + (layout.faceAwayFromRoad ? 2 : 0)));
            foreach (var item in lot.objects) ConsiderBuilding(item);
            if (previewBuilding != null) ConsiderBuilding(previewBuilding);
            if (float.IsPositiveInfinity(best))
            {
                foreach (var item in lot.objects) ConsiderBuilding(item, infer: true);
                if (previewBuilding != null) ConsiderBuilding(previewBuilding, infer: true);
            }
            door = bestDoor;
            forward = bestForward;
            return best < float.PositiveInfinity;

            void ConsiderBuilding(LotObjectDefinition item, bool infer = false)
            {
                var asset = catalog ? catalog.Find(item.assetId) : null;
                if (asset?.category != "Buildings") return;
                var info = LotBuildingInfo.LoadEntry(item.assetId);
                if (info != null && info.entryType != LotEntryType.Pedestrian) return;
                if (info == null)
                {
                    if (!infer || (asset.growableZone != TownZone.Commercial1 &&
                        LotClassification.FindCategory(asset.group)?.id != "commercial")) return;
                    var side = LotGroundOverlay.QuarterTurn(Vector2.down,
                        -Mathf.RoundToInt(LotGrowableBuildings.FrontageOffset(asset.id) / 90f));
                    info = new LotBuildingInfo
                    {
                        entrance = Vector2.Scale(side, asset.footprint) * .5f,
                        facing = (LotFrontage)((4 - Mathf.RoundToInt(LotGrowableBuildings.FrontageOffset(asset.id) / 90f)) % 4)
                    };
                }
                var rotation = Quaternion.Euler(0f, item.yaw, 0f);
                var offset = rotation * new Vector3(info.entrance.x, 0, info.entrance.y) * item.scale;
                var facing = LotGroundOverlay.QuarterTurn(Vector2.down, (int)info.facing);
                var normal = rotation * new Vector3(facing.x, 0, facing.y);
                Consider(item.position + new Vector2(offset.x, offset.z), new Vector2(normal.x, normal.z));
            }

            void Consider(Vector2 position, Vector2 direction)
            {
                float distance = (near - position).sqrMagnitude;
                if (distance > best) return;
                best = distance;
                bestDoor = position;
                bestForward = direction.normalized;
            }
        }

        internal void Tick(float delta)
        {
            if (delta <= 0f) return;
            switch (stage)
            {
                case Stage.Inside:
                    timer -= delta;
                    if (timer > 0f) break;
                    Place(door, forward);
                    Show(true);
                    Animate("Walk");
                    stage = Stage.Leaving;
                    break;
                case Stage.Leaving:
                    if (!WalkTo(outside, forward, delta)) break;
                    timer = LookDuration;
                    stage = Stage.Looking;
                    break;
                case Stage.Looking:
                    timer -= delta;
                    if (timer <= 0f)
                    {
                        Animate("Walk");
                        stage = Stage.Returning;
                        break;
                    }
                    bool hasFold = Animate("FoldArms");
                    float progress = 1f - timer / LookDuration;
                    float scan = Mathf.Sin(progress * Mathf.PI * 2f) * 27f;
                    Place(outside, forward);
                    if (!hasFold) FoldArms(Mathf.SmoothStep(0f, 1f, Mathf.Min(progress, 1f - progress) * 9f));
                    if (head) head.localRotation =
                        Quaternion.AngleAxis(scan, head.parent.InverseTransformDirection(view.up)) * headRest;
                    else view.localRotation *= Quaternion.Euler(0f, scan, 0f);
                    break;
                case Stage.Returning:
                    if (!WalkTo(door, -forward, delta)) break;
                    Show(false);
                    Animate(idle);
                    timer = InsideWait;
                    stage = Stage.Inside;
                    break;
            }
        }

        internal void ShowForEditing()
        {
            Show(true);
            stage = Stage.Inside;
            timer = .55f;
            Animate(idle);
        }

        private bool WalkTo(Vector2 target, Vector2 direction, float delta)
        {
            var location = view.localPosition;
            var next = Vector2.MoveTowards(new Vector2(location.x, location.z), target, delta * WalkSpeed);
            Place(next, direction);
            return next == target;
        }

        private void Place(Vector2 point, Vector2 direction)
        {
            view.localPosition = new Vector3(point.x, height(point), point.y);
            view.localRotation = Quaternion.Euler(0f, Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 0f);
        }

        private void Show(bool show)
        {
            if (visible == show) return;
            visible = show;
            foreach (var renderer in renderers)
                if (renderer) renderer.enabled = show;
        }

        private bool Animate(string desired)
        {
            if (!animator || !animator.runtimeAnimatorController || animator.layerCount == 0) return false;
            string prefix = animator.GetLayerName(0) + ".";
            bool supported = animator.HasState(0, Animator.StringToHash(prefix + desired));
            var next = supported ? desired : idle;
            if (animation != next && animator.HasState(0, Animator.StringToHash(prefix + next)))
            {
                animation = next;
                animator.Play(Animator.StringToHash(prefix + next), 0, 0);
                animator.Update(0);
            }
            return supported;
        }

        private void FoldArms(float blend)
        {
            foreach (var (upper, forearm, hand, side) in arms)
            {
                if (!upper || !forearm || !hand) continue;
                float outward = Mathf.Sign(Vector3.Dot(upper.position - view.position, view.right));
                if (outward == 0f) outward = side;
                Aim(upper, forearm, new Vector3(outward * .20f, -.89f, .43f), blend);
                Aim(forearm, hand, new Vector3(-outward * .93f, .29f, .13f + outward * .09f), blend);
            }
        }

        private void Aim(Transform joint, Transform tip, Vector3 direction, float blend)
        {
            var current = tip.position - joint.position;
            var target = view.TransformDirection(direction);
            if (current.sqrMagnitude > .000001f)
                joint.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(current, target), blend) *
                    joint.rotation;
        }
    }
}
