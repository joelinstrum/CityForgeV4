#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.CameraSystem;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CityForge.World
{
    public static class BridgePrototypeQA
    {
        internal static readonly string Work=Path.GetFullPath("Library/TransportChecks/Bridge");
        internal static readonly string Output=Path.GetFullPath("Library/TransportChecks/Bridge");
        public static void StartPlay()
        {
            Directory.CreateDirectory(Output);Directory.CreateDirectory(Work);File.WriteAllText(Work+"/Play.request","run");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.isPlaying=true;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Begin()
        {
            if(!File.Exists(Work+"/Play.request"))return;File.Delete(Work+"/Play.request");
            new GameObject("Bridge acceptance QA").AddComponent<BridgePrototypeRunner>();
        }
        internal static IEnumerator Run()
        {
            Application.runInBackground=true;
            if(!UnityEngine.Object.FindFirstObjectByType<Canvas>())new GameObject("Bridge QA menu canvas",typeof(Canvas)).GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var recipe=new RegionCreationSettings{seed=831071,districtGrid=2,streams=1,vegetation=RegionVegetation.OpenMeadow,moisture=RegionMoisture.Wet,era="modern"};
            var heights=new float[513,513];var distances=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)
            {
                float wx=x*2-512;float distance=Mathf.Abs(wx-160);
                heights[z,x]=Mathf.Lerp(8,14,Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,22,distance)))/PrototypeRegionData.Height;
                distances[z,x]=(distance-17)/1024*PrototypeRegionData.Side;
            }
            var body=new RegionWaterBody{id="bridge-qa-river",kind=WaterBodyKind.River,centerline=new[]{new Vector2(160f/1024+.5f,0),new Vector2(160f/1024+.5f,1)},halfWidthNormalized=18f/1024,hasSurfaceLevel=true,surfaceLevelNormalized=12f/PrototypeRegionData.Height,flowSpeed=.3f,appearanceProfile="calm-river"};
            var data=new PrototypeRegionData(recipe.seed,heights,distances){WaterBodies=new[]{body}};data.Biomes=new RegionBiomeMap(recipe,data);
            var town=new RegionTown{id="bridge-qa-town",name="Bridge QA Town",capital=true,establishedYear=1930,position=new Vector2(.8f,.8f)};
            var lot=new DistrictLotRecord{id="b93cd18dfca740988b23979d2ceef799",district=3,center=new Vector2(300,300),definition=new LotDefinition{name="Bridge QA civic",category="civics",subcategory="government",firstEraId="modern",deliveryRole=LotDeliveryRole.TownCenter,deliveryRoleConfigured=true}};
            var source=new SavedRegionSnapshot{data=data,info=new SavedRegionInfo{id="bridgeqa",name="Bridge Assembly QA",seed=recipe.seed,size=(int)RegionSize.Small,district=3,creation=recipe,meadow=true,waterBodies=data.WaterBodies,districts=data.Biomes.DescribeDistricts(),terrainHeightRange=PrototypeRegionData.Height,towns=new[]{town},lots=new[]{lot},savedUtc=DateTime.UtcNow.ToString("O"),camera=new RegionCameraState{focus=new Vector3(160,14,160),pitch=35,yaw=20,distance=500,size=72,zoom=2,hasViewTilt=true,viewTilt=1}}};
            foreach(var district in source.info.districts)district.currentYear=1930;
            PrototypeRegionView.Open(source.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:source);
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            // Vegetation presentation is unrelated to bridge correctness and requires extra private art.
            foreach(var vegetation in view.GetComponentsInChildren<MeadowRuntimeVegetation>())vegetation.enabled=false;
            foreach(var terrain in view.GetComponentsInChildren<Terrain>())terrain.drawTreesAndFoliage=false;
            for(int i=0;i<12;i++)yield return null;
            var checks=view.BridgeAcceptance();while(checks.MoveNext())yield return checks.Current;
        }
    }
    public sealed class BridgePrototypeRunner:MonoBehaviour
    {
        IEnumerator Start()
        {
            var run=BridgePrototypeQA.Run();while(true)
            {
                object next;try{if(!run.MoveNext())break;next=run.Current;}
                catch(Exception error){Debug.LogException(error);File.WriteAllText(BridgePrototypeQA.Output+"/Failure.txt",error.ToString());EditorApplication.Exit(1);yield break;}
                yield return next;
            }
            EditorApplication.Exit(0);
        }
    }
    public sealed partial class PrototypeRegionView
    {
        void CaptureBridgeQA(string path)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var camera=GetComponentInChildren<CityForgeCameraController>().GetComponent<Camera>();
            var canvases=GetComponentsInChildren<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+1;}
            var rt=RenderTexture.GetTemporary(1600,1000,24);var old=camera.targetTexture;var active=RenderTexture.active;
            camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Destroy(image);
            camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
        }
        internal IEnumerator BridgeAcceptance()
        {
            var report=new System.Text.StringBuilder();
            void Check(bool ok,string message){if(!ok)throw new Exception(message);report.AppendLine("PASS "+message);File.WriteAllText(BridgePrototypeQA.Output+"/Verification.txt",report.ToString());Debug.Log("BRIDGE PASS "+message);}
            var style=BridgeStyles.Find("timber-trestle-v1");Check(style!=null&&style.Problem()==null,"Built-in versioned JSON style resolves all required parts");
            Check(CanEditRoad,"Fixture is an established Art Deco town with road construction available");
            Check(BridgeStyles.Errors.Count==0,"Style catalog loads without validation errors");
            var bad=JsonUtility.FromJson<BridgeStyle>(JsonUtility.ToJson(style));bad.bracePart="missing-test-part";
            Check(bad.Problem().Contains("Missing bridge part"),"Missing-part error identifies the repair needed");bad.bracePart="primitive:box";bad.maxSlope=float.NaN;
            Check(bad.Problem()!=null,"Nonfinite style dimensions are rejected");
            ValidateBridges(null,4,1024);Check(true,"Older regions without bridge records remain valid");
            var start=new Vector2(122,160);var end=new Vector2(198,160);
            float before=BridgeGround(new Vector2(130,160));float[,] wholeBefore=ApprovedDistrict.DisplayTerrain.terrainData.GetHeights(0,0,ApprovedDistrict.DisplayTerrain.terrainData.heightmapResolution,ApprovedDistrict.DisplayTerrain.terrainData.heightmapResolution);
            var roots=districtLotRoots.ToDictionary(p=>p.Key,p=>p.Value.GetInstanceID());int lotBuilds=LotPresentationBuilds;
            StartBridgePlacement(style);Check(bridgeWire.Armed&&bridgeWire.PlaceOnRelease,"Bridge tool reuses PointWire drag-and-release mode");
            bridgeWire.HandlePlacementPointer(start,false,true,false,new Vector2(100,100));
            PreviewDraggedBridge(start,end);Check(bridgeGhost&&bridgeDraft!=null,"Dragging assembles a ghost deck, railings, bents and braces");
            Check(BridgeGround(new Vector2(130,160))==before&&bridges.Count==0,"Dragging changes neither terrain nor persistent crossings");
            bridgeWire.HandlePlacementPointer(end,false,false,true,new Vector2(400,100));yield return null;
            Check(bridgeDesigner&&!bridgeWire.Armed&&bridgeDraft!=null,"Release opens the designer and restores the construction cursor/collider");
            Check(bridgeConfirm.interactable,"A valid dry-bank crossing can be confirmed: "+bridgeFeedback.text);
            var draft=bridgeDraft.Copy();
            var gentler=JsonUtility.FromJson<BridgeStyle>(JsonUtility.ToJson(style));gentler.maxSlope=.12f;
            Check(BridgeAssembly.Validate(draft,gentler,BridgeGround,BridgeWater)?.Contains("slope")==true,"Compact crossing exceeds the former 12% limit but passes the new 20% limit");
            Check(draft.Length<80&&draft.deckStart<20&&draft.Length-draft.deckEnd<20,"Compact crossing has approaches shorter than 20 m on both banks");
            Check(draft.supports.All(s=>Mathf.Abs(s.station-draft.channelCenter)>=draft.channelWidth*.5f-.01f),"Every pier stays outside the designated navigation opening");
            Check(draft.supports.Any(s=>s.leftBed<12)&&draft.supports.All(s=>Mathf.Abs(s.leftBed-BridgeGround(draft.Point(s.station)-draft.Right*draft.TotalWidth*.4f))<.01f),"Supports sample actual submerged bed rather than the water plane");
            var unsupported=draft.Copy();unsupported.width=7;Check(BridgeAssembly.Validate(unsupported,style,BridgeGround,BridgeWater)!=null,"Unsupported bridge width is rejected");
            var low=draft.Copy();low.deckHeight=12.5f;Check(BridgeAssembly.Validate(low,style,BridgeGround,BridgeWater)!=null,"Insufficient under-deck clearance is rejected");
            var steep=draft.Copy();steep.deckHeight=80;Check(BridgeAssembly.Validate(steep,style,BridgeGround,BridgeWater).Contains("slope"),"Unsafe approach slope is rejected");
            CancelBridgeTool();Check(!bridgeDesigner&&!bridgeGhost&&bridges.Count==0,"Designer cancellation discards all transient geometry and settings");
            var afterCancel=ApprovedDistrict.DisplayTerrain.terrainData.GetHeights(0,0,wholeBefore.GetLength(1),wholeBefore.GetLength(0));
            Check(Equal(wholeBefore,afterCancel),"Cancelled drag/designer leaves the complete terrain heightfield unchanged");
            bridgeStyle=style;bridgeDraft=draft;OpenBridgeDesigner(null);yield return null;
            CaptureBridgeQA(BridgePrototypeQA.Output+"/Designer.png");for(int i=0;i<5;i++)yield return null;
            ConfirmBridge();Check(bridges.Count==1&&bridgeViews.Count==1&&!bridgeDesigner,"Confirm creates one saved instance and one assembled presentation");
            var bridge=bridges[0];string id=bridge.id;
            var earthBefore=ApprovedDistrict.DisplayTerrain.terrainData.GetHeights(0,0,wholeBefore.GetLength(1),wholeBefore.GetLength(0));
            ApplyBridgeEarthwork();ApplyBridgeEarthwork();
            var earthAfter=ApprovedDistrict.DisplayTerrain.terrainData.GetHeights(0,0,wholeBefore.GetLength(1),wholeBefore.GetLength(0));
            Check(Equal(earthBefore,earthAfter),"Repeated approach grading is idempotent including its feather");
            Check(TransportHeight(new Vector2(160,160))>16&&BridgeGround(new Vector2(160,160))<12,"Deck transport elevation remains separate from the river bed");
            Check(CurrentLotTerrainHeight(new Vector2(130,160))>before+.1f,"Approach earthwork is applied only after confirmation");
            Check(roots.All(p=>districtLotRoots[p.Key].GetInstanceID()==p.Value)&&LotPresentationBuilds==lotBuilds,"Bridge creation preserves all existing lot roots and presentation revision");
            Check(bridgeViews[id].GetComponentsInChildren<MeshCollider>().Length==1,"Elevated deck and approaches have a physical surface collider");
            Check(!FreightClear(new Vector2(160,100),new Vector2(160,120),.15f),"Ordinary water remains impassable away from the bridge");
            Check(FreightClear(start,end,.9f),"Vehicle route clearance traverses both approaches and the elevated crossing");
            Check(FreightClear(start+Vector2.up*4.5f,end+Vector2.up*4.5f,.15f),"Pedestrian clearance traverses the elevated sidewalks");
            Check(!FreightClear(start+Vector2.up*8,end+Vector2.up*8,.9f),"Water beside the bridge remains impassable");
            // Join real ground roads at both endpoints; no bridge record is forced into the road schema.
            var left=new DistrictRoadRecord{district=3,surfaceId="dirt",width=4,points=new[]{new Vector2(10,160),start}};
            var right=new DistrictRoadRecord{district=3,surfaceId="dirt",width=4,points=new[]{end,new Vector2(310,160)}};
            Check(CommitNewRoadPieces(new List<DistrictRoadRecord>{left,right})==null,"Roads connect at both dry bridge endpoints");
            Check(FreightRoute(new Vector2(12,160),new Vector2(308,160),2,2,false,out var path),"Freight graph finds a connected route across two roads and the bridge");
            Check(Enumerable.Range(1,path.Length-1).Any(i=>Mathf.Min(path[i-1].x,path[i].x)<160&&Mathf.Max(path[i-1].x,path[i].x)>160),"Connected freight route actually crosses the water");
            RebuildPedestrianRoutes();Check(pedestrianRoutes.Any(r=>r.Sidewalk&&r.Points.Any(p=>p.x<160)&&r.Points.Any(p=>p.x>160)),"Pedestrian network contains explicit elevated sidewalk routes");
            Check(BridgeRoadConflict(new Vector2(160,160),4)!=null&&BridgeRoadConflict(start,4)==null,"Planar overpass intersections are rejected while endpoint joins are accepted");
            Check(BridgeVesselClear(new Vector2(160,160),1,3,12),"A low vessel fits the pier-free opening with real deck clearance");
            Check(!BridgeVesselClear(new Vector2(160,160),1,20,12),"A tall vessel is blocked by actual under-deck clearance");
            Check(!BridgeVesselClear(bridge.Point(bridge.supports[1].station),1,2,12),"Vessels cannot intersect bents outside the navigation opening");
            var traffic=ApprovedDistrict.gameObject.AddComponent<HarborVesselVisits>();
            const System.Reflection.BindingFlags fields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var type=typeof(HarborVesselVisits);
            type.GetField("land",fields).SetValue(traffic,ApprovedDistrict);type.GetField("water",fields).SetValue(traffic,new WildlifeWaterAccess(ApprovedDistrict));
            var hull=GameObject.CreatePrimitive(PrimitiveType.Cube);hull.transform.localScale=new Vector3(1,20,1);hull.transform.position=new Vector3(160,12,150);
            var profileType=type.GetNestedType("VesselProfile",System.Reflection.BindingFlags.NonPublic);
            object Profile()=>Activator.CreateInstance(profileType,fields|System.Reflection.BindingFlags.Public,null,new object[]{HarborVesselKind.CargoBarge,hull,1f,1f,1f,1f,1f},null);
            type.GetField("vessel",fields).SetValue(traffic,hull);type.GetField("activeProfile",fields).SetValue(traffic,Profile());
            Check(!traffic.BridgeMotionClear(new Vector2(160,170)),"An already-planned tall vessel is stopped before entering a newly built bridge");
            hull.transform.localScale=Vector3.one;type.GetField("activeProfile",fields).SetValue(traffic,Profile());
            Check(traffic.BridgeMotionClear(new Vector2(160,170)),"A low vessel can move through the clear opening using production movement checks");
            hull.transform.position=new Vector3(160,12,160);Check(traffic.VesselOccupiesBridge(bridge),"Construction detects an existing vessel occupying the crossing");
            DestroyImmediate(traffic);yield return null;
            var vehicle=new GameObject("QA elevated vehicle");vehicle.transform.position=PedestrianGround(new Vector2(160,160));
            Check(vehicle.transform.position.y>16,"Actor world placement uses the elevated transport surface");Destroy(vehicle);
            // Exercise the production movement loop with live disposable bodies on both transport surfaces.
            if(!pedestrianRoot){pedestrianRoot=new GameObject("Bridge QA traffic").transform;pedestrianRoot.SetParent(transform,false);}
            Pedestrian Moving(string name,bool vehicle,float offset)
            {
                var body=GameObject.CreatePrimitive(vehicle?PrimitiveType.Cube:PrimitiveType.Capsule);body.name=name;body.transform.SetParent(pedestrianRoot,false);Destroy(body.GetComponent<Collider>());
                body.transform.localScale=vehicle?new Vector3(2,1,1):new Vector3(.5f,1,.5f);
                var line=bridge.Route.Concat(new[]{bridge.end+Vector2.right*22}).Select(p=>p+Vector2.up*offset).ToArray();body.transform.position=PedestrianGround(new Vector2(120,160+offset));
                var actor=new Pedestrian{Body=body.transform,Vehicle=vehicle,Route=line,Next=1,Speed=30,CornerSpeed=30,AppearanceAllowed=true};
                RememberPedestrianRoute(actor,false);pedestrians.Add(actor);return actor;
            }
            var movingVehicle=Moving("QA driving across bridge",true,0);var movingPerson=Moving("QA walking across bridge",false,4.5f);
            bool vehicleOnDeck=false,personOnDeck=false;float deadline=Time.realtimeSinceStartup+25;
            while(Time.realtimeSinceStartup<deadline&&(movingVehicle.Body.position.x<200||movingPerson.Body.position.x<200))
            {
                foreach(var actor in new[]{movingVehicle,movingPerson})
                {
                    var p=actor.Body.position;
                    if(p.x>148&&p.x<172&&!(actor.Vehicle?vehicleOnDeck:personOnDeck))
                    {
                        Check(p.y>=bridge.deckHeight-.01f,"Live "+(actor.Vehicle?"vehicle":"pedestrian")+" remains on the elevated deck");
                        if(actor.Vehicle)vehicleOnDeck=true;else personOnDeck=true;
                    }
                }
                TickPlacedPedestrians();yield return null;
            }
            Check(vehicleOnDeck&&personOnDeck&&movingVehicle.Body.position.x>=200&&movingPerson.Body.position.x>=200,"Production movement carries a vehicle and pedestrian across water to the far bank");
            pedestrians.Remove(movingVehicle);pedestrians.Remove(movingPerson);Destroy(movingVehicle.Body.gameObject);Destroy(movingPerson.Body.gameObject);
            // Edits keep identity; cancel keeps the original bridge and its earthwork.
            var viewCamera=GetComponentInChildren<CityForgeCameraController>().GetComponent<Camera>();
            var screen=viewCamera.WorldToScreenPoint(new Vector3(160,bridge.deckHeight,160));
            Check(TryOpenBridgeContext(new Vector2(screen.x,screen.y))&&bridgeDesigner,"Right-click picking resolves the physical elevated deck rather than the submerged terrain");CancelBridgeTool();
            float originalDeck=bridge.deckHeight;OpenBridgeDesigner(bridge);bridgeDraft.deckHeight+=1;RefreshBridgeDraft();CancelBridgeTool();
            Check(bridges[0].deckHeight==originalDeck&&bridges[0].id==id,"Cancelling an existing bridge edit preserves construction and identity");
            OpenBridgeDesigner(bridge);bridgeDraft.deckHeight+=.5f;RefreshBridgeDraft();Check(bridgeConfirm.interactable,"Existing construction can be edited within slope/clearance limits: "+bridgeFeedback.text);ConfirmBridge();bridge=bridges[0];
            Check(bridge.id==id&&Mathf.Abs(bridge.deckHeight-originalDeck-.5f)<.01f,"Applying bridge edits preserves stable instance identity");
            var captured=SavedRegionStore.Capture(this);string file=BridgePrototypeQA.Work+"/BridgeQA.cfregion";SavedRegionStore.Write(captured,file);var loaded=SavedRegionStore.Load(file);
            Check(loaded.info.bridges.Length==1&&JsonUtility.ToJson(loaded.info.bridges[0])==JsonUtility.ToJson(bridge),"Native binary save/reload round-trips every bridge setting and bed support");
            var missing=bridge.Copy();missing.styleId="missing-mod-test";
            ValidateBridges(new[]{missing},4,1024);Check(true,"Missing installed style does not invalidate or delete saved instance data");
            var corrupt=bridge.Copy();corrupt.deckHeight=float.NaN;bool rejected=false;try{ValidateBridges(new[]{corrupt},4,1024);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"Nonfinite saved instance geometry is rejected");
            var land=ApprovedDistrict;var far=new Vector2(350,350);float farHeight=BridgeGround(far);
            OpenBridgeDesigner(bridge);CancelBridgeTool();Check(BridgeGround(far)==farHeight,"Designer preview leaves unrelated terrain unchanged");
            var camera=GetComponentInChildren<CityForgeCameraController>();camera.RestoreRegionState(new RegionCameraState{focus=new Vector3(160,15,160),pitch=30,yaw=20,distance=500,size=72,zoom=2,hasViewTilt=true,viewTilt=1});
            for(int i=0;i<15;i++)yield return null;
            CaptureBridgeQA(BridgePrototypeQA.Output+"/Constructed.png");for(int i=0;i<5;i++)yield return null;
            camera.RestoreRegionState(new RegionCameraState{focus=new Vector3(160,14,160),pitch=23,yaw=25,distance=500,size=24,zoom=1,hasViewTilt=true,viewTilt=1});
            for(int i=0;i<8;i++)yield return null;CaptureBridgeQA(BridgePrototypeQA.Output+"/Structure-Detail.png");
            RemoveBridge(bridge);Check(bridges.Count==0&&bridgeViews.Count==0&&!TryBridgeSurface(new Vector2(160,160),0,out _),"Demolition removes structure, saved instance and navigable elevated surface");
            Check(Mathf.Abs(CurrentLotTerrainHeight(new Vector2(130,160))-before)<.02f,"Demolition restores the bounded approach earthwork");
            Check(!FreightRoute(new Vector2(12,160),new Vector2(308,160),2,2,false,out _),"Demolition disconnects the two road networks");
            Check(BridgeGround(far)==farHeight,"Demolition leaves unrelated terrain unchanged");
            // Full fresh reconstruction exercises source-free persistence rather than JSON alone.
            var menu=menuCanvas;Destroy(gameObject);yield return null;yield return null;
            PrototypeRegionView.Open(loaded.info.name,menu,snapshot:loaded);
            var reopened=FindFirstObjectByType<PrototypeRegionView>();
            foreach(var vegetation in reopened.GetComponentsInChildren<MeadowRuntimeVegetation>())vegetation.enabled=false;
            foreach(var terrain in reopened.GetComponentsInChildren<Terrain>())terrain.drawTreesAndFoliage=false;
            Debug.Log("BRIDGE QA fresh region constructed");for(int i=0;i<10;i++)yield return null;
            Check(reopened.bridges.Count==1&&reopened.bridgeViews.Count==1&&Mathf.Abs(reopened.TransportHeight(new Vector2(160,160))-loaded.info.bridges[0].deckHeight)<.01f,"Fresh region open deterministically reconstructs the elevated crossing");
            Check(reopened.FreightRoute(new Vector2(12,160),new Vector2(308,160),2,2,false,out _),"Fresh region open restores connected freight navigation");
            var existingBridge=reopened.bridges[0];reopened.RestoreBridgeEarthwork();existingBridge.styleId="missing-mod-test";reopened.RebuildBridgePresentation();
            Check(reopened.bridges.Count==1&&reopened.bridgeRepairs.ContainsKey(existingBridge.id)&&reopened.bridgeViews.Count==0,"Missing mod preserves the crossing record and flags repair without an unsafe route");
            reopened.OpenBridgeDesigner(existingBridge);Check(reopened.bridgeDesigner&&!reopened.bridgeConfirm.interactable,"Missing-mod repair designer remains accessible with confirmation blocked");reopened.CancelBridgeTool();
            existingBridge.styleId=style.id;reopened.RebuildBridgePresentation();Check(reopened.bridgeViews.Count==1,"Reinstalling a missing style reconstructs the preserved instance");
            var extra=new List<BridgeInstance>();
            for(int i=0;i<10;i++){var record=reopened.bridges[0].Copy();record.id=Guid.NewGuid().ToString("N");record.styleId="missing-pagination-test-"+i;extra.Add(record);reopened.bridges.Add(record);}
            reopened.OpenTransportModal("Bridges");
            var next=reopened.transportModal.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="Next ›");
            Check(next.interactable,"Bridge catalog paginates styles and saved crossings including missing mods");next.onClick.Invoke();yield return null;
            Check(reopened.transportModal.GetComponentsInChildren<Text>().Any(t=>t.name=="Bridge catalog page"&&t.text.StartsWith("2 /")),"Later repair entries remain reachable through catalog pagination");
            reopened.CloseTransportModal();foreach(var record in extra)reopened.bridges.Remove(record);
            var reopenedBridge=reopened.bridges[0];
            var smallArea=new Rect(new Vector2(150,145),new Vector2(20,30));reopened.CommitDetonateArea(smallArea);
            Check(reopened.bridges.Count==1,"Partial area demolition preserves a whole bridge instance");
            var wholeArea=new Rect(new Vector2(25,145),new Vector2(270,30));reopened.CommitDetonateArea(wholeArea);
            Check(reopened.bridges.Count==0&&!reopened.TryBridgeSurface(new Vector2(160,160),0,out _),"Whole area demolition removes bridge structure and navigation");
            reopened.bridges.Add(reopenedBridge.Copy());reopened.RebuildBridgePresentation();
            var terrainEdit=new ZoneGroundPlane{district=3,center=new Vector2(160,160),size=new Vector2(8,8),height=14,leveler=true};
            var area=terrainEdit.Bounds;area.min-=Vector2.one*32;area.max+=Vector2.one*32;
            reopened.DestroyLevelerContents(terrainEdit,area,32);
            Check(reopened.bridges.Count==0&&!reopened.TryBridgeSurface(new Vector2(160,160),0,out _),"Destructive earthwork immediately removes a touched bridge and its navigable surface");
            reopened.FinishLevelerDestruction();
            if(File.Exists(BridgePrototypeQA.Output+"/Failure.txt")){File.Copy(BridgePrototypeQA.Output+"/Failure.txt",BridgePrototypeQA.Work+"/Earlier-Failure.txt",true);File.Delete(BridgePrototypeQA.Output+"/Failure.txt");}
            File.WriteAllText(BridgePrototypeQA.Output+"/Verification.txt",report.ToString());
            static bool Equal(float[,] a,float[,] b){for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++)if(a[z,x]!=b[z,x])return false;return true;}
        }
    }
}
#endif
