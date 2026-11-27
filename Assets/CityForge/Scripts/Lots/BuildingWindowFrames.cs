using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace CityForge.Lots
{
    // Explicit binding only. Never infer paintability from dark colors or renderer names.
    internal sealed class BuildingWindowFrames : MonoBehaviour
    {
        internal static readonly BuildingAppearance.PaintColor[] Colors = {
            new("warm-ivory","Warm ivory","#D4CCB9"),new("weathered-cream","Weathered cream","#B5A58A"),
            new("dark-walnut","Dark walnut","#594536"),new("forest-green","Forest green","#414F40"),
            new("soft-charcoal","Soft charcoal","#454440"),new("weathered-bronze","Weathered bronze","#665C49")
        };
        internal readonly struct Surface
        {
            internal readonly string Id,Label; internal readonly float Metallic,Smoothness,Grain;
            internal Surface(string id,string label,float metallic,float smoothness,float grain){Id=id;Label=label;Metallic=metallic;Smoothness=smoothness;Grain=grain;}
        }
        internal static readonly Surface[] Surfaces={new("painted-wood","Painted wood",0,.30f,.035f),new("painted-metal","Painted metal",0,.42f,.008f),new("exposed-metal","Exposed metal",1,.32f,.018f),new("stained-wood","Stained wood",0,.28f,.065f)};
        internal static void Validate(BuildingAppearanceSettings s)
        {
            if(!string.IsNullOrEmpty(s.windowFrameColor)&&!Colors.Any(c=>c.Id==s.windowFrameColor))throw new InvalidDataException("Invalid window frame color.");
            if(!string.IsNullOrEmpty(s.windowFrameMaterial)&&!Surfaces.Any(c=>c.Id==s.windowFrameMaterial))throw new InvalidDataException("Invalid window frame material.");
        }
        internal static bool HasBindings(GameObject prefab)
        {
            if(!prefab)return false;
            var profiles=prefab.GetComponentsInChildren<BuildingWindowFrameProfile>(true);
            if(profiles.Length!=1||profiles[0].bindings==null||profiles[0].bindings.Length==0)return false;
            var renderers=prefab.GetComponentsInChildren<Renderer>(true);
            return profiles[0].bindings.All(b=>b!=null&&b.original&&b.customizable&&b.customizable.HasProperty("_FrameMask")&&b.customizable.HasProperty("_FrameTint")&&renderers.Any(r=>r.sharedMaterials.Contains(b.original)));
        }
        internal static bool Supports(LotAssetCatalogEntry asset)=>asset?.category=="Buildings"&&HasBindings(asset.prefab)&&HasBindings(asset.distantPrefab);
        sealed class Slot {internal Renderer renderer;internal int index;internal Material original,custom;internal MaterialPropertyBlock baseline;}
        readonly List<Slot> slots=new(); MaterialPropertyBlock block; bool active;
        const string ColorSet="_FrameColorSet",SurfaceSet="_FrameSurfaceSet",Tint="_FrameTint",Metal="_FrameMetallic",Smooth="_FrameSmoothness",Grain="_FrameGrain";
        internal static void Attach(GameObject instance,LotAssetCatalogEntry asset,BuildingAppearanceSettings state)
        {
            if(!Supports(asset))return;
            var component=instance.AddComponent<BuildingWindowFrames>();component.Initialize();component.Set(state);
        }
        void Initialize()
        {
            block??=new MaterialPropertyBlock();
            if(slots.Count!=0)return;
            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var profile=renderer.GetComponentInParent<BuildingWindowFrameProfile>(true);if(!profile)continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var binding=profile.bindings.FirstOrDefault(b=>b!=null&&b.original==materials[i]);if(binding==null)continue;
                    var baseline=new MaterialPropertyBlock();renderer.GetPropertyBlock(baseline,i);
                    slots.Add(new Slot{renderer=renderer,index=i,original=materials[i],custom=binding.customizable,baseline=baseline});
                }
            }
        }
        internal void Set(BuildingAppearanceSettings state)
        {
            Validate(state);Initialize();bool original=string.IsNullOrEmpty(state.windowFrameColor)&&string.IsNullOrEmpty(state.windowFrameMaterial);
            if(original&&!active)return;
            var surface=Surfaces.FirstOrDefault(c=>c.Id==state.windowFrameMaterial);
            var color=Colors.FirstOrDefault(c=>c.Id==state.windowFrameColor);
            foreach(var slot in slots)
            {
                if(!slot.renderer)continue;
                var materials=slot.renderer.sharedMaterials;materials[slot.index]=original?slot.original:slot.custom;slot.renderer.sharedMaterials=materials;
                // Merge with lighting/SH property blocks; never clear another feature's properties.
                slot.renderer.GetPropertyBlock(block,slot.index);
                block.SetFloat(ColorSet,string.IsNullOrEmpty(state.windowFrameColor)?0:1);
                block.SetFloat(SurfaceSet,string.IsNullOrEmpty(state.windowFrameMaterial)?0:1);
                if(!string.IsNullOrEmpty(state.windowFrameColor))block.SetColor(Tint,color.Color);
                if(!string.IsNullOrEmpty(state.windowFrameMaterial)){block.SetFloat(Metal,surface.Metallic);block.SetFloat(Smooth,surface.Smoothness);block.SetFloat(Grain,surface.Grain);}
                slot.renderer.SetPropertyBlock(block,slot.index);
            }
            active=!original;
        }
    }
}
