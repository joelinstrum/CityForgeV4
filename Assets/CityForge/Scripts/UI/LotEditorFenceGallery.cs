using CityForge.Lots;
using UnityEngine;
namespace CityForge.UI
{
    internal sealed partial class LotEditorPanel
    {
        private void ArmFenceRun(LotAssetCatalogEntry asset)
        {
            StartFencePath();
            chosenPathFence = asset;
            Refresh("Draw " + asset.label + " · Enter to finish · Click first marker to close.", rebuildPreview:false);
        }
        private void CreateLotFenceGuide()
        {
            StartFencePath();
            var half = Vector2.Max(Vector2.one * .25f, session.Lot.Size * .5f -
                Vector2.one * LotPathSurface.ResidentialSidewalkWidth);
            fencePath.TryAdd(new Vector2(-half.x, -half.y));
            fencePath.TryAdd(new Vector2(half.x, -half.y));
            fencePath.TryAdd(new Vector2(half.x, half.y));
            fencePath.TryAdd(new Vector2(-half.x, half.y));
            FinishFencePath(true);
        }
        private void SurroundLotWithSelectedFence(LotAssetCatalogEntry asset)
        {
            CreateLotFenceGuide();
            if (fencePath.Finished) FillFencePath(asset);
        }
    }
}
