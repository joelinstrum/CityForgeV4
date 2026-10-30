using System;
namespace CityForge.World
{
    /// <summary>Local diagnostic save IDs; density is a nested subset of the canonical population.</summary>
    public static class PanningTestDistricts
    {
        public static bool TryGetPercent(string id,out int percent){percent=0;switch(id){case "pan-test-meadow":return true;case "pan-test-trees-30":percent=30;return true;case "pan-test-coast-50":case "pan-test-trees-50":percent=50;return true;case "pan-test-trees-80":percent=80;return true;default:return false;}}
        public static bool[] Mask(int count,int percent){var order=new int[count];for(int i=0;i<count;i++)order[i]=i;uint state=730421;for(int i=count-1;i>0;i--){state^=state<<13;state^=state>>17;state^=state<<5;int j=(int)(state%(uint)(i+1));int t=order[i];order[i]=order[j];order[j]=t;}var mask=new bool[count];for(int i=0;i<count*percent/100;i++)mask[order[i]]=true;return mask;}
        public static T[] Subset<T>(T[] source,int percent){var mask=Mask(source.Length,percent);var result=new T[source.Length*percent/100];int n=0;for(int i=0;i<source.Length;i++)if(mask[i])result[n++]=source[i];return result;}
    }
}
