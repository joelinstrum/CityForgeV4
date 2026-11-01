#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.Editor {
public static class NpcLocomotionChecks {
 public static void Run(string output)=>EditorCoroutineRunner.Begin(Guard(output));
 static IEnumerator Guard(string output){
  Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Motion-Status.txt"),"RUNNING");var work=Check(output);
  while(true){bool more;try{more=work.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"Motion-Status.txt"),"FAIL "+e);yield break;}if(!more)break;yield return work.Current;}
  File.WriteAllText(Path.Combine(output,"Motion-Status.txt"),"PASS");
 }
 static IEnumerator Check(string output){
  var district=UnityEngine.Object.FindFirstObjectByType<NativeCampDistrict>();var pop=district.Populations.Single();var people=pop.People.ToArray();var rig=district.GetComponent<MeadowDistrictLandscape>().View.GetComponentInChildren<CityForgeCameraController>();
  var last=people.Select(p=>p.Visual.position).ToArray();var roles=new HashSet<NpcDefinition>();int moving=0,stopped=0,invalid=0;
  foreach(int level in new[]{0,1,2}){
   rig.SetZoomLevel(3);yield return null;yield return null;rig.SetZoomLevel(level);
   // Exercise every supplied model deterministically instead of relying on random idle durations.
   foreach(var definition in people.Select(p=>p.Definition).Distinct()){
    var actor=people.First(p=>p.Definition==definition);actor.Cancel("Locomotion regression setup");bool started=false;
    var random=new System.Random(517+level);
    for(int attempt=0;attempt<200;attempt++){
     var path=pop.Navigation.FindPath(actor.Record.position,pop.Navigation.Sample(random));
     if(path==null||path.Count<3||!pop.CanOccupy(actor,path[0])||!pop.CanOccupy(actor,path[1]))continue;
     actor.Set(new NpcWalkAction(path));started=true;break;
    }
    if(!started)throw new Exception("No clear regression path for "+definition.prefab.name);
   }
   // Deliberately simulate a reset on one moving visual; the next movement must repair the real Animator state.
   var reset=people.FirstOrDefault(p=>p.ActionId=="Walk");if(reset!=null){reset.Animator.Play(reset.Definition.idleState);reset.Animator.Update(0);}
   for(int i=0;i<people.Length;i++)last[i]=people[i].Visual.position;
   float until=Time.time+12;
   while(Time.time<until){yield return new WaitForEndOfFrame();
    for(int i=0;i<people.Length;i++){
     var p=people[i];var delta=p.Visual.position-last[i];delta.y=0;last[i]=p.Visual.position;
     if(delta.sqrMagnitude>.000001f){moving++;roles.Add(p.Definition);
      if(!p.Animator.GetCurrentAnimatorStateInfo(0).IsName(p.Definition.walkState)||!p.Animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name.EndsWith("walk",StringComparison.OrdinalIgnoreCase)))invalid++;
     }else if(!p.IsMoving&&p.ActionId!="Talk"){
      stopped++;if(!p.Animator.GetCurrentAnimatorStateInfo(0).IsName(p.Definition.idleState))invalid++;
     }
    }
   }
  }
  rig.SetZoomLevel(1);
  File.WriteAllText(Path.Combine(output,"Motion-Checks.txt"),$"Moving samples={moving}; stopped samples={stopped}; mismatches={invalid}; moving character definitions={roles.Count}. Zoom0/1/2, hide/show, deliberate Animator idle reset, actual displacement versus real state and walk clip.\n");
  if(moving<100||stopped<50||roles.Count!=3||invalid!=0)throw new Exception("Locomotion state/clip regression: see Motion-Checks.txt");
 }
}
}
#endif
