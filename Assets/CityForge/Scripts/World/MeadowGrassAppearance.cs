using System;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Grades two owned grass textures on settings changes; adds no terrain samples or per-pixel work.</summary>
    public sealed class MeadowGrassAppearance : MonoBehaviour
    {
        public MeadowGrassSettings settings;
        Terrain terrain;
        readonly Texture2D[] originals=new Texture2D[2],graded=new Texture2D[2];
        readonly Vector4[] previous={new Vector4(1.25f,1.12f,1.20f,1),new Vector4(1.42f,1.18f,1.18f,1)};
        bool applied,wasEnabled;
        float saturation,brightness,variation;
        Color tint;
        Material processor;
        bool ownsSettings;

        public void Initialize(Terrain target)
        {
            terrain=target;
            settings=Resources.Load<MeadowGrassSettings>("Region/MeadowGrassSettings");
            if(!settings){settings=ScriptableObject.CreateInstance<MeadowGrassSettings>();ownsSettings=true;}
            var layers=terrain.terrainData.terrainLayers;
            for(int i=0;i<2;i++)originals[i]=layers[i+1].diffuseTexture;
            Apply();
        }
        void Update()
        {
            if(settings&&(!applied||wasEnabled!=settings.applyRefinement||saturation!=settings.saturation||
                brightness!=settings.brightness||variation!=settings.variationStrength||tint!=settings.tint))Apply();
        }
        [ContextMenu("Apply grass settings")]
        public void Apply()
        {
            if(!terrain||!settings)return;
            // Generate replacements first so failed allocation/shader setup leaves the visible terrain intact.
            var next=new Texture2D[2];
            try{if(settings.applyRefinement)for(int i=0;i<2;i++)next[i]=Grade(originals[i],i);}
            catch{foreach(var texture in next)if(texture)Destroy(texture);throw;}
            var layers=terrain.terrainData.terrainLayers;
            for(int i=0;i<2;i++){
                layers[i+1].diffuseTexture=settings.applyRefinement?next[i]:originals[i];
                layers[i+1].diffuseRemapMin=Vector4.zero;
                layers[i+1].diffuseRemapMax=settings.applyRefinement?Vector4.one:previous[i];
            }
            terrain.terrainData.terrainLayers=layers;terrain.Flush();
            for(int i=0;i<2;i++){if(graded[i])Destroy(graded[i]);graded[i]=next[i];}
            applied=true;wasEnabled=settings.applyRefinement;saturation=settings.saturation;
            brightness=settings.brightness;variation=settings.variationStrength;tint=settings.tint;
        }
        Texture2D Grade(Texture2D source,int index)
        {
            if(!processor){
                var shader=Shader.Find("Hidden/CityForge/Meadow Grass Grade");
                if(!shader||!shader.isSupported)throw new InvalidOperationException("Meadow grass grading shader unavailable.");
                processor=new Material(shader);
            }
            var mean=(previous[0]+previous[1])*.5f;
            processor.SetVector("_SourceTint",Vector4.LerpUnclamped(mean,previous[index],settings.variationStrength));
            processor.SetColor("_GrassTint",settings.tint);
            processor.SetFloat("_Saturation",settings.saturation);processor.SetFloat("_Brightness",settings.brightness);
            var target=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var old=RenderTexture.active;bool oldWrite=GL.sRGBWrite;Texture2D result=null;
            try{
                GL.sRGBWrite=QualitySettings.activeColorSpace==ColorSpace.Linear;
                Graphics.Blit(source,target,processor);RenderTexture.active=target;
                result=new Texture2D(source.width,source.height,TextureFormat.RGBA32,true,false){
                    name=source.name+" (grass refinement)",wrapMode=source.wrapMode,
                    filterMode=source.filterMode,anisoLevel=source.anisoLevel,mipMapBias=source.mipMapBias};
                result.ReadPixels(new Rect(0,0,source.width,source.height),0,0,false);result.Apply(true,true);
                return result;
            }catch{if(result)Destroy(result);throw;}
            finally{GL.sRGBWrite=oldWrite;RenderTexture.active=old;RenderTexture.ReleaseTemporary(target);}
        }
        void OnDestroy()
        {
            // Terrain layers are owned by ApprovedDistrictForest; restore references before releasing textures.
            if(terrain&&terrain.terrainData){var layers=terrain.terrainData.terrainLayers;for(int i=0;i<2;i++)if(layers.Length>i+1&&originals[i]){
                layers[i+1].diffuseTexture=originals[i];layers[i+1].diffuseRemapMax=previous[i];
            }terrain.terrainData.terrainLayers=layers;}
            foreach(var texture in graded)if(texture)Destroy(texture);
            if(processor)Destroy(processor);if(ownsSettings&&settings)Destroy(settings);
        }
    }
}
