using System;using System.Collections.Generic;using UnityEngine;using CityForge.World;
namespace CityForge.Editor { static class MeadowVegetationReferenceCheck { const float TileSide=32;
        public static GameObject BuildTile(MeadowDistrictLandscape landscape,Camera camera,Vector2Int cell)
        {
            var baseline=landscape.Baseline;var rng=landscape.RandomFor(cell.x,cell.y,95314);float R()=>(float)rng.NextDouble();
            var lists=new Dictionary<(Mesh,Material,int),List<Matrix4x4>>();
            for(int k=0;k<2100;k++){
                var p=new Vector2((cell.x+R())*TileSide,(cell.y+R())*TileSide);
                if(!landscape.Contains(p)||landscape.Height(p)<landscape.View.SeaLevel+.12f||landscape.RoadDistance(p)<baseline.roadWidth+.4f)continue;
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
                var p=new Vector2((cell.x+R())*TileSide,(cell.y+R())*TileSide);
                if(!landscape.Contains(p)||landscape.Canopy(p)<.22f||landscape.Noise(p,13,8)<.48f||landscape.Height(p)<landscape.View.SeaLevel+.15f||landscape.RoadDistance(p)<baseline.roadWidth+1)continue;
                var plant=baseline.forestFloorPlants[k%baseline.forestFloorPlants.Length];var matrix=Matrix4x4.TRS(new Vector3(p.x,landscape.Height(p)+.01f,p.y),Quaternion.Euler(0,R()*360,0),Vector3.one*baseline.forestFloorScale*(.7f+R()*.5f));
                foreach(var piece in plant.pieces){var key=(piece.mesh,piece.material,piece.submesh);if(!lists.TryGetValue(key,out var list))lists[key]=list=new List<Matrix4x4>();list.Add(matrix*piece.local);}
            }
            var batches=new List<NativeMeadowBatch>();var bounds=new Bounds(new Vector3(cell.x*TileSide+16,landscape.DisplayTerrain.terrainData.size.y/2,cell.y*TileSide+16),new Vector3(40,landscape.DisplayTerrain.terrainData.size.y+10,40));
            foreach(var pair in lists)for(int start=0;start<pair.Value.Count;start+=256){int count=Mathf.Min(256,pair.Value.Count-start);var matrices=new Matrix4x4[count];pair.Value.CopyTo(start,matrices,0,count);batches.Add(new NativeMeadowBatch{mesh=pair.Key.Item1,material=pair.Key.Item2,submesh=pair.Key.Item3,matrices=matrices,bounds=bounds});}
            var go=new GameObject("Native meadow tile "+cell);go.SetActive(false);go.transform.SetParent(landscape.transform,false);
            var renderer=go.AddComponent<MeadowGroundCoverRenderer>();renderer.combineCompatibleBatches=true;renderer.presentationCamera=camera;renderer.lod0Density=baseline.lod0Density;renderer.closestZoomDensity=baseline.closestZoomDensity;renderer.closestOrthographicThreshold=baseline.referenceViewSize*1.35f;renderer.batches=batches.ToArray();renderer.RebuildDensityCache();return go;
        }
}}
