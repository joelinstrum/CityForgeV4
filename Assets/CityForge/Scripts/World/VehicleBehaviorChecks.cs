#if UNITY_EDITOR
using System;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;
namespace CityForge.Editor
{
    public static class VehicleBehaviorChecks
    {
        private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        public static void Run()
        {
            VehicleMotionChecks.Run();
            var harbor=new LotDefinition();
            harbor.objects.Add(new LotObjectDefinition{assetId="harbor-council-house"});
            Require(harbor.DeliveryRole==LotDeliveryRole.TownCenter,"Harbor Council did not default to Town Center");
            var savedHarbor=LotDefinition.Parse(JsonUtility.ToJson(harbor));
            Require(savedHarbor.DeliveryRole==LotDeliveryRole.TownCenter,"Existing Harbor Council lost Town Center role on reload");
            savedHarbor.deliveryRoleConfigured=true;savedHarbor.deliveryRole=LotDeliveryRole.None;
            Require(LotDefinition.Parse(JsonUtility.ToJson(savedHarbor)).DeliveryRole==LotDeliveryRole.None,"Explicit None override was ignored");
            harbor.deliveryRole=LotDeliveryRole.LumberMill;
            Require(harbor.DeliveryRole==LotDeliveryRole.LumberMill,"Existing explicit receiver role was overwritten");
            Require(new LotDefinition().DeliveryRole==LotDeliveryRole.None,"Ordinary lots became Town Centers");
            var settings=new LotVehicleScript{configured=true,enabled=true,behavior=LotVehicleBehavior.ResourceDelivery,
                category=VehicleTaskCategory.Resource,resource=VehicleResource.Fish,when=RegionTimeOfDay.Noon,destination=VehicleDestination.TownCenter,capacity=2,handlingSeconds=1};
            Require(settings.IsValid,"Configured delivery invalid");
            var job=new LotVehicleJobState();
            Require(!VehicleSchedule.CanDepart(settings,RegionTimeOfDay.Morning,job),"Departure ignored selected time");
            Require(VehicleSchedule.CanDepart(settings,RegionTimeOfDay.Noon,job),"Selected time did not allow departure");
            job.departedThisWindow=true;
            Require(!VehicleSchedule.CanDepart(settings,RegionTimeOfDay.Noon,job),"Repeated departure allowed in same period");
            Require(JsonUtility.FromJson<LotVehicleJobState>(JsonUtility.ToJson(job)).departedThisWindow,"Departure window lost across save");
            settings.category=VehicleTaskCategory.Emergency;job.departedThisWindow=false;
            Require(!VehicleSchedule.CanDepart(settings,RegionTimeOfDay.Noon,job),"Empty emergency category ran");
            settings.category=VehicleTaskCategory.Event;
            Require(!VehicleSchedule.CanDepart(settings,RegionTimeOfDay.Noon,job),"Empty event category ran");
            settings.category=VehicleTaskCategory.Resource;settings.enabled=false;
            Require(!VehicleSchedule.CanDepart(settings,RegionTimeOfDay.Noon,job),"Inactive behavior ran");
            settings.enabled=true;
            var source=new DistrictLotRecord{definition=new LotDefinition(),storedTimber=10};
            var destination=new DistrictLotRecord{id=Guid.NewGuid().ToString("N"),definition=new LotDefinition{deliveryRole=LotDeliveryRole.TownCenter}};
            Require(!LotFreightLedger.Load(source,job,2,destination.id,VehicleResource.Fish),"Cargo was created without stock");
            VehicleResources.Store(source,VehicleResource.Fish,5);
            Require(LotFreightLedger.Load(source,job,2,destination.id,VehicleResource.Fish)&&VehicleResources.Stored(source,VehicleResource.Fish)==3&&source.storedTimber==10,"Wrong resource consumed");
            job=JsonUtility.FromJson<LotVehicleJobState>(JsonUtility.ToJson(job));
            Require(LotFreightLedger.Deliver(destination,job)&&VehicleResources.Received(destination,VehicleResource.Fish)==2&&destination.receivedTimber==0,"Wrong resource delivered");
            Require(!LotFreightLedger.Deliver(destination,job),"Resource credited twice");
            var restored=JsonUtility.FromJson<LotVehicleScript>(JsonUtility.ToJson(settings));
            Require(restored.resource==VehicleResource.Fish&&restored.when==RegionTimeOfDay.Noon&&restored.enabled,"Behavior settings did not persist");
            Require(VehicleResources.All.Length==13&&(int)LotVehicleBehavior.LumberWagon==1,"Resource coverage or legacy ID changed");
            Debug.Log("VEHICLE BEHAVIOR CHECKS PASS: selected resource accounting, no invented cargo, correct-time departure, once-per-period gate, disabled/empty categories, settings and in-flight cargo roundtrip, existing freight regressions.");
        }
    }
}
#endif
