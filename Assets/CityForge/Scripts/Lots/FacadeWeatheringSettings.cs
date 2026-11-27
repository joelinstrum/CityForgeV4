using System;
using System.IO;
namespace CityForge.Lots
{
    // Value-only save data; validation is safe on background persistence threads.
    [Serializable] public struct FacadeWeatheringSettings
    {
        public bool enabled;
        public float surfaceVariation, rainwaterStreaks, sootPollution, foundationGrime, sunFading, mineralDeposits;
        // Zero derives a stable seed from the persistent building ID, never its position.
        public int seed;
        public static FacadeWeatheringSettings Preset(int index) => index switch {
            0 => new FacadeWeatheringSettings { enabled=true },
            1 => new FacadeWeatheringSettings { enabled=true,surfaceVariation=.35f,rainwaterStreaks=.30f,sootPollution=.20f,foundationGrime=.35f,sunFading=.15f,mineralDeposits=.05f },
            2 => new FacadeWeatheringSettings { enabled=true,surfaceVariation=.60f,rainwaterStreaks=.55f,sootPollution=.40f,foundationGrime=.60f,sunFading=.30f,mineralDeposits=.15f },
            3 => new FacadeWeatheringSettings { enabled=true,surfaceVariation=.85f,rainwaterStreaks=.80f,sootPollution=.65f,foundationGrime=.85f,sunFading=.45f,mineralDeposits=.30f },
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        public float Get(int channel) => channel switch {0=>surfaceVariation,1=>rainwaterStreaks,2=>sootPollution,3=>foundationGrime,4=>sunFading,5=>mineralDeposits,_=>throw new ArgumentOutOfRangeException(nameof(channel))};
        public void Set(int channel,float value){switch(channel){case 0:surfaceVariation=value;break;case 1:rainwaterStreaks=value;break;case 2:sootPollution=value;break;case 3:foundationGrime=value;break;case 4:sunFading=value;break;case 5:mineralDeposits=value;break;default:throw new ArgumentOutOfRangeException(nameof(channel));}enabled=true;}
        public static bool Same(FacadeWeatheringSettings a,FacadeWeatheringSettings b)
        {if(a.enabled!=b.enabled)return false;if(!a.enabled)return true;if(a.seed!=b.seed)return false;for(int i=0;i<6;i++)if(a.Get(i)!=b.Get(i))return false;return true;}
        public static void Validate(FacadeWeatheringSettings s){for(int i=0;i<6;i++)if(!float.IsFinite(s.Get(i))||s.Get(i)<0||s.Get(i)>1)throw new InvalidDataException("Invalid facade weathering amount.");}
        public static int StableSeed(string id){unchecked{uint h=2166136261;foreach(char c in id??""){h^=c;h*=16777619;}return (int)(h&0x7fffffff);}}
    }
}
