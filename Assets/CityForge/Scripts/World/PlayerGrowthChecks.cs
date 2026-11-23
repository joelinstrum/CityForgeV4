#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CityForge.Lots;
using UnityEngine;
namespace CityForge.Editor
{
    public static class PlayerGrowthChecks
    {
        const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
        static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
        static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
        public static void Run()
        {
            RegionEventChecks.Run();
            var streets=new[]{(new[]{Vector2.zero,new Vector2(100,0)},false)};
            Need(!World.FreightRoadGraph.TryRoute(streets,Vector2.zero,new Vector2(100,0),1,1,out _,()=>true),
                "Cancelled road search ignored its frame budget");
            var stepped=new World.FreightRoadGraph.Search(streets,Vector2.zero,new Vector2(100,0),1,1);
            int steps=0;while(!stepped.Complete && steps++<100)stepped.Step();
            Need(stepped.Complete && stepped.Route.Length>1 && steps>1,"Time-sliced street search lost progress or its route");
            var root=new GameObject("Disposable player rules");
            try
            {
                var view=root.AddComponent<World.PrototypeRegionView>();
                Set(view,"<SelectedDistrict>k__BackingField",3);
                var town=new World.RegionTown{id="test",position=Vector2.one*.6f,capital=true,establishedYear=1700,
                    development=new World.TownDevelopmentState()};
                Set(view,"<Towns>k__BackingField",new[]{town});
                Need(!view.DeveloperMode,"Default mode must be Player");
                var cells=new RectInt(5,5,3,3);
                bool Allowed()=> (bool)Call(view,"PlayerGrowthAllowed",cells,World.TownZone.Residential1,LotWealth.Low);
                Need(!Allowed(),"Zero population permits construction");
                town.development.population=120;
                Need(!Allowed(),"Population ashore alone must not unlock every home");
                var person=new World.EventImmigrant{id=Guid.NewGuid().ToString("N"),assetId="v3-founders-cook",district=3,
                    representedPopulation=10,targetIsZone=true,destinationLotId="zone:5:5",destinationZoneCell=new Vector2Int(5,5)};
                var state=new World.RegionEventState{eventId="immigrant-first-wave",sourceLotId="test"};state.immigrants.Add(person);view.RegionEvents.Add(state);
                Need(!Allowed(),"En-route settlers triggered construction before arrival");
                person.arrivedAtZone=true;Need(Allowed(),"Arrived residents did not unlock their parcel");
                Need(!(bool)Call(view,"PlayerGrowthAllowed",new RectInt(20,20,3,3),World.TownZone.Residential1,LotWealth.Low),"One arrival unlocked unrelated homesites");
                town.development.population=0;Need(!Allowed(),"Zero-population guard bypassed by stale arrived group");town.development.population=120;
                var lot=new World.DistrictLotRecord{id=Guid.NewGuid().ToString("N"),district=3,center=new Vector2(100,100),
                    definition=new LotDefinition{width=8,length=8,category="residential",wealth=LotWealth.Low,
                        zoningLayout=new LotZoningLayout{zone=World.TownZone.Residential1,wealth=LotWealth.Low}}};
                view.PlacedLots.Add(lot);Call(view,"BindResidentsToBuilding",lot,(RectInt?)cells);
                Need(!person.targetIsZone&&!person.arrivedAtZone&&person.destinationLotId==lot.id&&!Allowed(),"Arrival credit was reused for another house");
                Need(!view.DomesticAnimalObstructed(new Vector2(110,110)),"R1 yard rejects domestic animals");
                Need(view.WildlifeObstructed(new Vector2(110,110)),"Wild wildlife was allowed into residential lots");
                lot.definition.zoningLayout.zone=World.TownZone.Residential2;
                Need(view.DomesticAnimalObstructed(new Vector2(110,110)),"Domestic exception leaked into higher-density lots");
                lot.definition.zoningLayout.zone=World.TownZone.Residential1;
                var building=LotAssetCatalog.Load().entries.First(a=>a.category=="Buildings"&&a.prefab);
                var item=new LotObjectDefinition{assetId=building.id};lot.definition.objects.Add(item);Set(view,"workObstacleRevision",-1);
                var footprint=LotRuntimeLoader.Footprint(item,building);
                Need(view.DomesticAnimalObstructed(lot.center+footprint.center),"Domestic animal could occupy a building");
                Call(view,"HoldLotPopulation",lot);
                var construction=(System.Collections.IEnumerator)Call(view,"ConstructZonedLot",lot);
                while(construction.MoveNext()){}
                var holds=(System.Collections.Generic.Dictionary<World.DistrictLotRecord,float>)view.GetType().GetField("lotPopulationAfter",F).GetValue(view);
                Need(!float.IsPositiveInfinity(holds[lot]),"Construction without a model retained an infinite population hold");
                holds[lot]=0;lot.definition.category="";lot.definition.wealth=LotWealth.None;
                var rule=World.RegionEventCatalog.Definitions.First();
                Need(((System.Collections.IEnumerable)Call(view,"ImmigrantDestinations",rule)).Cast<object>().Any(),
                    "Real zoned homes with a generic category were not recognized as immigrant destinations");
                state.moveIns.Add(new World.ImmigrantSettlement{lotId=lot.id,population=20});state.settledPopulation=20;
                view.PlacedLots.Remove(lot);Call(view,"ReleaseImmigrantHomes",(object)new[]{lot});
                Need(state.moveIns.Count==0&&state.settledPopulation==0&&state.immigrants.Sum(p=>p.Population)==30&&
                    state.immigrants.Any(p=>p.awaitingHousing&&p.Population==20),"Demolition lost its housed population instead of returning it to waiting");
                view.PlacedLots.Add(lot);
                view.SetDeveloperMode(true);town.development.population=0;Need(Allowed(),"Developer mode did not bypass demand");
                var info=JsonUtility.FromJson<World.SavedRegionInfo>(JsonUtility.ToJson(new World.SavedRegionInfo{developerMode=true,pendingPlayerLots=new[]{lot}}));
                Need(info.developerMode&&info.pendingPlayerLots.Length==1,"Saved mode or pending homesites lost");
                view.SetDeveloperMode(false);Need(!Allowed(),"Switching to Player kept unrestricted growth enabled");
                Debug.Log("PLAYER RULE CHECKS PASS: default Player; zero population; no build before physical arrival; parcel-scoped one-use arrival credit; developer toggle/save; R1 domestic yards vs wild/building/higher-density exclusion.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
#endif
