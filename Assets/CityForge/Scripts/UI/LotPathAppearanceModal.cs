using CityForge.Lots;
namespace CityForge.UI
{
    internal sealed partial class LotEditorPanel
    {
        private void OpenPathAppearancePopup(LotPathDefinition path)
        {
            if(path==null||PlayerMode||assetPopup||!session.Lot.paths.Contains(path))return;
            CloseChoices();var options=PedestrianPathOptions.From(path);
            assetPopup=PedestrianPathModal.Show(transform,font,options,true,
                ()=>{session.Edit(_=>options.Apply(path));CloseAssetPopup();},CloseAssetPopup,
                ()=>{session.Undo();selectedPath=null;CloseAssetPopup();},handleUndoKeys:true);
        }
    }
}
