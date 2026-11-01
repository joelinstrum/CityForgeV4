using System;
using System.Linq;
using UnityEngine;
namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        float[,,] campBaseAlphamaps;
        // Derived presentation only: save canonical ground so wear never compounds on reload.
        void PaintCampCourtyards()
        {
            var camps=View.NativeCamps.Where(c=>View.ActiveDistrictMembers.Contains(c.district)).ToArray();
            if(camps.Length==0||campBaseAlphamaps!=null)return;
            int n=terrainData.alphamapResolution, count=terrainData.terrainLayers.Length;
            campBaseAlphamaps=terrainData.GetAlphamaps(0,0,n,n);
            var layers=new TerrainLayer[count+1];Array.Copy(terrainData.terrainLayers,layers,count);
            var supplied=Resources.Load<Texture2D>("NativeCamps/Courtyard-Dirt");
            var earth=Own(new TerrainLayer{name="Worn camp earth",diffuseTexture=supplied?supplied:Own(StreamBankBlend.EarthTexture(true)),tileSize=supplied?new Vector2(2.5f,2.5f):new Vector2(11.3f,11.3f),diffuseRemapMin=Vector4.zero,diffuseRemapMax=supplied?Vector4.one:new Vector4(.40f,.32f,.23f,1),smoothness=0,metallic=0,specular=new Color(.02f,.02f,.02f)});
            SharedDirtSurface.Apply(earth);
            layers[count]=earth;terrainData.terrainLayers=layers;
            var maps=new float[n,n,count+1];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                var point=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;
                float wear=0;foreach(var camp in camps)wear=Mathf.Max(wear,CampGroundWear(camp,point));
                for(int i=0;i<count;i++)maps[z,x,i]=campBaseAlphamaps[z,x,i]*(1-wear);
                maps[z,x,count]=wear;
            }
            terrainData.SetAlphamaps(0,0,maps);
        }
        public static float CampGroundWear(NativeCampRecord camp,Vector2 p)
        {
            var local=p-camp.center;float distance=local.magnitude;
            if(distance>camp.Radius)return 0;
            float patch=Mathf.PerlinNoise(p.x*.48f+17,p.y*.48f+31);
            float broad=Mathf.PerlinNoise(p.x*.17f+71,p.y*.17f+83);
            float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(camp.Radius*.57f,camp.Radius*.95f,distance+(broad-.5f)*3));
            float wear=edge*Mathf.Lerp(.58f,.95f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.72f,patch)));
            float core=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(camp.Radius*.38f,camp.Radius*.72f,distance+(broad-.5f)*1.5f));
            wear=Mathf.Max(wear,core*Mathf.Lerp(.94f,.995f,patch));
            // Narrow, fading desire paths lead from the courtyard to the inward-facing entrances.
            for(int i=0;i<camp.teepees;i++){
                var end=camp.TeepeePosition(i)-camp.center;
                float along=Mathf.Clamp01(Vector2.Dot(local,end)/end.sqrMagnitude);
                float cross=Vector2.Distance(local,end*along);
                float path=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1.25f,cross)))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,along)));
                wear=Mathf.Max(wear,path*Mathf.Lerp(.65f,.90f,patch));
            }
            return wear;
        }
    }
}
