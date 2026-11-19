#if UNITY_EDITOR
using System;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
namespace CityForge.Editor
{
    public static class HarassChecks
    {
        public static void Run()
        {
            if ((int)LotScriptedBehavior.Lumberjack != 3 || (int)LotScriptedBehavior.Harass != 4)
                throw new Exception("Saved behavior IDs changed");
            var settings = new LotCharacterScript { configured = true, scriptedBehavior = LotScriptedBehavior.Harass };
            if (!settings.IsValid || JsonUtility.FromJson<LotCharacterScript>(JsonUtility.ToJson(settings)).scriptedBehavior != LotScriptedBehavior.Harass)
                throw new Exception("Harass save roundtrip failed");
            var asset = LotAssetCatalog.Load().Find("v3-hooligan");
            if (!LotHarass.Supports(asset.prefab)) throw new Exception("Hooligan rig not supported");
            var view = LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id }, asset, null);
            try
            {
                var animator = view.GetComponentInChildren<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var behavior = new LotHarass(view.transform);
                var unarmed = Resources.Load<Mesh>("Lots/V3Characters/Hooligan/HarassUnarmed");
                if (!unarmed || view.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh != unarmed)
                    throw new Exception("Harass did not use the unarmed mesh");
                var bones = view.GetComponentsInChildren<Transform>();
                var left = bones.Single(b => b.name == "L_Hand");
                var right = bones.Single(b => b.name == "R_Hand");
                var head = bones.Single(b => b.name == "Head");
                var origin = view.transform.position;
                var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                int raised = 0, lowered = 0;
                for (int i = 0; i < 600; i++)
                {
                    animator.Update(1f / 30f); behavior.Tick(1f / 30f);
                    float weight = LotHarass.GestureWeight((i + 1) / 30f);
                    if (weight == 0 && (left.position.y > head.position.y || right.position.y > head.position.y))
                        throw new Exception("Raised pose leaked into normal walking interval");
                    if (left.position.y > head.position.y && right.position.y > head.position.y) raised++;
                    if (left.position.y < head.position.y && right.position.y < head.position.y) lowered++;
                    min = Vector3.Min(min, left.position); max = Vector3.Max(max, left.position);
                    if (Vector3.Distance(origin, view.transform.position) > .001f) throw new Exception("Harass wandered from its assigned spot");
                }
                if (raised < 50 || lowered < 250) throw new Exception("Harass did not alternate brief raised-arm gestures with normal walking");
                if ((max - min).magnitude < .1f) throw new Exception("Harass pose did not dance");
                Debug.Log("HARASS CHECKS PASS: saved enum compatibility and script roundtrip; Hooligan supports rig; intermittent raised hands, lowered walking arms and varied gesture motion and stationary origin across 20 seconds.");
            }
            finally { UnityEngine.Object.DestroyImmediate(view); }
        }
    }
}
#endif
