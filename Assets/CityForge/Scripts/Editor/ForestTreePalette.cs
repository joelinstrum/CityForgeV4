#if UNITY_EDITOR
using UnityEngine;
namespace CityForge.EditorTools
{
    public static class ForestTreePalette
    {
        public static void Apply(Material material,string path)
        {
            var color=new Color(.65f,.65f,.65f,1);
            if(path.Contains("SilverMaple")&&path.Contains("GreenLeaf"))color=new Color(.20f,.32f,.25f,1);
            else if(path.Contains("SilverMaple")&&path.Contains("YellowLeaf"))color=new Color(.55f,.5f,.45f,1);
            else if(path.Contains("CanyonLiveOak")&&path.Contains("Leaf"))color=new Color(.24f,.4f,.22f,1);
            else if(path.Contains("SouthernLiveOak")&&path.Contains("Leaf"))color=new Color(.3f,.45f,.28f,1);
            else if(path.Contains("AngelOak")&&path.Contains("_16"))color=new Color(.22f,.4f,.28f,1);
            else if(path.Contains("AngelOak")&&!path.Contains("_18"))color=new Color(.45f,.5f,.4f,1);
            else if(path.Contains("OakPilot")&&path.Contains("Leaf"))color=path.Contains("/Summer/")?new Color(.4f,.6f,.42f,1):new Color(.6f,.55f,.5f,1);
            material.SetColor("_BaseColor",color);
        }
    }
}
#endif
