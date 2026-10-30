using UnityEngine;
using WaveHarmonic.Crest;
namespace CityForge.World
{
    /// <summary>Opt-in coastal trial: a shallow wave packet and matching foam input, evaluated on the GPU.</summary>
    public sealed class CoastalShorelineSwash:MonoBehaviour
    {
        Mesh mesh;Material waves,foam;
        public static bool Applies(PrototypeRegionView view)=>view.Coastline&&view.CoastVersion>=3;
        public void Initialize(MeadowDistrictLandscape land)
        {
            const int along=512,across=32;var vertices=new Vector3[(along+1)*(across+1)];var uv=new Vector2[vertices.Length];var ground=new Vector2[vertices.Length];var indices=new int[along*across*6];
            for(int z=0;z<=along;z++){float worldZ=land.Origin.y+land.Side*z/along;float shore=CoastlineTestRegion.ShoreX(worldZ);
                for(int x=0;x<=across;x++){float d=Mathf.Lerp(-40,20,x/(float)across);int i=z*(across+1)+x;var point=new Vector2(shore+d,worldZ);vertices[i]=new Vector3(point.x,land.View.SeaLevel,point.y);uv[i]=new Vector2(d,worldZ);ground[i]=new Vector2(land.Height(point)-land.View.SeaLevel,0);
                    if(x<across&&z<along){int t=(z*across+x)*6;indices[t]=i;indices[t+1]=i+across+1;indices[t+2]=i+1;indices[t+3]=i+1;indices[t+4]=i+across+1;indices[t+5]=i+across+2;}
                }
            }
            mesh=new Mesh{name="Shallow coastline swash input"};mesh.vertices=vertices;mesh.uv=uv;mesh.uv2=ground;mesh.triangles=indices;mesh.RecalculateBounds();
            waves=MakeMaterial("Waves");foam=MakeMaterial("Foam");
            var waveRenderer=MakeInput("Shallow shoreline displacement",waves);var waveInput=WaveHarmonic.Crest._Extensions.AddComponent<AnimatedWavesLodInput>(waveRenderer.gameObject,LodInputMode.Renderer);waveInput.MaximumDisplacementVertical=.5f;waveInput.GetData<AnimatedWavesRendererLodInputData>().Renderer=waveRenderer;
            var foamRenderer=MakeInput("Swash edge foam",foam);var foamInput=WaveHarmonic.Crest._Extensions.AddComponent<FoamLodInput>(foamRenderer.gameObject,LodInputMode.Renderer);foamInput.GetData<FoamRendererLodInputData>().Renderer=foamRenderer;
        }
        Material MakeMaterial(string name){var shader=Resources.Load<Shader>("CoastalSwash/"+name);if(!shader||!shader.isSupported)throw new System.InvalidOperationException("Missing coastal swash shader: "+name);return new Material(shader){name="Coastal swash "+name};}
        MeshRenderer MakeInput(string name,Material material){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.forceRenderingOff=true;return renderer;}
        void Update(){if(waves)waves.SetFloat("_SwashTime",Time.time);if(foam)foam.SetFloat("_SwashTime",Time.time);}
        void OnDestroy(){if(mesh)Destroy(mesh);if(waves)Destroy(waves);if(foam)Destroy(foam);}
    }
}
