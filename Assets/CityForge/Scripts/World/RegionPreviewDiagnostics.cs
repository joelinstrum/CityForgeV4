using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    /// <summary>Render-only relief inspection. Source heights, collision, water and scene rig remain unchanged.</summary>
    public sealed class RegionPreviewDiagnostics : MonoBehaviour
    {
        private MeshRenderer land;
        private Material originalMaterial, grayMaterial;
        private Vector3 originalPosition, originalScale;
        private LineRenderer[] districtLines;
        private Vector3[][] originalLines;
        private float seaLevel = 12f, guideOffset = 10f;
        private Text toggleCaption;
        bool controlsEnabled;
        private Button[] scaleButtons;
        private Text[] scaleCaptions;
        public bool IsEnabled { get; private set; }
        public int Exaggeration { get; private set; } = 1;

        public void Initialize(MeshRenderer surface, LineRenderer[] lines, Transform hud, Font font, float waterLevel = 12f, float lineOffset = 10f, bool showControls = true)
        {
            seaLevel = waterLevel; guideOffset = lineOffset;controlsEnabled=showControls;
            land = surface;
            originalMaterial = land.sharedMaterial;
            originalPosition = land.transform.localPosition;
            originalScale = land.transform.localScale;
            grayMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                { name = "Temporary neutral gray relief diagnostic" };
            grayMaterial.SetColor("_BaseColor", new Color(.5f, .5f, .5f, 1f));
            grayMaterial.SetFloat("_Metallic", 0f);
            grayMaterial.SetFloat("_Smoothness", .05f);
            districtLines = lines;
            originalLines = new Vector3[lines.Length][];
            for (var i = 0; i < lines.Length; i++)
            {
                originalLines[i] = new Vector3[lines[i].positionCount];
                lines[i].GetPositions(originalLines[i]);
            }
            if(!showControls){SetDiagnostic(false,1);return;}
            var toggle = MakeButton(hud, font, "ReliefDiagnosticToggle", -86f, -230f, 205f, out toggleCaption);
            toggle.onClick.AddListener(() => SetDiagnostic(!IsEnabled, Exaggeration));
            scaleButtons = new Button[3]; scaleCaptions = new Text[3];
            var factors = new[] { 1, 2, 4 };
            for (var i = 0; i < factors.Length; i++)
            {
                var factor = factors[i];
                scaleButtons[i] = MakeButton(hud, font, "ReliefScale" + factor, -130f,
                    -230f + i * 71f, 63f, out scaleCaptions[i]);
                scaleButtons[i].onClick.AddListener(() => SetDiagnostic(true, factor));
            }
            SetDiagnostic(false, 1);
        }

        public void SetDiagnostic(bool enabled, int factor)
        {
            if (factor != 1 && factor != 2 && factor != 4)
                throw new System.ArgumentOutOfRangeException(nameof(factor));
            IsEnabled = enabled; Exaggeration = factor;
            var scale = enabled ? factor : 1;
            land.sharedMaterial = enabled ? grayMaterial : originalMaterial;
            // Exaggerate around sea level, retaining the exact river/coastal intersections.
            // Only the render transform changes; the generated mesh and TerrainData do not.
            land.transform.localScale = new Vector3(originalScale.x, originalScale.y * scale, originalScale.z);
            land.transform.localPosition = originalPosition + Vector3.up * (seaLevel * (1 - scale));
            for (var i = 0; i < districtLines.Length; i++)
            {
                var positions = (Vector3[])originalLines[i].Clone();
                if (scale != 1)
                    for (var j = 0; j < positions.Length; j++)
                        positions[j].y = seaLevel + (positions[j].y - seaLevel - guideOffset) * scale + guideOffset;
                districtLines[i].SetPositions(positions);
            }
            if(!toggleCaption)return;
            toggleCaption.text = enabled ? "GRAY RELIEF: ON  [G]" : "GRAY RELIEF: OFF  [G]";
            for (var i = 0; i < scaleButtons.Length; i++)
            {
                var value = i == 0 ? 1 : i == 1 ? 2 : 4;
                scaleButtons[i].interactable = enabled;
                scaleCaptions[i].text = value + "×";
                scaleButtons[i].GetComponent<Image>().color = enabled && value == factor
                    ? new Color(.38f, .36f, .29f, .98f) : new Color(.13f, .15f, .13f, .95f);
            }
        }

        private static Button MakeButton(Transform parent, Font font, string name,
            float top, float left, float width, out Text caption)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(left + width, top);
            rect.sizeDelta = new Vector2(width, 36f);
            go.GetComponent<Image>().color = new Color(.13f, .15f, .13f, .95f);
            var label = new GameObject("Caption", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            caption = label.GetComponent<Text>();
            caption.font = font; caption.fontSize = 15; caption.alignment = TextAnchor.MiddleCenter;
            caption.color = new Color(.93f, .88f, .72f); caption.raycastTarget = false;
            caption.rectTransform.anchorMin = Vector2.zero; caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = caption.rectTransform.offsetMax = Vector2.zero;
            return go.GetComponent<Button>();
        }

        private void Update()
        {
            if(!controlsEnabled)return;
            if (Input.GetKeyDown(KeyCode.G)) SetDiagnostic(!IsEnabled, Exaggeration);
            if (!IsEnabled) return;
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetDiagnostic(true, 1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetDiagnostic(true, 2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetDiagnostic(true, 4);
        }
        private void OnDestroy() { if (grayMaterial != null) Destroy(grayMaterial); }
    }
}
