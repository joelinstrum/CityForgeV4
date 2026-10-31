using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Stable summit-only snow shared by regional cartography and detailed districts.</summary>
    public sealed class MountainSummitSnow : MonoBehaviour
    {
        public struct Summit { public Vector2 uv; public float height; }
        public static bool Applies(PrototypeRegionView view)=>view.CreationSettings?.version>=10&&view.CreationSettings.terrainShape==RegionTerrainShape.MountainBorder;
        public static Summit[] Select(PrototypeRegionData data,int maximum=3)
        {
            var candidates=new List<Summit>();var h=data.Heights;int n=h.GetLength(0);
            for(int z=2;z<n-2;z+=2)for(int x=2;x<n-2;x+=2){float v=h[z,x];bool peak=true;
                for(int dz=-2;dz<=2;dz+=2)for(int dx=-2;dx<=2;dx+=2)if(h[z+dz,x+dx]>v)peak=false;
                if(peak&&v>.15f)candidates.Add(new Summit{uv=new Vector2(x/(float)(n-1),z/(float)(n-1)),height=v});
            }
            candidates.Sort((a,b)=>b.height.CompareTo(a.height));var chosen=new List<Summit>();
            foreach(var peak in candidates){if(chosen.Count==maximum)break;if(peak.height<candidates[0].height*.72f)break;bool separate=true;foreach(var other in chosen)if(Vector2.Distance(other.uv,peak.uv)<.12f)separate=false;if(separate)chosen.Add(peak);}
            return chosen.ToArray();
        }
        public static float Cover(Summit[] peaks,Vector2 uv,float height,int seed)
        {
            float cover=0;foreach(var peak in peaks){float d=Vector2.Distance(uv,peak.uv);if(d>.075f)continue;
                float edge=(Mathf.PerlinNoise(uv.x*180+seed%997,uv.y*180)-.5f)*.009f;
                float snow=Mathf.SmoothStep(0,1,Mathf.InverseLerp(peak.height*.80f+edge,peak.height*.90f+edge,height));
                cover=Mathf.Max(cover,snow*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.045f,.075f,d))));
            }return cover;
        }
        Mesh mesh;Material material;Texture2D mask;
        public void Initialize(MeadowDistrictLandscape land)
        {
            var peaks=Select(land.View.Data,MountainBorderVariation.CapCount(land.View));var td=land.DisplayTerrain.terrainData;int n=td.heightmapResolution;
            var heights=td.GetHeights(0,0,n,n);var vertices=new Vector3[n*n];var uv=new Vector2[n*n];var pixels=new Color[n*n];var triangles=new int[(n-1)*(n-1)*6];int t=0;bool any=false;
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){int i=z*n+x;float u=x/(float)(n-1),v=z/(float)(n-1);var p=land.Origin+new Vector2(u,v)*land.Side;var regional=p/land.View.MapSide+Vector2.one*.5f;
                float cover=Cover(peaks,regional,heights[z,x],land.Seed);bool dry=land.Contains(p)&&land.WaterDistance(p)>1.2f&&land.ShoreDistance(p)>.6f;bool snowy=dry&&cover>.4f;any|=snowy;pixels[i]=snowy?Color.white:Color.black;
                vertices[i]=new Vector3(u*land.Side,heights[z,x]*td.size.y+.025f,v*land.Side);uv[i]=new Vector2(u,v);
                if(x<n-1&&z<n-1){triangles[t++]=i;triangles[t++]=i+n;triangles[t++]=i+1;triangles[t++]=i+1;triangles[t++]=i+n;triangles[t++]=i+n+1;}
            }
            if(!any){Destroy(gameObject);return;}
            mask=new Texture2D(n,n,TextureFormat.R8,false,true){wrapMode=TextureWrapMode.Clamp};mask.SetPixels(pixels);mask.Apply();
            mesh=new Mesh{name="Permanent summit snow",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            material=new Material(Resources.Load<Shader>("Weather/DistrictSnow"));material.SetTexture("_Land",mask);material.SetFloat("_Coverage",1);
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;transform.position=land.DisplayTerrain.transform.position;
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);if(mask)Destroy(mask);}
    }
}
