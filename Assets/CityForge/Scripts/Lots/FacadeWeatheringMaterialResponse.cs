using UnityEngine;
namespace CityForge.Lots
{
    [CreateAssetMenu(menuName="CityForge/Architecture/Facade Material Response")]
    public sealed class FacadeWeatheringMaterialResponse : ScriptableObject
    {
        [Range(0,2)] public float absorption=1;
        [Range(0,2)] public float mineralSusceptibility=1;
        [Range(0,2)] public float fadingResponse=1;
        [Range(0,.03f)] public float microNormalStrength=.008f;
        public void Apply(Material material)
        {if(material&&material.HasProperty("_CFMaterialResponse"))material.SetVector("_CFMaterialResponse",new Vector4(absorption,mineralSusceptibility,fadingResponse,microNormalStrength));}
    }
}
