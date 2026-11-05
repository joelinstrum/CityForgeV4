using System;
using UnityEngine;
using CityForge.CameraSystem;
namespace CityForge.World
{
    /// <summary>One transient, ocean-only great white patrol per navigable coastal district.</summary>
    public sealed class CoastalSharkPopulation:MonoBehaviour
    {
        MeadowDistrictLandscape land;CoastalOceanHabitat habitat;CityForgeCameraController rig;
        Transform shark;Animator animator;Renderer[] renderers;System.Random random;
        Vector2 target;float accumulator,distanceTravelled;bool visible;
        public int SharkCount=>shark?1:0;
        public float DistanceTravelled=>distanceTravelled;
        public CoastalOceanHabitat Habitat=>habitat;
        public Vector2 Position=>shark?new Vector2(shark.position.x,shark.position.z):default;
        public bool ValidPosition=>shark&&habitat!=null&&habitat.SharkPoint(Position);
        float R()=>(float)random.NextDouble();
        public static bool Applies(MeadowDistrictLandscape source)
        {
            if(!source||source.IsRegionOverview||!source.View.AnimalsEnabled||source.View.Waterless)return false;
            var area=new Rect(source.Origin/source.View.MapSide+Vector2.one*.5f,Vector2.one*(source.Side/source.View.MapSide));
            foreach(var b in source.View.Data.WaterBodies)if(b.kind==WaterBodyKind.Ocean&&b.Intersects(area))return true;return false;
        }
        public void Initialize(MeadowDistrictLandscape source)
        {
            land=source;if(!Applies(land))return;
            var prefab=Resources.Load<GameObject>("CoastalWildlife/GreatWhite/GreatWhiteShark");
            if(!prefab){Debug.LogWarning("Private great white prefab missing. Use CityForge / Wildlife / Prepare supplied great white.");return;}
            habitat=new CoastalOceanHabitat(land);if(habitat.PatrolPoints.Count==0)return;
            random=new System.Random(unchecked(land.Seed^land.DistrictIndex*73856093^240491));
            var home=habitat.PatrolPoints[random.Next(habitat.PatrolPoints.Count)];
            shark=Instantiate(prefab,transform).transform;shark.name="Coastal great white";shark.position=new Vector3(home.x,habitat.Sea-.65f,home.y);shark.rotation=Quaternion.Euler(0,R()*360,0);
            animator=shark.GetComponentInChildren<Animator>();if(animator){animator.applyRootMotion=false;animator.speed=.85f;animator.Play(0,0,R());}
            renderers=shark.GetComponentsInChildren<Renderer>();rig=land.View.GetComponentInChildren<CityForgeCameraController>();
            PickPatrolTarget();rig.ZoomChanged+=ZoomChanged;ZoomChanged(rig.ZoomLevel);
        }
        void ZoomChanged(int level){visible=level<=3;foreach(var r in renderers)if(r)r.enabled=visible;if(animator)animator.enabled=visible;}
        void PickPatrolTarget()
        {
            var p=Position;target=p;
            for(int i=0;i<100;i++){var next=habitat.PatrolPoints[random.Next(habitat.PatrolPoints.Count)];float distance=Vector2.Distance(p,next);
                if(distance<18||distance>90||!habitat.SharkPath(p,next))continue;target=next;return;
            }
            foreach(var next in habitat.PatrolPoints){float distance=Vector2.Distance(p,next);if(distance>6&&distance<50&&habitat.SharkPath(p,next)){target=next;return;}}
        }
        void Update()
        {
            if(!shark||!land||habitat==null||land.View.Data==null)return;
            accumulator+=Mathf.Min(Time.deltaTime,.25f);float step=visible?.05f:.2f;if(accumulator<step)return;float dt=Mathf.Min(accumulator,.25f);accumulator=0;
            var p=Position;if(Vector2.Distance(p,target)<3)PickPatrolTarget();
            var direction=target-p;if(direction.sqrMagnitude<.01f)return;
            shark.rotation=Quaternion.RotateTowards(shark.rotation,Quaternion.LookRotation(new Vector3(direction.x,0,direction.y)),55*dt);
            var next=p+new Vector2(shark.forward.x,shark.forward.z)*1.6f*dt;
            if(!habitat.SharkPath(p,next)){PickPatrolTarget();return;}
            distanceTravelled+=Vector2.Distance(p,next);shark.position=new Vector3(next.x,habitat.Sea-.65f,next.y);
        }
        public void FindShark()
        {
            if(!shark||!rig)return;var state=rig.CaptureRegionState();state.focus=shark.position+Vector3.up*.65f;rig.RestoreRegionState(state);rig.SetZoomLevel(0);ZoomChanged(rig.ZoomLevel);
        }
        void OnDestroy(){if(rig)rig.ZoomChanged-=ZoomChanged;}
    }
}
