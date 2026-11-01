using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    public interface INpcAction
    {
        string Id {get;}
        void Enter(NpcBrain npc);
        bool Tick(NpcBrain npc,float dt);
        void Exit(NpcBrain npc);
    }
    public sealed class NpcIdleAction:INpcAction
    {
        float remaining;
        public string Id=>"Idle";
        public void Enter(NpcBrain npc){remaining=npc.Profile.Duration(npc.Profile.idleSeconds,npc.Random);npc.Animate(npc.Definition.idleState);}
        public bool Tick(NpcBrain npc,float dt){remaining-=dt;return remaining<=0;}
        public void Exit(NpcBrain npc){}
    }
    public sealed class NpcWalkAction:INpcAction
    {
        readonly List<Vector3> path;int next;
        public string Id=>"Walk";
        public NpcWalkAction(List<Vector3> path){this.path=path;}
        // A route is an intention; only actual displacement should play locomotion.
        public void Enter(NpcBrain npc)=>npc.Animate(npc.Definition.idleState);
        public bool Tick(NpcBrain npc,float dt){
            var p=npc.Record.position;
            // Consume reached waypoints without inserting a stationary walking frame between segments.
            while(next<path.Count){var offset=path[next]-p;offset.y=0;if(offset.sqrMagnitude>=.015f)break;next++;}
            if(next>=path.Count){npc.Animate(npc.Definition.idleState);return true;}
            var target=path[next];var delta=target-p;delta.y=0;
            var position=npc.Owner.Navigation.Ground(Vector3.MoveTowards(p,target,npc.Definition.walkSpeed*dt));
            var displacement=position-p;displacement.y=0;
            if(displacement.sqrMagnitude>.00000001f&&npc.Owner.CanOccupy(npc,position)){
                npc.Animate(npc.Definition.walkState);
                npc.Record.position=position;npc.Record.heading=Mathf.MoveTowardsAngle(npc.Record.heading,Quaternion.LookRotation(delta).eulerAngles.y,240*dt);
            }else npc.Animate(npc.Definition.idleState);
            return false;
        }
        public void Exit(NpcBrain npc)=>npc.Animate(npc.Definition.idleState);
    }
    public sealed class NpcTalkAction:INpcAction
    {
        public string Id=>"Talk";
        public void Enter(NpcBrain npc)=>npc.Animate(npc.Definition.talkState);
        public bool Tick(NpcBrain npc,float dt){
            var partner=npc.Partner;if(partner==null)return true;
            var d=partner.Record.position-npc.Record.position;d.y=0;
            if(d.sqrMagnitude>.01f)npc.Record.heading=Mathf.MoveTowardsAngle(npc.Record.heading,Quaternion.LookRotation(d).eulerAngles.y,240*dt);
            return false; // Shared meeting owns the duration and releases both participants together.
        }
        public void Exit(NpcBrain npc){}
    }
    public sealed class NpcBrain
    {
        public readonly NpcRecord Record;
        public readonly NpcDefinition Definition;
        public readonly NpcBehaviorProfile Profile;
        public readonly System.Random Random;
        public readonly NpcPopulation Owner;
        public readonly Transform Visual;
        public readonly Animator Animator;
        public NpcBrain Partner {get;internal set;}
        public bool Reserved=>Partner!=null;
        public string ActionId=>action?.Id??"None";
        public bool IsMoving {get;private set;}
        INpcAction action;float elapsed;string animation;
        public NpcBrain(NpcPopulation owner,NpcDefinition definition,NpcBehaviorProfile profile,int index,Vector3 position){
            Owner=owner;Definition=definition;Profile=profile;Random=new System.Random(unchecked(owner.BehaviorSeed+index*431));
            Record=new NpcRecord{id=owner.RecordPrefix+index,position=position,heading=index*57};
            Visual=Object.Instantiate(definition.prefab,owner.transform).transform;Visual.name=Record.id;
            Animator=Visual.GetComponentInChildren<Animator>();Animator.applyRootMotion=false;Animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;Animator.keepAnimatorStateOnDisable=true;
            Set(new NpcIdleAction());Sync();
        }
        public void Animate(string state,bool immediate=false){
            // Check the Animator itself: hiding/showing a visual can invalidate a cached request.
            bool current=Animator.GetCurrentAnimatorStateInfo(0).IsName(state);
            bool transitioning=Animator.IsInTransition(0);
            if(immediate){
                animation=state;
                if(current&&!transitioning)return;
                Animator.Play(state,0,0);Animator.Update(0);return;
            }
            if(animation==state&&(current||transitioning&&Animator.GetNextAnimatorStateInfo(0).IsName(state)))return;
            animation=state;Animator.CrossFadeInFixedTime(state,.2f);
        }
        public void Set(INpcAction next){action?.Exit(this);action=next;elapsed=0;Record.activity=next.Id;next.Enter(this);}
        public bool WalkTo(Vector3 target){var path=Owner.Navigation.FindPath(Record.position,target);if(path==null||path.Count==0)return false;Set(new NpcWalkAction(path));return true;}
        public void Cancel(string reason){Record.lastFailure=reason;if(Reserved)Owner.ReleaseMeeting(this,false);else Set(new NpcIdleAction());}
        public void Tick(float dt){
            var before=Record.position;elapsed+=dt;
            if(elapsed>Profile.activityTimeout){Cancel("Activity timed out");UpdateMovementAnimation(before);Sync();return;}
            if(action.Tick(this,dt)){
                Record.completedActivities++;if(action.Id=="Walk")Record.completedWalks++;
                if(Reserved)Set(new NpcIdleAction());else Choose();
            }
            UpdateMovementAnimation(before);Sync();
        }
        void UpdateMovementAnimation(Vector3 before){
            var displacement=Record.position-before;displacement.y=0;IsMoving=displacement.sqrMagnitude>.00000001f;
            // Movement is authoritative, including the last step before an action switches to idle/talk.
            // Enter walk immediately; blending from an idle pose while translating causes visible skating.
            RefreshMovementAnimation();
        }
        internal void RefreshMovementAnimation()=>Animate(IsMoving?Definition.walkState:ActionId=="Talk"?Definition.talkState:Definition.idleState,true);
        void Choose(){
            float idle=Mathf.Max(0,Profile.idleWeight),wander=Mathf.Max(0,Profile.wanderWeight),talk=Mathf.Max(0,Profile.talkWeight);
            float total=idle+wander+talk;
            float draw=(float)Random.NextDouble()*total;
            if(draw<talk){
                if(Owner.TryMeet(this))return;
                // No eligible partner: reweight the remaining activities rather than gifting talk weight to wandering.
                draw=talk+(float)Random.NextDouble()*(idle+wander);
            }
            if(draw<talk+wander){for(int i=0;i<12;i++)if(WalkTo(Owner.Navigation.Sample(Random)))return;}
            Set(new NpcIdleAction());
        }
        void Sync(){Visual.SetPositionAndRotation(Record.position,Quaternion.Euler(0,Record.heading,0));}
    }
}
