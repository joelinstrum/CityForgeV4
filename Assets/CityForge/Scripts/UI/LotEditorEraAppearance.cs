using System;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal sealed partial class LotEditorPanel
    {
        private Text EraLabel(Transform parent, string text, float x, float y, float width, float height)
        {
            var result = Label(parent, text, x, y, width, 16);
            At(result.rectTransform, x, y, width, height);
            result.horizontalOverflow = HorizontalWrapMode.Wrap;
            return result;
        }
        private Dropdown EraMenu(Transform parent, string[] labels, int selected, float x, float y, float width, Action<int> changed)
        {
            var obj = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            obj.transform.SetParent(parent, false); At((RectTransform)obj.transform, x, y, width, 36);
            var menu = obj.GetComponent<Dropdown>(); menu.ClearOptions(); menu.AddOptions(labels.ToList());
            foreach (var text in obj.GetComponentsInChildren<Text>(true)) { text.font = font; text.fontSize = 17; text.color = new Color32(245,232,203,255); }
            foreach (var image in obj.GetComponentsInChildren<Image>(true)) image.color = new Color32(22,42,57,255);
            menu.SetValueWithoutNotify(Mathf.Max(0, selected)); menu.onValueChanged.AddListener(value => changed(value));
            return menu;
        }
        internal void SetAppearancePreviewEra(string era) { appearancePreviewEra = era; Refresh(); }
        private string appearancePreviewEra;
        private void OpenEraAppearance()
        {
            if (!lotOpen || assetPopup) return;
            var draft = JsonUtility.FromJson<EraAppearanceSettings>(JsonUtility.ToJson(session.Lot.eraAppearance ?? new EraAppearanceSettings()));
            ShowAppearance();
            void ShowAppearance()
            {
                CloseAssetPopup();
                var frame = CreateModal("Era appearance", 760, 680);
                EraLabel(frame, "Appearance changes props and paving; the building's first era stays the same.", 20, 52, 720, 40);
                var eras = new[] { "" }.Concat(CityForgeTimeline.Eras.Select(e => e.id)).ToArray();
                EraLabel(frame, "Appearance era", 20, 102, 180, 36);
                EraMenu(frame, eras.Select(id => string.IsNullOrEmpty(id) ? "Automatic · district year in game" : CityForgeTimeline.Find(id).Name).ToArray(), Array.IndexOf(eras, draft.eraId ?? ""), 210, 102, 530, value => {draft.eraId = eras[value];ShowAppearance();});
                var preserve = Checkbox(frame, "Preserve authored appearance", "Preserve authored props and paving (heritage exception)", 20, 146, 720, draft.preserveAuthored);
                preserve.onValueChanged.AddListener(value => { draft.preserveAuthored = value; ShowAppearance(); });
                var paving = Checkbox(frame, "Custom paving", "Custom paving by purpose (otherwise use the era default)", 20, 186, 720, draft.pavingConfigured);
                paving.onValueChanged.AddListener(value => { draft.pavingConfigured = value; ShowAppearance(); });
                string[] values = { draft.generalMaterial, draft.entranceMaterial, draft.courtyardMaterial, draft.drivewayMaterial };
                var styles = new[] { "" }.Concat(LotPathMaterial.All.Where(m => (m.IsAvailable || values.Contains(m.Id))&&m.IsAvailableInEra(string.IsNullOrEmpty(draft.eraId)?appearancePreviewEra??ResidentialGround.LotEra(session.Lot):draft.eraId)).Select(m => m.Id)).ToArray();
                string[] labels = { "General / sidewalk", "Entrance", "Courtyard", "Driveway" };
                for (int i = 0; i < labels.Length; i++)
                {
                    int role = i;
                    EraLabel(frame, labels[i], 20, 232 + i * 42, 180, 36);
                    var button = EraMenu(frame, styles.Select(id => string.IsNullOrEmpty(id) ? "Keep authored surface" : LotPathMaterial.Find(id).Label + (LotPathMaterial.Find(id).IsAvailable ? "" : " (unavailable)")).ToArray(), Array.IndexOf(styles, values[i] ?? ""), 210, 232 + i * 42, 530, value =>
                    {
                        string next = styles[value];
                        switch (role) { case 0: draft.generalMaterial = next; break; case 1: draft.entranceMaterial = next; break; case 2: draft.courtyardMaterial = next; break; default: draft.drivewayMaterial = next; break; }
                    });
                    button.interactable = draft.pavingConfigured && !draft.preserveAuthored;

                }
                Control(frame, "Manage prop exceptions…", 20, 414, 720, () => ShowProps(draft, ShowAppearance));
                EraLabel(frame, "Art Deco and later default to concrete and era-compatible vehicles. A path's material lock wins over the era profile. Buildings are always retained.", 20, 462, 720, 60);
                EraLabel(frame, LotPathMaterial.Find("concrete")?.IsAvailable != true ? "Concrete texture is unavailable. Authored surfaces will stay visible until the private material is installed." : "Motor freight replacements and era sound banks are not yet supplied. Hidden freight vehicles keep their saved jobs and cargo.", 20, 530, 720, 48);
                Control(frame, "Cancel", 460, 620, 120, CloseAssetPopup);
                Control(frame, "Apply", 600, 620, 140, () =>
                {
                    EraAppearance.Validate(draft);
                    session.Edit(lot => lot.eraAppearance = draft);
                    CloseAssetPopup(); Refresh("Era appearance applied. Save the lot to keep it.");
                });
            }
        }
        private void ShowProps(EraAppearanceSettings draft, Action back)
        {
            CloseAssetPopup();
            var frame = CreateModal("Era prop exceptions", 760, 680);
            Control(frame, "Back", 20, 52, 120, back);
            EraLabel(frame, "Cycle a prop between Automatic, Keep, and Hide. Building meshes are protected.", 160, 52, 580, 40);
            CreatePickerResults(frame, 110, 20);
            var props = session.Lot.objects.Select(o => catalog ? catalog.Find(o.assetId) : null)
                .Where(a => a != null && a.category != "Buildings").GroupBy(a => a.id).Select(g => g.First()).OrderBy(a => a.label).ToArray();
            assetList.sizeDelta = new Vector2(0, Mathf.Max(50, props.Length * 44));
            for (int i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                int mode = draft.hiddenProps?.Contains(prop.id) == true ? 2 : draft.keptProps?.Contains(prop.id) == true ? 1 : 0;
                Control(assetList, prop.label + ": " + new[] { "Automatic", "Keep", "Hide" }[mode] + "  ▸", 8, i * 44, 680, () =>
                {
                    draft.hiddenProps = (draft.hiddenProps ?? Array.Empty<string>()).Where(id => id != prop.id).Concat(mode == 1 ? new[] { prop.id } : Array.Empty<string>()).ToArray();
                    draft.keptProps = (draft.keptProps ?? Array.Empty<string>()).Where(id => id != prop.id).Concat(mode == 0 ? new[] { prop.id } : Array.Empty<string>()).ToArray();
                    ShowProps(draft, back);
                });
            }
            if (props.Length == 0) EraLabel(assetList, "This lot has no props yet.", 12, 12, 660, 36);
        }
    }
}
