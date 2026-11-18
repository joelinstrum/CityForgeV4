#if UNITY_EDITOR
using System;
using CityForge.Lots;
using CityForge.World;
using UnityEditor;
using UnityEngine;

namespace CityForge.Editor
{
    public static class CargoProgressChecks
    {
        private static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        public static void Run()
        {
            EditorApplication.ExecuteMenuItem("CityForge/Lots/Prepare delivery cargo");
            VehicleBehaviorChecks.Run();
            foreach(var resource in VehicleResources.All)
            {
                var lot=new DistrictLotRecord{definition=new LotDefinition()};
                var wagon=new LotObjectDefinition{assetId="v3-vehicle-lumber-wagon"};lot.definition.objects.Add(wagon);
                var job=new LotVehicleJobState{objectId=wagon.id};lot.vehicleJobs.Add(job);
                VehicleResources.Store(lot,resource,4);
                Require(LotFreightLedger.LoadSome(lot,job,1,4,resource)==1&&job.cargo==1&&VehicleResources.Stored(lot,resource)==3,"First load unit did not transfer");
                Require(!LotFreightLedger.Dispatch(job,4,"destination"),"Partial wagon departed");
                job.handlingProgress=.5f;
                var restored=JsonUtility.FromJson<DistrictLotRecord>(JsonUtility.ToJson(lot));LotFreightValidation.Validate(restored);
                Require(restored.vehicleJobs[0].cargo==1&&restored.vehicleJobs[0].cargoResource==resource&&restored.vehicleJobs[0].handlingProgress==.5f,"Partial load did not persist");
                Require(LotFreightLedger.LoadSome(lot,job,20,4,resource)==3&&job.cargo==4&&VehicleResources.Stored(lot,resource)==0,"Full load lost or duplicated stock");
                Require(LotFreightLedger.Dispatch(job,4,"destination"),"Full wagon did not dispatch");
                var destination=new DistrictLotRecord{id="destination",definition=new LotDefinition{deliveryRole=LotDeliveryRole.TownCenter}};
                job.phase=LotFreightPhase.Unloading;
                Require(LotFreightLedger.UnloadSome(destination,job,1)==1&&job.cargo==3&&VehicleResources.Received(destination,resource)==1,"Unloading did not decrease cargo");
                job=JsonUtility.FromJson<LotVehicleJobState>(JsonUtility.ToJson(job));
                Require(LotFreightLedger.UnloadSome(destination,job,10)==3&&job.cargo==0&&job.phase==LotFreightPhase.Returning&&VehicleResources.Received(destination,resource)==4,"Partial unloading duplicated cargo after reload");
                Require(LotFreightLedger.UnloadSome(destination,job,1)==0,"Empty wagon unloaded twice");
            }
            var root=new GameObject("Cargo view checks");
            try
            {
                var asset=LotAssetCatalog.Load().Find("v3-vehicle-lumber-wagon");
                var definition=new LotObjectDefinition{assetId=asset.id,vehicleScript=new LotVehicleScript{configured=true,enabled=true,
                    behavior=LotVehicleBehavior.ResourceDelivery,resource=VehicleResource.Stone,capacity=4}};
                var wagon=LotRuntimeLoader.InstantiateObject(definition,asset,root.transform);
                var profile=VehicleProfile.Find(asset.id);
                Require(wagon.GetComponents<VehicleCargoPresentation>().Length==1,"Assigned wagon did not start with one empty cargo view");
                Require(Resources.Load<Mesh>(profile.emptyCargoMesh),"Empty wagon mesh missing");
                var view=VehicleCargoPresentation.Attach(wagon.transform,profile);
                Require(wagon.GetComponents<VehicleCargoPresentation>().Length==1&&view.VisibleStages==0,"Editor/runtime attachment duplicated cargo or started full");
                foreach(var resource in VehicleResources.All)
                {
                    view.SetLoad(resource,0,4);Require(view.VisibleStages==0,"Empty view still loaded");
                    view.SetLoad(resource,1,4);Require(view.VisibleStages==3,"Quarter view incorrect");
                    view.SetLoad(resource,4,4);Require(view.VisibleStages==12,"Full view incorrect");
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            Debug.Log("CARGO PROGRESS CHECKS PASS: all13 resources load incrementally, partial loads wait, full loads dispatch, partial loading/unloading survive saves, exact accounting, empty/quarter/full visuals, no duplicate unloading, existing delivery regressions.");
        }
        public static void Render()
        {
            EditorApplication.ExecuteMenuItem("CityForge/Lots/Prepare delivery cargo");
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var stage=new GameObject("Cargo preview stage");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(stage,scene);
            var camera=new GameObject("Camera").AddComponent<Camera>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject,scene);
            camera.scene=scene;camera.orthographic=true;camera.orthographicSize=4.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.15f,.17f);
            camera.transform.position=new Vector3(10,4,-2);camera.transform.LookAt(new Vector3(0,1,-1.5f));
            var sun=new GameObject("Sun").AddComponent<Light>();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sun.gameObject,scene);sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(40,-30,0);
            var target=new RenderTexture(1200,800,24);camera.targetTexture=target;
            System.IO.Directory.CreateDirectory("Library/CargoProgressPreview");var previous=RenderTexture.active;
            try
            {
                foreach(var id in new[]{"v3-vehicle-lumber-wagon","v3-vehicle-food-wagon"})
                {
                    var asset=LotAssetCatalog.Load().Find(id);var vehicle=LotRuntimeLoader.InstantiateObject(new LotObjectDefinition{assetId=id},asset,stage.transform);
                    PrototypeRegionView.SetVehicleMotion(vehicle.transform,id,false);
                    foreach(var animator in vehicle.GetComponentsInChildren<Animator>())if(animator.runtimeAnimatorController){animator.Rebind();animator.Update(0);}
                    var view=VehicleCargoPresentation.Attach(vehicle.transform,VehicleProfile.Find(id));
                    foreach(var resource in new[]{VehicleResource.Timber,VehicleResource.Stone})
                    foreach(int amount in new[]{0,1,4})
                    {
                        view.SetLoad(resource,amount,4);camera.Render();RenderTexture.active=target;
                        var image=new Texture2D(1200,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,800),0,0);image.Apply();
                        System.IO.File.WriteAllBytes("Library/CargoProgressPreview/"+id+"-"+resource+"-"+amount+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                    }
                    UnityEngine.Object.DestroyImmediate(vehicle);
                }
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
            Debug.Log("CARGO VISUAL PREVIEWS PASS: empty, quarter and full Timber/Stone on lumber and food wagons rendered.");
        }
    }
}
#endif
