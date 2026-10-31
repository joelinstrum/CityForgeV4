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
        public bool mountainBorder, ruggedMountainBorder, naturalMountainBorder, compactMountainBorder, variedMountainBorder;
        public int seed,topography;public float side;
        public override void Generate(TileData data,StopToken stop)
        {
            var source=data.ReadInletProduct(this);var result=new MatrixWorld(source);float phase=(seed%997)*.0063f;
            var variation=mountainBorder&&variedMountainBorder?new MountainBorderVariation(seed):null;
            float amplitude=topography switch{0=>0,1=>.055f,2=>.14f,3=>.28f,_=>.40f};
            for(int z=result.rect.Min.z;z<result.rect.Max.z;z++)for(int x=result.rect.Min.x;x<result.rect.Max.x;x++){
                if(stop!=null&&stop.stop)return;
                float u=(result.worldPos.x+(x-result.rect.offset.x)/(float)(result.rect.size.x-1)*result.worldSize.x)/side;
                float v=(result.worldPos.z+(z-result.rect.offset.z)/(float)(result.rect.size.z-1)*result.worldSize.z)/side;
                float borderMask=1;
                if(mountainBorder&&naturalMountainBorder){
                    // Reuse the approved river-flanking massifs, translated to the north edge.
                    float north=compactMountainBorder?1-(1-v)*1.6f:v;v=u;u=RegionalRiverNetwork.MainCenter(v,seed)+.22f+(north-.82f);
                    borderMask=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.40f,1-north));
                    amplitude=Mathf.Max(amplitude,.28f);
                }
                if(mountainBorder&&!naturalMountainBorder){var p=RegionTerrainLayout.Orient(new Vector2(u,v),seed);float edge=1-p.x;
                    if(ruggedMountainBorder){result[x,z]=RockyBorder(edge,p.y,phase,source[x,z]);continue;}
                    float band=Mathf.Exp(-Mathf.Pow((edge-.085f)/.085f,2))* (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.30f,edge)));
                    float peaks=.58f+.24f*Mathf.Sin(p.y*19+phase)+.18f*Mathf.Sin(p.y*37-phase);
                    result[x,z]=Mathf.Max(amplitude,.28f)*band*peaks+.008f*Mathf.Clamp01(source[x,z]);continue;}
                float noise=Mathf.Clamp01((source[x,z]-.25f)*2f),range=0;
                for(int band=0;band<4;band++){
                    if(mountainBorder&&naturalMountainBorder&&band%2==0)continue;
                    if(variation!=null&&variation.Pattern<2&&band==3)continue;
                    int sign=band%2==0?-1:1;float low=band<2?.06f:.50f,high=band<2?.51f:.96f;
                    if(variation!=null&&variation.Pattern<2){low=.035f;high=.965f;}
                    float along=Mathf.Clamp01((v-low)/(high-low));float vv=Mathf.Clamp(v,low,high);
                    float axis=RegionalRiverNetwork.MainCenter(vv,seed)+sign*(.22f+.045f*Mathf.Sin(vv*13+band+phase));
                    float warp=.018f*Mathf.Sin(v*39+noise*4+band);
                    float width=.065f+.025f*Mathf.Sin(vv*17+band)*Mathf.Sin(vv*17+band);
                    float distance=new Vector2((u-axis+warp)/width,(v-vv)/.055f).magnitude;
                    float crest=Mathf.Exp(-Mathf.Pow(distance,1.15f));
                    float taper=Mathf.Pow(Mathf.Sin(Mathf.PI*Mathf.Clamp(along,.015f,.985f)),.45f);
                    float peaks=.55f+.45f*Mathf.Pow(.5f+.5f*Mathf.Sin(vv*34+band*2+phase),2);
                    if(variation!=null&&variation.Pattern<2)peaks=variation.Peaks(vv);
                    range=Mathf.Max(range,crest*taper*(.7f+.3f*noise)*peaks);
                }
                float valley=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.12f,Mathf.Abs(u-RegionalRiverNetwork.MainCenter(v,seed))));
                float foot=.055f+.11f*noise;
                // Branched ridge ribs break up smooth range walls while retaining the main massifs.
                float ribA=1-Mathf.Abs(Mathf.PerlinNoise(u*29+phase,v*29)*2-1);
                float ribB=1-Mathf.Abs(Mathf.PerlinNoise(u*61-phase,v*61)*2-1);
                float ribs=Mathf.Pow(ribA,3)*.7f+Mathf.Pow(ribB,3)*.3f;
                float hill=topography<=1?noise*.65f:foot+range*(.94f+.07f*ribs);
                result[x,z]=mountainBorder&&naturalMountainBorder? .008f*Mathf.Clamp01(source[x,z])+amplitude*(foot+range*(.94f+.07f*ribs))*borderMask : amplitude*hill*valley;
            }
            data.StoreProduct(this,result);
        }
        float RockyBorder(float edge,float along,float phase,float source)
        {
            // Parallel, wandering divides with branched ribs; the last foothills end before the interior.
            float envelope=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.23f,.32f,edge));
            float mass=0;
            for(int ridge=0;ridge<3;ridge++){
                float axis=.055f+ridge*.078f+.012f*Mathf.Sin(along*19+phase+ridge*2)
                    +.008f*Mathf.Sin(along*47-phase+ridge);
                float width=.050f+ridge*.010f;
                float crest=Mathf.Exp(-Mathf.Pow(Mathf.Abs(edge-axis)/width,1.5f));
                float peaks=.70f+.30f*Mathf.Pow(.5f+.5f*Mathf.Sin(along*(35+ridge*8)+phase+ridge*3),.7f);
                float ribs=1-Mathf.Abs(Mathf.PerlinNoise(edge*105+phase+ridge*7,along*78-ridge*3)*2-1);
                float facets=.97f+.03f*Mathf.Pow(ribs,2);
                mass=Mathf.Max(mass,crest*peaks*facets*(1-ridge*.25f));
            }
            float spurs=Mathf.Pow(1-Mathf.Abs(Mathf.PerlinNoise(edge*38+phase,along*46)*2-1),3);
            return .008f*Mathf.Clamp01(source)+envelope*(mass*(.78f+topography*.0225f)+spurs*.012f);
        }
    }
}
