using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Streams the test's actual six mesh/material pieces using its placement and thinning rules.</summary>
    public sealed class MeadowRuntimeVegetation : MonoBehaviour
    {
        const float TileSide=32;
        MeadowDistrictLandscape landscape;Camera camera;CityForge.CameraSystem.CameraMotionQuality motion;
        readonly Dictionary<Vector2Int,GameObject> tiles=new();
        readonly List<Vector2Int> expired=new();
        readonly List<Vector2Int> pending=new();int nextPending;
        IEnumerator building;Vector2Int buildingCell;double tileCpuMilliseconds;long sliceStart;
        const double SliceBudgetMilliseconds=2;
        public double MaximumStreamingSliceMilliseconds {get;private set;}
        bool BudgetSpent=>(System.Diagnostics.Stopwatch.GetTimestamp()-sliceStart)*1000.0/System.Diagnostics.Stopwatch.Frequency>=SliceBudgetMilliseconds;
        public double LastTileBuildMilliseconds {get;private set;}
        public double MaximumTileBuildMilliseconds {get;private set;}
        public int BuiltTiles {get;private set;}
        public int PendingTiles=>pending.Count-nextPending+(building!=null?1:0);
        Vector2Int lastCell=new(int.MinValue,int.MinValue);int lastRadius;
        public int LoadedTiles=>tiles.Count;
        public void Initialize(MeadowDistrictLandscape source,Camera view){landscape=source;camera=view;motion=view.GetComponent<CityForge.CameraSystem.CameraMotionQuality>();}
        void LateUpdate()
        {
            if(!landscape||!camera||(motion&&motion.NavigationWorkSuspended))return;
            var ray=new Ray(camera.transform.position,camera.transform.forward);var plane=new Plane(Vector3.up,Vector3.up*landscape.View.SeaLevel);
            if(!plane.Raycast(ray,out var distance))return;var focus=camera.transform.position+camera.transform.forward*distance;
            var cell=Vector2Int.FloorToInt(new Vector2(focus.x,focus.z)/TileSide);int radius=Mathf.CeilToInt(Mathf.Clamp(camera.orthographicSize*2.4f+32,96,256)/TileSide);
            if(cell!=lastCell||radius!=lastRadius){
                lastCell=cell;lastRadius=radius;expired.Clear();
                if(building!=null&&(Mathf.Abs(buildingCell.x-cell.x)>radius+1||Mathf.Abs(buildingCell.y-cell.y)>radius+1)){(building as IDisposable)?.Dispose();building=null;}
                // A one-tile margin avoids destruction/recreation when crossing a cell edge.
                foreach(var pair in tiles)if(Mathf.Abs(pair.Key.x-cell.x)>radius+1||Mathf.Abs(pair.Key.y-cell.y)>radius+1)expired.Add(pair.Key);
                foreach(var key in expired){Destroy(tiles[key]);tiles.Remove(key);}
                pending.Clear();nextPending=0;
                for(int z=cell.y-radius;z<=cell.y+radius;z++)for(int x=cell.x-radius;x<=cell.x+radius;x++){
                    var key=new Vector2Int(x,z);if(!tiles.ContainsKey(key)&&!(building!=null&&key==buildingCell)&&Intersects(key))pending.Add(key);
                }
                pending.Sort((a,b)=>(a-cell).sqrMagnitude.CompareTo((b-cell).sqrMagnitude));
            }
            // Preserve the tile RNG across resumable slices. Partial tiles stay hidden.
            sliceStart=System.Diagnostics.Stopwatch.GetTimestamp();
            if(building==null&&nextPending<pending.Count){buildingCell=pending[nextPending++];building=BuildTile(buildingCell);tileCpuMilliseconds=0;}
            if(building!=null){
                bool running=building.MoveNext();
                double used=(System.Diagnostics.Stopwatch.GetTimestamp()-sliceStart)*1000.0/System.Diagnostics.Stopwatch.Frequency;
                tileCpuMilliseconds+=used;MaximumStreamingSliceMilliseconds=Math.Max(MaximumStreamingSliceMilliseconds,used);
                if(!running){(building as IDisposable)?.Dispose();building=null;LastTileBuildMilliseconds=tileCpuMilliseconds;MaximumTileBuildMilliseconds=Math.Max(MaximumTileBuildMilliseconds,tileCpuMilliseconds);BuiltTiles++;}
            }
        }
        bool Intersects(Vector2Int key){var p=(Vector2)key*TileSide;var o=landscape.Origin;return p.x+TileSide>o.x&&p.y+TileSide>o.y&&p.x<o.x+landscape.Side&&p.y<o.y+landscape.Side;}
        IEnumerator BuildTile(Vector2Int cell)
        {
            var baseline=landscape.Baseline;var rng=landscape.RandomFor(cell.x,cell.y,95314);float R()=>(float)rng.NextDouble();
            var lists=new Dictionary<(Mesh,Material,int),List<Matrix4x4>>();
            for(int k=0;k<2100;k++){
                if(k%32==0&&BudgetSpent)yield return null;
                var p=new Vector2((cell.x+R())*TileSide,(cell.y+R())*TileSide);
                if(landscape.View.InCampClearing(p)||landscape.MountainVegetation(p)<.35f||!landscape.Contains(p)||landscape.WaterDistance(p)<.3f||landscape.ExposedRiverBed(p)||landscape.Height(p)<landscape.View.SeaLevel+.12f||landscape.RoadDistance(p)<baseline.roadWidth+.4f)continue;
                if(landscape.View.Coastline&&(landscape.Height(p)<landscape.View.SeaLevel+7||landscape.MountainSlope(p)>24))continue;
                float forest=landscape.Canopy(p),density=Mathf.Lerp(.75f,.18f,forest)*(.8f+.2f*landscape.Noise(p,31));if(R()>density)continue;
                int type=R()<.52f?0:1;if(R()<.045f+.12f*forest)type=2;
                float patch=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.53f,.76f,landscape.Noise(p,26,14)));
                if(R()<.055f*patch*(1-forest))type=3+(int)(R()*3);
                float quietPatch=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.48f,.7f,landscape.Noise(p,18,7)));
                float bank=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,10,Mathf.Abs(landscape.ShoreDistance(p))));
                float edge=Mathf.Max(forest,bank);float keep=type>=2?quietPatch*Mathf.Lerp(.12f,.7f,edge):Mathf.Lerp(.27f,.82f,Mathf.Max(quietPatch,edge));if(R()>keep)continue;
                float scale=.8f+R()*.35f;Vector3 size=type<2?new Vector3(scale,scale*(.43f+.16f*forest),scale):Vector3.one*(type==2?.43f+R()*.2f:.65f+R()*.25f);
                var matrix=Matrix4x4.TRS(new Vector3(p.x,landscape.Height(p),p.y),Quaternion.Euler(0,R()*360,0),size);
                foreach(var piece in baseline.plants[type].pieces){var key=(piece.mesh,piece.material,piece.submesh);if(!lists.TryGetValue(key,out var list))lists[key]=list=new List<Matrix4x4>();list.Add(matrix*piece.local);}
            }
            if(baseline.forestFloorPlants!=null)for(int k=0;k<180;k++){
                if(k%32==0&&BudgetSpent)yield return null;
                var p=new Vector2((cell.x+R())*TileSide,(cell.y+R())*TileSide);
                if(landscape.View.InCampClearing(p)||landscape.MountainVegetation(p)<.35f||!landscape.Contains(p)||landscape.WaterDistance(p)<.3f||landscape.ExposedRiverBed(p)||landscape.Canopy(p)<.22f||landscape.Noise(p,13,8)<.48f||landscape.Height(p)<landscape.View.SeaLevel+.15f||landscape.RoadDistance(p)<baseline.roadWidth+1)continue;
                var plant=baseline.forestFloorPlants[k%baseline.forestFloorPlants.Length];var matrix=Matrix4x4.TRS(new Vector3(p.x,landscape.Height(p)+.01f,p.y),Quaternion.Euler(0,R()*360,0),Vector3.one*baseline.forestFloorScale*(.7f+R()*.5f));
                foreach(var piece in plant.pieces){var key=(piece.mesh,piece.material,piece.submesh);if(!lists.TryGetValue(key,out var list))lists[key]=list=new List<Matrix4x4>();list.Add(matrix*piece.local);}
            }
            var batches=new List<NativeMeadowBatch>();var bounds=new Bounds(new Vector3(cell.x*TileSide+16,landscape.DisplayTerrain.terrainData.size.y/2,cell.y*TileSide+16),new Vector3(40,landscape.DisplayTerrain.terrainData.size.y+10,40));
            foreach(var pair in lists)for(int start=0;start<pair.Value.Count;start+=256){int count=Mathf.Min(256,pair.Value.Count-start);var matrices=new Matrix4x4[count];pair.Value.CopyTo(start,matrices,0,count);batches.Add(new NativeMeadowBatch{mesh=pair.Key.Item1,material=pair.Key.Item2,submesh=pair.Key.Item3,matrices=matrices,bounds=bounds});}
            if(BudgetSpent)yield return null;
            var go=new GameObject("Native meadow tile "+cell);go.SetActive(false);go.transform.SetParent(transform,false);bool finished=false;
            try{
                var renderer=go.AddComponent<MeadowGroundCoverRenderer>();renderer.combineCompatibleBatches=true;renderer.presentationCamera=camera;renderer.navigationQuality=motion;renderer.lod0Density=baseline.lod0Density;renderer.closestZoomDensity=baseline.closestZoomDensity;renderer.closestOrthographicThreshold=baseline.referenceViewSize*1.35f;renderer.batches=batches.ToArray();
                var cache=renderer.BuildDensityCache();while(cache.MoveNext())if(BudgetSpent)yield return null;
                if(BudgetSpent)yield return null;
                go.SetActive(true);tiles[cell]=go;finished=true;
            }finally{if(!finished&&go)Destroy(go);}
        }
        void OnDestroy(){(building as IDisposable)?.Dispose();building=null;foreach(var tile in tiles.Values)if(tile){var renderer=tile.GetComponent<MeadowGroundCoverRenderer>();renderer.batches=null;}tiles.Clear();}
    }
}
