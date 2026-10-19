using UnityEngine;
using MapMagic.Nodes;
using MapMagic.Products;
using Den.Tools.Matrices;
namespace CityForge.World
{
    /// <summary>CityForge's regional range/valley constraints inside the MapMagic graph.</summary>
    [System.Serializable]
    public sealed class RegionalLandformNode:Generator,IInlet<MatrixWorld>,IOutlet<MatrixWorld>
    {
        public int seed,topography;public float side;
        public override void Generate(TileData data,StopToken stop)
        {
            var source=data.ReadInletProduct(this);var result=new MatrixWorld(source);float phase=(seed%997)*.0063f;
            float amplitude=topography switch{0=>0,1=>.055f,2=>.14f,3=>.28f,_=>.40f};
            for(int z=result.rect.Min.z;z<result.rect.Max.z;z++)for(int x=result.rect.Min.x;x<result.rect.Max.x;x++){
                if(stop!=null&&stop.stop)return;
                float u=(result.worldPos.x+(x-result.rect.offset.x)/(float)(result.rect.size.x-1)*result.worldSize.x)/side;
                float v=(result.worldPos.z+(z-result.rect.offset.z)/(float)(result.rect.size.z-1)*result.worldSize.z)/side;
                float noise=Mathf.Clamp01((source[x,z]-.25f)*2f),range=0;
                for(int band=0;band<4;band++){
                    int sign=band%2==0?-1:1;float low=band<2?.06f:.50f,high=band<2?.51f:.96f;
                    float along=Mathf.Clamp01((v-low)/(high-low));float vv=Mathf.Clamp(v,low,high);
                    float axis=RegionalRiverNetwork.MainCenter(vv,seed)+sign*(.22f+.045f*Mathf.Sin(vv*13+band+phase));
                    float warp=.018f*Mathf.Sin(v*39+noise*4+band);
                    float width=.065f+.025f*Mathf.Sin(vv*17+band)*Mathf.Sin(vv*17+band);
                    float distance=new Vector2((u-axis+warp)/width,(v-vv)/.055f).magnitude;
                    float crest=Mathf.Exp(-Mathf.Pow(distance,1.15f));
                    float taper=Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Clamp(along,.015f,.985f)),.45f);
                    float peaks=.55f+.45f*Mathf.Pow(.5f+.5f*Mathf.Sin(vv*34+band*2+phase),2);
                    range=Mathf.Max(range,crest*taper*(.7f+.3f*noise)*peaks);
                }
                float valley=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.12f,Mathf.Abs(u-RegionalRiverNetwork.MainCenter(v,seed))));
                float foot=.055f+.11f*noise;
                // Branched ridge ribs break up smooth range walls while retaining the main massifs.
                float ribA=1-Mathf.Abs(Mathf.PerlinNoise(u*29+phase,v*29)*2-1);
                float ribB=1-Mathf.Abs(Mathf.PerlinNoise(u*61-phase,v*61)*2-1);
                float ribs=Mathf.Pow(ribA,3)*.7f+Mathf.Pow(ribB,3)*.3f;
                float hill=topography<=1?noise*.65f:foot+range*(.94f+.07f*ribs);
                result[x,z]=amplitude*hill*valley;
            }
            data.StoreProduct(this,result);
        }
    }
}
