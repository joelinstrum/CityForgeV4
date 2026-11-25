using System.Collections.Generic;
using System.Linq;
using CityForge.World;
using UnityEngine;

namespace CityForge.Lots
{
    // Per-instance property blocks. Shared prefab materials and original atlases are immutable.
    internal sealed class BuildingPaint : MonoBehaviour
    {
        sealed class Map { internal RenderTexture Texture; internal int Users; }
        static readonly Dictionary<Texture,Map> maps=new();
        static readonly int BaseMap=Shader.PropertyToID("_BaseMap"), BaseColor=Shader.PropertyToID("_BaseColor");
        readonly List<(Renderer Renderer,int Slot,Texture Map,Color Color,string Region,Texture Neutral)> slots=new();
        readonly Dictionary<Texture,Map> leases=new();
        bool painted;
        internal static void Attach(GameObject instance,LotAssetCatalogEntry asset,BuildingAppearanceSettings state)
        {
            bool hasTrim=BuildingAppearance.SupportsTrim(asset),hasAwnings=BuildingAppearance.SupportsAwnings(asset),hasWalls=BuildingAppearance.SupportsWalls(asset);
            if(!hasTrim&&!hasAwnings&&!hasWalls)return;
            var paint=instance.AddComponent<BuildingPaint>();
            var block=new MaterialPropertyBlock();
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.GetComponentInParent<AuthoredSignLighting>())continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var profile=renderer.GetComponentInParent<BuildingTintProfile>();
                    var binding=profile?.bindings?.FirstOrDefault(b=>b!=null&&b.material==materials[i]&&b.neutralAlbedo&&
                        (b.region=="trim"&&hasTrim||b.region=="awnings"&&hasAwnings||b.region=="walls"&&hasWalls));
                    bool legacy=asset.id==BuildingAppearance.Marquee&&BuildingAppearance.TrimMaterial(materials[i]);
                    if(binding==null&&!legacy)continue;
                    block.Clear();renderer.GetPropertyBlock(block,i);
                    var map=block.HasTexture(BaseMap)?block.GetTexture(BaseMap):materials[i].GetTexture(BaseMap);
                    var color=block.HasColor(BaseColor)?block.GetColor(BaseColor):materials[i].GetColor(BaseColor);
                    if(map)paint.slots.Add((renderer,i,map,color,binding?.region??"trim",binding?.neutralAlbedo));
                }
            }
            paint.Set(state);
        }
        internal void Set(BuildingAppearanceSettings state)
        {
            BuildingAppearance.Validate(state);
            bool allOriginal=BuildingAppearance.Original(state)&&string.IsNullOrEmpty(state.awningColor)&&string.IsNullOrEmpty(state.wallColor);
            if(allOriginal&&!painted)return;
            var block=new MaterialPropertyBlock();
            foreach(var slot in slots)
            {
                if(!slot.Renderer)continue;
                var id=slot.Region=="walls"?state.wallColor:slot.Region=="awnings"?state.awningColor:state.trimColor;
                bool original=string.IsNullOrEmpty(id);
                var color=original?slot.Color:BuildingAppearance.Find(id).Color;
                if(!original&&slot.Region=="walls")
                {
                    Color.RGBToHSV(color,out var hue,out var saturation,out var value);
                    color=Color.HSVToRGB(hue,saturation*BuildingAppearance.WallSaturation(state),value);
                }
                var texture=original?slot.Map:slot.Neutral?slot.Neutral:Acquire(slot.Map);
                block.Clear();slot.Renderer.GetPropertyBlock(block,slot.Slot);
                block.SetTexture(BaseMap,texture);block.SetColor(BaseColor,original?slot.Color:color);
                slot.Renderer.SetPropertyBlock(block,slot.Slot);
            }
            painted=!allOriginal;if(allOriginal)Release();
        }
        Texture Acquire(Texture source)
        {
            if(leases.TryGetValue(source,out var held))return held.Texture;
            if(!maps.TryGetValue(source,out var map))
            {
                var shader=Resources.Load<Shader>("BuildingTrimNeutralize");
                if(!shader)throw new System.InvalidOperationException("Missing building paint shader.");
                var material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
                var target=new RenderTexture(Mathf.Min(2048,source.width),Mathf.Min(2048,source.height),0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
                {name="Neutral building trim",useMipMap=true,autoGenerateMips=false,filterMode=FilterMode.Trilinear,wrapMode=source.wrapMode,anisoLevel=source.anisoLevel};
                var previous=RenderTexture.active;
                try{target.Create();Graphics.Blit(source,target,material);target.GenerateMips();}
                catch{target.Release();Destroy(target);throw;}
                finally{RenderTexture.active=previous;Destroy(material);}
                map=new Map{Texture=target};maps.Add(source,map);
            }
            map.Users++;leases.Add(source,map);return map.Texture;
        }
        void Release()
        {
            foreach(var pair in leases)if(--pair.Value.Users==0){pair.Value.Texture.Release();Destroy(pair.Value.Texture);maps.Remove(pair.Key);}
            leases.Clear();
        }
        void OnDestroy()=>Release();
    }
}
