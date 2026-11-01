#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CityForge.World;
using CityForge.UI;
using CityForge.CameraSystem;
namespace CityForge.Editor
{
    public static class NpcStudyChecks
    {
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(string output)=>EditorCoroutineRunner.Begin(Guard(output));
        static IEnumerator Guard(string output){
            Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");var check=Check(output);
            while(true){bool more;try{more=check.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+e);(check as IDisposable)?.Dispose();yield break;}if(!more)break;yield return check.Current;}
            File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS");
        }
        static IEnumerator CheckStationaryIdle(NpcPopulation population){
            var a=population.People[0];var b=population.People[1];
            var oldA=a.Record.position;var oldB=b.Record.position;var oldCull=a.Animator.cullingMode;
            bool wasEnabled=population.enabled;population.enabled=false;
            try{
                a.Cancel("Locomotion validation");b.Cancel("Locomotion validation");
                Vector3 start=default,target=default;bool found=false;var random=new System.Random(9281);
                for(int attempt=0;attempt<1000;attempt++){
                    start=population.Navigation.Sample(random);target=population.Navigation.Ground(start+Vector3.right);
                    if(!population.Navigation.Suitable(target)||Vector3.Distance(oldB,start)<2||Vector3.Distance(oldB,target)<2)continue;
                    found=population.People.Where(p=>p!=a&&p!=b).All(p=>Vector3.Distance(p.Record.position,start)>1.5f&&Vector3.Distance(p.Record.position,target)>1.5f);
                    if(found)break;
                }
                Require(found,"Clear neighboring cells for forced blocked-path regression");
                a.Record.position=start;a.Visual.position=start;b.Record.position=target;b.Visual.position=target;
                a.Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                a.Set(new NpcWalkAction(new System.Collections.Generic.List<Vector3>{target}));
                for(int tick=0;tick<20;tick++)a.Tick(.1f);
                var blocked=a.Record.position;
                for(int tick=0;tick<5;tick++)a.Tick(.1f);
                Require(a.Record.position==blocked&&a.ActionId=="Walk","Blocked route waits without displacement");
                float until=Time.time+.35f;while(Time.time<until)yield return null;
                Require(!a.Animator.IsInTransition(0)&&a.Animator.GetCurrentAnimatorStateInfo(0).IsName(a.Definition.idleState),"Blocked walker plays actual idle state");
                b.Record.position=oldB;b.Visual.position=oldB;
                until=Time.time+.25f;while(Time.time<until){a.Tick(Mathf.Min(Time.deltaTime,.05f));yield return null;}
                Require(Vector3.Distance(a.Record.position,blocked)>.05f&&a.Animator.GetCurrentAnimatorStateInfo(0).IsName(a.Definition.walkState),"Cleared route resumes movement and actual walk state");
                a.Set(new NpcIdleAction());var stopped=a.Record.position;
                until=Time.time+.35f;while(Time.time<until)yield return null;
                Require(a.Record.position==stopped&&!a.Animator.IsInTransition(0)&&a.Animator.GetCurrentAnimatorStateInfo(0).IsName(a.Definition.idleState),"Stopped actor returns to idle animation");
                var route=new NpcWalkAction(new System.Collections.Generic.List<Vector3>{a.Record.position});route.Enter(a);
                Require(route.Tick(a,.1f),"Reached destination completes immediately");route.Exit(a);
                until=Time.time+.3f;while(Time.time<until)yield return null;
                Require(a.Animator.GetCurrentAnimatorStateInfo(0).IsName(a.Definition.idleState),"Arrived walker stays idle");
            }finally{
                a.Record.position=oldA;a.Visual.position=oldA;b.Record.position=oldB;b.Visual.position=oldB;
                a.Set(new NpcIdleAction());b.Set(new NpcIdleAction());a.Animator.cullingMode=oldCull;population.enabled=wasEnabled;
            }
        }
        static IEnumerator Check(string output){
            Application.runInBackground=true;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!view||view.SavedRegionId!=StreamRockTestRegion.Id||view.SelectedDistrict!=3){
                if(view){view.ReturnToMenu();yield return null;yield return null;}
                string path=Path.Combine(SavedRegionStore.DirectoryPath,StreamRockTestRegion.Id+".cfregion");
                Require(File.Exists(path),"Existing accepted stream fixture required; never create or replace saves");
                var loaded=SavedRegionStore.Load(path);var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();Require(menu,"Main menu");menu.OpenSavedRegion(loaded.info);
                double deadline=EditorApplication.timeSinceStartup+240;
                while(menu.IsOpeningRegion&&EditorApplication.timeSinceStartup<deadline)yield return null;
                view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&!menu.IsOpeningRegion,"Saved overview load");view.RequestDistrict(3);
                deadline=EditorApplication.timeSinceStartup+240;while(view.DistrictLoading&&EditorApplication.timeSinceStartup<deadline)yield return null;
            }
            Require(view&&!view.DistrictLoading&&view.SelectedDistrict==3,"Normal saved district load");
            var existing=UnityEngine.Object.FindFirstObjectByType<NpcPopulation>();if(existing){UnityEngine.Object.Destroy(existing.gameObject);yield return null;}
            NpcStudyAssetBuilder.StartStudy();var population=UnityEngine.Object.FindFirstObjectByType<NpcPopulation>();
            Require(population&&population.People.Count==10&&population.Navigation.OpenCount>100,"Ten people and bounded walkable area");
            var rig=view.GetComponentInChildren<CityForgeCameraController>();
            var initial=population.People.Select(p=>p.Record.position).ToArray();
            var leg=population.People[0].Visual.GetComponentsInChildren<Transform>().First(t=>t.name.EndsWith("LeftUpLeg"));
            Quaternion initialLeg=leg.localRotation;float boneMotion=0;
            float until=Time.time+65;int invalid=0,overlaps=0;bool sawTalk=false,sawWalk=false;float groundError=0;
            while(Time.time<until){
                boneMotion=Mathf.Max(boneMotion,Quaternion.Angle(initialLeg,leg.localRotation));
                foreach(var p in population.People){
                    if(!population.Navigation.Suitable(p.Record.position))invalid++;
                    groundError=Mathf.Max(groundError,Mathf.Abs(p.Record.position.y-population.Navigation.Ground(p.Record.position).y));
                    sawTalk|=p.ActionId=="Talk";sawWalk|=p.ActionId=="Walk";
                    Require(p.Animator&&p.Animator.isInitialized,"Live Animator initialized");
                }
                for(int i=0;i<population.People.Count;i++)for(int j=i+1;j<population.People.Count;j++)if(Vector3.Distance(population.People[i].Record.position,population.People[j].Record.position)<.68f)overlaps++;
                yield return null;
            }
            Require(invalid==0&&overlaps==0&&groundError<.001f,"Safe positions, personal space and ground contact");
            Require(boneMotion>5,"Imported rig animation moves the leg bone");
            Require(sawWalk&&sawTalk&&population.CompletedMeetings>0,"Autonomous walk and completed paired conversations");
            int moved=population.People.Where((p,i)=>Vector3.Distance(p.Record.position,initial[i])>1).Count();Require(moved>=6,"Most people visibly moved");
            Require(population.Navigation.FindPath(population.Center,population.Center+Vector3.right*100)==null,"Unreachable target rejected");
            var locomotion=CheckStationaryIdle(population);while(locomotion.MoveNext())yield return locomotion.Current;
            population.SetDiagnostics(false);
            foreach(int level in new[]{0,1,2}){rig.SetZoomLevel(level);until=Time.time+1;while(Time.time<until)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,$"NPC-Zoom-{level}.png"));yield return null;yield return null;}
            var positions=population.People.Select(p=>p.Record.position).ToArray();rig.SetZoomLevel(3);until=Time.time+2;while(Time.time<until)yield return null;
            Require(!population.PresentationVisible&&population.People.All(p=>!p.Visual.gameObject.activeInHierarchy),"Far view removes visuals");
            Require(population.People.Select((p,i)=>p.Record.position==positions[i]).All(v=>v),"Transient study pauses records at far zoom");rig.SetZoomLevel(0);
            Require(population.People.All(p=>p.Visual.gameObject.activeInHierarchy),"Close view restores visuals");
            var a=population.People.FirstOrDefault(p=>p.Reserved);if(a==null){population.People[0].Set(new NpcIdleAction());population.People[1].Set(new NpcIdleAction());population.TryMeet(population.People[0]);a=population.People.FirstOrDefault(p=>p.Reserved);}
            Require(a!=null,"Reservation available for cancellation test");var b=a.Partner;a.Cancel("Validation interruption");Require(!a.Reserved&&!b.Reserved&&a.ActionId=="Idle"&&b.ActionId=="Idle","Cancellation releases both participants");
            int completed=population.CompletedMeetings,failed=population.FailedMeetings;
            var roots=population.People.Select(p=>p.Visual).ToArray();UnityEngine.Object.Destroy(population.gameObject);yield return null;yield return null;
            Require(roots.All(r=>!r)&&!UnityEngine.Object.FindFirstObjectByType<NpcPopulation>(),"Teardown removes all representations");
            NpcStudyAssetBuilder.StartStudy();population=UnityEngine.Object.FindFirstObjectByType<NpcPopulation>();population.SetDiagnostics(false);
            until=Time.time+3;while(Time.time<until)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"NPC-Study.png"));yield return null;yield return null;
            File.WriteAllText(Path.Combine(output,"Checks.txt"),$"PASS: normal saved stream load; 10 animated residents, {population.Navigation.OpenCount} walkable grid cells; {moved} moved >1m; animated leg rotation {boneMotion:F1} degrees; {completed} completed conversations; {failed} cancelled/timed-out meetings; no invalid positions/overlaps; max ground error {groundError:F5}m; unreachable path rejection; forced blocked walker uses Idle, clears into Walk, stops/arrives into Idle (actual Animator states); paired interruption cleanup; zoom0/1/2 frames; zoom3 visual removal and study pause; close resume; teardown/restart.\nTransient study only: no resident persistence, economy, district-wide navigation or population performance qualification. Fold-arms clip is a conversation placeholder.\n");
        }
    }
}
#endif
