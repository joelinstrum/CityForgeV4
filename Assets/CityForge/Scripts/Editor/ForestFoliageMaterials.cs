using System;
using UnityEditor;
using UnityEngine;

namespace CityForge.Editor
{
    // Any tree importer can supply its leaf atlas and mesh-space crown bounds.
    public static class ForestFoliageMaterials
    {
        public static Material Create(string path, Texture albedo, Bounds bounds)
        {
            var shader=Shader.Find("CityForge/Forest Foliage");
            if(shader==null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Forest foliage shader must compile before importing trees.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(shader); AssetDatabase.CreateAsset(material,path); }
            material.shader=shader;
            material.SetTexture("_BaseMap",albedo);
            material.SetColor("_BaseColor",new Color(.58f,.78f,.40f,1));
            material.SetFloat("_Cutoff",.3f);
            material.SetVector("_CrownCenter",bounds.center);
            // Imported FBX mesh coordinates can be centimetres with a scaled/rotated
            // renderer transform. Keep these bounds in the same space as positionOS.
            material.SetVector("_CrownRadii",new Vector4(Mathf.Max(.0001f,bounds.extents.x),Mathf.Max(.0001f,bounds.extents.y),Mathf.Max(.0001f,bounds.extents.z),0));
            material.SetFloat("_CrownNormalBlend",.8f);
            material.SetFloat("_SkyFill",1.15f);
            material.SetFloat("_Transmission",.18f);
            material.SetFloat("_ShadowFill",.14f);
            material.enableInstancing=true; material.renderQueue=2450;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
