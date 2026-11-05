#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.Editor
{
    public static class CoastalSharkChecks
    {
        const string Report="Library/CoastalSharkChecks.txt";
        [MenuItem("CityForge/Wildlife/Check coastal shark patrol")]
        public static void Begin()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!Application.isPlaying||!view||view.Data==null||view.Data.WaterBodies==null||!view.ApprovedDistrict||!view.AnimalsEnabled||!CoastalSharkPopulation.Applies(view.ApprovedDistrict))throw new InvalidOperationException("Open a coastal district with Animals enabled first.");
            view.StartCoroutine(Check(view));
        }
        static IEnumerator Check(PrototypeRegionView view)
        {
            var text=new StringBuilder();bool complete=false;
            try{
                view.SetAnimalsEnabled(false);yield return null;yield return null;
                Require(UnityEngine.Object.FindObjectsByType<CoastalSharkPopulation>(FindObjectsSortMode.None).Length==0,"Animals Off removes shark patrol");
                view.SetAnimalsEnabled(true);yield return null;
                var population=view.ApprovedDistrict.Sharks;Require(population&&population.SharkCount==1,"One supplied great white spawned");
                Require(population.ValidPosition,"Shark begins in deep, contained ocean");
                var land=view.ApprovedDistrict;
                Require(!population.Habitat.SharkPoint(land.Origin-Vector2.one*5),"District exterior rejected");
                bool dryRejected=false;for(int z=0;z<16&&!dryRejected;z++)for(int x=0;x<16;x++){var p=land.Origin+new Vector2(x/15f,z/15f)*land.Side;if(land.Contains(p)&&land.Height(p)>view.SeaLevel+.2f){Require(!population.Habitat.SharkPoint(p),"Dry land rejected");dryRejected=true;break;}}
                Require(dryRejected,"Coastal fixture includes dry land");
                float start=population.DistanceTravelled;int frames=0;
                for(int i=0;i<30;i++){yield return new WaitForSecondsRealtime(.2f);frames++;Require(population.ValidPosition,"Patrol remains in checked ocean");}
                Require(population.DistanceTravelled>start+.5f,"Shark patrol actually moves");
                foreach(var t in population.GetComponentsInChildren<Transform>())Require(!t.name.StartsWith("Coastal swimmer "),"Patrol creates no swimmers");
                text.AppendLine("PASS coastal shark patrol").AppendLine("Sharks="+population.SharkCount+"; swimmers are deferred.").AppendLine("Movement="+(population.DistanceTravelled-start).ToString("F2")+"m; checked movement samples="+frames).AppendLine("Animals toggle, ocean/depth/footprint bounds and actual patrol movement passed.").AppendLine("No swimmer or encounter system is included. No player save written.");complete=true;
            }finally{
                if(view&&!view.AnimalsEnabled)view.SetAnimalsEnabled(true);
                if(!complete)text.AppendLine("FAIL: coastal shark patrol check did not complete; see Console.");
                File.WriteAllText(Report,text.ToString());Debug.Log(text.ToString());
            }
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
#endif
