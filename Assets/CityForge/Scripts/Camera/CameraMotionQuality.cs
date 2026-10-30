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
        Vector3 previousPosition;
        Quaternion previousRotation;
        float previousSize, previousFov, lastMotion;
        float originalLodBias, originalRenderScale;
        UniversalRenderPipelineAsset pipeline;
        bool initialized;
        public bool ReducedQuality { get; private set; }
        public bool SuspendOptionalWork {get;set;}=true;
        public bool NavigationWorkSuspended=>SuspendOptionalWork&&ReducedQuality;

        void OnEnable() { view = GetComponent<Camera>(); initialized = false; }
        void LateUpdate()
        {
            if (!view || !view.enabled) { Restore(); initialized = false; return; }
            bool moved = initialized && ((transform.position - previousPosition).sqrMagnitude > .000001f
                || Quaternion.Angle(transform.rotation, previousRotation) > .001f
                || Mathf.Abs(view.orthographicSize - previousSize) > .0001f
                || Mathf.Abs(view.fieldOfView - previousFov) > .0001f);
            previousPosition = transform.position; previousRotation = transform.rotation;
            previousSize = view.orthographicSize; previousFov = view.fieldOfView; initialized = true;
            if (moved) { lastMotion = Time.unscaledTime; Reduce(); }
            else if (ReducedQuality && Time.unscaledTime - lastMotion >= SettleSeconds) Restore();
        }
        void Reduce()
        {
            if (ReducedQuality) return;
            pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            originalLodBias = QualitySettings.lodBias;
            originalRenderScale = pipeline ? pipeline.renderScale : 1f;
            // Render about 56% as many pixels; screen-space UI retains its native resolution.
            if (pipeline) pipeline.renderScale = Mathf.Max(.1f, originalRenderScale * .75f);
            QualitySettings.lodBias = Mathf.Max(.01f, originalLodBias * .5f);
            ReducedQuality = true;
        }
        void Restore()
        {
            if (!ReducedQuality) return;
            QualitySettings.lodBias = originalLodBias;
            if (pipeline) pipeline.renderScale = originalRenderScale;
            pipeline = null; ReducedQuality = false;
        }
        void OnDisable() { Restore(); initialized = false; }
        void OnDestroy() { Restore(); }
    }
}
