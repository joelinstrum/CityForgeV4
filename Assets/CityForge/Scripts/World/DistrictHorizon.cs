using CityForge.CameraSystem;
using UnityEngine;

namespace CityForge.World
{
    [DefaultExecutionOrder(50)]
    internal sealed class DistrictHorizon : MonoBehaviour
    {
        const int Bearings = 256;
        const float FullSkyPitch = 15f;
        const float HiddenSkyPitch = 27f;
        static readonly int Visibility = Shader.PropertyToID("_Visibility");

        PrototypeRegionView region;
        CityForgeCameraController rig;
        Camera districtCamera;
        Skybox skybox;
        Material material;
        Texture2D skyline;
        float heightRange;
        int sampledDistrict = -1;

        public void Initialize(PrototypeRegionView source, CityForgeCameraController controller, float terrainHeightRange)
        {
            var shader = Resources.Load<Shader>("Region/DistrictHorizon");
            if (!shader || !shader.isSupported)
            {
                enabled = false;
                return;
            }

            region = source;
            rig = controller;
            heightRange = terrainHeightRange;
            districtCamera = GetComponent<Camera>();
            material = new Material(shader) { name = "District horizon backdrop" };
            material.SetColor("_ClearColor", districtCamera.backgroundColor);
            skybox = gameObject.AddComponent<Skybox>();
            skybox.material = material;
            skybox.enabled = false;
        }

        void LateUpdate()
        {
            float visibility = region.SelectedDistrict >= 0 && !region.DistrictLoading
                ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FullSkyPitch, HiddenSkyPitch, rig.ViewPitch))
                : 0f;
            if (visibility > 0f)
            {
                if (sampledDistrict != region.SelectedDistrict)
                    BuildSkyline();
                material.SetFloat(Visibility, visibility);
            }

            bool visible = visibility > 0f;
            skybox.enabled = visible;
            districtCamera.clearFlags = visible ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        }

        void BuildSkyline()
        {
            if (!skyline)
            {
                skyline = new Texture2D(Bearings, 1, TextureFormat.RGBA32, false, true)
                {
                    name = "Regional horizon elevations",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear
                };
                material.SetTexture("_Skyline", skyline);
            }

            var bounds = region.ActiveDistrictBounds;
            float side = region.DistrictSide;
            float firstDistance = .5f * bounds.size.magnitude + .35f * side;
            var samples = new Color32[Bearings];
            for (int i = 0; i < Bearings; i++)
            {
                float angle = ((i + .5f) / Bearings - .5f) * 2f * Mathf.PI;
                var direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                float nearer = 0f;
                float farther = 0f;
                for (int step = 0; step < 24; step++)
                {
                    float distance = firstDistance + step * side * .35f;
                    var uv = (bounds.center + direction * distance) / region.MapSide + Vector2.one * .5f;
                    if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) break;

                    float elevation = SmoothRiverSurface.Sample(region.Data.Heights, uv.x, uv.y) * heightRange;
                    float aboveSea = elevation - region.SeaLevel;
                    if (!region.Waterless && aboveSea <= .5f) continue;

                    // Give low country a shallow profile and keep tall ridges below the cloud layer.
                    float silhouette = Mathf.Clamp01(.15f + Mathf.Max(0f, aboveSea) / distance * 4f);
                    if (step < 5) nearer = Mathf.Max(nearer, silhouette);
                    else farther = Mathf.Max(farther, silhouette);
                }

                samples[i] = new Color32((byte)Mathf.RoundToInt(nearer * 255f),
                    (byte)Mathf.RoundToInt(farther * 255f), 0, 255);
            }

            skyline.SetPixels32(samples);
            skyline.Apply(false);
            sampledDistrict = region.SelectedDistrict;
        }

        void OnDestroy()
        {
            if (skyline) Destroy(skyline);
            if (material) Destroy(material);
        }
    }
}
