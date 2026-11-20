using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    // Transient ambience: no population state, pathfinding graph, colliders or save records.
    internal sealed class DistrictRats : MonoBehaviour
    {
        sealed class Rat {internal string lotId;internal Transform root,near,far;internal Vector2 home,target,lotCenter;internal float lotFacing;internal float wait,phase;internal System.Random random;}
        const int Limit=24;
        readonly List<Rat> rats=new();PrototypeRegionView view;CityForgeCameraController cameraRig;
        GameObject model,distant;Material body,whiskers,eyes;float refreshAt;
        internal int Count=>rats.Count;
        internal void Initialize(PrototypeRegionView source){view=source;cameraRig=view.GetComponentInChildren<CityForgeCameraController>();}
        void Update()
        {
            if(!view||view.IsSaving||view.DistrictLoading)return;
            if(Time.unscaledTime>=refreshAt){refreshAt=Time.unscaledTime+5;Refresh();}
            int zoom=cameraRig?cameraRig.ZoomLevel:5;
            foreach(var rat in rats)
            {
                bool visible=zoom<=3&&view.SelectedDistrict>=0&&ActorSimulationVisibility.NearCamera(cameraRig.GetComponent<Camera>(),rat.root.position);
                rat.root.gameObject.SetActive(visible);if(!visible)continue;
                rat.near.gameObject.SetActive(zoom<=2);rat.far.gameObject.SetActive(zoom>=3);
                Tick(rat,Mathf.Min(Time.deltaTime,.05f));
            }
        }
        void Refresh()
        {
            var homes=view.SelectedDistrict<0?Array.Empty<DistrictLotRecord>():view.PlacedLots.Where(l=>view.InActiveDistrict(l.center)&&l.definition!=null&&l.definition.type==LotType.Zoning&&l.definition.zoningLayout.wealth==LotWealth.Low).OrderBy(l=>l.id,StringComparer.Ordinal).Take(Limit/2).ToArray();
            var ids=homes.ToDictionary(l=>l.id,l=>l);
            for(int i=rats.Count-1;i>=0;i--)if(!ids.TryGetValue(rats[i].lotId,out var lot)||lot.center!=rats[i].lotCenter||lot.FacingDegrees!=rats[i].lotFacing){Destroy(rats[i].root.gameObject);rats.RemoveAt(i);}
            if(homes.Length==0)return;
            model??=Resources.Load<GameObject>("UrbanRats/Rat");distant??=Resources.Load<GameObject>("UrbanRats/Distant");
            body??=Resources.Load<Material>("UrbanRats/Body");whiskers??=Resources.Load<Material>("UrbanRats/Whiskers");eyes??=Resources.Load<Material>("UrbanRats/Eyes");
            if(!model||!distant||!body||!whiskers||!eyes)return;
            foreach(var lot in homes)
            {
                if(rats.Any(r=>r.lotId==lot.id))continue;
                var random=new System.Random(unchecked((int)LotPropStyles.Hash(lot.id+":rats")));
                for(int i=0;i<2&&rats.Count<Limit;i++)for(int attempt=0;attempt<16;attempt++)
                {
                    var half=lot.definition.Size*.5f;var p=new Vector2((float)(random.NextDouble()*2-1)*half.x,(float)(random.NextDouble()*2-1)*half.y);
                    if(attempt%2==0)p.y=(attempt%4==0?-1:1)*(half.y+.5f);else p.x=(attempt%4==1?-1:1)*(half.x+.5f);
                    var rotated=Quaternion.Euler(0,lot.FacingDegrees,0)*new Vector3(p.x,0,p.y);p=lot.center+new Vector2(rotated.x,rotated.z);
                    if(!view.PatrolClear(p,p))continue;
                    var root=new GameObject("Low wealth rat").transform;root.SetParent(transform,false);root.position=new Vector3(p.x,view.FarmerGround(p)+.015f,p.y);
                    var near=Instantiate(model,root).transform;var far=Instantiate(distant,root).transform;
                    foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.sharedMaterial=renderer.name.StartsWith("0000")?whiskers:renderer.name.StartsWith("0002")?eyes:body;
                        renderer.shadowCastingMode=ShadowCastingMode.Off;
                    }
                    rats.Add(new Rat{lotId=lot.id,lotCenter=lot.center,lotFacing=lot.FacingDegrees,root=root,near=near,far=far,home=p,target=p,random=random,wait=(float)random.NextDouble()*2});break;
                }
            }
        }
        void Tick(Rat rat,float dt)
        {
            var p=new Vector2(rat.root.position.x,rat.root.position.z);
            if((rat.target-p).sqrMagnitude>.005f)
            {
                var next=Vector2.MoveTowards(p,rat.target,dt*1.15f);var delta=next-p;
                if(delta.sqrMagnitude>.000001f)rat.root.rotation=Quaternion.RotateTowards(rat.root.rotation,Quaternion.LookRotation(new Vector3(delta.x,0,delta.y)),dt*720);
                rat.phase+=dt*27;rat.root.position=new Vector3(next.x,view.FarmerGround(next)+.015f+Mathf.Abs(Mathf.Sin(rat.phase))*.008f,next.y);
                return;
            }
            rat.wait-=dt;if(rat.wait>0)return;
            rat.wait=.5f+(float)rat.random.NextDouble()*2.5f;
            for(int attempt=0;attempt<6;attempt++)
            {
                float angle=(float)rat.random.NextDouble()*Mathf.PI*2,distance=.7f+(float)rat.random.NextDouble()*2.3f;
                var target=p+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*distance;
                if((target-rat.home).sqrMagnitude>36||!view.PatrolClear(p,target))continue;
                rat.target=target;return;
            }
        }
    }
    public sealed partial class PrototypeRegionView
    {
        void EnsureDistrictRats(){if(!TryGetComponent<DistrictRats>(out var rats)){rats=gameObject.AddComponent<DistrictRats>();rats.Initialize(this);}}
    }
}
