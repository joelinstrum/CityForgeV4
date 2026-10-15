using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Still, bounded lake/pond surface with cached depth and a planar reflection.</summary>
    public sealed class StillWaterRenderer : MonoBehaviour
    {
        Mesh mesh;Material material;Texture2D mask;
        public CityForge.Environment.PondPlanarReflection Reflection {get;private set;}
        public void Initialize(MeadowDistrictLandscape land,RegionWaterBody body)
        {
            if(body.kind!=WaterBodyKind.Lake&&body.kind!=WaterBodyKind.Pond)throw new System.ArgumentException("Still water requires a lake or pond.");
            float level=body.surfaceLevelNormalized*land.DisplayTerrain.terrainData.size.y;
            const int resolution=512;var pixels=new Color[resolution*resolution];
            for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++){
                var p=land.Origin+new Vector2((x+.5f)*land.Side/resolution,(z+.5f)*land.Side/resolution);
                bool inside=body.Contains(p/land.View.MapSide+Vector2.one*.5f);
                pixels[z*resolution+x]=new Color(Mathf.Max(0,level-land.Height(p)),inside?1:0,0,1);
            }
            mask=new Texture2D(resolution,resolution,TextureFormat.RGBAHalf,false,true){name="Seeded lake depth and footprint",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};mask.SetPixels(pixels);mask.Apply(false,true);
            material=new Material(Resources.Load<Material>("Water/StillReflectiveWater")){name=body.kind+" still reflective surface"};material.SetTexture("_DepthMask",mask);material.SetVector("_RegionOriginScale",new Vector4(land.Origin.x,land.Origin.y,1/land.Side,0));
            var bounds=body.Bounds;var min=(bounds.min-Vector2.one*.5f)*land.View.MapSide;var max=(bounds.max-Vector2.one*.5f)*land.View.MapSide;
            mesh=new Mesh{name="Bounded still lake surface"};mesh.vertices=new[]{new Vector3(min.x,level,min.y),new Vector3(min.x,level,max.y),new Vector3(max.x,level,min.y),new Vector3(max.x,level,max.y)};mesh.triangles=new[]{0,1,2,2,1,3};mesh.RecalculateBounds();
            gameObject.layer=4;gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var tag=gameObject.AddComponent<RegionWaterBodyTag>();tag.bodyId=body.id;tag.kind=body.kind;
            Reflection=gameObject.AddComponent<CityForge.Environment.PondPlanarReflection>();Reflection.sourceCamera=land.View.GetComponentInChildren<Camera>();Reflection.waterMaterial=material;Reflection.waterHeight=level;Reflection.textureWidth=1024;Reflection.updatesPerSecond=8;Reflection.updateOnCameraMotion=true;Reflection.renderShadows=false;
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);if(mask)Destroy(mask);}
    }
}
