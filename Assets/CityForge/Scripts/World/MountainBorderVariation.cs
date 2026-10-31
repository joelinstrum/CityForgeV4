using UnityEngine;
namespace CityForge.World
{
    /// <summary>Seeded silhouettes, with the approved cross-range profile and foothills retained.</summary>
    public sealed class MountainBorderVariation
    {
        public int Pattern { get; }
        public int SnowCaps=>Pattern==0?1:Pattern==1?2:3;
        readonly float first,second,width;
        public MountainBorderVariation(int seed)
        {
            Pattern=(int)((uint)seed%3);var random=new System.Random(seed^0x51a7b);
            first=Pattern==0?.36f+(float)random.NextDouble()*.28f:.23f+(float)random.NextDouble()*.12f;
            second=.65f+(float)random.NextDouble()*.12f;width=.075f+(float)random.NextDouble()*.045f;
        }
        public float Peaks(float along)
        {
            float a=Mathf.Exp(-Mathf.Pow((along-first)/width,2));
            if(Pattern==0)return .74f+.40f*a;
            float b=Mathf.Exp(-Mathf.Pow((along-second)/(width*1.12f),2));
            return .43f+.62f*Mathf.Max(a,b*.94f);
        }
        public static int CapCount(PrototypeRegionView view)=>view.CreationSettings?.version>=11?new MountainBorderVariation(view.Data.Seed).SnowCaps:3;
    }
}
