using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    // One combined mesh per material group. No per-beam behaviours or collider objects.
    internal sealed class BridgePresentation : MonoBehaviour
    {
        readonly List<Object> owned=new();
        internal void Build(BridgeInstance bridge,BridgeStyle style,bool ghost,bool valid=true,System.Func<Vector2,float> ground=null)
        {
            var deck=new Boxes();var rails=new Boxes();var frame=new Boxes();
            Vector3 World(float at,float across,float height) { var p=bridge.Point(at)+bridge.Right*across;return new Vector3(p.x,height,p.y); }
            Vector3 forward=new(bridge.Direction.x,0,bridge.Direction.y),right=new(bridge.Right.x,0,bridge.Right.y);
            var yaw=Quaternion.LookRotation(forward);
            int segments=Mathf.CeilToInt(bridge.Length/4);
            for(int i=0;i<segments;i++)
            {
                float a=bridge.Length*i/segments,b=bridge.Length*(i+1)/segments;
                var from=World(a,0,bridge.Height(a)-style.deckThickness*.5f);
                var to=World(b,0,bridge.Height(b)-style.deckThickness*.5f);
                Add(deck,style.deckPart,(from+to)*.5f,Quaternion.LookRotation(to-from),new Vector3(bridge.TotalWidth,style.deckThickness,(to-from).magnitude+.02f));
                for(int side=-1;side<=1;side+=2)
                {
                    float across=side*(bridge.TotalWidth*.5f-.12f);
                    var p=World(a,across,bridge.Height(a));var q=World(b,across,bridge.Height(b));
                    Add(rails,style.railingPart,p+Vector3.up*style.railingHeight*.5f,yaw,new Vector3(.16f,style.railingHeight,.16f));
                    Beam(rails,style.railingPart,p+Vector3.up*style.railingHeight,q+Vector3.up*style.railingHeight,.14f);
                    Beam(rails,style.railingPart,p+Vector3.up*style.railingHeight*.45f,q+Vector3.up*style.railingHeight*.45f,.11f);
                    // Low curb separates the walk from the carriageway.
                    Beam(rails,style.railingPart,World(a,side*bridge.width*.5f,bridge.Height(a)+.04f),World(b,side*bridge.width*.5f,bridge.Height(b)+.04f),.08f);
                }
            }
            foreach(var support in bridge.supports)
            {
                float across=bridge.TotalWidth*.4f,top=bridge.deckHeight-style.deckThickness;
                var left=World(support.station,-across,support.leftBed-.4f);
                var rightFoot=World(support.station,across,support.rightBed-.4f);
                var leftTop=World(support.station,-across,top);
                var rightTop=World(support.station,across,top);
                Beam(frame,style.supportPart,left,leftTop,.38f);Beam(frame,style.supportPart,rightFoot,rightTop,.38f);
                Beam(frame,style.supportPart,leftTop,rightTop,.4f);
                Beam(frame,style.bracePart,left+Vector3.up*.3f,rightTop-Vector3.up*.4f,.22f);
                Beam(frame,style.bracePart,rightFoot+Vector3.up*.3f,leftTop-Vector3.up*.4f,.22f);
            }
            for(int i=1;i<bridge.supports.Length;i++)
            {
                var a=bridge.supports[i-1];var b=bridge.supports[i];
                // Leave the full designated boat opening free of low diagonal bracing.
                if(a.station<bridge.channelCenter && b.station>bridge.channelCenter)continue;
                for(int side=-1;side<=1;side+=2)
                {
                    float across=side*bridge.TotalWidth*.4f;
                    float bedA=side<0?a.leftBed:a.rightBed,bedB=side<0?b.leftBed:b.rightBed;
                    Beam(frame,style.bracePart,World(a.station,across,bedA+.5f),World(b.station,across,bridge.deckHeight-style.deckThickness-.4f),.18f);
                }
            }
            if(ghost && bridge.earthwork && ground!=null)
            {
                var vertices=new List<Vector3>();var triangles=new List<int>();
                float half=bridge.TotalWidth*.5f,reach=half+bridge.earthworkFeather;
                var across=new[]{-reach,-half,half,reach};
                for(int bank=0;bank<2;bank++)
                {
                    float from=bank==0?0:bridge.deckEnd,to=bank==0?bridge.deckStart:bridge.Length;
                    int count=Mathf.Max(1,Mathf.CeilToInt((to-from)/4));int first=vertices.Count;
                    for(int i=0;i<=count;i++)for(int j=0;j<across.Length;j++)
                    {
                        float at=Mathf.Lerp(from,to,(float)i/count);var point=bridge.Point(at)+bridge.Right*across[j];
                        float blend=Mathf.Clamp01((Mathf.Abs(across[j])-half)/Mathf.Max(.01f,bridge.earthworkFeather));blend=blend*blend*(3-2*blend);
                        float height=Mathf.Lerp(bridge.Height(at)-.11f,ground(point)+.025f,blend);
                        vertices.Add(new Vector3(point.x,height,point.y));
                    }
                    for(int i=0;i<count;i++)for(int j=0;j<across.Length-1;j++)
                    {
                        int a=first+i*4+j,b=a+1,c=a+4,d=c+1;
                        triangles.AddRange(new[]{a,b,c,b,d,c});
                    }
                }
                var mesh=new Mesh{name="Proposed approach grading"};owned.Add(mesh);mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var fill=new GameObject("Proposed approach grading",typeof(MeshFilter),typeof(MeshRenderer));fill.transform.SetParent(transform,false);
                fill.GetComponent<MeshFilter>().sharedMesh=mesh;fill.GetComponent<MeshRenderer>().sharedMaterial=Material(new Color(.9f,.7f,.2f),true);
            }
            MeshGroup("Deck and approaches",deck,new Color(.43f,.36f,.26f),!ghost);
            MeshGroup("Railings and curbs",rails,new Color(.22f,.17f,.11f),false);
            MeshGroup("Trestle bents and braces",frame,new Color(.31f,.23f,.14f),false);

            void Add(Boxes boxes,string part,Vector3 center,Quaternion rotation,Vector3 size)
            {
                if(part=="primitive:box")boxes.Add(center,rotation,size);
                else
                {
                    // The canonical solid road envelope remains the collision surface for modular deck art.
                    if(boxes==deck)boxes.Add(center,rotation,size);
                    var prefab=Resources.Load<GameObject>(part);var item=Instantiate(prefab,transform);
                    item.transform.localPosition=center;item.transform.localRotation=rotation;item.transform.localScale=size;
                    foreach(var collider in item.GetComponentsInChildren<Collider>())collider.enabled=false;
                    if(ghost)foreach(var renderer in item.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=Material(new Color(.25f,.8f,.8f));
                }
            }
            void Beam(Boxes boxes,string part,Vector3 a,Vector3 b,float thickness)
            {
                if((b-a).sqrMagnitude<.0001f)return;
                Add(boxes,part,(a+b)*.5f,Quaternion.LookRotation(b-a),new Vector3(thickness,thickness,(b-a).magnitude));
            }
            Material Material(Color color,bool grading=false)
            {
                if(ghost)color=!valid?new Color(1,.36f,.22f,.5f):grading?new Color(.95f,.7f,.2f,.4f):new Color(.25f,.8f,.72f,.46f);
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));owned.Add(material);
                material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.1f);
                if(ghost)
                {
                    material.SetFloat("_Surface",1);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
                }
                return material;
            }
            void MeshGroup(string label,Boxes boxes,Color color,bool collision)
            {
                if(boxes.Vertices.Count==0)return;
                var mesh=new Mesh{name="Assembled bridge "+label,indexFormat=IndexFormat.UInt32};owned.Add(mesh);
                mesh.SetVertices(boxes.Vertices);mesh.SetTriangles(boxes.Triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var child=new GameObject(label,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(transform,false);
                child.GetComponent<MeshFilter>().sharedMesh=mesh;child.GetComponent<MeshRenderer>().sharedMaterial=Material(color);
                if(collision)child.AddComponent<MeshCollider>().sharedMesh=mesh;
            }
        }
        void OnDestroy(){foreach(var asset in owned)if(asset)Destroy(asset);}
        sealed class Boxes
        {
            internal readonly List<Vector3> Vertices=new();internal readonly List<int> Triangles=new();
            static readonly int[] corners={0,2,3,1,4,5,7,6,0,1,5,4,2,6,7,3,0,4,6,2,1,3,7,5};
            internal void Add(Vector3 center,Quaternion rotation,Vector3 size)
            {
                for(int face=0;face<6;face++)
                {
                    int first=Vertices.Count;
                    for(int j=0;j<4;j++){int c=corners[face*4+j];var v=new Vector3((c&1)==0?-.5f:.5f,(c&2)==0?-.5f:.5f,(c&4)==0?-.5f:.5f);Vertices.Add(center+rotation*Vector3.Scale(v,size));}
                    Triangles.Add(first);Triangles.Add(first+1);Triangles.Add(first+2);Triangles.Add(first);Triangles.Add(first+2);Triangles.Add(first+3);
                }
            }
        }
    }
}
