using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private string newStreetTreeStyle;
        private int streetTreeBuildVersion;
        private sealed class StreetTreeView
        {
            internal GameObject Root;internal string Style;internal RegionSeason Season;internal bool Complete;
            internal List<StreetTreePalette.Plant> Plants;
        }
        private readonly Dictionary<DistrictRoadRecord,StreetTreeView> automaticStreetTrees=new();
        private Material streetTreeSoil;private Mesh streetTreePit;

        // Called by completed sidewalk/road mutations, never from an idle frame scan.
        private void RefreshAutomaticStreetTrees(bool force=false)
        {var work=RefreshAutomaticStreetTreesStages(force);while(work.MoveNext()){} }
        private System.Collections.IEnumerator RefreshAutomaticStreetTreesStages(bool force=false)
        {
            int version=++streetTreeBuildVersion;
            var roads=districtRoadMeshes.Keys.ToArray();
            PruneAutomaticStreetTrees();
            if(!roads.Any(r=>HasSidewalks(r.surfaceId)&&StreetTreePalette.Find(r.streetTreeStyle)!=null))yield break;
            var lines=roads.ToDictionary(r=>r,r=>RoadCenterline(r));
            var catalog=LotAssetCatalog.Load();var water=DistrictLotWater();int total=0;
            var entries=new List<(Vector2[] Line,bool Closed,float Radius)>();
            foreach(var lot in districtLotRoots.Keys)
            {
                var footprint=new DistrictLotFootprint(lot);var layout=lot.definition.zoningLayout;
                if(layout.hasEntrance)Entry(footprint.WorldPoint(layout.entrance));
                if(layout.hasDriveway)Entry(footprint.WorldPoint(layout.driveway));
                foreach(var path in lot.definition.paths)if(path.points?.Length>1)entries.Add((path.points.Select(footprint.WorldPoint).ToArray(),path.closed,Mathf.Max(1,path.width*.5f)));
                foreach(var item in lot.definition.objects)
                    if(catalog?.Find(item.assetId)?.category=="Buildings"&&LotShopkeeper.TryEntrance(lot.definition,catalog,item.position,null,out var door,out _))Entry(footprint.WorldPoint(door));
            }
            void Entry(Vector2 door)
            {
                var nearest=roads.OrderBy(r=>(ClosestRoadPoint(r,door)-door).sqrMagnitude).FirstOrDefault();
                if(nearest!=null)entries.Add((new[]{door,ClosestRoadPoint(nearest,door)},false,1.25f));
            }
            var occupied=new Dictionary<Vector2Int,List<Vector2>>();
            foreach(var road in roads)
            {
                yield return null;if(version!=streetTreeBuildVersion)yield break;
                var style=StreetTreePalette.Find(road.streetTreeStyle);if(style==null||!HasSidewalks(road.surfaceId))continue;
                var plants=StreetTreePalette.Plan(road,lines[road],Clear);
                if(plants.Count>Math.Max(0,1024-total))plants=plants.Take(Math.Max(0,1024-total)).ToList();total+=plants.Count;
                foreach(var plant in plants){var cell=Cell(plant.Point);if(!occupied.TryGetValue(cell,out var points))occupied[cell]=points=new();points.Add(plant.Point);}
                if(automaticStreetTrees.TryGetValue(road,out var old)&&old.Complete&&!force&&old.Style==road.streetTreeStyle&&old.Season==VisualSeason&&Same(old.Plants,plants))continue;
                if(old!=null)DisposeStreetTrees(old);
                var root=new GameObject("Automatic street trees · "+style.Label);root.transform.SetParent(transform,false);
                var view=new StreetTreeView{Root=root,Style=road.streetTreeStyle,Season=VisualSeason,Plants=plants};automaticStreetTrees[road]=view;
                int built=0;
                foreach(var plant in plants)
                {
                    if((built++%8)==0){yield return null;if(version!=streetTreeBuildVersion)yield break;}
                    GameObject tree;
                    if(style.AssetId==null){tree=CreateStreetPalm(root.transform);if(tree)tree.transform.localRotation=Quaternion.Euler(0,plant.Yaw,0); }
                    else
                    {
                        var asset=catalog?catalog.Find(style.AssetId):null;
                        if(asset==null||!asset.prefab||LotAssetCatalog.IsRetiredTree(asset.id))continue;
                        tree=LotRuntimeLoader.InstantiateObject(new LotObjectDefinition{assetId=asset.id,scale=style.Scale,yaw=plant.Yaw},asset,root.transform,VisualSeason,GetComponentInChildren<CityForgeCameraController>());
                    }
                    if(!tree)continue;
                    tree.transform.position=new Vector3(plant.Point.x,LotHeight(plant.Point)+(IsDividedBoulevard(road.surfaceId ) ? .17f : 0),plant.Point.y);
                    if(!style.HasPlanter&&!IsDividedBoulevard(road.surfaceId))AddStreetTreePit(root.transform,plant.Point);
                }
                view.Complete=true;

                bool Clear(Vector2 point)
                {
                    if(!InActiveDistrict(point)||total>=1024)return false;
                    float height=LotHeight(point);
                    if(TryLotWater(point,height,water,out _)||(!Waterless&&water==null&&!IsTownLand(point/MapSide+Vector2.one*.5f)))return false;
                    if(roadBuildings.Any(b=>b.Contains(point,style.CrownWidth*.5f+.15f)))return false;
                    foreach(var other in roads)
                    {
                        if(other==road&&IsDividedBoulevard(road.surfaceId))continue;
                        float clearance=other==road?road.width*.5f+.45f:other.width*.5f+(HasSidewalks(other.surfaceId)?LotPathSurface.ResidentialSidewalkWidth:0)+2f;
                        if(IsDividedBoulevard(road.surfaceId)&&other!=road)clearance=Mathf.Max(clearance,other.width*.5f+14);
                        if(LineDistance(point,lines[other],other.closed)<clearance)return false;
                    }
                    // Protect resolved entrance/driveway corridors and authored paths.
                    if(entries.Any(e=>LineDistance(point,e.Line,e.Closed)<e.Radius))return false;
                    if(PlacedActors.Any(a=>(a.tree||a.streetscape!=null)&&(a.position-point).sqrMagnitude<4))return false;
                    if(lotTreePositions.Any(p=>(p-point).sqrMagnitude<4))return false;
                    var at=Cell(point);for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)
                        if(occupied.TryGetValue(at+new Vector2Int(x,z),out var neighbors)&&neighbors.Any(p=>(p-point).sqrMagnitude<25))return false;
                    return true;
                }
            }
        }
        private void PruneAutomaticStreetTrees()
        {
            foreach(var stale in automaticStreetTrees.Keys.Where(r=>!districtRoadMeshes.ContainsKey(r)||!HasSidewalks(r.surfaceId)||string.IsNullOrEmpty(r.streetTreeStyle)).ToArray())
            {DisposeStreetTrees(automaticStreetTrees[stale]);automaticStreetTrees.Remove(stale);}
        }
        private bool NearAutomaticStreetTree(Vector2 point,float radius)=>automaticStreetTrees.Values.Any(v=>v.Plants.Any(p=>(p.Point-point).sqrMagnitude<radius*radius));
        private static Vector2Int Cell(Vector2 p)=>Vector2Int.FloorToInt(p/8);
        private static bool Same(List<StreetTreePalette.Plant> a,List<StreetTreePalette.Plant> b)
        {if(a.Count!=b.Count)return false;for(int i=0;i<a.Count;i++)if(a[i].Point!=b[i].Point||a[i].Yaw!=b[i].Yaw)return false;return true;}
        private static float LineDistance(Vector2 p,IReadOnlyList<Vector2> line,bool closed)
        {
            float best=float.PositiveInfinity;for(int i=0;i<line.Count-(closed?0:1);i++)
            {var a=line[i];var d=line[(i+1)%line.Count]-a;var near=a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.000001f,d.sqrMagnitude));best=Mathf.Min(best,(p-near).magnitude);}return best;
        }
        private GameObject CreateStreetPalm(Transform parent)
        {
            var assets=Resources.Load<TropicalPalmAssets>("TropicalCoconut/Palms");
            var variant=assets&&assets.variants?.Length>0?assets.variants[0]:null;
            if(variant?.lods==null||variant.lods.Length==0||!variant.lods[0]||!assets.material)return null;
            var root=new GameObject("Street coconut palm");root.transform.SetParent(parent,false);
            var bounds=variant.lods[0].bounds;float scale=8/Mathf.Max(.01f,bounds.size.y);
            var model=new GameObject("Authored palm LODs");model.transform.SetParent(root.transform,false);model.transform.localScale=Vector3.one*scale;model.transform.localPosition=Vector3.up*(-bounds.min.y*scale);
            var lods=new List<LOD>();float[] thresholds={.22f,.1f,.04f,.008f};
            for(int i=0;i<variant.lods.Length&&i<4;i++)
            {
                if(!variant.lods[i])continue;var go=new GameObject("Palm LOD "+i,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(model.transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=variant.lods[i];var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=assets.material;lods.Add(new LOD(thresholds[i],new Renderer[]{renderer}));
            }
            var group=model.AddComponent<LODGroup>();group.SetLODs(lods.ToArray());group.RecalculateBounds();return root;
        }
        private void AddStreetTreePit(Transform parent,Vector2 p)
        {
            if(!streetTreeSoil){streetTreeSoil=LotPathMaterial.Find("rich-brown-garden-soil").CreateMaterial();streetTreeSoil.renderQueue=DistrictLotSurfaces.SidewalkRenderQueue+1;}
            if(!streetTreePit)
            {
                streetTreePit=new Mesh{name="Street planting pit"};streetTreePit.vertices=new[]{new Vector3(-.55f,0,-.55f),new Vector3(-.55f,0,.55f),new Vector3(.55f,0,.55f),new Vector3(.55f,0,-.55f)};
                streetTreePit.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};streetTreePit.triangles=new[]{0,1,2,0,2,3};streetTreePit.RecalculateNormals();streetTreePit.RecalculateBounds();
            }
            var pit=new GameObject("Street tree soil pit",typeof(MeshFilter),typeof(MeshRenderer));pit.transform.SetParent(parent,false);pit.transform.position=new Vector3(p.x,LotHeight(p)+SidewalkLift+.012f,p.y);
            pit.GetComponent<MeshFilter>().sharedMesh=streetTreePit;pit.GetComponent<MeshRenderer>().sharedMaterial=streetTreeSoil;pit.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        private void DisposeStreetTrees(StreetTreeView view){if(view.Root){view.Root.SetActive(false);Destroy(view.Root);}}
        private void ClearAutomaticStreetTrees()
        {
            streetTreeBuildVersion++;
            foreach(var view in automaticStreetTrees.Values)DisposeStreetTrees(view);automaticStreetTrees.Clear();
            if(streetTreeSoil)Destroy(streetTreeSoil);if(streetTreePit)Destroy(streetTreePit);streetTreeSoil=null;streetTreePit=null;
        }
    }
}
