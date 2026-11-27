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
        readonly List<(Renderer Renderer,int Slot,Texture Map,Color Color,string Region,Texture Neutral,BuildingWallFinish[] Finishes,Texture Calm,Color Mean,Surface Surface)> slots=new();
        readonly Dictionary<Texture,Map> leases=new();
        sealed class Surface {
            internal Texture Normal,Packed;internal float Bump,Smooth,Metallic,NormalStrength,UsePbr,Courses;internal Vector4 UV;
            internal Surface(Material m){NormalStrength=m.HasProperty("_NormalStrength")?m.GetFloat("_NormalStrength"):1;UsePbr=m.HasProperty("_UsePBRMap")?m.GetFloat("_UsePBRMap"):0;Courses=m.HasProperty("_MasonryCoursesPerTile")?m.GetFloat("_MasonryCoursesPerTile"):10;Normal=m.HasProperty("_BumpMap")?m.GetTexture("_BumpMap"):null;Packed=m.HasProperty("_MetallicGlossMap")?m.GetTexture("_MetallicGlossMap"):null;Bump=m.HasProperty("_BumpScale")?m.GetFloat("_BumpScale"):1;Smooth=m.HasProperty("_Smoothness")?m.GetFloat("_Smoothness"):.2f;Metallic=m.HasProperty("_Metallic")?m.GetFloat("_Metallic"):0;var scale=m.GetTextureScale("_BaseMap");var offset=m.GetTextureOffset("_BaseMap");UV=new Vector4(scale.x,scale.y,offset.x,offset.y);}
        }
        bool painted, initialized, reapplyAfterEnable;
        BuildingAppearanceSettings current;
        void OnEnable() => reapplyAfterEnable = true;
        void LateUpdate()
        {
            if (!reapplyAfterEnable || !initialized) return;
            reapplyAfterEnable = false;
            // Prepared preview controls can initialize material blocks when a hidden stage opens.
            // Reapply the player's saved finish after all child OnEnable callbacks have run.
            Set(current);
        }
        internal static void Attach(GameObject instance,LotAssetCatalogEntry asset,BuildingAppearanceSettings state)
        {
            bool hasTrim=BuildingAppearance.SupportsTrim(asset),hasAwnings=BuildingAppearance.SupportsAwnings(asset),hasWalls=BuildingAppearance.SupportsWalls(asset);
            if(!hasTrim&&!hasAwnings&&!hasWalls)return;
            var paint=instance.AddComponent<BuildingPaint>();
            var block=new MaterialPropertyBlock();
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.GetComponentInParent<AuthoredSignLighting>(true))continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var profile=renderer.GetComponentInParent<BuildingTintProfile>(true);
                    var binding=profile?.bindings?.FirstOrDefault(b=>b!=null&&b.material==materials[i]&&b.neutralAlbedo&&
                        (b.region=="trim"&&hasTrim||b.region=="awnings"&&hasAwnings||b.region=="walls"&&hasWalls));
                    bool legacy=asset.id==BuildingAppearance.Marquee&&BuildingAppearance.TrimMaterial(materials[i]);
                    if(binding==null&&!legacy)continue;
                    block.Clear();renderer.GetPropertyBlock(block,i);
                    var map=block.HasTexture(BaseMap)?block.GetTexture(BaseMap):materials[i].GetTexture(BaseMap);
                    var color=block.HasColor(BaseColor)?block.GetColor(BaseColor):materials[i].GetColor(BaseColor);
                    if(map)paint.slots.Add((renderer,i,map,color,binding?.region??"trim",binding?.neutralAlbedo,binding?.finishes,materials[i].HasProperty("_CalmBaseMap")?materials[i].GetTexture("_CalmBaseMap"):null,materials[i].HasProperty("_SourceMeanColor")?materials[i].GetColor("_SourceMeanColor"):Color.white,new Surface(materials[i])));
                }
            }
            paint.Set(state);
        }
        internal void Set(BuildingAppearanceSettings state)
        {
            BuildingAppearance.Validate(state);
            current = BuildingAppearance.Copy(state);
            initialized = true;
            bool allOriginal=BuildingAppearance.Original(state)&&string.IsNullOrEmpty(state.awningColor)&&string.IsNullOrEmpty(state.wallColor);
            if(allOriginal&&!painted)return;
            var block=new MaterialPropertyBlock();
            foreach(var slot in slots)
            {
                if(!slot.Renderer)continue;
                var id=slot.Region=="walls"?state.wallColor:slot.Region=="awnings"?state.awningColor:state.trimColor;
                bool fixedBrick = slot.Region == "walls" && slot.Finishes != null && slot.Finishes.Length > 0;
                if (fixedBrick) id = BuildingAppearance.BrickFinishId(id);
                bool original=string.IsNullOrEmpty(id);
                var finish = fixedBrick && !original ? slot.Finishes.FirstOrDefault(f=>f!=null&&f.id==id) : null;
                var color=original?slot.Color:fixedBrick?Color.white:BuildingAppearance.Find(id).Color;
                if(!original&&slot.Region=="walls"&&!fixedBrick)
                {
                    Color.RGBToHSV(color,out var hue,out var saturation,out var value);
                    color=Color.HSVToRGB(hue,saturation*BuildingAppearance.WallSaturation(state),value);
                }
                var texture=original?slot.Map:finish!=null&&finish.albedo?finish.albedo:slot.Neutral?slot.Neutral:Acquire(slot.Map);
                block.Clear();slot.Renderer.GetPropertyBlock(block,slot.Slot);
                block.SetTexture(BaseMap,texture);block.SetColor(BaseColor,color);
                if (fixedBrick && slot.Calm)
                {
                    block.SetTexture("_CalmBaseMap",original?slot.Calm:finish?.calmAlbedo?finish.calmAlbedo:texture);
                    block.SetColor("_SourceMeanColor",original?slot.Mean:finish?.meanColor??slot.Mean);
                }
                if(fixedBrick){
                    bool custom=finish!=null&&finish.customSurface;
                    block.SetVector("_BaseMap_ST",custom?finish.uvTransform:slot.Surface.UV);
                    block.SetTexture("_BumpMap",custom?finish.normal:slot.Surface.Normal?slot.Surface.Normal:Texture2D.normalTexture);
                    block.SetTexture("_MetallicGlossMap",custom?finish.packedSurface:slot.Surface.Packed?slot.Surface.Packed:Texture2D.whiteTexture);
                    block.SetFloat("_BumpScale",custom?.45f:slot.Surface.Bump);
                    block.SetFloat("_Smoothness",custom?.18f:slot.Surface.Smooth);
                    block.SetFloat("_Metallic",custom?0:slot.Surface.Metallic);
                    block.SetFloat("_NormalStrength",custom?.45f:slot.Surface.NormalStrength);
                    block.SetFloat("_UsePBRMap",custom?1:slot.Surface.UsePbr);
                    block.SetFloat("_MasonryCoursesPerTile",custom?finish.coursesPerTile:slot.Surface.Courses);
                }
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
