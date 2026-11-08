using System;
using System.Linq;
using CityForge.World;
using UnityEditor;
using UnityEngine;

namespace CityForge.EditorTools
{
    public sealed class RegionalResourceDebugWindow : EditorWindow
    {
        readonly string[] layers = { "timber", "stone", "iron", "coal", "fish", "fertile-soil", "pasture",
            "fresh-water", "natural-harbor", "scenic-beauty" };
        Texture2D preview;
        PrototypeRegionView view;
        SettlementSuitability evaluation;
        Vector2 selected;
        int layer;
        bool showUndiscovered;
        int year = 1750;

        [MenuItem("CityForge/Diagnostics/Regional Resources")]
        static void Open() => GetWindow<RegionalResourceDebugWindow>("Regional Resources");

        void OnGUI()
        {
            var current = Application.isPlaying ? FindFirstObjectByType<PrototypeRegionView>() : null;
            if (current != view || (current && !preview))
            {
                view = current;
                evaluation = null;
                Rebuild();
            }
            if (!view)
            {
                EditorGUILayout.HelpBox("Open a region in Play Mode to inspect its survey.", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            layer = EditorGUILayout.Popup("Resource", layer, layers);
            showUndiscovered = EditorGUILayout.Toggle("Show hidden deposits", showUndiscovered);
            bool surveyChanged = EditorGUI.EndChangeCheck();
            if (GUILayout.Button("Refresh survey") || surveyChanged) Rebuild();
            EditorGUI.BeginChangeCheck();
            year = EditorGUILayout.IntField("Evaluation year", year);
            if (EditorGUI.EndChangeCheck() && evaluation != null) Evaluate();

            float size = Mathf.Clamp(position.width - 24, 100, 420);
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            if (preview) GUI.DrawTexture(rect, preview);
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                Vector2 p = Event.current.mousePosition - rect.position;
                selected = new Vector2(p.x / rect.width, 1 - p.y / rect.height);
                Evaluate();
                Event.current.Use();
            }
            if (evaluation == null) return;
            var marker = rect.position + new Vector2(selected.x, 1 - selected.y) * size;
            EditorGUI.DrawRect(new Rect(marker.x - 3, marker.y - 3, 6, 6), Color.white);
            EditorGUILayout.LabelField($"Map position {selected:F3}");
            foreach (SuitabilityCategory category in Enum.GetValues(typeof(SuitabilityCategory)))
                EditorGUILayout.LabelField(category.ToString(), evaluation.scores[(int)category].ToString("F1"));
            EditorGUILayout.LabelField("Visible resources", string.Join(", ", evaluation.visibleResources.Keys));
            foreach (var line in evaluation.geography.Concat(evaluation.limitations)) EditorGUILayout.LabelField(line);
        }

        void Evaluate() => evaluation = view.EvaluateSettlement((selected - Vector2.one * .5f) * view.MapSide, year);

        void Rebuild()
        {
            if (preview) DestroyImmediate(preview);
            if (!view) return;
            const int side = 96;
            preview = new Texture2D(side, side) { hideFlags = HideFlags.HideAndDontSave };
            var colors = new Color[side * side];
            for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++)
            {
                var point = new Vector2(x, y) / (side - 1);
                var resources = showUndiscovered ? view.RegionalResources.DebugSurveyAt(point) : view.RegionalResources.At(point);
                resources.TryGetValue(layers[layer], out float strength);
                colors[y * side + x] = Color.Lerp(new Color(.07f, .09f, .12f), new Color(.75f, .94f, .35f), strength);
            }
            preview.SetPixels(colors);
            preview.Apply();
            if (evaluation != null) Evaluate();
        }

        void OnDisable()
        {
            if (preview) DestroyImmediate(preview);
        }
    }
}
