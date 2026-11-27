using CityForge.Lots;
using UnityEngine.UI;
namespace CityForge.UI
{
    internal sealed partial class LotEditorPanel
    {
        private Button editBuildingButton;
        private void ToggleHandTool()
        {
            viewport.HandTool = !viewport.HandTool;
            if (viewport.HandTool)
            {
                armed = null; moving = false; repeatSource = null; pickingLotEntrance = false;
                if (viewport.TexturePainting) viewport.EndTexturePaint();
                ClearGroundPath(); ClearFencePath(); StopOverlay();
                terrainMode = false; terrainTile = null;
            }
            Refresh(rebuildPreview:false);
        }
        private void OpenBuildingEditor()
        {
            var item=Selection;
            if(PlayerMode||assetPopup||item==null||catalog.Find(item.assetId)?.category!="Buildings")return;
            CloseChoices();
            assetPopupOpener=UnityEngine.EventSystems.EventSystem.current?UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject:null;
            assetPopup=BuildingEditorModal.Show(transform,font,new[]{item},catalog,"Lot Editor · building settings will be saved with this lot",CloseAssetPopup,item.id, changes=>
            {
                session.Edit(lot=>BuildingAppearance.Apply(lot,changes,catalog));
                CloseAssetPopup();
                Refresh("Building appearance applied. Save the lot to keep it.");
            });
            Refresh(rebuildPreview:false);
        }
    }
}
