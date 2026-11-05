using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Seeded ambient wildlife, isolated to one district. No combat, farming or simulation.</summary>
    public sealed partial class DistrictWildlife:MonoBehaviour
    {
        [Serializable] public sealed class Animal
        {
            public WildlifeSpecies species;public Transform root;public Animator animator;
            public Vector2 home,target;public float wait;public bool moving;
            public WildlifeActivity activity;public float thirst;public Vector2 drinkOrigin,waterFacing;
            public bool canDrink;
            internal Vector2? lastDryPosition;
        }
        [SerializeField] List<Animal> animals=new();
        public IReadOnlyList<Animal> Animals=>animals;
        public MeadowDistrictLandscape Land {get;private set;}
        readonly Dictionary<Vector2Int,List<Vector2>> trunks=new();
        System.Random random;
        CityForge.CameraSystem.CityForgeCameraController camera;
        Renderer[] renderers;bool shown;
        public bool Visible=>shown;
        public void UpdateVisibility(){
            bool visible=camera&&camera.ZoomLevel<=2;
            if(renderers==null||visible==shown)return;shown=visible;
            foreach(var renderer in renderers)if(renderer)renderer.enabled=visible;
            foreach(var animal in animals)if(animal.animator){animal.animator.enabled=visible;if(visible&&animal.activity==WildlifeActivity.Drinking)Animate(animal,false,true);}
        }
        public void FindAnimal(){
            if(!camera||animals.Count==0)return;
            Animal best=null;float score=float.MaxValue;
            foreach(var a in animals){if(a.species.habitat==WildlifeHabitat.Freshwater||a.species.habitat==WildlifeHabitat.River)continue;
                float value=Land.Canopy(new Vector2(a.root.position.x,a.root.position.z))+(a.species.id=="deer"?0:2);
                if(value<score){score=value;best=a;}
            }
            best??=animals[0];var state=camera.CaptureRegionState();state.focus=best.root.position+Vector3.up*.6f;camera.RestoreRegionState(state);camera.SetZoomLevel(1);UpdateVisibility();
        }
        public void FindRiverLife(){
            if(!camera)return;Animal best=null;float score=float.MaxValue;
            foreach(var a in animals){if(a.species.habitat!=WildlifeHabitat.Freshwater&&a.species.habitat!=WildlifeHabitat.River&&a.species.habitat!=WildlifeHabitat.RiverBankShallows)continue;
                var p=new Vector2(a.root.position.x,a.root.position.z);float distance=Mathf.Abs(Land.ShoreDistance(p));if(distance<score){score=distance;best=a;}}
            if(best==null)return;var state=camera.CaptureRegionState();state.focus=best.root.position;camera.RestoreRegionState(state);camera.SetZoomLevel(1);UpdateVisibility();
        }
        float R()=>(float)random.NextDouble();
        public void Initialize(MeadowDistrictLandscape land)
        {
            Land=land;WaterAccess=new WildlifeWaterAccess(land);waterRevision=land.View.WoodlandRevision;random=new System.Random(unchecked(land.Seed^land.DistrictIndex*73856093^15485863));
            var library=Resources.Load<WildlifeLibrary>("Wildlife/TemperateWildlife");
            if(!library){Debug.LogWarning("Animal Pack Deluxe wildlife library is missing. Build local wildlife assets before enabling animals.");return;}
            IndexTrees();
            int count=Mathf.Clamp(Mathf.RoundToInt(land.Side*land.Side/12000),12,96);
            for(int i=0;i<count;i++)Spawn(library,false,false);
            int fish=Mathf.Clamp(Mathf.RoundToInt(land.Side*land.Side/15000),24,80);
            for(int i=0;i<fish;i++)Spawn(library,true,false);
            for(int i=0;i<24;i++)Spawn(library,false,false,true);
            int birds=Mathf.Clamp(Mathf.RoundToInt(land.Side*land.Side/5000),24,220);
            for(int i=0;i<birds;i++)Spawn(library,false,true);
            camera=land.View.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();
            renderers=GetComponentsInChildren<Renderer>();shown=true;UpdateVisibility();
        }
        void IndexTrees(){
            trunks.Clear();
            foreach(var tree in Land.CanonicalTreeInstances){if(tree.prototypeIndex<3)continue;var p=Land.Origin+new Vector2(tree.position.x,tree.position.z)*Land.Side;
                var cell=Vector2Int.FloorToInt(p/4);if(!trunks.TryGetValue(cell,out var list))trunks[cell]=list=new List<Vector2>();list.Add(p);}
        }
        void OnEnable(){if(Land!=null)IndexTrees();}
        void Spawn(WildlifeLibrary library,bool aquatic,bool birds,bool crabs=false)
        {
            var candidates=new List<WildlifeSpecies>();int weight=0;
            foreach(var s in library.species)if(s.prefab&&s.weight>0&&crabs==(s.habitat==WildlifeHabitat.RiverBankShallows)&&birds==(s.habitat==WildlifeHabitat.WoodlandBird||s.habitat==WildlifeHabitat.FlyingBird)&&aquatic==(s.habitat==WildlifeHabitat.Freshwater||s.habitat==WildlifeHabitat.River)){
                candidates.Add(s);weight+=s.weight;}
            if(weight==0)return;
            int pick=random.Next(weight);var species=candidates[0];foreach(var candidate in candidates){pick-=candidate.weight;if(pick<0){species=candidate;break;}}
            if(species.id=="wolf"||species.id=="bear")foreach(var a in animals)if(a.species.id==species.id)return;
            for(int attempt=0;attempt<(crabs?800:160);attempt++){
                var p=Land.Origin+new Vector2(.03f+R()*.94f,.03f+R()*.94f)*Land.Side;
                if(!Suitable(species,p,out float y))continue;
                bool occupied=false;foreach(var a in animals)if((a.home-p).sqrMagnitude<(aquatic||crabs?4:36)){occupied=true;break;}if(occupied)continue;
                var go=Instantiate(species.prefab,new Vector3(p.x,y,p.y),Quaternion.Euler(0,R()*360,0),transform);
                go.name=species.id;var animator=go.GetComponentInChildren<Animator>();
                if(animator){animator.applyRootMotion=false;animator.keepAnimatorStateOnDisable=true;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;animator.Play("Idle",0,R());}
                animals.Add(new Animal{species=species,root=go.transform,animator=animator,home=p,target=p,wait=species.habitat==WildlifeHabitat.FlyingBird?0:2+R()*8,
                    thirst=Mathf.Lerp(12,45,Mathf.Repeat(p.x*.123f+p.y*.217f,1)),canDrink=species.id=="deer"&&animator&&animator.HasState(0,Animator.StringToHash("Base Layer.Drink"))});return;
            }
        }
        public bool Suitable(WildlifeSpecies species,Vector2 p,out float y,bool waterVisit=false)
        {
            y=0;if(!Land.Contains(p)||p.x<Land.Origin.x+2||p.y<Land.Origin.y+2||p.x>Land.Origin.x+Land.Side-2||p.y>Land.Origin.y+Land.Side-2)return false;
            float ground=Land.Height(p);var uv=p/Land.View.MapSide+Vector2.one*.5f;
            bool aquatic=species.habitat==WildlifeHabitat.Freshwater||species.habitat==WildlifeHabitat.River;
            if(species.habitat==WildlifeHabitat.RiverBankShallows){
                bool riverNearby=false;
                foreach(var body in Land.View.Data.WaterBodies){if(body.kind!=WaterBodyKind.River)continue;
                    var bounds=Land.View.CreationSettings?.version>=5?LakeWaterRenderer.GeneratedRiverBounds(body):body.Bounds;
                    bounds.min-=Vector2.one*(6/Land.View.MapSide);bounds.max+=Vector2.one*(6/Land.View.MapSide);
                    if(!bounds.Contains(uv))continue;
                    float level=WaterAccess.Level(body,p);
                    if(ground>=level-.65f&&ground<=level+.9f){riverNearby=true;break;}
                }
                if(!riverNearby||Mathf.Abs(Land.ShoreDistance(p))>6||Land.MountainSlope(p)>30||Land.RoadDistance(p)<Land.Baseline.roadWidth+2)return false;
                y=ground+.015f;return true;
            }
            if(species.habitat==WildlifeHabitat.FlyingBird){
                if(Land.Woodland(p)<.15f)return false;
                y=ground+32;return true;
            }
            if(aquatic){
                if (!WaterAccess.Wet(p, ground, out var freshwater, out float water) ||
                    freshwater.kind == WaterBodyKind.Ocean ||
                    (species.habitat == WildlifeHabitat.River && freshwater.kind != WaterBodyKind.River)) return false;
                float depth=water-ground;if(depth<.65f)return false;
                y=water-.22f;return true;
            }
            if (ground <= Land.View.SeaLevel + .12f || (!waterVisit && Land.ShoreDistance(p) < 2) ||
                Land.RoadDistance(p) < Land.Baseline.roadWidth + 4 || Land.MountainSlope(p) > 23 ||
                !WaterAccess.DryFootprint(p, ground)) return false;
            float woodland=Land.Woodland(p);
            if(!waterVisit&&species.habitat==WildlifeHabitat.WoodlandBird&&(woodland<.1f||Land.Canopy(p)>.35f))return false;
            if(!waterVisit&&(species.habitat==WildlifeHabitat.Woodland&&woodland<.4f||species.habitat==WildlifeHabitat.WoodlandEdge&&(woodland<.15f||woodland>.8f)))return false;
            if(species.habitat==WildlifeHabitat.FreshwaterBank){
                if(Land.ShoreDistance(p)>10)return false;
                bool nearby=false;foreach(var body in Land.View.Data.WaterBodies){if(body.kind!=WaterBodyKind.River&&body.kind!=WaterBodyKind.Lake&&body.kind!=WaterBodyKind.Pond)continue;
                    var bounds=body.kind==WaterBodyKind.River&&Land.View.CreationSettings?.version>=5?LakeWaterRenderer.GeneratedRiverBounds(body):body.Bounds;bounds.min-=Vector2.one*(12/Land.View.MapSide);bounds.max+=Vector2.one*(12/Land.View.MapSide);if(bounds.Contains(uv)){nearby=true;break;}}
                if(!nearby)return false;
            }
            var cell=Vector2Int.FloorToInt(p/4);
            for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)if(trunks.TryGetValue(cell+new Vector2Int(x,z),out var list))foreach(var t in list)if((p-t).sqrMagnitude<4)return false;
            y=ground;return true;
        }
        void Update()
        {
            if(!Land)return;
            UpdateVisibility();if(!shown)return;
            Tick(Mathf.Min(Time.deltaTime,.1f));
        }
    }
}
