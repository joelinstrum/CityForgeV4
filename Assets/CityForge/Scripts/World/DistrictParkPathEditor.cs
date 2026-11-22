using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        DistrictParkPathRecord selectedParkPath;
        internal bool DistrictPathTextInputActive => parkPathOptions && EventSystem.current && EventSystem.current.currentSelectedGameObject && EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
        int pathDraftWidth=2,pathDraftFeather=25,pathDraftRotation;
        bool pathDraftCurved=true;
        readonly List<Action> pathUndo=new();
        GameObject pathSelection;
        Material pathSelectionMaterial;
        void RecordPathUndo(Action undo){if(pathUndo.Count==64)pathUndo.RemoveAt(0);pathUndo.Add(undo);}
        void UndoParkPath()
        {
            if(pathUndo.Count==0){ShowDistrictMessage("No path changes to undo.");return;}
            CancelParkPath();int i=pathUndo.Count-1;var undo=pathUndo[i];pathUndo.RemoveAt(i);undo();
            RebuildParkPaths();pedestrianRoutesDirty=true;RebuildPlacedPedestrians();ShowDistrictMessage("Path change undone · Save to keep your changes.");
        }
        void UpdateParkPathEditor()
        {
            if(DistrictLoading||IsSaving||playerLotWorkspace||lotPlacementDraft!=null||DistrictScriptDialogOpen)return;
            if(EventSystem.current&&EventSystem.current.currentSelectedGameObject&&EventSystem.current.currentSelectedGameObject.GetComponent<InputField>())return;
            if(pathUndo.Count>0&&!zoneToolArmed&&!RoadPlacementArmed&&!ParkPathArmed&&!ParkTreeBrushArmed&&streetscapeAsset==null&&!RemakeBlocksInput&&lotPlacementDraft==null&&(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl)||Input.GetKey(KeyCode.LeftCommand)||Input.GetKey(KeyCode.RightCommand))&&Input.GetKeyDown(KeyCode.Z))UndoParkPath();
            if(parkPathOptions&&Input.GetKeyDown(KeyCode.Escape))CancelParkPath();
        }
        void OpenParkPaths()=>OpenParkPathEditor(null);
        void OpenParkPathEditor(DistrictParkPathRecord selected)=>BuildParkPathEditor(selected,false);
        PedestrianPathOptions pathAppearance=new();
        LotPathDepth pathDraftDepth;LotPathBorder pathDraftBorder;LotPathBorderSides pathDraftSides;
        void ReadPathAppearance()
        {
            parkPathMaterial=pathAppearance.materialId;pathDraftWidth=pathAppearance.width;pathDraftFeather=pathAppearance.feather;pathDraftRotation=pathAppearance.rotation;pathDraftCurved=pathAppearance.curved;parkPathShape=pathAppearance.shape;
            pathDraftDepth=pathAppearance.depth;pathDraftBorder=pathAppearance.border;pathDraftSides=pathAppearance.sides;
        }
        void BuildParkPathEditor(DistrictParkPathRecord selected,bool keepDraft)
        {
            CancelParkPath();CloseTownSection();CancelLotPlacement();CancelRoadPlacement();CancelZoneDrag();CancelZonedBlock(false);zoneToolArmed=false;SelectDistrictLot(null);
            selectedParkPath=selected;if(selected!=null&&!keepDraft)pathAppearance=PedestrianPathOptions.From(selected.path);
            if(selected!=null)ShowPathSelection(selected.path);
            parkPathOptions=PedestrianPathModal.Show(townToolbar.parent,lotFont,pathAppearance,selected!=null,
                ()=>{ReadPathAppearance();if(selected==null)BeginParkPath();else ApplyParkPath(selected);},CancelParkPath,UndoParkPath,
                selected==null?null:()=>{int index=parkPaths.IndexOf(selected);parkPaths.Remove(selected);RecordPathUndo(()=>parkPaths.Insert(Mathf.Min(index,parkPaths.Count),selected));CancelParkPath();RebuildParkPaths();pedestrianRoutesDirty=true;RebuildPlacedPedestrians();});
        }
        void OpenParkPathEditorWithDraft(DistrictParkPathRecord selected)=>BuildParkPathEditor(selected,true);
        void BeginParkPath()
        {
            string id=parkPathMaterial;int width=pathDraftWidth;bool curved=pathDraftCurved;
            if(parkPathOptions){Destroy(parkPathOptions);parkPathOptions=null;}
            var go=new GameObject("Park path drawing");go.transform.SetParent(transform,false);parkPathWire=go.AddComponent<PointWireTool>();
            parkPathWire.Initialize(GetComponentInChildren<CameraSystem.CityForgeCameraController>().GetComponent<Camera>(),townToolbar.parent,new ParkPathSurface(this),new Color(1,.8f,.35f));
            parkPathWire.OpenPath=true;parkPathWire.RibbonWidth=width;parkPathWire.Label="Path";parkPathWire.AllowClosedUndo=false;
            parkPathWire.DrawingHint=parkPathShape>0?"Click to place the center · Esc cancels":"Click to add markers · Enter to finish · First marker closes loop · Backspace undoes marker · Esc cancels";
            parkPathWire.CompletionRequested=p=>CreateParkPath(p,false,id,width,curved);parkPathWire.LoopCompletionRequested=p=>CreateParkPath(p,true,id,width,curved);parkPathWire.Completed+=_=>CancelParkPath();parkPathWire.Arm();
        }
        void ApplyParkPath(DistrictParkPathRecord selected)
        {
            var before=JsonUtility.ToJson(selected.path);var candidate=JsonUtility.FromJson<LotPathDefinition>(before);
            candidate.materialId=parkPathMaterial;candidate.width=pathDraftWidth;candidate.edgeBlend=pathDraftFeather;candidate.textureRotation=pathDraftRotation;candidate.curved=pathDraftCurved;candidate.depth=pathDraftDepth;candidate.recessed=false;candidate.border=pathDraftBorder;candidate.borderSides=pathDraftSides;
            var mesh=LotPathSurface.BuildMesh(candidate,LotPathMaterial.Find(candidate.materialId).TileSize,.065f,LotHeight);var problem=RoadSiteProblem(mesh);Destroy(mesh);
            if(problem!=null){ShowDistrictMessage(problem);return;}
            bool geometry=selected.path.width!=candidate.width||selected.path.curved!=candidate.curved;
            selected.path=candidate;RecordPathUndo(()=>selected.path=JsonUtility.FromJson<LotPathDefinition>(before));RebuildParkPaths();ShowPathSelection(candidate);
            if(geometry){pedestrianRoutesDirty=true;RebuildPlacedPedestrians();}ShowDistrictMessage("Path updated · Save the region to keep it.");
        }
        bool TrySelectParkPath(Ray ray)
        {
            var collider=RoadTerrain?RoadTerrain.GetComponent<TerrainCollider>():null;
            if(!collider||!collider.Raycast(ray,out var hit,10000))return false;
            var p=new Vector2(hit.point.x,hit.point.z);
            var roadMask=new StreetSurfaceMask();foreach(var road in districtRoadMeshes.Keys)roadMask.AddRibbon(road,RoadCenterline(road),road.width+(HasSidewalks(road.surfaceId)?2*LotPathSurface.ResidentialSidewalkWidth:0));if(roadMask.Contains(p))return false;
            DistrictParkPathRecord closest=null;float best=float.MaxValue;
            foreach(var record in parkPaths)
            {
                if(!ActiveDistrictMembers.Contains(record.district))continue;var path=record.path;var line=PathDisplayPoints(path);
                for(int i=0;i<line.Length-(path.closed?0:1);i++){var a=line[i];var d=line[(i+1)%line.Length]-a;float t=d.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude):0;float distance=Vector2.Distance(p,a+t*d);if(distance<=path.width*.5f+.25f&&distance<best){best=distance;closest=record;}}
            }
            if(closest!=null){OpenParkPathEditor(closest);return true;}
            if(selectedParkPath!=null){CancelParkPath();return true;}return false;
        }
        static Vector2[] PathDisplayPoints(LotPathDefinition path){if(!path.curved)return path.points;var rounded=new List<Vector2>();LotPathSurface.RoundCorners(path.points,null,path.closed,path.width,rounded);return rounded.ToArray();}
        void ClearPathSelection(){if(pathSelection)Destroy(pathSelection);if(pathSelectionMaterial)Destroy(pathSelectionMaterial);pathSelection=null;pathSelectionMaterial=null;}
        void ShowPathSelection(LotPathDefinition path)
        {
            ClearPathSelection();pathSelection=new GameObject("Selected path outline");pathSelection.transform.SetParent(transform,false);
            var line=pathSelection.AddComponent<LineRenderer>();pathSelectionMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));pathSelectionMaterial.color=new Color(1,.8f,.35f);line.sharedMaterial=pathSelectionMaterial;line.widthMultiplier=.12f;line.loop=path.closed;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var points=PathDisplayPoints(path);line.positionCount=points.Length;for(int i=0;i<points.Length;i++)line.SetPosition(i,new Vector3(points[i].x,LotHeight(points[i])+.14f,points[i].y));
        }
    }
}
