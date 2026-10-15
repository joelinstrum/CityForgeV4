using UnityEngine;
namespace CityForge.World
{
    /// <summary>Asset-independent woodland presentation; change the resource folder to try another pack.</summary>
    [System.Serializable]
    public sealed class ForestPresentationSettings
    {
        public string resourceFolder = "Trees/SiberianElm";
        public int fullCrownThroughZoom = 3;
        public float testStandCompression = .4f;
        public static ForestPresentationSettings Load()
        {
            var source=Resources.Load<TextAsset>("Trees/ForestPresentation");
            return source==null ? new ForestPresentationSettings() : JsonUtility.FromJson<ForestPresentationSettings>(source.text);
        }
        public int MeshTier(int zoom,int count) => Mathf.Clamp(zoom<=fullCrownThroughZoom ? 0 : zoom-1,0,count-1);
        public Vector2 PackTestStand(Vector2 world,float side)
        {
            var p=world/side; var a=new Vector2(.19f,.29f); var b=new Vector2(.32f,.33f); var axis=b-a;
            var nearest=a+axis*Mathf.Clamp01(Vector2.Dot(p-a,axis)/axis.sqrMagnitude);
            if(Vector2.Distance(p,nearest)>.06f) return world;
            return ((a+b)*.5f+(p-(a+b)*.5f)*Mathf.Clamp(testStandCompression,.35f,1f))*side;
        }
    }
}
