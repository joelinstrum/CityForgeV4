using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.CameraSystem
{
    /// <summary>Transient navigation quality; never changes terrain data or authored materials.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public sealed class CameraMotionQuality : MonoBehaviour
    {
        const float SettleSeconds = .2f;
        Camera view;
        CityForgeCameraController controller;
        Vector3 previousPosition;
        Quaternion previousRotation;
        float previousSize, previousFov, lastMotion;
        float originalLodBias, originalRenderScale, originalCascadeSplit;
        int originalCascades;
        bool reducedWide;
        UniversalRenderPipelineAsset pipeline;
        bool initialized;
        public bool ReducedQuality { get; private set; }
        public event System.Action<bool> QualityChanged;
        internal bool ReduceAuthoredDetailDuringNavigation { get; set; } = true;
        public bool SuspendOptionalWork {get;set;}=true;
        public bool NavigationWorkSuspended=>SuspendOptionalWork&&ReducedQuality;

        void OnEnable() { view = GetComponent<Camera>(); controller = GetComponent<CityForgeCameraController>(); initialized = false; }
        void LateUpdate()
        {
            if (!view || !view.enabled) { Restore(); initialized = false; return; }
            bool moved = initialized && ((transform.position - previousPosition).sqrMagnitude > .000001f
                || Quaternion.Angle(transform.rotation, previousRotation) > .001f
                || Mathf.Abs(view.orthographicSize - previousSize) > .0001f
                || Mathf.Abs(view.fieldOfView - previousFov) > .0001f);
            previousPosition = transform.position; previousRotation = transform.rotation;
            previousSize = view.orthographicSize; previousFov = view.fieldOfView; initialized = true;
            if (controller && controller.FlyingIntoDistrict) moved = true;
            if (moved) { lastMotion = Time.unscaledTime; Reduce(); }
            else if (ReducedQuality && Time.unscaledTime - lastMotion >= SettleSeconds) Restore();
        }
        void Reduce()
        {
            bool wide = ReduceAuthoredDetailDuringNavigation && controller && controller.ZoomLevel >= 3;
            if (ReducedQuality && reducedWide == wide) return;
            bool notify = !ReducedQuality;
            if (notify)
            {
                pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                originalLodBias = QualitySettings.lodBias;
                originalRenderScale = pipeline ? pipeline.renderScale : 1f;
                originalCascades = pipeline ? pipeline.shadowCascadeCount : 1;
                originalCascadeSplit = pipeline ? pipeline.cascade2Split : .5f;
            }
            // Keep close panning at its accepted quality. Wider views can use cheaper
            // geometry, fewer shadow passes and 65% resolution until navigation settles.
            if (pipeline)
            {
                pipeline.renderScale = Mathf.Max(.1f, originalRenderScale * (wide ? .65f : .75f));
                pipeline.shadowCascadeCount = wide ? Mathf.Min(originalCascades, 2) : originalCascades;
                pipeline.cascade2Split = wide ? .5f : originalCascadeSplit;
            }
            QualitySettings.lodBias = Mathf.Max(.01f, originalLodBias * (wide ? .4f : .5f));
            reducedWide = wide; ReducedQuality = true;
            if (notify) QualityChanged?.Invoke(true);
        }
        void Restore()
        {
            if (!ReducedQuality) return;
            QualitySettings.lodBias = originalLodBias;
            if (pipeline)
            {
                pipeline.renderScale = originalRenderScale;
                pipeline.shadowCascadeCount = originalCascades;
                pipeline.cascade2Split = originalCascadeSplit;
            }
            pipeline = null; ReducedQuality = false; reducedWide = false;
            QualityChanged?.Invoke(false);
        }
        void OnDisable() { Restore(); initialized = false; }
        void OnDestroy() { Restore(); }
    }
}
