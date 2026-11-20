using UnityEngine;
namespace CityForge.World
{
    /// <summary>Animates authored axle pivots consistently on both mesh tiers.</summary>
    public sealed class WaterMillWheel : MonoBehaviour
    {
        Transform[] wheels;Quaternion[] rest;Vector3[] axes;float angle;
        public static void Attach(GameObject mill)
        {
            var component=mill.AddComponent<WaterMillWheel>();var found=new System.Collections.Generic.List<Transform>();
            foreach(var child in mill.GetComponentsInChildren<Transform>(true))if(child.name=="Mill_Wheel")found.Add(child);
            component.wheels=found.ToArray();component.rest=new Quaternion[found.Count];component.axes=new Vector3[found.Count];
            for(int i=0;i<found.Count;i++){
                component.rest[i]=found[i].localRotation;var size=found[i].GetComponent<MeshFilter>().sharedMesh.bounds.size;
                component.axes[i]=size.x<size.y&&size.x<size.z?Vector3.right:size.y<size.z?Vector3.up:Vector3.forward;
            }
        }
        public void Tick(float seconds){if(wheels==null)return;angle=Mathf.Repeat(angle+seconds*18,360);for(int i=0;i<wheels.Length;i++)if(wheels[i])wheels[i].localRotation=rest[i]*Quaternion.AngleAxis(angle,axes[i]);}
        void Update()=>Tick(Time.deltaTime);
    }
}
