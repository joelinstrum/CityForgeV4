using System;
using System.Linq;
using UnityEngine;

namespace CityForge.Lots
{
    // A stationary, nonviolent crime gesture. Navigation and police response are separate systems.
    internal sealed class LotHarass
    {
        private readonly Transform view, spine;
        private Quaternion heading;
        private readonly Quaternion spineRest;
        private readonly Quaternion[] armRest;
        private readonly Animator animator;
        private readonly (Transform upper, Transform elbow, Transform hand, float side)[] arms;
        private float time;
        private bool animationStarted;

        internal static bool Supports(GameObject model)
        {
            if (!model) return false;
            var bones = model.GetComponentsInChildren<Transform>();
            return Find(bones, "L_Upperarm", "LeftArm", "UpperArm.L") &&
                Find(bones, "R_Upperarm", "RightArm", "UpperArm.R") &&
                Find(bones, "L_Forearm", "LeftForeArm", "Forearm.L") &&
                Find(bones, "R_Forearm", "RightForeArm", "Forearm.R") &&
                Find(bones, "L_Hand", "LeftHand", "Hand.L") && Find(bones, "R_Hand", "RightHand", "Hand.R");
        }

        internal LotHarass(Transform view, float? authoredYaw = null, bool startAnimation = true)
        {
            this.view = view;
            var unarmed = Resources.Load<Mesh>("Lots/V3Characters/Hooligan/HarassUnarmed");
            var hooligan = Resources.Load<GameObject>("Lots/V3Characters/Hooligan/Character");
            var armed = hooligan ? hooligan.GetComponentInChildren<SkinnedMeshRenderer>()?.sharedMesh : null;
            if (unarmed && armed)
                foreach (var renderer in view.GetComponentsInChildren<SkinnedMeshRenderer>())
                    if (renderer.sharedMesh == armed)
                        renderer.sharedMesh = unarmed;
            heading = authoredYaw.HasValue ? Quaternion.Euler(0, authoredYaw.Value, 0) : view.localRotation;
            var bones = view.GetComponentsInChildren<Transform>();
            spine = Find(bones, "Spine02", "Spine2", "Spine.002");
            spineRest = spine ? spine.localRotation : Quaternion.identity;
            arms = new[]
            {
                (Find(bones, "L_Upperarm", "LeftArm", "UpperArm.L"), Find(bones, "L_Forearm", "LeftForeArm", "Forearm.L"), Find(bones, "L_Hand", "LeftHand", "Hand.L"), -1f),
                (Find(bones, "R_Upperarm", "RightArm", "UpperArm.R"), Find(bones, "R_Forearm", "RightForeArm", "Forearm.R"), Find(bones, "R_Hand", "RightHand", "Hand.R"), 1f)
            };
            armRest = arms.SelectMany(arm => new[] { arm.Item1 ? arm.Item1.localRotation : Quaternion.identity, arm.Item2 ? arm.Item2.localRotation : Quaternion.identity }).ToArray();
            animator = view.GetComponentInChildren<Animator>();
            if (startAnimation) StartAnimation();
        }

        private void StartAnimation()
        {
            animationStarted = true;
            if (animator && animator.runtimeAnimatorController)
            {
                animator.applyRootMotion = false;
                int walk = Animator.StringToHash(animator.GetLayerName(0) + ".Walk");
                if (animator.HasState(0, walk)) animator.Play(walk, 0, 0);
                animator.speed = .75f;
                animator.Update(0);
            }
        }

        internal void Face(Quaternion worldRotation) => heading = view.parent
            ? Quaternion.Inverse(view.parent.rotation) * worldRotation : worldRotation;

        internal void Stop()
        {
            if (!animationStarted) return;
            animationStarted = false;
            view.localRotation = heading;
            if (spine) spine.localRotation = spineRest;
            for (int i = 0; i < arms.Length; i++)
            {
                if (arms[i].upper) arms[i].upper.localRotation = armRest[i * 2];
                if (arms[i].elbow) arms[i].elbow.localRotation = armRest[i * 2 + 1];
            }
            if (animator && animator.runtimeAnimatorController)
            {
                animator.speed = 1;
                int idle = Animator.StringToHash(animator.GetLayerName(0) + ".Idle");
                if (animator.HasState(0, idle)) animator.Play(idle, 0, 0);
                animator.Update(0);
            }
        }

        internal void Tick(float delta)
        {
            if (!animationStarted) StartAnimation();
            time += Mathf.Max(0, delta);
            float beat = time * 3.6f;
            view.localRotation = heading * Quaternion.Euler(0, 13f * Mathf.Sin(beat * .5f), 0);
            if (spine) spine.localRotation = spineRest * Quaternion.Euler(0, 0, 7f * Mathf.Sin(beat));
            foreach (var arm in arms)
            {
                if (!arm.upper || !arm.elbow || !arm.hand) continue;
                float wave = Mathf.Sin(beat + arm.side * .8f);
                var upperDirection = view.TransformDirection(new Vector3(arm.side * (.3f + .12f * wave), .9f, .14f));
                arm.upper.rotation = Quaternion.FromToRotation(arm.elbow.position - arm.upper.position, upperDirection) * arm.upper.rotation;
                var foreDirection = view.TransformDirection(new Vector3(-arm.side * .22f, .95f, .16f * wave));
                arm.elbow.rotation = Quaternion.FromToRotation(arm.hand.position - arm.elbow.position, foreDirection) * arm.elbow.rotation;
            }
        }

        private static Transform Find(Transform[] bones, params string[] names) => bones.FirstOrDefault(bone =>
            names.Any(name => bone.name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                bone.name.EndsWith(":" + name, StringComparison.OrdinalIgnoreCase)));
    }
}
