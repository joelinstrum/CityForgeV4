using UnityEngine;
using WaveHarmonic.Crest;
namespace CityForge.World
{
    /// <summary>One owned Crest level input aligns channel water with local ground.</summary>
    public sealed class GroundFollowingWaterLevels:MonoBehaviour
    {
        Texture2D levels;
        public void Initialize(MeadowDistrictLandscape land,WaterRenderer water,float datum)
        {
            bool ocean=false;foreach(var body in land.View.Data.WaterBodies)if(body.kind==WaterBodyKind.Ocean)ocean=true;
            const int n=513;var pixels=new Color[n*n];float low=float.PositiveInfinity,high=float.NegativeInfinity;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                var p=land.Origin+new Vector2((x+.5f)/n,(z+.5f)/n)*land.Side;
                float h=RegionalRiverNetwork.GroundWaterLevel(land.View.Data,p.x/land.View.MapSide+.5f,p.y/land.View.MapSide+.5f);
                // A shared ocean retains its original datum and shelf depth.
                if(ocean)h=Mathf.Max(datum,h);
                low=Mathf.Min(low,h);high=Mathf.Max(high,h);pixels[z*n+x]=new Color(h-datum,0,0,1);
            }
            levels=new Texture2D(n,n,TextureFormat.RFloat,false,true){name="Ground-following channel water levels",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};levels.SetPixels(pixels);levels.Apply(false,true);
            water.LevelLod.Enabled=true;water.LevelLod.Resolution=512;
            // Crest packs local levels into this buffer even when there are no wave inputs.
            water.AnimatedWavesLod.Enabled=true;
            transform.position=new Vector3(land.Origin.x+land.Side/2,datum,land.Origin.y+land.Side/2);transform.localScale=new Vector3(land.Side,1,land.Side);
            var input=WaveHarmonic.Crest._Extensions.AddComponent<LevelLodInput>(gameObject,LodInputMode.Texture);
            input.OverrideHeight=true;input.HeightRange=new Vector2(low,high);input.GetData<LevelTextureLodInputData>().Texture=levels;
        }
        void OnDestroy(){if(levels)Destroy(levels);}
    }
}
