using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.CameraSystem;
namespace CityForge.World
{
    /// <summary>Small, terrain-seated camp hearth. Shared private river stones are scaled per instance.</summary>
    public sealed class NativeCampfire:MonoBehaviour
    {
        readonly List<UnityEngine.Object> owned=new();
        readonly List<ParticleSystem> effects=new();
        CityForgeCameraController rig;Light glow;int seed;bool visible;
        public Bounds Exclusion {get;private set;}
        public int StoneCount {get;private set;}
        public bool EffectsVisible=>visible;
        T Own<T>(T value) where T:UnityEngine.Object{owned.Add(value);return value;}
        public static Vector2 Location(NativeCampRecord camp)=>camp.center+new Vector2(Mathf.Sin(camp.heading*Mathf.Deg2Rad),Mathf.Cos(camp.heading*Mathf.Deg2Rad))*4.3f;
        public void Initialize(MeadowDistrictLandscape land,NativeCampRecord camp)
        {
            seed=camp.seed;var p=Location(camp);transform.position=new Vector3(p.x,land.Height(p)+.025f,p.y);
            var assets=Resources.Load<RiverBedRockAssets>("RiverBeds/RichRockAssets");
            if(!assets||assets.meshes.Length<3)throw new InvalidOperationException("Missing prepared river stones for camp hearth");
            var random=new System.Random(seed);float R()=>(float)random.NextDouble();
            for(int i=0;i<12;i++){
                int type=i%3;var mesh=assets.meshes[type];var b=mesh.bounds;float a=i*Mathf.PI/6;
                var q=p+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*.94f;
                var go=new GameObject("Hearth stone "+(i+1),typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=assets.materials[type];
                float width=Mathf.Lerp(.43f,.56f,R());var scale=new Vector3(width/b.size.x,Mathf.Lerp(.23f,.30f,R())/b.size.y,width/b.size.z);
                var rotation=Quaternion.Euler(0,R()*360,0);go.transform.localScale=scale;go.transform.rotation=rotation;
                go.transform.position=new Vector3(q.x,land.Height(q)-.035f,q.y)-rotation*Vector3.Scale(new Vector3(b.center.x,b.min.y,b.center.z),scale);StoneCount++;
            }
            var wood=Own(new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Charred camp logs"});wood.SetColor("_BaseColor",new Color(.12f,.065f,.028f));wood.SetFloat("_Smoothness",.03f);
            for(int i=0;i<5;i++){
                var log=GameObject.CreatePrimitive(PrimitiveType.Cylinder);log.name="Charred log "+(i+1);log.transform.SetParent(transform,false);Destroy(log.GetComponent<Collider>());
                log.transform.localPosition=new Vector3(0,.15f+(i%2)*.13f,0);log.transform.localRotation=Quaternion.Euler(86,i*72,0);log.transform.localScale=new Vector3(.17f,.69f,.17f);log.GetComponent<Renderer>().sharedMaterial=wood;
            }
            var flame=Own(new Material(Resources.Load<Shader>("Campfire/Flame")){name="Camp flame"});
            var smoke=Own(new Material(Resources.Load<Shader>("Weather/Precipitation")){name="Camp soft smoke"});
            var fire=Emitter("Flames",flame,28,.6f,.43f,new Color(1,.42f,.035f,.85f),new Color(.8f,.05f,.005f,0),.7f,.36f);
            var main=fire.main;main.startSize3D=true;main.startSizeX=new ParticleSystem.MinMaxCurve(.28f,.5f);main.startSizeY=new ParticleSystem.MinMaxCurve(.65f,1.05f);main.startSizeZ=.4f;
            Emitter("Embers",flame,4,1.3f,.045f,new Color(1,.45f,.05f,1),new Color(1,.12f,.01f,0),1,.45f);
            var plume=Emitter("Light smoke",smoke,7,7f,1.05f,new Color(.62f,.60f,.56f,.42f),new Color(.70f,.69f,.66f,0),.85f,.20f);
            var drift=plume.velocityOverLifetime;drift.x=new ParticleSystem.MinMaxCurve(.12f,.28f);drift.z=new ParticleSystem.MinMaxCurve(-.06f,.12f);
            var turbulence=plume.noise;turbulence.enabled=true;turbulence.strength=.18f;turbulence.frequency=.45f;turbulence.scrollSpeed=.18f;turbulence.quality=ParticleSystemNoiseQuality.Low;
            glow=new GameObject("Warm hearth light",typeof(Light)).GetComponent<Light>();glow.transform.SetParent(transform,false);glow.transform.localPosition=Vector3.up*.8f;glow.type=LightType.Point;glow.color=new Color(1,.40f,.10f);glow.range=5;glow.shadows=LightShadows.None;
            // Include the entire ring plus personal space and the grid's half-cell rounding margin.
            Exclusion=new Bounds(transform.position+Vector3.up, new Vector3(3.5f,6,3.5f));
            rig=land.View.GetComponentInChildren<CityForgeCameraController>();rig.ZoomChanged+=ZoomChanged;ZoomChanged(rig.ZoomLevel);
        }
        ParticleSystem Emitter(string label,Material material,float rate,float life,float size,Color start,Color end,float rise,float spread)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*.26f;
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=unchecked((uint)(seed+effects.Count*131+1));
            var main=ps.main;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.maxParticles=96;main.startLifetime=new ParticleSystem.MinMaxCurve(life*.7f,life);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(size*.7f,size);main.startColor=Color.white;main.cullingMode=ParticleSystemCullingMode.Pause;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=spread;shape.rotation=new Vector3(90,0,0);
            var emission=ps.emission;emission.rateOverTime=rate;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;velocity.x=new ParticleSystem.MinMaxCurve(-.08f,.12f);velocity.y=new ParticleSystem.MinMaxCurve(rise*.7f,rise);velocity.z=new ParticleSystem.MinMaxCurve(-.08f,.08f);
            var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(start,0),new GradientColorKey(end,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(start.a,.15f),new GradientAlphaKey(end.a,1)});color.color=gradient;
            var sizeLife=ps.sizeOverLifetime;sizeLife.enabled=true;sizeLife.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,label=="Light smoke"?.4f:1,1,label=="Light smoke"?3.2f:.15f));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.renderMode=ParticleSystemRenderMode.Billboard;
            effects.Add(ps);return ps;
        }
        void ZoomChanged(int level){visible=level<=2;foreach(var ps in effects){if(visible)ps.Play();else ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}if(glow)glow.enabled=visible;}
        void Update(){if(visible&&glow)glow.intensity=1.4f+Mathf.PerlinNoise(Time.time*7,seed%997)*.65f;}
        void OnDestroy(){if(rig)rig.ZoomChanged-=ZoomChanged;foreach(var asset in owned)if(asset)Destroy(asset);}
    }
}
