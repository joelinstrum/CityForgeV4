using System.Collections.Generic;
using CityForge.World;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CityForge.UI
{
    public sealed class WaterRemovalTool : MonoBehaviour
    {
        PrototypeRegionView view;
        NatureToolbar toolbar;
        Camera mapCamera;
        TerrainCollider terrain;
        bool armed, cursorWasVisible, colliderWasEnabled;
        bool showCursor, waitRelease;
        readonly List<RaycastResult> uiHits = new();
        PointerEventData pointerData;
        RegionWaterBody target;
        GUIStyle removalCursor, hint;

        public void Initialize(PrototypeRegionView source, NatureToolbar tools)
        {
            view = source;
            toolbar = tools;
            mapCamera = view.GetComponentInChildren<Camera>();
            toolbar.ToolSelected += _ => Cancel();
            toolbar.Popup.OptionChanged += _ => Cancel();
            toolbar.Popup.WaterRemovalRequested += Arm;
        }

        void Arm()
        {
            if (armed || view.DistrictLoading || view.IsSaving || !view.ApprovedDistrict) return;
            terrain = view.ApprovedDistrict.DisplayTerrain.GetComponent<TerrainCollider>();
            colliderWasEnabled = terrain.enabled;
            terrain.enabled = true;
            cursorWasVisible = Cursor.visible;
            armed = true;
            waitRelease = Input.GetMouseButton(0);
            toolbar.Popup.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!armed) return;
            if (!view || !terrain || view.DistrictLoading || view.IsSaving || toolbar.Selected != NatureTool.Water)
            {
                Cancel();
                return;
            }

            if (waitRelease && !Input.GetMouseButton(0)) waitRelease = false;
            showCursor = Application.isFocused && !OverUI();
            Cursor.visible = showCursor ? false : cursorWasVisible;
            if (!showCursor) return;
            var ray = mapCamera.ScreenPointToRay(Input.mousePosition);
            float groundDistance = terrain.Raycast(ray, out var ground, mapCamera.farClipPlane)
                ? ground.distance : mapCamera.farClipPlane;
            target = view.ApprovedDistrict.PickPlayerWater(ray, groundDistance);
            if (waitRelease || target == null || !Input.GetMouseButtonDown(0)) return;

            if (view.RemovePlayerWater(target)) toolbar.Dismiss();
        }

        bool OverUI()
        {
            var events = EventSystem.current;
            if (!events) return false;
            pointerData ??= new PointerEventData(events);
            pointerData.position = Input.mousePosition;
            uiHits.Clear();
            events.RaycastAll(pointerData, uiHits);
            return uiHits.Count > 0;
        }

        void OnGUI()
        {
            if (!armed || Event.current.type != EventType.Repaint) return;
            hint ??= new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            GUI.Box(new Rect((Screen.width - 430) * .5f, 16, 430, 34),
                target == null ? "Remove Water · Click a player-added water body · Esc to cancel"
                    : $"Click to remove this {(target.isStream ? "stream" : target.playerChannel ? "river" : "lake or pond")} · Esc to cancel", hint);
            if (!showCursor) return;
            removalCursor ??= new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var pointer = Event.current.mousePosition;
            var rect = new Rect(pointer.x - 16, pointer.y - 16, 32, 32);
            var previousColor = GUI.color;
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), "×", removalCursor);
            GUI.color = target != null ? new Color(1, .16f, .12f) : new Color(.85f, .24f, .21f);
            GUI.Label(rect, "×", removalCursor);
            GUI.color = previousColor;
        }

        void Cancel()
        {
            if (!armed) return;
            armed = showCursor = false;
            target = null;
            Cursor.visible = cursorWasVisible;
            if (terrain) terrain.enabled = colliderWasEnabled;
            terrain = null;
        }

        void OnDisable() => Cancel();
    }
}
