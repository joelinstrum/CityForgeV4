using CityForge.CameraSystem;
using CityForge.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal sealed class RegionCameraGizmo : MonoBehaviour, IDragHandler
    {
        private static readonly Vector3[] Axes = { Vector3.right, Vector3.up, Vector3.forward };
        private static readonly Color32[] Colors =
        {
            new(219, 91, 83, 255), new(102, 191, 107, 255), new(92, 155, 231, 255)
        };
        private readonly RectTransform[] arms = new RectTransform[3], handles = new RectTransform[3];
        private PrototypeRegionView view;
        private CityForgeCameraController rig;
        private Camera mapCamera;
        private CanvasGroup visibility;

        internal static void Create(Transform parent, PrototypeRegionView view, Camera camera, Font font)
        {
            var root = new GameObject("Region camera axes", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            root.GetComponent<Image>().color = Color.clear;
            var control = root.AddComponent<RegionCameraGizmo>();
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-106f, 110f);
            rect.sizeDelta = new Vector2(170f, 170f);
            control.view = view;
            control.mapCamera = camera;
            control.rig = camera.GetComponent<CityForgeCameraController>();
            control.visibility = root.GetComponent<CanvasGroup>();

            for (int i = 0; i < Axes.Length; i++)
            {
                var line = AddImage(root.transform, "Axis", Colors[i]);
                line.rectTransform.pivot = new Vector2(0f, .5f);
                line.raycastTarget = false;
                control.arms[i] = line.rectTransform;

                var face = AddImage(root.transform, "View along " + "XYZ"[i], Colors[i]);
                face.rectTransform.sizeDelta = new Vector2(32f, 30f);
                var drag = face.gameObject.AddComponent<RegionCameraAxisButton>();
                drag.Initialize(control, i);
                control.handles[i] = face.rectTransform;
                AddLabel(face.transform, "XYZ"[i].ToString(), font, 18);
            }

            var caption = AddLabel(root.transform, "Drag to orbit · Click an axis", font, 14);
            caption.rectTransform.anchoredPosition = new Vector2(0f, -62f);
            caption.rectTransform.sizeDelta = new Vector2(198f, 24f);
        }

        private void LateUpdate()
        {
            bool visible = view && view.CanOpenDistrictContext && rig;
            visibility.alpha = visible ? 1f : 0f;
            visibility.blocksRaycasts = visible && rig.enabled;
            if (!visible) return;

            for (int i = 0; i < Axes.Length; i++)
            {
                var axis = ScreenAxis(i);
                if (axis.sqrMagnitude < .01f) axis = Vector2.down;
                var end = axis.normalized * Mathf.Lerp(29f, 58f, Mathf.Clamp01(axis.magnitude));
                arms[i].sizeDelta = new Vector2(end.magnitude, 3f);
                arms[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(end.y, end.x) * Mathf.Rad2Deg);
                handles[i].anchoredPosition = end;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && visibility.blocksRaycasts && Application.isFocused)
                rig.OrbitRegion(eventData.delta);
        }

        internal void SnapAxis(int axis)
        {
            if (visibility.blocksRaycasts && Application.isFocused) rig.SnapRegionAxis(Axes[axis]);
        }

        private Vector2 ScreenAxis(int axis) => new Vector2(Vector3.Dot(Axes[axis], mapCamera.transform.right),
            Vector3.Dot(Axes[axis], mapCamera.transform.up));

        private static Image AddImage(Transform parent, string name, Color color)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image));
            node.transform.SetParent(parent, false);
            var image = node.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text AddLabel(Transform parent, string label, Font font, int size)
        {
            var node = new GameObject(label, typeof(RectTransform), typeof(Text));
            node.transform.SetParent(parent, false);
            var text = node.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
    }

    internal sealed class RegionCameraAxisButton : MonoBehaviour, IPointerClickHandler
    {
        private RegionCameraGizmo gizmo;
        private int axis;

        internal void Initialize(RegionCameraGizmo owner, int index)
        {
            gizmo = owner;
            axis = index;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                gizmo.SnapAxis(axis);
        }
    }
}
