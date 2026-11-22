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
                    !Enum.IsDefined(typeof(LotPathDepth),p.depth)||!Enum.IsDefined(typeof(LotPathBorder),p.border)||!Enum.IsDefined(typeof(LotPathBorderSides),p.borderSides)||p.textureRotation<0||p.textureRotation>3||p.edgeBlend<0||p.edgeBlend>100||RoadGuideProblem(p.points,p.closed,mapSide*.5f)!=null)
                    throw new InvalidDataException("Invalid park path.");
            }
        }
        private void CancelParkPath()
        {
            selectedParkPath=null;ClearPathSelection();
            if(parkPathWire){parkPathWire.Cancel();Destroy(parkPathWire.gameObject);parkPathWire=null;}
            if(parkPathOptions){parkPathOptions.SetActive(false);Destroy(parkPathOptions);parkPathOptions=null;}
        }
        private void UpdateParkPathPreset()
        {
            if(parkPathShape==0||!ParkPathArmed||parkPathWire.Points.Count!=1)return;
            var center=parkPathWire.Points[0];var points=new List<Vector2>();bool loop=parkPathShape==1;
            if(parkPathShape==2)foreach(var p in new[]{new Vector2(-6,-4),new Vector2(-6,4),new Vector2(6,4),new Vector2(6,-4)})points.Add(center+p);
            else for(int i=0;i<(loop?24:13);i++){float a=Mathf.PI*i/12;points.Add(center+6*new Vector2(Mathf.Cos(a),Mathf.Sin(a)));}
            int width=pathDraftWidth;var problem=CreateParkPath(points,loop,parkPathMaterial,width,false);
            CancelParkPath();if(problem!=null)ShowDistrictMessage(problem);
        }
        private string CreateParkPath(IReadOnlyList<Vector2> points,bool closed,string material,int width,bool curved)
        {
            if(parkPaths.Count>=512)return "This region has reached its path limit.";
            var problem=RoadGuideProblem(points,closed,MapSide*.5f);if(problem!=null)return problem;
            var path=new LotPathDefinition{points=points.ToArray(),closed=closed,curved=curved,width=width,materialId=material,use=LotPathUse.Pedestrian,edgeBlend=pathDraftFeather,textureRotation=pathDraftRotation,depth=pathDraftDepth,border=pathDraftBorder,borderSides=pathDraftSides};
            var style=LotPathMaterial.Find(material);var mesh=LotPathSurface.BuildMesh(path,style.TileSize,.065f,LotHeight);
            // Paths may cross lots and sidewalks, but must stay on dry land and avoid buildings.
            problem=RoadSiteProblem(mesh);Destroy(mesh);if(problem!=null)return problem;
            var added=new DistrictParkPathRecord{district=SelectedDistrict,path=path};parkPaths.Add(added);RecordPathUndo(()=>parkPaths.Remove(added));RebuildParkPaths();pedestrianRoutesDirty=true;RebuildPlacedPedestrians();
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
