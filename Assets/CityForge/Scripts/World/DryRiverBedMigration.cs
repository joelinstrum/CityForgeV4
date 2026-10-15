using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    public static class DryRiverBedMigration
    {
        public static void Channel(PrototypeRegionData data,float side,float u,float v,out float distance,out float width,out float depth,int version=1)
        {
            distance=Mathf.Abs(u-data.MainRiverCenter(v))*side;width=22;depth=6;
            if(v>=.52f){float d=Mathf.Abs(u-data.TributaryRiverCenter(v))*side;
                if(d/14<distance/22){distance=d;width=14;depth=3.5f;}}
            if(version>=2){distance=Mathf.Abs(u-data.TributaryRiverCenter(v))*side;width=10;depth=3.5f;if(v<.52f)distance=side;}
            if(v<PrototypeRegionData.Coast(u))distance=side;
        }
        public static void Apply(SavedRegionSnapshot snapshot,bool singleRiver=false)
        {
            if(!snapshot.info.waterless||snapshot.meadow==null||snapshot.info.riverBedVersion!=0)throw new System.InvalidOperationException("Requires original dry meadow snapshot.");
            float side=1024*Mathf.Pow(2,snapshot.info.size),vertical=1200*side/PrototypeRegionData.Side;
            int version=singleRiver?2:1;
            if(singleRiver){
                var original=(float[,])snapshot.data.Heights.Clone();
                for(int z=0;z<513;z++)for(int x=0;x<513;x++){
                    float u=x/512f,v=z/512f;float main=snapshot.data.MainRiverCenter(v),stream=snapshot.data.TributaryRiverCenter(v);
                    float filled=original[z,x];
                    foreach(float center in v>=.52f?new[]{main,stream}:new[]{main}){
                        float half=40/side,d=Mathf.Abs(u-center);if(d>=half)continue;
                        float bank=Mathf.Lerp(Sample(original,center-half,v),Sample(original,center+half,v),Mathf.InverseLerp(center-half,center+half,u));
                        filled=Mathf.Lerp(filled,Mathf.Max(filled,bank),1-Mathf.SmoothStep(0,1,d/half));
                    }
                    snapshot.data.Heights[z,x]=filled;
                }
            }
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){
                Channel(snapshot.data,side,x/512f,z/512f,out float d,out float w,out float depth,version);
                float incision=depth*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(w*.28f,w,d)));
                // Raise the vertical datum to allow excavation below the old zero-height floor.
                float h=snapshot.data.Heights[z,x]+(8-incision)/vertical;
                if(h<0||h>1)throw new System.InvalidOperationException("Riverbed exceeds terrain range.");snapshot.data.Heights[z,x]=h;
                if(singleRiver)snapshot.data.WaterDistance[z,x]=d*PrototypeRegionData.Side/side;
            }
            var old=snapshot.meadow.alphamaps;int n=old.GetLength(0);var paint=new float[n,n,7];
            int district=snapshot.info.district;float ox=(district%2)*.5f,oz=(district/2)*.5f;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                float u=ox+x/(float)(n-1)*.5f,v=oz+z/(float)(n-1)*.5f;
                Channel(snapshot.data,side,u,v,out float d,out float w,out _,version);
                float bed=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(w*.70f,w*1.12f,d));
                for(int l=0;l<4;l++)paint[z,x,l]=old[z,x,l]*(1-bed);
                float noise=Mathf.PerlinNoise(u*side/7+snapshot.info.seed*.001f,v*side/7);
                float gravel=singleRiver?Mathf.Lerp(.12f,.24f,noise):Mathf.Lerp(.40f,.75f,noise),silt=singleRiver?Mathf.Lerp(.60f,.74f,1-noise):(1-gravel)*Mathf.Lerp(.25f,.75f,1-Mathf.Clamp01(d/w));
                paint[z,x,4]=bed*gravel;paint[z,x,5]=bed*(1-gravel-silt);paint[z,x,6]=bed*silt;
            }
            snapshot.meadow.alphamaps=paint;
            var trees=new List<TreeInstance>();foreach(var original in snapshot.meadow.trees){var t=original;float u=ox+t.position.x*.5f,v=oz+t.position.z*.5f;
                Channel(snapshot.data,side,u,v,out float d,out float w,out _,version);if(d<w*1.12f)continue;
                t.position.y=Sample(snapshot.data.Heights,u,v);trees.Add(t);
            }
            snapshot.meadow.trees=trees.ToArray();snapshot.info.riverBedVersion=version;
            if(snapshot.info.camera!=null){var pose=snapshot.info.camera;float v=.78f;float x=(snapshot.data.TributaryRiverCenter(v)-.5f)*side;
                pose.focus=new Vector3(x,Sample(snapshot.data.Heights,x/side+.5f,v)*vertical+2,(v-.5f)*side);pose.size=80;pose.zoom=2;}
        }
        public static void RefineAppearance(SavedRegionSnapshot snapshot)
        {
            if(!snapshot.info.waterless||snapshot.info.riverBedVersion<2)throw new System.InvalidOperationException("Requires the saved single river.");
            var old=snapshot.meadow.alphamaps;int n=old.GetLength(0);var paint=new float[n,n,8];float side=1024*Mathf.Pow(2,snapshot.info.size);
            float ox=(snapshot.info.district%2)*.5f,oz=(snapshot.info.district/2)*.5f;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                float u=ox+x/(float)(n-1)*.5f,v=oz+z/(float)(n-1)*.5f;
                for(int l=0;l<4;l++)paint[z,x,l]=old[z,x,l];
                float bed=0;for(int l=4;l<old.GetLength(2);l++)bed+=old[z,x,l];if(bed==0)continue;
                Channel(snapshot.data,side,u,v,out float d,out float w,out _,2);
                float px=u*side,pz=v*side;
                float broad=Mathf.PerlinNoise(px/17+snapshot.info.seed*.001f,pz/26);
                float bar=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.37f,.62f,broad));
                float fine=Mathf.PerlinNoise(px/4+13,pz/6);
                float gravel=Mathf.Lerp(.12f,.85f,bar);
                float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.95f,d/w));
                float moss=edge*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.47f,.67f,Mathf.PerlinNoise(px/10+51,pz/15)))*.43f;
                float sand=(1-gravel)*Mathf.Lerp(.12f,.68f,fine)*(1-edge*.65f);
                float soil=Mathf.Max(0,1-gravel-sand);float remaining=1-moss;
                paint[z,x,4]=bed*gravel*remaining;paint[z,x,5]=bed*sand*remaining;paint[z,x,6]=bed*soil*remaining;paint[z,x,7]=bed*moss;
            }
            snapshot.meadow.alphamaps=paint;snapshot.info.riverBedVersion=Mathf.Max(3,snapshot.info.riverBedVersion);
        }
        static float Sample(float[,] heights,float u,float v){float x=Mathf.Clamp01(u)*512,z=Mathf.Clamp01(v)*512;int ix=Mathf.Min(511,(int)x),iz=Mathf.Min(511,(int)z);return Mathf.Lerp(Mathf.Lerp(heights[iz,ix],heights[iz,ix+1],x-ix),Mathf.Lerp(heights[iz+1,ix],heights[iz+1,ix+1],x-ix),z-iz);}
    }
}
