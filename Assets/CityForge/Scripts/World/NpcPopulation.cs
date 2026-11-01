using System;
using System.Collections.Generic;
using UnityEngine;
using CityForge.CameraSystem;
namespace CityForge.World
{
    /// <summary>Shared bounded ambient population for the opt-in study and saved camp locations. Individual activity state is transient.</summary>
    public sealed class NpcPopulation:MonoBehaviour
    {
        sealed class Meeting {public NpcBrain a,b;public Vector3 aSpot,bSpot;public float elapsed,talking,duration;public bool started;}
        readonly List<NpcBrain> people=new();readonly List<Meeting> meetings=new();

        CityForgeCameraController cameraRig;float accumulator;bool diagnostic;
        public string RecordPrefix {get;private set;}="npc-study-";
        public int BehaviorSeed {get;private set;}=7919;
        public NpcLocalNavigation Navigation {get;private set;}
        public IReadOnlyList<NpcBrain> People=>people;
        public Vector3 Center {get;private set;}
        public int CompletedMeetings {get;private set;}
        public int FailedMeetings {get;private set;}
        public bool PresentationVisible {get;private set;}
        public void Initialize(MeadowDistrictLandscape land,NpcDefinition definition)
        {
            if(people.Count>0)throw new InvalidOperationException("Study already initialized");
            if(!definition||!definition.prefab)throw new InvalidOperationException("Prepare the local NPC definition first");
            var obstacles=new List<Bounds>();
            foreach(var renderer in land.GetComponentsInChildren<MeshRenderer>()){
                var b=renderer.bounds;if(b.size.y<.3f||b.size.x>10||b.size.z>10)continue;b.Expand(new Vector3(definition.radius*2,0,definition.radius*2));obstacles.Add(b);
            }
            Vector2 center=new Vector2(StreamRockTestRegion.Center(170)+23,170);
            InitializeAt(land,new[]{definition,definition,definition,definition,definition,definition,definition,definition,definition,definition},center,obstacles,33,float.PositiveInfinity,83731);
        }
        public void InitializeCamp(MeadowDistrictLandscape land,NativeCampRecord camp,NativeCampAssets assets,IReadOnlyList<Bounds> obstacles){
            RecordPrefix="camp-"+camp.district+"-";BehaviorSeed=camp.seed;
            var definitions=new NpcDefinition[camp.teepees+3];definitions[0]=assets.chief;
            int warriors=(definitions.Length-1)/2;
            for(int i=1;i<definitions.Length;i++)definitions[i]=i<=warriors?assets.warrior:assets.resident;
            InitializeAt(land,definitions,camp.center,obstacles,65,camp.Clearance,camp.seed);
        }
        void InitializeAt(MeadowDistrictLandscape land,NpcDefinition[] definitions,Vector2 center,IReadOnlyList<Bounds> obstacles,int gridSide,float maximumDistance,int seed){
            var definition=definitions[0];
            Navigation=new NpcLocalNavigation(land,center,definition.radius,obstacles,gridSide,maximumDistance);
            if(Navigation.OpenCount<100)throw new InvalidOperationException("Insufficient walkable study cells");
            Center=Navigation.Ground(new Vector3(center.x,0,center.y));
            if(definition.profiles==null||definition.profiles.Length==0||Array.Exists(definition.profiles,p=>!p))throw new InvalidOperationException("Assign at least one behavior profile to the character definition");
            var random=new System.Random(seed);
            for(int i=0;i<definitions.Length;i++){
                definition=definitions[i];
                Vector3 p=default;bool found=false;
                for(int attempt=0;attempt<200;attempt++){p=Navigation.Sample(random);found=true;foreach(var other in people)if(Vector3.Distance(other.Record.position,p)<1.5f){found=false;break;}if(found)break;}
                if(!found)throw new InvalidOperationException("Study spawn capacity exhausted");
                people.Add(new NpcBrain(this,definition,definition.profiles[i%definition.profiles.Length],i,p));
            }
            cameraRig=land.View.GetComponentInChildren<CityForgeCameraController>();cameraRig.ZoomChanged+=ZoomChanged;ZoomChanged(cameraRig.ZoomLevel);
            // First pair demonstrates the same reservation/path workflow used by autonomous selections.
            TryMeet(people[0]);
        }
        public bool CanOccupy(NpcBrain person,Vector3 position){
            if(!Navigation.Suitable(position))return false;
            foreach(var other in people)if(other!=person&&(other.Record.position-position).sqrMagnitude<Mathf.Pow(person.Definition.radius+other.Definition.radius,2))return false;
            return true;
        }
        public bool TryMeet(NpcBrain a){
            if(a.Reserved)return false;
            foreach(var b in people){
                if(a==b||b.Reserved||b.ActionId!="Idle"||Vector3.Distance(a.Record.position,b.Record.position)>22)continue;
                for(int attempt=0;attempt<16;attempt++){
                    var spot=Navigation.Sample(a.Random);var second=Navigation.Ground(spot+Vector3.right*2);
                    if(!Navigation.Suitable(second)||!CanOccupy(a,spot)||!CanOccupy(b,second))continue;
                    bool reserved=false;
                    foreach(var meeting in meetings)if(Vector3.Distance(spot,meeting.aSpot)<2||Vector3.Distance(spot,meeting.bSpot)<2||Vector3.Distance(second,meeting.aSpot)<2||Vector3.Distance(second,meeting.bSpot)<2){reserved=true;break;}
                    if(reserved)continue;
                    var ap=Navigation.FindPath(a.Record.position,spot);var bp=Navigation.FindPath(b.Record.position,second);
                    if(ap==null||bp==null||ap.Count==0||bp.Count==0)continue;
                    a.Partner=b;b.Partner=a;a.Set(new NpcWalkAction(ap));b.Set(new NpcWalkAction(bp));
                    meetings.Add(new Meeting{a=a,b=b,aSpot=spot,bSpot=second,duration=a.Profile.Duration(a.Profile.talkSeconds,a.Random)});return true;
                }
            }
            return false;
        }
        public void ReleaseMeeting(NpcBrain person,bool completed){
            for(int i=meetings.Count-1;i>=0;i--){var m=meetings[i];if(m.a!=person&&m.b!=person)continue;
                m.a.Partner=null;m.b.Partner=null;
                if(completed){CompletedMeetings++;m.a.Record.completedTalks++;m.b.Record.completedTalks++;}else FailedMeetings++;
                m.a.Set(new NpcIdleAction());m.b.Set(new NpcIdleAction());meetings.RemoveAt(i);
            }
        }
        void ZoomChanged(int level){PresentationVisible=level<=2;foreach(var p in people)p.Visual.gameObject.SetActive(PresentationVisible);accumulator=0;}
        void Update(){
            if(!PresentationVisible)return;accumulator+=Mathf.Min(Time.deltaTime,.1f);
            // One budgeted scheduler, 30 Hz close and 10 Hz at medium zoom. No per-person Update components.
            float step=cameraRig.ZoomLevel<2?1f/30: .1f;if(accumulator<step)return;float dt=accumulator;accumulator=0;
            foreach(var p in people)p.Tick(dt);
            for(int i=meetings.Count-1;i>=0;i--){var m=meetings[i];m.elapsed+=dt;
                if(m.elapsed>45){ReleaseMeeting(m.a,false);continue;}
                if(!m.started&&Vector3.Distance(m.a.Record.position,m.aSpot)<.2f&&Vector3.Distance(m.b.Record.position,m.bSpot)<.2f){m.started=true;m.a.Set(new NpcTalkAction());m.b.Set(new NpcTalkAction());}
                if(m.started){m.talking+=dt;if(m.talking>=m.duration)ReleaseMeeting(m.a,true);}
            }
            // Meeting completion/start may switch actions after individual movement ticks.
            foreach(var p in people)p.RefreshMovementAnimation();
        }
        public void SetDiagnostics(bool value)=>diagnostic=value;
        void OnGUI(){
            if(!diagnostic)return;
            GUI.Box(new Rect(14,150,325,300),"NPC STUDY — transient population");
            GUI.Label(new Rect(25,175,300,25),$"People {people.Count} | Conversations {CompletedMeetings}");
            for(int i=0;i<people.Count;i++){var p=people[i];GUI.Label(new Rect(25,204+i*21,300,23),$"{p.Record.id}: {p.ActionId}"+(p.Reserved?" (paired)":"")+(p.Record.lastFailure.Length>0?" !":""));}
        }
        void OnDestroy(){
            if(cameraRig)cameraRig.ZoomChanged-=ZoomChanged;
            foreach(var p in people)if(p.Visual){p.Visual.gameObject.SetActive(false);Destroy(p.Visual.gameObject);}
            meetings.Clear();people.Clear();
        }
    }
}
