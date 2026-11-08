using UnityEngine;
using WaveHarmonic.Crest;
namespace CityForge.World
{
    /// <summary>One owned Crest level input aligns channel water with local ground.</summary>
    public sealed class GroundFollowingWaterLevels:MonoBehaviour
    {
        Texture2D levels,foamLimit;
        Mesh foamMesh;
        Material foamMaterial;
        public void Initialize(MeadowDistrictLandscape land,WaterRenderer water,float datum,bool applyLevels=true)
        {
            bool ocean=false;foreach(var body in land.View.Data.WaterBodies)if(body.kind==WaterBodyKind.Ocean)ocean=true;
            var tidalRiver=System.Array.Find(land.View.Data.WaterBodies,body=>body.id=="tidal-river");
            var riverBounds=tidalRiver?.Bounds??Rect.zero;
            var layout=tidalRiver!=null?new RegionTerrainLayout(land.View.CreationSettings):null;
            float mapSide=land.View.MapSide,side=land.Side;
            var origin=land.Origin;
            const int n=513;var pixels=applyLevels?new Color[n*n]:null;var foam=ocean?new Color[n*n]:null;float low=float.PositiveInfinity,high=float.NegativeInfinity;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                var p=origin+new Vector2((x+.5f)/n,(z+.5f)/n)*side;
                var uv=p/mapSide+Vector2.one*.5f;
                float h=applyLevels?RegionalRiverNetwork.GroundWaterLevel(land.View.Data,uv.x,uv.y):datum;
                if(ocean)h=Mathf.Max(datum,h);
                if(applyLevels){low=Mathf.Min(low,h);high=Mathf.Max(high,h);pixels[z*n+x]=new Color(h-datum,0,0,1);}
                if(ocean){
                    float limit=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(datum+.5f,datum+2,h));
                    if(tidalRiver!=null){
                        float inside=riverBounds.Contains(uv)?tidalRiver.halfWidthNormalized-tidalRiver.IndexedCenterlineDistance(uv):0;
                        float estuary=land.RoundedEstuaryDistance(uv);
                        if(estuary<0&&layout.LandDistance(uv)>=0)inside=Mathf.Max(inside,-estuary);
                        inside*=mapSide;
                        float river=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,8,inside));
                        if(river>0){
                            float offshore=-layout.LandDistance(uv)*mapSide;
                            float surf=Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,75,offshore));
                            limit*=Mathf.Lerp(1,surf,river);
                        }
                    }
                    foam[z*n+x]=new Color(limit,0,0,1);
                }
            }
            transform.position=new Vector3(land.Origin.x+land.Side/2,datum,land.Origin.y+land.Side/2);
            transform.localScale=new Vector3(land.Side,1,land.Side);
            if(applyLevels){
                levels=new Texture2D(n,n,TextureFormat.RFloat,false,true){name="Ground-following channel water levels",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
                levels.SetPixels(pixels);levels.Apply(false,true);
                water.LevelLod.Enabled=true;water.LevelLod.Resolution=512;
                // Crest packs local levels into this buffer even when there are no wave inputs.
                water.AnimatedWavesLod.Enabled=true;
                var input=WaveHarmonic.Crest._Extensions.AddComponent<LevelLodInput>(gameObject,LodInputMode.Texture);
                input.OverrideHeight=true;input.HeightRange=new Vector2(low,high);input.GetData<LevelTextureLodInputData>().Texture=levels;
            }
            if(ocean){
                // Preserve surf at the ocean datum; elevated inland beds must not
                // inherit ocean shoreline foam/under-terrain priming.
                foamLimit=new Texture2D(n,n,TextureFormat.RHalf,false,true){name="Coastal inland foam limit",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
                foamLimit.SetPixels(foam);foamLimit.Apply(false,true);
                var shader=Resources.Load<Shader>("Water/InlandFoamExclusion");
                if(!shader)throw new System.InvalidOperationException("Missing inland foam exclusion shader.");
                foamMaterial=new Material(shader);foamMaterial.SetTexture("_FoamLimit",foamLimit);
                foamMesh=new Mesh{name="Inland foam exclusion footprint"};
                foamMesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f)};
                foamMesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.right,Vector2.one};foamMesh.triangles=new[]{0,1,2,2,1,3};foamMesh.RecalculateBounds();
                var host=new GameObject("Inland channel foam exclusion",typeof(MeshFilter),typeof(MeshRenderer));host.transform.SetParent(transform,false);
                host.GetComponent<MeshFilter>().sharedMesh=foamMesh;
                var renderer=host.GetComponent<MeshRenderer>();renderer.sharedMaterial=foamMaterial;renderer.forceRenderingOff=true;
                var exclusion=WaveHarmonic.Crest._Extensions.AddComponent<FoamLodInput>(host,LodInputMode.Renderer);
                exclusion.GetData<FoamRendererLodInputData>().Renderer=renderer;exclusion.Queue=1000;
            }
        }
        void OnDestroy(){if(levels)Destroy(levels);if(foamLimit)Destroy(foamLimit);if(foamMesh)Destroy(foamMesh);if(foamMaterial)Destroy(foamMaterial);}
    }
}
