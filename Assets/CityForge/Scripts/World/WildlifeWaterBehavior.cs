using System;
using UnityEngine;
namespace CityForge.World
{
    public enum WildlifeActivity { Idle,Wandering,ApproachingWater,Drinking,ReturningHome }

    /// <summary>Uses the same saved footprints and surface grades as rendered water.</summary>
    public sealed class WildlifeWaterAccess
    {
        static readonly Vector2[] FootprintDirections = {
            Vector2.right, new(.7071068f,.7071068f), Vector2.up, new(-.7071068f,.7071068f),
            Vector2.left, new(-.7071068f,-.7071068f), Vector2.down, new(.7071068f,-.7071068f)
        };
        readonly MeadowDistrictLandscape land;
        RegionWaterBody[] indexedBodies;
        Rect[] bodyBounds = Array.Empty<Rect>();
        bool groundFollowingRivers;

        void RefreshBounds()
        {
            var bodies = land.View.Data.WaterBodies ?? Array.Empty<RegionWaterBody>();
            if (indexedBodies == bodies) return;
            indexedBodies = bodies;
            bodyBounds = Array.ConvertAll(bodies, body => body.Bounds);
            groundFollowingRivers = RegionalRiverNetwork.HasGroundFollowingWater(land.View.Data);
        }
        public WildlifeWaterAccess(MeadowDistrictLandscape source){land=source;}
        public float Level(RegionWaterBody body,Vector2 p)
        {
            if(body.playerChannel)return PlayerChannelGeometry.Level(body,p,land.View.MapSide);
            RefreshBounds();
            if(body.kind==WaterBodyKind.River&&groundFollowingRivers){
                var uv=p/land.View.MapSide+Vector2.one*.5f;return RegionalRiverNetwork.GroundWaterLevel(land.View.Data,uv.x,uv.y);
            }
            return body.hasSurfaceLevel?body.surfaceLevelNormalized*land.DisplayTerrain.terrainData.size.y:land.View.SeaLevel;
        }
        public bool Wet(Vector2 p,out RegionWaterBody water,out float level)
        {
            return Wet(p, land.Height(p), out water, out level);
        }

        internal bool Wet(Vector2 p, float ground, out RegionWaterBody water, out float level, float surfaceTolerance = .025f)
        {
            RefreshBounds();
            var uv = p / land.View.MapSide + Vector2.one * .5f;
            for (int i = 0; i < indexedBodies.Length; i++)
            {
                if (!bodyBounds[i].Contains(uv)) continue;
                var body = indexedBodies[i];
                bool graded = body.playerChannel || (body.kind == WaterBodyKind.River && groundFollowingRivers);
                // Reject land above fixed water levels before walking the polygon.
                if (graded && !body.Contains(uv)) continue;
                float surface = Level(body, p);
                if (ground > surface + surfaceTolerance || (!graded && !body.Contains(uv))) continue;
                water = body;
                level = surface;
                return true;
            }
            water = null;
            level = 0;
            return false;
        }

        public bool Dry(Vector2 p,float clearance=.5f)
            => land.Contains(p) && DryFootprint(p, land.Height(p), clearance);

        internal bool DryFootprint(Vector2 p, float ground, float clearance = .5f)
        {
            if (Wet(p, ground, out _, out _)) return false;
            foreach (var direction in FootprintDirections)
            {
                var sample = p + direction * clearance;
                if (!land.Contains(sample) || Wet(sample, out _, out _)) return false;
            }
            return true;
        }
        public static bool Terrestrial(WildlifeSpecies species)=>species.habitat!=WildlifeHabitat.Freshwater&&species.habitat!=WildlifeHabitat.River&&species.habitat!=WildlifeHabitat.RiverBankShallows&&species.habitat!=WildlifeHabitat.FlyingBird;
    }

    public sealed partial class DistrictWildlife
    {
        public WildlifeWaterAccess WaterAccess {get;private set;}
        int waterRevision;
        RegionWaterBody[] lastWaterBodies;
        public bool CanTravel(WildlifeSpecies species,Vector2 from,Vector2 to,bool waterVisit=false)
            => CanTravel(species, from, to, out _, waterVisit);

