using CityForge.CameraSystem;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private Button editSelectedBuildingButton;
        private void ApplyBuildingAppearance(DistrictLotRecord record,System.Collections.Generic.IReadOnlyDictionary<string,BuildingAppearanceSettings> changes,LotAssetCatalog catalog)
        {
            if(!PlacedLots.Contains(record))throw new System.IO.InvalidDataException("This placed lot is no longer available.");
            var copy=LotDefinition.Parse(JsonUtility.ToJson(record.definition));
            if(!BuildingAppearance.Apply(copy,changes,catalog))return;
            record.definition=copy;
            // Repaint existing near/medium renderers in place: preserve clocks, agents and every other lot.
            foreach(var pair in changes)
                if(lotBuildingViews.TryGetValue((record,pair.Key),out var root)&&root)
                    {root.GetComponent<BuildingPaint>()?.Set(pair.Value);root.GetComponent<BuildingSignInstances>()?.Set(pair.Value);}
        }
        private void EditSelectedBuilding()
        {
            var record=selectedDistrictLot;
            if(record==null||SelectedDistrict<0||DistrictLoading||IsSaving||playerLotWorkspace||lotPlacementDraft!=null)return;
            var catalog=LotAssetCatalog.Load();
            var modal=BuildingEditorModal.Show(selectedLotPanel.transform.parent,lotFont,record.definition.objects,catalog,
                "In game · building settings will affect only this placed instance",ClosePlayerLot,apply: changes=>
                {
                    ApplyBuildingAppearance(record,changes,catalog);
                    ClosePlayerLot();
                    if(selectedLotFeedback)selectedLotFeedback.text="Building appearance applied. Save the region to keep it.";
                });
            if(!modal)return;
            playerLotWorkspace=modal;
            lotCamera=GetComponentInChildren<CityForgeCameraController>();
            if(lotCamera)lotCamera.enabled=false;
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
