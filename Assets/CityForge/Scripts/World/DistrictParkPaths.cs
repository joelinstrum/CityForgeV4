using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
namespace CityForge.World
{
    [Serializable] public sealed class DistrictParkPathRecord
    {
        public int district;
        public LotPathDefinition path;
    }
    public sealed partial class PrototypeRegionView
    {
        private readonly List<DistrictParkPathRecord> parkPaths = new();
        private PointWireTool parkPathWire;
        private GameObject parkPathOptions, parkPathRoot;
        private int parkPathShape;private string parkPathMaterial;
        private readonly List<UnityEngine.Object> parkPathAssets = new();
        private bool ParkPathArmed => parkPathWire && parkPathWire.Armed;
        internal DistrictParkPathRecord[] CaptureParkPaths() => parkPaths.ToArray();
        private void RestoreParkPaths(DistrictParkPathRecord[] paths) { if(paths!=null)parkPaths.AddRange(paths); }
        internal static void ValidateParkPaths(DistrictParkPathRecord[] paths,int districtCount,float mapSide)
        {
            if(paths==null)return;
            if(paths.Length>512)throw new InvalidDataException("Too many park paths.");
            foreach(var record in paths)
            {
                var p=record?.path;
                if(record==null||record.district<0||record.district>=districtCount||p==null||p.use!=LotPathUse.Pedestrian||
                    LotPathMaterial.Find(p.materialId)==null||Array.IndexOf(LotPathSurface.Widths,p.width)<0||
                    p.edgeBlend<0||p.edgeBlend>100||RoadGuideProblem(p.points,p.closed,mapSide*.5f)!=null)
                    throw new InvalidDataException("Invalid park path.");
            }
        }
        private void CancelParkPath()
        {
            if(parkPathWire){parkPathWire.Cancel();Destroy(parkPathWire.gameObject);parkPathWire=null;}
            if(parkPathOptions){parkPathOptions.SetActive(false);Destroy(parkPathOptions);parkPathOptions=null;}
        }
        private void OpenParkPaths()
        {
            CloseTownSection();CancelLotPlacement();CancelRoadPlacement();CancelZoneDrag();CancelZonedBlock(false);zoneToolArmed=false;SelectDistrictLot(null);
            var panel=FounderRect(townToolbar.parent,"Park path options",new Vector2(0,220),new Vector2(680,300));
            var background=panel.gameObject.AddComponent<Image>();background.sprite=ZoneModalArtwork.Panel;background.type=Image.Type.Sliced;parkPathOptions=panel.gameObject;
            FounderText(panel,"Paths · Pedestrians welcome",new Vector2(0,122),new Vector2(640,36),26).color=TownGold;
            var styles=LotPathMaterial.All.Where(s=>s.IsAvailable).ToArray();
            var material=Dropdown("Material",-170,styles.Select(s=>s.Label).ToList());
            material.value=Array.FindIndex(styles,s=>s.Id=="packed-earth");
            var width=Dropdown("Width",170,LotPathSurface.Widths.Select(w=>$"{w} m wide").ToList());
            var shape=Dropdown("Shape",-170,new List<string>{"Freehand","Roundabout · 3 m","Open rectangle · 3 m","Half circle · 6 m","Half circle · 3 m"});
            ((RectTransform)shape.transform).anchoredPosition=new Vector2(-170,-55);
            var curve=DefaultControls.CreateToggle(new DefaultControls.Resources()).GetComponent<Toggle>();curve.transform.SetParent(panel,false);curve.isOn=true;
            var rect=(RectTransform)curve.transform;rect.anchoredPosition=new Vector2(-160,-15);rect.sizeDelta=new Vector2(260,32);
            curve.GetComponentInChildren<Text>().font=lotFont;curve.GetComponentInChildren<Text>().fontSize=19;curve.GetComponentInChildren<Text>().text="Curved angles";
            var check=curve.graphic as Image;if(check){check.sprite=ZoneModalArtwork.Gold;check.color=Color.white;}
            var box=curve.targetGraphic as Image;if(box){box.sprite=ZoneModalArtwork.Dark;box.type=Image.Type.Sliced;}curve.GetComponentInChildren<Text>().color=TownGold;
            SkinPathButton(TownButton(panel,lotFont,"Draw path",170,-235,180,()=>
            {
                string id=styles[Mathf.Max(0,material.value)].Id;int meters=LotPathSurface.Widths[width.value];bool curved=curve.isOn;
                parkPathShape=shape.value;parkPathMaterial=id;
                panel.gameObject.SetActive(false);Destroy(panel.gameObject);parkPathOptions=null;
                var go=new GameObject("Park path drawing");go.transform.SetParent(transform,false);parkPathWire=go.AddComponent<PointWireTool>();
                parkPathWire.Initialize(GetComponentInChildren<CameraSystem.CityForgeCameraController>().GetComponent<Camera>(),townToolbar.parent,new ParkPathSurface(this),new Color(1,.80f,.35f));
                parkPathWire.OpenPath=true;parkPathWire.RibbonWidth=meters;parkPathWire.Label="Path";parkPathWire.AllowClosedUndo=false;
                parkPathWire.DrawingHint=parkPathShape>0?"Path preset · Click to place the center · Esc cancels":"Path · Click to add markers · Enter to finish · Click the first marker for a loop · Backspace to undo · Esc to cancel";
                parkPathWire.CompletionRequested=points=>CreateParkPath(points,false,id,meters,curved);
                parkPathWire.LoopCompletionRequested=points=>CreateParkPath(points,true,id,meters,curved);
                parkPathWire.Completed+=_=>CancelParkPath();parkPathWire.Arm();
            }),true);
            SkinPathButton(TownButton(panel,lotFont,"Cancel",390,-235,140,CancelParkPath),false);
            void SkinPathButton(Button button,bool gold){button.image.sprite=gold?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;button.image.type=Image.Type.Sliced;button.image.color=Color.white;}
            Dropdown Dropdown(string name,float x,List<string> items)
            {
                var d=DefaultControls.CreateDropdown(new DefaultControls.Resources()).GetComponent<Dropdown>();d.transform.SetParent(panel,false);
                var r=(RectTransform)d.transform;r.anchoredPosition=new Vector2(x,40);r.sizeDelta=new Vector2(300,40);d.ClearOptions();d.AddOptions(items);
                d.captionText.font=lotFont;d.captionText.fontSize=19;d.captionText.color=TownGold;d.itemText.font=lotFont;d.itemText.color=TownGold;d.GetComponent<Image>().sprite=ZoneModalArtwork.Dark;d.GetComponent<Image>().type=Image.Type.Sliced;
                var arrow=d.transform.Find("Arrow");if(arrow){arrow.GetComponent<Image>().enabled=false;var icon=new GameObject("Dropdown chevron",typeof(RectTransform),typeof(Text));icon.transform.SetParent(arrow,false);var iconRect=(RectTransform)icon.transform;iconRect.anchorMin=Vector2.zero;iconRect.anchorMax=Vector2.one;iconRect.offsetMin=iconRect.offsetMax=Vector2.zero;var t=icon.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text="▼";t.fontSize=16;t.color=TownGold;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;}
                foreach(var image in d.template.GetComponentsInChildren<Image>(true))image.color=new Color(.10f,.16f,.20f);return d;
            }
        }
        private void UpdateParkPathPreset()
        {
            if(parkPathShape==0||!ParkPathArmed||parkPathWire.Points.Count!=1)return;
            var center=parkPathWire.Points[0];var points=new List<Vector2>();bool loop=parkPathShape==1;
            if(parkPathShape==2)foreach(var p in new[]{new Vector2(-6,-4),new Vector2(-6,4),new Vector2(6,4),new Vector2(6,-4)})points.Add(center+p);
            else for(int i=0;i<(loop?24:13);i++){float a=Mathf.PI*i/12;points.Add(center+6*new Vector2(Mathf.Cos(a),Mathf.Sin(a)));}
            int width=parkPathShape==3?6:3;var problem=CreateParkPath(points,loop,parkPathMaterial,width,false);
            CancelParkPath();if(problem!=null)ShowDistrictMessage(problem);
        }
        private string CreateParkPath(IReadOnlyList<Vector2> points,bool closed,string material,int width,bool curved)
        {
            if(parkPaths.Count>=512)return "This region has reached its path limit.";
            var problem=RoadGuideProblem(points,closed,MapSide*.5f);if(problem!=null)return problem;
            var path=new LotPathDefinition{points=points.ToArray(),closed=closed,curved=curved,width=width,materialId=material,use=LotPathUse.Pedestrian,edgeBlend=25};
            var style=LotPathMaterial.Find(material);var mesh=LotPathSurface.BuildMesh(path,style.TileSize,.065f,LotHeight);
            // Paths may cross lots and sidewalks, but must stay on dry land and avoid buildings.
            problem=RoadSiteProblem(mesh);Destroy(mesh);if(problem!=null)return problem;
            parkPaths.Add(new DistrictParkPathRecord{district=SelectedDistrict,path=path});RebuildParkPaths();pedestrianRoutesDirty=true;RebuildPlacedPedestrians();
            ShowDistrictMessage("Path placed · Pedestrians can use this route. Save the region to keep it.");return null;
        }
        private void RebuildParkPaths(StreetSurfaceMask roadMask=null)
        {
            if(parkPathRoot){parkPathRoot.SetActive(false);Destroy(parkPathRoot);}
            foreach(var asset in parkPathAssets)if(asset)Destroy(asset);parkPathAssets.Clear();
            if(SelectedDistrict<0)return;
            parkPathRoot=new GameObject("District park paths");parkPathRoot.transform.SetParent(transform,false);
            foreach(var record in parkPaths.Where(p=>ActiveDistrictMembers.Contains(p.district)))
            {
                var path=record.path;var style=LotPathMaterial.Find(path.materialId);
                var source=LotPathSurface.BuildMesh(path,style.TileSize,.065f,LotHeight);
                if(roadMask==null)
                {
                    roadMask=new StreetSurfaceMask();
                    foreach(var road in districtRoadMeshes.Keys)roadMask.AddRibbon(road,RoadCenterline(road),road.width+(HasSidewalks(road.surfaceId)?2*LotPathSurface.ResidentialSidewalkWidth:0));
                }
                var mesh=roadMask.Clip(source,Matrix4x4.identity,.55f);
                if(mesh!=source)Destroy(source);
                var material=style.CreateMaterial();LotPathMaterial.ApplyEdgeTreatment(material,path,path.edgeBlend,0);
                var go=new GameObject("Pedestrian path",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parkPathRoot.transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                parkPathAssets.Add(mesh);parkPathAssets.Add(material);
            }
        }
        private sealed class ParkPathSurface:PointWireSurface
        {
            private readonly PrototypeRegionView view;internal ParkPathSurface(PrototypeRegionView v)=>view=v;
            public override bool Available=>view.CanBuildTown&&!view.DistrictLoading;
            public override bool CanEdit=>Available&&!view.IsSaving&&view.TownConstructionVisible;
            public override Collider Collider=>view.RoadTerrain?view.RoadTerrain.GetComponent<TerrainCollider>():null;
            public override bool Contains(Vector2 point)=>view.InActiveDistrict(point);
            public override float Height(Vector2 point)=>view.LotHeight(point);
        }
    }
}