        bool CanTravel(WildlifeSpecies species, Vector2 from, Vector2 to, out float height, bool waterVisit)
        {
            height = 0;
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / .4f));
            for (int i = 1; i < samples; i++)
                if (!Suitable(species, Vector2.Lerp(from, to, i / (float)samples), out _, waterVisit)) return false;
            return Suitable(species, to, out height, waterVisit);
        }
        public bool TryFindDrinkSpot(Animal animal,out Vector2 feet,out Vector2 facing)
        {
            feet=facing=default;if(!animal.canDrink||!animal.root)return false;
            var start=new Vector2(animal.root.position.x,animal.root.position.z);float best=65;
            float phase=Mathf.Repeat(start.x*.37f+start.y*.19f,Mathf.PI*2);bool found=false;
            for(int ray=0;ray<32;ray++){
                float a=phase+ray*Mathf.PI*2/32;var direction=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                for(float distance=1;distance<=Mathf.Min(64,best+1);distance+=2){
                    var p=start+direction*distance;if(!Land.Contains(p))break;
                    if(!WaterAccess.Wet(p,out var body,out _))continue;
                    if(body.kind==WaterBodyKind.Ocean)break;
                    float lo=Mathf.Max(0,distance-2),hi=distance;
                    for(int i=0;i<8;i++){float mid=(lo+hi)*.5f;if(WaterAccess.Wet(start+direction*mid,out _,out _))hi=mid;else lo=mid;}
                    var edge=start+direction*hi;var seat=edge-direction*.58f;
                    if(hi<best&&Suitable(animal.species,seat,out float ground,true)&&Mathf.Abs(ground-WaterAccess.Level(body,edge))<.8f&&CanTravel(animal.species,start,seat,true)){
                        feet=seat;facing=edge;best=hi;found=true;
                    }
                    break;
                }
            }
            return found;
        }
        void Animate(Animal animal,bool moving,bool drinking=false)
        {
            bool changed=animal.moving!=moving;animal.moving=moving;
            if(!animal.animator)return;
            animal.animator.SetBool("Moving",moving);
            if(animal.canDrink)animal.animator.SetBool("Drinking",drinking);
            if(drinking)animal.animator.CrossFadeInFixedTime("Drink",.25f,0);
            else if(changed||animal.activity==WildlifeActivity.Drinking)animal.animator.CrossFadeInFixedTime(moving?"Walk":"Idle",.12f,0);
        }
        void Rest(Animal animal,float duration)
        {
            Animate(animal,false);animal.activity=WildlifeActivity.Idle;animal.wait=duration;
        }
        bool RecoverDryGround(Animal animal)
        {
            var p = new Vector2(animal.root.position.x, animal.root.position.z);
            if (WaterAccess.Dry(p))
            {
                animal.root.gameObject.SetActive(true);
                return true;
            }
            // A player may create water beneath an existing animal. Relocate to the
            // nearest usable dry bank instead of letting it walk out through water.
            for(float radius=2;radius<=128;radius*=1.6f)for(int i=0;i<32;i++){
                float angle=i*Mathf.PI*2/32;var q=p+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                if(!Suitable(animal.species,q,out float y,true))continue;
                animal.root.position=new Vector3(q.x,y,q.y);animal.home=q;Rest(animal,3);animal.thirst=15;animal.root.gameObject.SetActive(true);return true;
            }
            Rest(animal,3);animal.root.gameObject.SetActive(false);return false;
        }
        public void Tick(float dt)
        {
            if(!Land||dt<=0||!float.IsFinite(dt))return;dt=Mathf.Min(dt,.1f);
            if(random==null)random=new System.Random(unchecked(Land.Seed^Land.DistrictIndex*73856093^15485863));
            bool forestChanged = waterRevision != Land.View.WoodlandRevision;
            bool changed = forestChanged || lastWaterBodies != Land.View.Data.WaterBodies;
            lastWaterBodies = Land.View.Data.WaterBodies;
            if (forestChanged)
            {
                waterRevision = Land.View.WoodlandRevision;
                IndexTrees();
            }
            bool searched=false;
            foreach(var animal in animals){
                if(!animal.root)continue;
                if(!animal.root.gameObject.activeSelf&&!changed){animal.wait-=dt;if(animal.wait>0)continue;}
                var position = animal.root.position;
                var p = new Vector2(position.x, position.z);
                bool terrestrial = WildlifeWaterAccess.Terrestrial(animal.species);
                // Successful movement already checked this footprint. Recheck when
                // terrain changes, an animal is moved externally, or it is recovering.
                if (terrestrial && (changed || !animal.root.gameObject.activeSelf ||
                    !animal.lastDryPosition.HasValue || !animal.lastDryPosition.Value.Equals(p)))
                {
                    if (!RecoverDryGround(animal)) continue;
                    position = animal.root.position;
                    p = new Vector2(position.x, position.z);
                    animal.lastDryPosition = p;
                }
                if(animal.canDrink)animal.thirst-=dt;
                if(animal.activity==WildlifeActivity.Drinking){
                    if(!WaterAccess.Wet(animal.waterFacing,out var drinkingWater,out _)||drinkingWater.kind==WaterBodyKind.Ocean){Rest(animal,3);animal.thirst=20;continue;}
                    animal.wait-=dt;
                    if(animal.wait>0)continue;
                    animal.target=animal.drinkOrigin;Animate(animal,true);animal.activity=WildlifeActivity.ReturningHome;
                }
                if(!animal.moving&&animal.activity!=WildlifeActivity.ApproachingWater){
                    if(animal.canDrink&&animal.thirst<=0&&!searched){
                        searched=true;
                        if(TryFindDrinkSpot(animal,out var feet,out var face)){animal.drinkOrigin=p;animal.waterFacing=face;animal.target=feet;animal.activity=WildlifeActivity.ApproachingWater;Animate(animal,true);}
                        else animal.thirst=20+R()*25;
                    }
                    if(!animal.moving){
                        animal.wait-=dt;if(animal.wait>0||animal.species.speed<=0)continue;
                        bool flying=animal.species.habitat==WildlifeHabitat.FlyingBird;
                        var destination=animal.home+new Vector2(R()-.5f,R()-.5f)*(flying?100:18);
                        if(!CanTravel(animal.species,p,destination)){animal.wait=2+R()*4;continue;}
                        animal.target=destination;animal.activity=WildlifeActivity.Wandering;Animate(animal,true);
                    }
                }
                var delta=animal.target-p;bool visiting=animal.activity==WildlifeActivity.ApproachingWater||animal.activity==WildlifeActivity.ReturningHome;
                if(delta.sqrMagnitude<(visiting?.0025f:.04f)){
                    if(animal.activity==WildlifeActivity.ApproachingWater){
                        Animate(animal,false);var direction=animal.waterFacing-p;
                        var rotation=Quaternion.LookRotation(new Vector3(direction.x,0,direction.y));animal.root.rotation=Quaternion.RotateTowards(animal.root.rotation,rotation,100*dt);
                        if(Quaternion.Angle(animal.root.rotation,rotation)>5)continue;
                        animal.activity=WildlifeActivity.Drinking;animal.wait=6+R()*4;animal.thirst=65+R()*70;Animate(animal,false,true);
                    }else Rest(animal,animal.species.habitat==WildlifeHabitat.FlyingBird?0:3+R()*9);
                    continue;
                }
                var next=Vector2.MoveTowards(p,animal.target,animal.species.speed*dt);
                if(!CanTravel(animal.species,p,next,out float height,visiting)){
                    if(visiting)animal.thirst=15+R()*15;Rest(animal,3+R()*6);continue;
                }
                animal.root.position=new Vector3(next.x,height,next.y);
                if (terrestrial) animal.lastDryPosition = next;
                animal.root.rotation=Quaternion.RotateTowards(animal.root.rotation,Quaternion.LookRotation(new Vector3(delta.x,0,delta.y)),100*dt);
            }
        }
    }
}
