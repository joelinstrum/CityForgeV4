using System;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    internal enum VehicleCargoShape { Crates, Logs, Boards, Blocks, Rocks }
    [Serializable] internal sealed class VehicleCargoStyle
    {
        public VehicleResource resource;
        public VehicleCargoShape shape;
        public Color color = Color.gray;
    }
    internal sealed class VehicleCargoPresentation : MonoBehaviour
    {
        [Serializable] private sealed class Catalog { public VehicleCargoStyle[] styles; }
        private static VehicleCargoStyle[] styles;
        private static readonly Dictionary<VehicleResource, Material> materials = new();
        private static Mesh floorMesh;
        private static Material floorMaterial;
        private static readonly Dictionary<(VehicleCargoShape, int), Mesh> meshes = new();
        private MeshFilter filter;
        private MeshRenderer renderer;
        private int visible = -1;
        private VehicleResource resource;
        private bool enclosed;
        internal int VisibleStages => Mathf.Max(0, visible);

        internal static VehicleCargoPresentation Attach(Transform vehicle, VehicleProfile profile)
        {
            if (!vehicle || vehicle.childCount == 0) return null;
            var attached = vehicle.GetComponent<VehicleCargoPresentation>();
            if (attached) return attached;
            var team = vehicle.GetChild(0);
            var wagon = profile?.team == true && team.childCount > 0 ? team.GetChild(0) : team;
            if (!string.IsNullOrEmpty(profile?.emptyCargoMesh))
            {
                var empty = Resources.Load<Mesh>(profile.emptyCargoMesh);
                if (empty) foreach (var part in wagon.GetComponentsInChildren<MeshFilter>())
                    if (part.name == profile.cargoMeshPart) { part.sharedMesh = empty; break; }
            }
            var holder = new GameObject("Delivery cargo"); holder.transform.SetParent(wagon, false);
            if (profile != null && profile.cargoSize.x > 0)
            {
                holder.transform.localPosition = profile.cargoCenter;
                holder.transform.localScale = profile.cargoSize;
            }
            else
            {
                // Standard cart bed follows the articulated wagon, between its wheel axles.
                Transform front = null, rear = null, right = null;
                foreach (var part in wagon.GetComponentsInChildren<Transform>())
                {
                    if (part.name == "Carriage_Front_Left") front = part;
                    if (part.name == "Carriage_Rear_Left") rear = part;
                    if (part.name == "Carriage_Rear_Right") right = part;
                }
                var a = rear ? wagon.InverseTransformPoint(rear.position) : new Vector3(-.6f,.45f,-1.6f);
                var b = front ? wagon.InverseTransformPoint(front.position) : new Vector3(-.6f,.45f,.3f);
                float width = right ? Mathf.Abs(wagon.InverseTransformPoint(right.position).x-a.x)*.72f : 1.05f;
                holder.transform.localPosition = new Vector3(0, Mathf.Max(a.y,b.y)+.45f, Mathf.Lerp(a.z,b.z,.35f));
                holder.transform.localScale = new Vector3(Mathf.Clamp(width,.6f,1.5f), .6f, Mathf.Max(.8f,Mathf.Abs(b.z-a.z)*.85f));
            }
            if (!string.IsNullOrEmpty(profile?.emptyCargoMesh))
            {
                if (!floorMesh)
                {
                    floorMesh = new Mesh { name = "Empty cart bed" };
                    var vertices = new Vector3[8];
                    for (int i=0;i<8;i++) vertices[i]=new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f);
                    floorMesh.vertices=vertices;floorMesh.triangles=new[]{0,2,3,0,3,1,4,5,7,4,7,6,0,4,6,0,6,2,1,3,7,1,7,5,2,6,7,2,7,3,0,1,5,0,5,4};floorMesh.RecalculateNormals();
                    floorMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Cart bed wood",color=new Color(.26f,.16f,.08f)};
                    floorMaterial.enableInstancing=true;
                }
                var bed=new GameObject("Empty cargo bed");bed.transform.SetParent(wagon,false);
                bed.transform.localPosition=profile.cargoCenter-Vector3.up*.04f;
                bed.transform.localScale=new Vector3(profile.cargoSize.x,.08f,profile.cargoSize.z);
                bed.AddComponent<MeshFilter>().sharedMesh=floorMesh;bed.AddComponent<MeshRenderer>().sharedMaterial=floorMaterial;
            }
            var mesh = holder.AddComponent<MeshFilter>(); var view = holder.AddComponent<MeshRenderer>();
            view.shadowCastingMode = ShadowCastingMode.On;
            var presentation=vehicle.gameObject.AddComponent<VehicleCargoPresentation>();
            presentation.filter=mesh;presentation.renderer=view;presentation.enclosed=profile?.enclosedCargo==true;
            return presentation;
        }

        internal void SetLoad(VehicleResource selected, int amount, int capacity)
        {
            int count = amount <= 0 ? 0 : Mathf.Clamp(Mathf.CeilToInt(12f * amount / Mathf.Max(1,capacity)), 1,12);
            if (visible == count && resource == selected) return;
            visible = count; resource = selected; renderer.enabled = count > 0 && !enclosed;
            if (count == 0 || enclosed) return;
            styles ??= JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Config/VehicleCargo").text).styles;
            var style = Array.Find(styles, entry => entry.resource == selected) ?? new VehicleCargoStyle { resource = selected };
            if (!materials.TryGetValue(selected,out var material))
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = selected + " delivery cargo" };
                material.color = style.color; material.SetFloat("_Smoothness",.12f); material.SetFloat("_Metallic",0f); material.enableInstancing = true; materials[selected] = material;
            }
            renderer.sharedMaterial = material;
            var key=(style.shape,count);
            if (!meshes.TryGetValue(key,out var mesh)) meshes[key]=mesh=Build(style.shape,count);
            filter.sharedMesh=mesh;
        }

        private static Mesh Build(VehicleCargoShape shape, int count)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<count;i++)
            {
                int x=i%3,z=(i/3)%2,y=i/6;
                var center=new Vector3((x-1)*.30f,.19f+y*.36f,(z-.5f)*.48f);
                var size=new Vector3(.27f,.32f,.43f);
                if(shape==VehicleCargoShape.Boards)size=new Vector3(.27f,.18f,.46f);
                if(shape==VehicleCargoShape.Blocks)size=new Vector3(.27f,.23f,.43f);
                center.y = shape switch
                {
                    VehicleCargoShape.Logs => .15f + y * .30f,
                    VehicleCargoShape.Rocks => .15f + y * .28f,
                    _ => size.y * (.5f + y)
                };
                if(shape==VehicleCargoShape.Rocks)
                {
                    for(int piece=0;piece<2;piece++)
                    {
                    int at=vertices.Count;float t=(1+Mathf.Sqrt(5))*.5f;
                    var rock=new[]{new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),
                        new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),
                        new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1)};
                    var offset = center + Vector3.forward * ((piece-.5f)*.24f);
                    float variation=.92f+((i*13+piece*7)%5)*.035f;
                    var rotation=Quaternion.Euler(0,(i*37+piece*83)%180,0);
                    foreach(var v in rock)vertices.Add(offset+Vector3.Scale(rotation*v.normalized,new Vector3(.17f,.18f,.12f))*variation);
                    foreach(int index in new[]{0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,
                        3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1})triangles.Add(at+index);
                    }
                }
                else if(shape==VehicleCargoShape.Logs)
                {
                    const int sides=8;int at=vertices.Count;
                    for(int end=0;end<2;end++)for(int side=0;side<sides;side++)
                    {float angle=side*Mathf.PI*2/sides;vertices.Add(center+new Vector3(Mathf.Cos(angle)*.135f,Mathf.Sin(angle)*.15f,(end-.5f)*.45f));}
                    for(int side=0;side<sides;side++)
                    {
                        int next=(side+1)%sides;
                        Face(at+side,at+next,at+sides+next);Face(at+side,at+sides+next,at+sides+side);
                        if(side>0&&side<sides-1){Face(at,at+next,at+side);Face(at+sides,at+sides+side,at+sides+next);}
                    }
                }
                else
                {
                    int at=vertices.Count;
                    for(int corner=0;corner<8;corner++)vertices.Add(center+Vector3.Scale(size,new Vector3((corner&1)==0?-.5f:.5f,(corner&2)==0?-.5f:.5f,(corner&4)==0?-.5f:.5f)));
                    foreach(int index in new[]{0,2,3,0,3,1,4,5,7,4,7,6,0,4,6,0,6,2,1,3,7,1,7,5,2,6,7,2,7,3,0,1,5,0,5,4})triangles.Add(at+index);
                }
            }
            var mesh=new Mesh{name="Cargo "+shape+" "+count+"/12"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            void Face(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
        }
    }
}
