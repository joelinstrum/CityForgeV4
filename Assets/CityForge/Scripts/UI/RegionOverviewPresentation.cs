using CityForge.CameraSystem;
using CityForge.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal sealed class RegionOverviewPresentation : MonoBehaviour
    {
        private PrototypeRegionView view;
        private Camera mapCamera;
        private GameObject controls, backdrop;
        private Material backdropMaterial;
        private float backdropAspect;

        internal static void Create(PrototypeRegionView view, CityForgeCameraController rig, Transform hud, Font font)
        {
            var presentation = view.gameObject.AddComponent<RegionOverviewPresentation>();
            presentation.view = view;
            presentation.mapCamera = rig.GetComponent<Camera>();
            presentation.CreateBackdrop();
            var toolbar = new GameObject("Overview camera toolbar", typeof(RectTransform));
            presentation.controls = toolbar;
            var rect = toolbar.GetComponent<RectTransform>();
            rect.SetParent(hud, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 38f);
            rect.sizeDelta = new Vector2(228f, 50f);
            Add("Zoom in", "zoom-in", "+", -81f, () => rig.StepZoom(true));
            Add("Zoom out", "zoom-out", "−", -27f, () => rig.StepZoom(false));
            Add("Change view angle", "view-angle", "◈", 27f, rig.ToggleViewAngle);
            Add("Reset camera", "reset-view", "↻", 81f, rig.ResetView);

            void Add(string name, string artwork, string fallback, float x, UnityEngine.Events.UnityAction action)
            {
                var face = LotEditorWorkspace.AddImage(name, toolbar.transform, new Color(.13f, .15f, .13f, .95f));
                face.rectTransform.anchoredPosition = new Vector2(x, 0f);
                face.rectTransform.sizeDelta = new Vector2(50f, 44f);
                face.raycastTarget = true;
                var button = face.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.onClick.AddListener(action);
                if (RegionBrassArtwork.SetButton(button, artwork)) return;
                var label = LotEditorWorkspace.AddText("Caption", face.transform, fallback, font, 22, TextAnchor.MiddleCenter);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private void CreateBackdrop()
        {
            var texture = Resources.Load<Texture2D>("UI/RegionBrass/backdrop");
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!texture || !shader) return;
            backdropAspect = (float)texture.width / texture.height;
            backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Region overview backdrop";
            backdrop.transform.SetParent(mapCamera.transform, false);
            Destroy(backdrop.GetComponent<Collider>());
            backdropMaterial = new Material(shader) { renderQueue = (int)RenderQueue.Background };
            backdropMaterial.SetTexture("_BaseMap", texture);
            backdropMaterial.SetFloat("_ZWrite", 0f);
            var renderer = backdrop.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = backdropMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
        }

        private void LateUpdate()
        {
            bool overview = view.SelectedDistrict < 0;
            controls.SetActive(overview && !view.DistrictLoading);
            if (!backdrop) return;
            backdrop.SetActive(overview);
            if (!overview) return;

            float depth = mapCamera.farClipPlane * .97f;
            var min = mapCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            var max = mapCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
            var diagonal = max - min;
            var cameraTransform = mapCamera.transform;
            float width = Vector3.Dot(diagonal, cameraTransform.right) * 1.01f;
            float height = Vector3.Dot(diagonal, cameraTransform.up) * 1.01f;
            float imageHeight = Mathf.Max(height, width / backdropAspect);
            backdrop.transform.position = (min + max) * .5f +
                cameraTransform.up * ((imageHeight - height) * .5f);
            backdrop.transform.localScale = new Vector3(imageHeight * backdropAspect, imageHeight, 1f);
        }

        private void OnDestroy()
        {
            if (backdropMaterial) Destroy(backdropMaterial);
        }
    }
}
