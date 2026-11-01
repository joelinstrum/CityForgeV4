using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Scene representation of saved camp records in the active original district members.</summary>
    public sealed class NativeCampDistrict:MonoBehaviour
    {
        public readonly List<NativeCampRecord> Camps=new();
        public readonly List<NpcPopulation> Populations=new();
        public readonly List<Transform> Teepees=new();
        public int TotemCount {get;private set;}
        public void Initialize(MeadowDistrictLandscape land){
            var records=land.View.NativeCamps.Where(c=>land.View.ActiveDistrictMembers.Contains(c.district)).ToArray();if(records.Length==0)return;
            var assets=Resources.Load<NativeCampAssets>("NativeCamps/Assets");
            if(!assets||!assets.teepee||!assets.totem||!assets.chief||!assets.warrior||!assets.resident)throw new InvalidOperationException("Prepare private NativeCamps assets before opening a populated district");
            foreach(var camp in records){
                var root=new GameObject("Native settlement / district "+camp.district);root.transform.SetParent(transform,false);var obstacles=new List<Bounds>();
                for(int i=0;i<camp.teepees;i++){
                    var p=camp.TeepeePosition(i);var center=new Vector3(camp.center.x,land.Height(camp.center),camp.center.y);
                    var go=Instantiate(assets.teepee,root.transform);go.name="Teepee "+(i+1);
                    float ground=land.Height(p);for(int k=0;k<12;k++){float a=k*Mathf.PI/6;ground=Mathf.Min(ground,land.Height(p+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*2));}
                    go.transform.position=new Vector3(p.x,ground-.04f,p.y);go.transform.rotation=Quaternion.LookRotation(new Vector3(center.x-p.x,0,center.z-p.y));
                    foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=assets.teepeeMaterials[(int)(((uint)camp.seed+(uint)i)%(uint)assets.teepeeMaterials.Length)];
                    Teepees.Add(go.transform);obstacles.Add(Footprint(go,.45f));
                }
                var totem=Instantiate(assets.totem,root.transform);totem.name="Central totem";totem.transform.position=new Vector3(camp.center.x,land.Height(camp.center)-.04f,camp.center.y);totem.transform.rotation=Quaternion.Euler(0,camp.heading,0);TotemCount++;
                obstacles.Add(Footprint(totem,.45f));
                var hearth=new GameObject("Campfire");hearth.transform.SetParent(root.transform,false);var fire=hearth.AddComponent<NativeCampfire>();fire.Initialize(land,camp);obstacles.Add(fire.Exclusion);
                root.AddComponent<NativeCampGrass>().Initialize(land,camp,obstacles);
                var people=new GameObject("Camp residents");people.transform.SetParent(root.transform,false);var population=people.AddComponent<NpcPopulation>();population.InitializeCamp(land,camp,assets,obstacles);
                Populations.Add(population);Camps.Add(camp);
            }
        }
        static Bounds Footprint(GameObject go,float padding){var renderers=go.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);bounds.Expand(padding*2);return bounds;}
    }
}
