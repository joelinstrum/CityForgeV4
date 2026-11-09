using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class HarborDockWorkers : MonoBehaviour
    {
        private sealed class Worker
        {
            internal Transform Actor, Crate;
            internal Animator Animator;
            internal Vector3 Pickup, Drop;
            internal int Idle, Walk, State;
            internal string Id;
            internal float StartsAt, EndsAt;
        }

        private readonly List<Worker> workers = new();
        private Material wood;
        private PrototypeRegionView view;
        private DistrictLotRecord port;
        private float elapsed;
        internal float VisitSeconds { get; private set; }

        internal static HarborDockWorkers Begin(Transform vessel, PrototypeRegionView view, DistrictLotRecord port, HarborVesselKind kind)
        {
            var catalog = LotAssetCatalog.Load();
            if (!catalog) return null;
            var definition = port.definition;
            if (port.customization != null)
            {
                try { definition = LotCustomizationService.Apply(definition, port.customization, catalog); }
                catch (System.IO.InvalidDataException) { /* Use the authored lot if a cosmetic edit is invalid. */ }
            }

            HarborDockWorkers crew = null;
            foreach (var item in definition.objects)
            {
                if (item.characterScript.scriptedBehavior != LotScriptedBehavior.DockWorker) continue;
                var asset = catalog.Find(item.assetId);
                if (asset == null || !asset.IsCharacter || !asset.prefab) continue;
                if (!crew)
                {
                    var root = new GameObject("Dock workers on visiting ship");
                    root.transform.SetParent(vessel, false);
                    crew = root.AddComponent<HarborDockWorkers>();
                    crew.view = view;
                    crew.port = port;
                }
                crew.AddWorker(item, asset, kind);
                if (crew.workers.Count == 2) break;
            }
            return crew;
        }

        private void AddWorker(LotObjectDefinition item, LotAssetCatalogEntry asset, HarborVesselKind kind)
        {
            // Measured deck lanes avoid the masts, raised forecastles and gunwales.
            var (x, y, start, end) = kind switch
            {
                HarborVesselKind.Galleon => (1.1f, 2.08f, -1.1f, 1.8f),
                HarborVesselKind.PirateShip => (1.25f, 1.15f, -3.8f, -1.8f),
                _ => (.68f, .87f, -2f, 2.4f)
            };
            if (workers.Count > 0) x = -x;
            var actor = LotRuntimeLoader.InstantiateObject(item, asset, transform).transform;
            var animator = actor.GetComponentInChildren<Animator>();
            var worker = new Worker
            {
                Actor = actor, Animator = animator, Pickup = new Vector3(x, y, start),
                Drop = new Vector3(x, y, end), Id = item.id,
                StartsAt = item.characterScript.dockArrivalDelay,
                EndsAt = item.characterScript.dockArrivalDelay + item.characterScript.DockWorkSeconds
            };
            if (animator && animator.runtimeAnimatorController && animator.layerCount > 0)
            {
                string layer = animator.GetLayerName(0) + ".";
                worker.Idle = Animator.StringToHash(layer + (string.IsNullOrEmpty(asset.idleState) ? "Idle" : asset.idleState));
                worker.Walk = Animator.StringToHash(layer + "Walk");
            }

            if (!item.characterScript.dockIdleOnly)
            {
                if (!wood)
                    wood = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.48f, .29f, .13f) };
                var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crate.name = "Cargo crate";
                var collider = crate.GetComponent<Collider>();
                collider.enabled = false;
                Destroy(collider);
                crate.GetComponent<MeshRenderer>().sharedMaterial = wood;
                crate.transform.SetParent(transform, false);
                crate.transform.localScale = new Vector3(.46f, .36f, .39f) * item.scale;
                worker.Crate = crate.transform;
            }

            workers.Add(worker);
            VisitSeconds = Mathf.Max(VisitSeconds, worker.EndsAt);
            UpdateWorker(worker);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            foreach (var worker in workers) UpdateWorker(worker);
        }

        private void UpdateWorker(Worker worker)
        {
            bool working = elapsed >= worker.StartsAt && elapsed < worker.EndsAt;
            if (worker.Actor.gameObject.activeSelf != working) worker.Actor.gameObject.SetActive(working);
            if (worker.Crate && worker.Crate.gameObject.activeSelf != working) worker.Crate.gameObject.SetActive(working);
            var home = view ? view.DockWorkerHome(port, worker.Id) : null;
            if (home && home.activeSelf == working) home.SetActive(!working);
            if (!working) return;

            var actor = worker.Actor;
            if (!worker.Crate)
            {
                actor.localPosition = worker.Pickup;
                actor.localRotation = Quaternion.identity;
                Animate(worker, worker.Idle);
                return;
            }

            float stride = Vector3.Distance(worker.Pickup, worker.Drop) / 1.15f;
            float phase = Mathf.Repeat(elapsed - worker.StartsAt, 2 * stride + 2f);
            float carry = Mathf.Clamp01((phase - 1f) / stride);
            float back = Mathf.Clamp01((phase - stride - 2f) / stride);
            bool returning = phase >= stride + 2f;
            bool walking = (carry > 0 && carry < 1) || (back > 0 && back < 1);
            actor.localPosition = Vector3.Lerp(worker.Pickup, worker.Drop, returning ? 1 - back : carry);
            actor.localRotation = returning ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

            // Lift, carry, and set down the crate, then walk back for another load.
            float height = Mathf.Clamp01(Mathf.Min(phase, stride + 2f - phase)) * .85f;
            var cargoPoint = returning ? worker.Drop : actor.localPosition;
            worker.Crate.localPosition = cargoPoint + new Vector3(0, (.18f + height) * actor.localScale.y, .34f);
            Animate(worker, walking ? worker.Walk : worker.Idle);
        }

        private static void Animate(Worker worker, int state)
        {
            if (state == worker.State || !worker.Animator || !worker.Animator.HasState(0, state)) return;
            worker.State = state;
            worker.Animator.CrossFadeInFixedTime(state, .12f);
        }

        private void OnDisable()
        {
            if (!view) return;
            foreach (var worker in workers)
            {
                var home = view.DockWorkerHome(port, worker.Id);
                if (home) home.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (wood) Destroy(wood);
        }
    }
}
