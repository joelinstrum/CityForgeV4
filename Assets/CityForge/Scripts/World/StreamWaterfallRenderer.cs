using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    /// <summary>Bounded lip, falling sheet and landing foam rebuilt from saved stream grades.</summary>
    public sealed class StreamWaterfallRenderer : MonoBehaviour
    {
        Mesh mesh; Material material;
        public int FallCount { get; private set; }
        public void Initialize(MeadowDistrictLandscape land, RegionWaterBody body)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var kinds=new List<Vector2>();var triangles=new List<int>();
            float side=land.View.MapSide;
            for(int i=0;i<body.centerline.Length-1&&FallCount<128;i++){
                var a=(body.centerline[i]-Vector2.one*.5f)*side;
                var b=(body.centerline[i+1]-Vector2.one*.5f)*side;
                float run=Vector2.Distance(a,b),drop=body.playerChannelLevels[i]-body.playerChannelLevels[i+1];
                if(run<.01f||drop/run<.6f)continue;
                int end=i+1;
                // Group adjacent steep stations into one real falling sheet.
                while(end<body.centerline.Length-1){
                    var next=(body.centerline[end+1]-Vector2.one*.5f)*side;
                    float nextRun=Vector2.Distance(b,next),nextDrop=body.playerChannelLevels[end]-body.playerChannelLevels[end+1];
                    if(nextRun<.01f||nextDrop/nextRun<.6f||Vector2.Distance(a,next)>8)break;
                    end++;b=next;
                }
                drop=body.playerChannelLevels[i]-body.playerChannelLevels[end];i=end-1;
                if(drop<.8f||!land.Contains(b))continue;
                var direction=(b-a).normalized;var cross=Vector2.Perpendicular(direction);
                float width=Mathf.Min(PlayerStreamWidth.AtPoint(body,a,side),PlayerStreamWidth.AtPoint(body,b,side))*.92f;
                float top=PlayerChannelGeometry.Level(body,a,side)+.025f,bottom=PlayerChannelGeometry.Level(body,b,side)+.025f;
                // A short overhanging lip joins the upstream surface to the vertical curtain.
                Quad(P(a-cross*width,top),P(a+cross*width,top),P(b+cross*width,top),P(b-cross*width,top),2,Vector2.Distance(a,b));
                const int rows=12,columns=12;
                for(int row=0;row<rows;row++)for(int col=0;col<columns;col++){
                    int start=vertices.Count;
                    for(int corner=0;corner<4;corner++){
                        float u=(col+(corner==1||corner==2?1:0))/(float)columns;
                        float v=(row+(corner>=2?1:0))/(float)rows;
                        // Slight outward arc separates the falling sheet from the cliff face.
                        var p=b+direction*(.08f+.38f*v*v)+cross*((u*2-1)*width);
                        vertices.Add(P(p,Mathf.Lerp(top,bottom,v)));uv.Add(new Vector2(u,v*drop));kinds.Add(Vector2.zero);
                    }
                    Indices(start);
                }
                var center=b+direction*.55f;
                Quad(P(center-cross*width-direction*.6f,bottom+.02f),P(center+cross*width-direction*.6f,bottom+.02f),P(center+cross*width+direction*1.6f,bottom+.02f),P(center-cross*width+direction*1.6f,bottom+.02f),1,1);
                FallCount++;
            }
            if(vertices.Count==0)return;
            mesh=new Mesh{name="Stream waterfall lips, curtains and splash",indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,kinds);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var shader=Resources.Load<Shader>("StreamWaterfall");
            material=new Material(shader){name="Animated stream falling water"};
            gameObject.layer=4;gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            Vector3 P(Vector2 p,float height)=>new Vector3(p.x,height,p.y);
            void Indices(int n){triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,int kind,float length){int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{Vector2.zero,Vector2.right,new Vector2(1,length),new Vector2(0,length)});for(int k=0;k<4;k++)kinds.Add(new Vector2(kind,0));Indices(n);}
        }
        void OnDestroy(){if(mesh)Destroy(mesh);if(material)Destroy(material);}
    }
}
