using System;
using System.Collections;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Promotes the accepted Crest river profile without editor menus or camera changes.</summary>
    public static class ApprovedRiverWater
    {
        public static IEnumerator Initialize(MeadowDistrictLandscape land)
        {
            LakeWaterRenderer river=null;
            foreach(var surface in land.GetComponentsInChildren<LakeWaterRenderer>())
                if(surface.SurfaceMaterial&&surface.SurfaceMaterial.HasProperty("_RiverSurface")&&surface.SurfaceMaterial.GetFloat("_RiverSurface")>.5f){river=surface;break;}
            if(!river)yield break;
            var template=Resources.Load<Material>("ApprovedWater/River");
            if(!template||!template.shader)throw new InvalidOperationException("Approved river material missing. Prepare the local runtime river shader/material before loading districts.");
            var host=new GameObject("Approved Crest district river");host.transform.SetParent(land.transform,false);
            var crest=host.AddComponent<CrestDistrictRiverPreview>();var setup=crest.Initialize(land,river,template);
            while(setup.MoveNext())yield return setup.Current;
            Configure(crest.Settings);
            if(StreamRockTestRegion.Revised(land.View))StreamRockTestRegion.ConfigureWater(crest.Settings,StreamRockTestRegion.Refined(land.View));
            else if(GeneratedStreamAppearance.Applies(land.View)&&GeneratedStreamAppearance.StreamOnlyDistrict(land)){
                StreamRockTestRegion.ConfigureWater(crest.Settings,true);
                // Mild suspended sediment: attenuate the bed and shift scattering
                // toward muted olive-brown, retaining readable shallow-edge stones.
                crest.Settings.SetVector("_Crest_Absorption",new Vector4(1.65f,1.35f,1.15f,0));
                crest.Settings.SetColor("_Crest_Scattering",new Color(.105f,.125f,.078f,1));
            }
        }
        public static void Configure(Material m)
        {
            m.SetFloat("_CityForgeGlintStart",.15f);m.SetFloat("_CityForgeGlintEnd",.5f);m.SetFloat("_CityForgeGlintGain",1);
            m.SetFloat("_CityForgeRippleEvolution",.018f);m.SetFloat("_CityForgeRippleEvolutionSpeed",2);
            m.SetFloat("_Crest_NormalMapStrength",.08f);m.SetFloat("_Crest_NormalMapScale",4);
            m.SetFloat("_Crest_Smoothness",.97f);m.SetFloat("_Crest_SmoothnessFar",.97f);
            m.SetFloat("_Crest_PlanarReflectionsIntensity",.25f);m.SetFloat("_Crest_Specular",.35f);
            // November 1 reference: muted teal depth with neutral, bed-visible shallows.
            m.SetVector("_Crest_Absorption",new Vector4(.55f,.38f,.40f,0));
            m.SetColor("_Crest_Scattering",new Color(.14f,.31f,.32f,1));
            m.SetFloat("_WorkflowMode",0);m.EnableKeyword("_SPECULAR_SETUP");
            m.EnableKeyword("_ALPHATEST_ON");m.EnableKeyword("_CREST_FLOW_LOD");
        }
    }
}
