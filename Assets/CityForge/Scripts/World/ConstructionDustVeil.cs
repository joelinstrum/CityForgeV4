using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    // A fixed, prefilled curtain hides the site until its owner explicitly reveals it.
    internal sealed class ConstructionDustVeil : MonoBehaviour
    {
        private Material material;
        private ParticleSystem rising;
        private float revealAt = -1f;
        internal bool Revealing => revealAt >= 0f;
        internal static ConstructionDustVeil Create(Transform parent, DistrictLotRecord lot, float ground, float height)
        {
            var shader = Resources.Load<Shader>("Weather/Precipitation");
            if (!shader) return null;
            var host = new GameObject("Stationary construction dust veil");
            host.transform.SetParent(parent, false);
            host.transform.SetPositionAndRotation(new Vector3(lot.center.x, ground, lot.center.y), Quaternion.Euler(0, lot.FacingDegrees, 0));
            var veil = host.AddComponent<ConstructionDustVeil>();
            veil.material = new Material(shader) { name = "Construction dust curtain" };
            veil.material.SetColor("_BaseColor", new Color(.48f, .40f, .31f, .44f));
            var size = lot.definition.Size*.85f;
            float step = Mathf.Max(3f, Mathf.Max(size.x, size.y) / 8f);
            float top = Mathf.Max(3f, height*.75f);
            var still = veil.Emitter("Fixed billows");
            var main = still.main; main.maxParticles = 4096;
            var emission = still.emission; emission.enabled = false;
            still.Play();
            for (float y = .5f; y <= top; y += step * .7f)
            for (float z = -size.y * .5f + step * .25f; z < size.y * .5f; z += step)
            for (float x = -size.x * .5f + step * .25f; x < size.x * .5f; x += step)
                still.Emit(new ParticleSystem.EmitParams
                {
                    position = new Vector3(x, y, z), velocity = Vector3.zero,
                    startSize = step * 1.55f, startLifetime = 100000f,
                    startColor = new Color(1,1,1,.35f), rotation = (x * 37 + z * 19 + y * 13) % 360
                }, 1);
            var rising = veil.rising = veil.Emitter("Reveal dust wisps");
            main = rising.main; main.startLifetime = 2.5f; main.startSpeed = 0f;
            main.startSize = step * 1.8f; main.startColor = new Color(1, 1, 1, .35f);
            emission = rising.emission; emission.rateOverTime = 18; emission.enabled=false;
            var shape = rising.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x, .3f, size.y);
            var velocity = rising.velocityOverLifetime; velocity.enabled = true; velocity.y = 3f;
            var fade = rising.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.7f, .2f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            return veil;
        }
        private ParticleSystem Emitter(string name)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var system = child.AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSpeed = 0; main.maxParticles = 2048;
            var shape = system.shape; shape.enabled = false;
            var renderer = system.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.maxParticleSize = 1f; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return system;
        }
        internal void Reveal() {
            if(revealAt>=0)return;revealAt=Time.unscaledTime;
            foreach(var system in GetComponentsInChildren<ParticleSystem>()){
                var velocity=system.velocityOverLifetime;velocity.enabled=true;velocity.y=2.5f;
            }
            if(rising){var emission=rising.emission;emission.enabled=true;rising.Play();}
        }
        private void Update()
        {
            if (revealAt < 0) return;
            float alpha = 1f - Mathf.Clamp01((Time.unscaledTime - revealAt) / 1.2f);
            material.SetColor("_BaseColor", new Color(.48f, .40f, .31f, alpha * .44f));
            if (alpha <= 0) Destroy(gameObject);
        }
        private void OnDestroy() { if (material) Destroy(material); }
    }
}
