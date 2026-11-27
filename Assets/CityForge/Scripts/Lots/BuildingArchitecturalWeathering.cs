using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CityForge.Lots
{
    // Explicit opt-in assembly capability. No material cloning, per-window decals or frame polling scans.
    [ExecuteAlways,DisallowMultipleComponent] public sealed class BuildingArchitecturalWeathering : MonoBehaviour
    {
        [Range(0,1)] public float intensity=.65f;
        public bool customizationEnabled;
        public Vector4 halfBoundsCenterHeight=new(3.6f,8,-1.15f,13.91f);
        public Material[] materials=Array.Empty<Material>();
        struct Slot {internal Renderer renderer;internal int index;}
        readonly List<Slot> slots=new();
        MaterialPropertyBlock block;
        Matrix4x4 lastTransform;bool applied,initialized;
        FacadeWeatheringSettings state;int stableSeed;
        void OnEnable(){initialized=false;Apply();}
        void OnValidate(){initialized=false;Apply();}
        void OnTransformChildrenChanged(){initialized=false;applied=false;}
        void LateUpdate(){if(!applied||lastTransform!=transform.localToWorldMatrix)Apply();}
        internal static bool Supports(LotAssetCatalogEntry asset)=>asset?.category=="Buildings"&&HasBindings(asset.prefab)&&HasBindings(asset.distantPrefab);
        static bool HasBindings(GameObject prefab)
        {
            if(!prefab)return false;var profiles=prefab.GetComponentsInChildren<BuildingArchitecturalWeathering>(true);
            return profiles.Length==1&&profiles[0].customizationEnabled&&profiles[0].materials!=null&&profiles[0].materials.Length>0&&profiles[0].materials.All(m=>m&&m.HasProperty("_CFFacadeA"));
        }
        internal static void Set(GameObject root,BuildingAppearanceSettings appearance,string buildingId=null)
        {
            foreach(var profile in root.GetComponentsInChildren<BuildingArchitecturalWeathering>(true))
            {if(!profile.customizationEnabled)continue;profile.state=appearance.facadeWeathering;if(buildingId!=null)profile.stableSeed=FacadeWeatheringSettings.StableSeed(buildingId);profile.Apply();}
        }
        void Initialize()
        {
            slots.Clear();block??=new MaterialPropertyBlock();
            foreach(var renderer in GetComponentsInChildren<Renderer>(true)){var ms=renderer.sharedMaterials;for(int i=0;i<ms.Length;i++)if(Array.IndexOf(materials,ms[i])>=0)slots.Add(new Slot{renderer=renderer,index=i});}
            initialized=true;
        }
        public void Apply()
        {
            if(materials==null||materials.Length==0)return;if(!initialized)Initialize();
            lastTransform=transform.localToWorldMatrix;applied=true;
            foreach(var slot in slots)
            {
                if(!slot.renderer)continue;
                slot.renderer.GetPropertyBlock(block,slot.index);
                block.SetFloat("_CFWeatherUseBuilding",1);block.SetMatrix("_CFWorldToBuilding",transform.worldToLocalMatrix);
                block.SetFloat("_CFWeatherIntensity",intensity);block.SetVector("_CFBounds",halfBoundsCenterHeight);
                block.SetFloat("_CFFacadeEnabled",customizationEnabled&&state.enabled?1:0);
                block.SetVector("_CFFacadeA",new Vector4(state.surfaceVariation,state.rainwaterStreaks,state.sootPollution,state.foundationGrime));
                block.SetVector("_CFFacadeB",new Vector4(state.sunFading,state.mineralDeposits,(state.seed==0?stableSeed:state.seed)&65535,0));
                slot.renderer.SetPropertyBlock(block,slot.index);
            }
        }
    }
}
