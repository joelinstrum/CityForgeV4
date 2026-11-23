using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
namespace CityForge.World
{
    internal sealed class EraLotAppearanceView : MonoBehaviour
    {
        private readonly List<(GameObject body, LotAssetCatalogEntry asset, bool active)> members = new();
        internal void Register(GameObject body, LotAssetCatalogEntry asset) => members.Add((body, asset, body.activeSelf || asset.IsCharacter));
        internal bool Allows(GameObject body, LotDefinition lot, string era)
        {
            foreach (var member in members) if (member.body == body) return EraAppearance.Allows(lot, member.asset, era);
            return true;
        }
        internal void Apply(LotDefinition lot, string era, bool populated)
        {
            foreach (var member in members)
                if (member.body) member.body.SetActive(EraAppearance.Allows(lot, member.asset, era) &&
                    member.active && (!member.asset.IsCharacter || populated));
            GetComponent<DistrictLotSurfaces>()?.ApplyEraMaterials(lot, era);
        }
    }
}
