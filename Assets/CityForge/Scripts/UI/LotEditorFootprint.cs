using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CityForge.UI
{
    internal sealed partial class LotEditorViewport
    {
        private Action<Rect> commitFootprint;
        private Action selectFootprint;
        private Func<Vector2, Vector2> snapFootprint;
        private bool footprintEditing, footprintSelected, footprintDragging;
        private int footprintHandle;
        private Rect footprintStart, footprintDraft;
        private Vector2 footprintDragStart;
        private Transform footprintRoot, footprintPreview;
        private Material footprintMaterial;

        private bool CanEditFootprint => footprintEditing && previewLot != null &&
            previewLot.zoningLayout.Configured && commitFootprint != null;

        public void ConfigureFootprintEditing(Action<Rect> commit, Func<Vector2, Vector2> snap, Action select)
        {
            commitFootprint = commit;
            snapFootprint = snap;
            selectFootprint = select;
        }

        public void EnableFootprintEditing(bool enabled)
        {
            footprintEditing = enabled;
            if (!enabled)
            {
                footprintSelected = false;
                SetFootprintCursor(-1);
            }
        }

        private readonly Texture2D[] footprintCursors = new Texture2D[5];
        private int activeFootprintCursor = -1;
        private bool footprintPointerInside;
        private Vector2 footprintPointer;
        private Camera footprintEventCamera;

        private void UpdateFootprintCursor()
        {
            if (!CanEditFootprint || (!footprintPointerInside && !footprintDragging))
            {
                SetFootprintCursor(-1);
                return;
            }
            int handle = footprintHandle;
            if (!footprintDragging)
            {
                if (!TryRay(footprintPointer, footprintEventCamera, out var ray) || !TryPoint(ray, out var point) ||
                    !TryHitFootprint(footprintPointer, footprintEventCamera, point, Pick(ray), out handle))
                {
                    SetFootprintCursor(-1);
                    return;
                }
            }
            SetFootprintCursor(handle < 0 ? 0 : FootprintResizeCursor(handle));
        }

        private int FootprintResizeCursor(int handle)
        {
            Vector2 direction;
            switch (handle)
            {
                case 1:
                case 5: direction = Vector2.up; break;
                case 3:
                case 7: direction = Vector2.right; break;
                case 0:
                case 4: direction = Vector2.one; break;
                default: direction = new Vector2(1, -1); break;
            }
            var origin = cameraView.WorldToViewportPoint(Origin);
            var endpoint = cameraView.WorldToViewportPoint(Origin + new Vector3(direction.x, 0, direction.y));
            var screen = Vector2.Scale(new Vector2(endpoint.x - origin.x, endpoint.y - origin.y),
                image.rectTransform.rect.size);
            int angle = Mathf.RoundToInt(Mathf.Atan2(screen.y, screen.x) * Mathf.Rad2Deg / 45f);
            return 1 + (angle % 4 + 4) % 4;
        }

        private void SetFootprintCursor(int index)
        {
            if (activeFootprintCursor == index) return;
            if (index >= 0 && !footprintCursors[index]) footprintCursors[index] = CreateFootprintCursor(index);
            Cursor.SetCursor(index < 0 ? null : footprintCursors[index],
                index < 0 ? Vector2.zero : new Vector2(15, 16), CursorMode.Auto);
            activeFootprintCursor = index;
        }

        private static Texture2D CreateFootprintCursor(int index)
        {
            const int size = 32;
            var pixels = new Color32[size * size];
            float angle = (index - 1) * 45f * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (Arrow(x, y)) pixels[y * size + x] = new Color32(255, 238, 188, 255);
                else if (Arrow(x - 1, y) || Arrow(x + 1, y) || Arrow(x, y - 1) || Arrow(x, y + 1))
                    pixels[y * size + x] = new Color32(24, 25, 28, 255);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = index == 0 ? "Footprint move cursor" : "Footprint resize cursor",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;

            bool Arrow(int x, int y)
            {
                var point = new Vector2(x - 15, y - 15);
                if (index == 0) return Along(point, Vector2.right) || Along(point, Vector2.up);
                return Along(point, direction);
            }

            static bool Along(Vector2 point, Vector2 axis)
            {
                float along = Mathf.Abs(Vector2.Dot(point, axis));
                float across = Mathf.Abs(point.x * axis.y - point.y * axis.x);
                return along <= 12f && (across <= 1f || (along >= 8f && across <= 12f - along));
            }
        }

        private static Vector2 HandlePoint(Rect bounds, int index)
        {
            switch (index)
            {
                case 0: return bounds.min;
                case 1: return new Vector2(bounds.center.x, bounds.yMin);
                case 2: return new Vector2(bounds.xMax, bounds.yMin);
                case 3: return new Vector2(bounds.xMax, bounds.center.y);
                case 4: return bounds.max;
                case 5: return new Vector2(bounds.center.x, bounds.yMax);
                case 6: return new Vector2(bounds.xMin, bounds.yMax);
                default: return new Vector2(bounds.xMin, bounds.center.y);
            }
        }

        private void DrawBuildingFootprint(Rect bounds)
        {
            if (footprintRoot)
            {
                footprintRoot.gameObject.SetActive(false);
                Destroy(footprintRoot.gameObject);
            }
            footprintRoot = new GameObject("Editable building footprint").transform;
            footprintRoot.SetParent(content, false);
            DrawTileOutline(bounds, footprintMaterial, footprintRoot, "Building footprint edge");
            if (!footprintSelected || !CanEditFootprint) return;
            for (int i = 0; i < 8; i++)
            {
                var point = HandlePoint(bounds, i);
                var handle = Box("Footprint resize handle", new Vector3(point.x, .22f, point.y),
                    new Vector3(.45f, .08f, .45f), footprintMaterial);
                handle.transform.SetParent(footprintRoot, false);
            }
        }

        private bool TryHitFootprint(Vector2 screen, Camera eventCamera, Vector2 point, string objectHit, out int handle)
        {
            handle = -1;
            var bounds = previewLot.zoningLayout.BuildingSlot;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, screen, eventCamera, out var local))
                return false;
            var rect = image.rectTransform.rect;
            for (int i = 0; i < 8; i++)
            {
                var p = HandlePoint(bounds, i);
                var projected = cameraView.WorldToViewportPoint(Origin + new Vector3(p.x, .2f, p.y));
                var position = rect.min + Vector2.Scale(rect.size, new Vector2(projected.x, projected.y));
                if ((position - local).sqrMagnitude <= 12f * 12f)
                {
                    handle = footprintSelected ? i : -1;
                    return true;
                }
            }
            // A selected footprint remains draggable through its translucent building preview.
            return bounds.Contains(point) && (footprintSelected || objectHit == null);
        }

        private bool TryBeginFootprintDrag(PointerEventData data, Ray ray, string objectHit)
        {
            if (!CanEditFootprint || !TryPoint(ray, out var point) ||
                !TryHitFootprint(data.pressPosition, data.pressEventCamera, point, objectHit, out var handle)) return false;
            footprintHandle = handle;
            footprintStart = previewLot.zoningLayout.BuildingSlot;
            footprintDraft = footprintStart;
            footprintDragStart = point;
            footprintSelected = true;
            footprintDragging = true;
            data.eligibleForClick = false;
            selectFootprint();
            DrawBuildingFootprint(footprintStart);
            UpdateFootprintCursor();
            return true;
        }

        private void DragBuildingFootprint(Vector2 point)
        {
            const float minimumSize = .25f;
            var land = previewLot.LandBounds;
            var bounds = footprintStart;
            var delta = point - footprintDragStart;
            if (footprintHandle < 0)
            {
                var center = snapFootprint(bounds.center + delta);
                bounds.center = new Vector2(
                    Mathf.Clamp(center.x, land.xMin + bounds.width / 2, land.xMax - bounds.width / 2),
                    Mathf.Clamp(center.y, land.yMin + bounds.height / 2, land.yMax - bounds.height / 2));
            }
            else
            {
                var end = snapFootprint(HandlePoint(footprintStart, footprintHandle) + delta);
                if (footprintHandle == 0 || footprintHandle == 6 || footprintHandle == 7)
                    bounds.xMin = Mathf.Clamp(end.x, land.xMin, bounds.xMax - minimumSize);
                if (footprintHandle == 2 || footprintHandle == 3 || footprintHandle == 4)
                    bounds.xMax = Mathf.Clamp(end.x, bounds.xMin + minimumSize, land.xMax);
                if (footprintHandle == 0 || footprintHandle == 1 || footprintHandle == 2)
                    bounds.yMin = Mathf.Clamp(end.y, land.yMin, bounds.yMax - minimumSize);
                if (footprintHandle == 4 || footprintHandle == 5 || footprintHandle == 6)
                    bounds.yMax = Mathf.Clamp(end.y, bounds.yMin + minimumSize, land.yMax);
            }
            footprintDraft = bounds;
            DrawBuildingFootprint(bounds);
            if (footprintPreview)
                footprintPreview.localPosition = new Vector3(bounds.center.x, 0, bounds.center.y);
        }
    }
}
