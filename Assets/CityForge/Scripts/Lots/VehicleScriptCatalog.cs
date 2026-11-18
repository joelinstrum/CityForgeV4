using System;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    internal sealed class VehicleScriptDefinition
    {
        public string id, name, category, summary, description, requiredCapability, settingsPanel;
        public LotVehicleBehavior behavior;
        internal bool Supports(VehicleProfile profile) => string.IsNullOrEmpty(requiredCapability) ||
            (requiredCapability == "lumber-freight" && profile?.supportsLumberFreight == true);
    }

    internal static class VehicleScriptCatalog
    {
        [Serializable] private sealed class Catalog { public VehicleScriptDefinition[] scripts; }
        private static VehicleScriptDefinition[] scripts;
        internal static VehicleScriptDefinition[] All
        {
            get
            {
                if (scripts != null) return scripts;
                var file = Resources.Load<TextAsset>("Config/VehicleScripts");
                scripts = file ? JsonUtility.FromJson<Catalog>(file.text)?.scripts : null;
                return scripts ??= Array.Empty<VehicleScriptDefinition>();
            }
        }
    }
}
