using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Seeded static rock placement; instanced batches, no per-rock update or collider.</summary>
    public sealed class RiverBedRockRenderer:MonoBehaviour
    {
        RiverBedRockAssets assets;Matrix4x4[][] matrices;Bounds bounds;
        public int RockCount {get;private set;}
        public void Initialize(MeadowDistrictLandscape landscape)
        {
            assets=Resources.Load<RiverBedRockAssets>(landscape.View.RiverBedVersion>=3?"RiverBeds/RichRockAssets":"RiverBeds/RockAssets");if(!assets)throw new System.Exception("Missing river rock assets.");
            var batches=new List<Matrix4x4>[assets.meshes.Length];for(int i=0;i<batches.Length;i++)batches[i]=new List<Matrix4x4>();
            var rng=landscape.RandomFor(0,0,941);float R()=>(float)rng.NextDouble();
            for(float z=landscape.Origin.y+2;z<landscape.Origin.y+landscape.Side-2;z+=3.5f){float v=z/landscape.View.MapSide+.5f;if(v<.52f)continue;
                bool rich=landscape.View.RiverBedVersion>=3;int count=rich?(R()<.35f?5:2):(R()<.28f?3:1);for(int j=0;j<count;j++){
                    float rz=z+(R()-.5f)*3;
                    // Keep most rocks near the upper slope and bank lip, above future water.
                    // The channel half-width is 10 m; a small minority remains in the bed.
                    float offset=rich?(R()<.85f?(R()<.5f?-1:1)*Mathf.Lerp(8f,10.5f,R()):(R()-.5f)*12f):(R()-.5f)*5f;
                    float x=(landscape.View.Data.TributaryRiverCenter(rz/landscape.View.MapSide+.5f)-.5f)*landscape.View.MapSide+offset;
                    var p=new Vector2(x,rz);if(!landscape.Contains(p))continue;float center=(landscape.View.Data.TributaryRiverCenter(rz/landscape.View.MapSide+.5f)-.5f)*landscape.View.MapSide;
                    bool moss=rich&&R()<(Mathf.Abs(x-center)>3?.65f:.12f);int type=rich?rng.Next(3)+(moss?3:0):rng.Next(batches.Length);var mesh=assets.meshes[type];float size=rich?Mathf.Lerp(.45f,moss?2f:1.5f,R()):Mathf.Lerp(.30f,1.1f,R());float scale=size/Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));
                    var rotation=Quaternion.Euler(R()*18,R()*360,R()*15);var position=new Vector3(x,landscape.Height(p)+size*.12f,rz);
                    batches[type].Add(Matrix4x4.TRS(position,rotation,Vector3.one*scale)*Matrix4x4.Translate(-mesh.bounds.center));RockCount++;
                }
            }
            matrices=new Matrix4x4[batches.Length][];for(int i=0;i<matrices.Length;i++)matrices[i]=batches[i].ToArray();
            bounds=new Bounds(new Vector3(landscape.Origin.x+landscape.Side*.5f,landscape.DisplayTerrain.terrainData.size.y*.5f,landscape.Origin.y+landscape.Side*.5f),new Vector3(landscape.Side+4,landscape.DisplayTerrain.terrainData.size.y+4,landscape.Side+4));
        }
        void LateUpdate(){if(!assets)return;for(int i=0;i<matrices.Length;i++){if(matrices[i].Length==0)continue;var rp=new RenderParams(assets.materials[i]){worldBounds=bounds,shadowCastingMode=ShadowCastingMode.On,receiveShadows=true};Graphics.RenderMeshInstanced(rp,assets.meshes[i],0,matrices[i]);}}
    }
}
