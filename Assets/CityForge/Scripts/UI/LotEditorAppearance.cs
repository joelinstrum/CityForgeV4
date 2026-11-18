using CityForge.Lots;
using UnityEngine;
using UnityEngine.UI;
using static CityForge.UI.LotEditorWorkspace;

namespace CityForge.UI
{
    internal sealed partial class LotEditorPanel
    {
        private Dropdown AddTextureWealthFilter(Transform frame, float y, UnityEngine.Events.UnityAction<int> change)
        {
            var menu = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            menu.name = "Texture wealth";
            menu.transform.SetParent(frame, false);
            At((RectTransform)menu.transform, 20, y, 520, 33);
            foreach (var text in menu.GetComponentsInChildren<Text>(true)) text.font = font;
            var dropdown = menu.GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string> { "All texture wealth levels",
                LotPropStyles.TextureWealthLabel(LotWealth.Low), LotPropStyles.TextureWealthLabel(LotWealth.Medium),
                LotPropStyles.TextureWealthLabel(LotWealth.High) });
            dropdown.onValueChanged.AddListener(change);
            return dropdown;
        }
        private LotWealth propWealthFilter;
        private void AddFenceWealthFilter(Transform frame, InputField search)
        {
            var menu = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            menu.name = "Fence wealth";
            menu.transform.SetParent(frame, false);
            At((RectTransform)menu.transform, 20, 191, 520, 33);
            foreach (var text in menu.GetComponentsInChildren<Text>(true)) text.font = font;
            var dropdown = menu.GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string> { "All wealth levels", "$ · Low", "$$ · Medium", "$$$ · High" });
            dropdown.onValueChanged.AddListener(value => { propWealthFilter = (LotWealth)value; PopulateAssets(search.text); });
        }
    }
}
