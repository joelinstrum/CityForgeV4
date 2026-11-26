using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.CameraSystem;
using CityForge.Geometry;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        readonly List<BridgeInstance> bridges=new();
        readonly Dictionary<string,BridgePresentation> bridgeViews=new();
        readonly Dictionary<string,string> bridgeRepairs=new();
        readonly List<BridgeInstance> navigableBridges=new();
        PointWireTool bridgeWire;
        BridgePresentation bridgeGhost;
        BridgeInstance bridgeDraft,bridgeEditing;
        BridgeStyle bridgeStyle;
        RectTransform bridgeDesigner;
        Text bridgeFeedback;
        Button bridgeConfirm;
        int bridgeCatalogPage;
        float nextBridgePreview;
        Vector2 bridgePreviewEnd;
        bool hasBridgePreview;
        bool BridgeInputActive => bridgeDesigner || bridgeWire && bridgeWire.Armed;
        internal BridgeInstance[] CaptureBridges() => bridges.Select(b=>b.Copy()).ToArray();
        internal void RestoreBridges(BridgeInstance[] records) { if(records!=null)bridges.AddRange(records.Select(b=>b.Copy())); }
        internal static void ValidateBridges(BridgeInstance[] records,int districtCount,float mapSide)
        {
            if(records==null)return;
            if(records.Length>256)throw new InvalidDataException("Too many bridges.");
            var ids=new HashSet<string>();
            foreach(var b in records)
            {
                string error=b==null?"Empty bridge record.":b.DataProblem(districtCount,mapSide*.5f);
                if(error!=null || !ids.Add(b.id))throw new InvalidDataException(error??"Duplicate bridge ID.");
            }
        }
        IEnumerable<BridgeInstance> ActiveBridges => bridges.Where(b=>ActiveDistrictMembers.Contains(b.district));
        IEnumerable<BridgeInstance> UsableBridges => ActiveBridges.Where(b=>bridgeViews.ContainsKey(b.id)&&!bridgeRepairs.ContainsKey(b.id));
        internal float TransportHeight(Vector2 p) => TryBridgeSurface(p,0,out float height)?height:LotHeight(p);
        internal bool TryBridgeSurface(Vector2 p,float inset,out float height)
        {
            foreach(var b in navigableBridges)if(b.Project(p,inset,out float at)){height=b.Height(at);return true;}
            height=0;return false;
        }
        IEnumerable<(Vector2[] Points,bool Closed)> BridgeRoadRoutes() => UsableBridges.Select(b=>(b.Route,false));
        IEnumerable<(Vector2[] Points,bool Closed)> TransportRoadRoutes() => districtRoadMeshes.Keys.Select(r=>(RoadCenterline(r).ToArray(),r.closed)).Concat(BridgeRoadRoutes());
        float? BridgeWater(Vector2 p)
        {
            var water=DistrictLotWater();
            return TryLotWater(p,BridgeGround(p),water,out float level)?level:null;
        }
        float BridgeGround(Vector2 p)
        {
            var ground=ApprovedDistrict?ApprovedDistrict.DisplayTerrain:null;
            return ground && ground.TryGetComponent<BridgeEarthwork>(out var earth)?earth.SourceHeight(p):CurrentLotTerrainHeight(p);
        }
        Vector2 SnapBridgeEnd(Vector2 p)
        {
            float best=RoadEndpointSnapDistance*RoadEndpointSnapDistance;
            foreach(var road in districtRoadMeshes.Keys)
            {
                var q=ClosestRoadPoint(road,p);float distance=(q-p).sqrMagnitude;
                if(distance>best)continue;best=distance;p=q;
            }
            return p;
        }
        void BuildBridgesPopup()
        {
            BridgeStyles.Reload();
            var panel=FounderRect(transportModal,"Bridge assembly catalog",Vector2.zero,new Vector2(720,640));
            panel.gameObject.AddComponent<GrowableModalFit>().Initialize(new Vector2(720,640));StyleTownPanel(panel,TownPanelColor);
            FounderText(panel,"Bridge Assembly",new Vector2(0,280),new Vector2(660,45),30).color=TownGold;
            var copy=FounderText(panel,"Straight trestles · Drag between dry banks, release to design.\nPlain shapes · Elevated road and walking surfaces",new Vector2(0,220),new Vector2(660,65),18);
            copy.alignment=TextAnchor.MiddleLeft;
            var entries=new List<(string Label,Action Action)>();
            foreach(var style in BridgeStyles.All)
            {
                var chosen=style;entries.Add(("Build "+style.label,()=>StartBridgePlacement(chosen)));
            }
            foreach(var bridge in ActiveBridges)
            {
                var chosen=bridge;string problem=BridgeStyles.Find(bridge.styleId)==null?"Missing style · repair required":bridgeRepairs.GetValueOrDefault(bridge.id);
                entries.Add(((problem!=null?"Repair / remove · ":"Edit · ")+(BridgeStyles.Find(bridge.styleId)?.label??bridge.styleId)+$" · {bridge.Length:0} m",()=>OpenBridgeDesigner(chosen)));
            }
            int pages=Mathf.Max(1,Mathf.CeilToInt(entries.Count/8f));bridgeCatalogPage=Mathf.Clamp(bridgeCatalogPage,0,pages-1);
            int row=0;
            foreach(var entry in entries.Skip(bridgeCatalogPage*8).Take(8))
                TownButton(panel,lotFont,entry.Label,30,-145-row++*42,660,()=>entry.Action());
            var pageLabel=FounderText(panel,$"{bridgeCatalogPage+1} / {pages} · Right-click a crossing to edit",new Vector2(0,-200),new Vector2(640,30),16);pageLabel.name="Bridge catalog page";
            TownButton(panel,lotFont,"‹ Previous",30,-520,170,()=>Page(-1)).interactable=bridgeCatalogPage>0;
            TownButton(panel,lotFont,"Next ›",520,-520,170,()=>Page(1)).interactable=bridgeCatalogPage<pages-1;
            void Page(int delta){bridgeCatalogPage+=delta;panel.gameObject.SetActive(false);Destroy(panel.gameObject);BuildBridgesPopup();}
            if(BridgeStyles.Errors.Count>0)
                FounderText(panel,string.Join("\n",BridgeStyles.Errors.Take(2)),new Vector2(0,-245),new Vector2(660,65),15).color=new Color(1,.65f,.4f);
            TownButton(panel,lotFont,"Close",278,-580,164,CloseTransportModal);
        }
        void StartBridgePlacement(BridgeStyle style)
        {
            if(!CanEditRoad)return;
            CancelRoadPlacement();CloseTownSection();CancelLotPlacement();SelectDistrictLot(null);SetRoadPaths(false);
            bridgeStyle=style;bridgeDraft=null;bridgeEditing=null;hasBridgePreview=false;
            if(!bridgeWire)
            {
                bridgeWire=gameObject.AddComponent<PointWireTool>();
                var camera=GetComponentInChildren<CityForgeCameraController>().GetComponent<Camera>();
                bridgeWire.Initialize(camera,townToolbar.parent,new BridgeWireSurface(this),new Color(.3f,.9f,.8f));
                bridgeWire.OpenPath=true;bridgeWire.PlaceOnRelease=true;bridgeWire.AllowClosedUndo=false;
                bridgeWire.RestrictUiToHud=true;bridgeWire.HandleEscape=false;bridgeWire.SnapPoint=SnapBridgeEnd;
                bridgeWire.ProjectGuide=(points,closed)=>
                {
                    if(points.Count>=2)PreviewDraggedBridge(points[0],points[^1]);
                    return points.Count>=2?new[]{points[0],points[^1]}:points;
                };
                bridgeWire.CompletionRequested=points=>
                {
                    if(points.Count<2)return "Drag between two banks.";
                    bridgeDraft=NewBridge(points[0],points[^1]);
                    string error=BridgeAssembly.Fit(bridgeDraft,bridgeStyle,BridgeGround,BridgeWater,true);
                    // Slope/height failures remain adjustable in the designer; an absent crossing does not.
                    if(bridgeDraft.supports==null)return error;
                    return null;
                };
                bridgeWire.Completed+=_=>{bridgeWire.Cancel();OpenBridgeDesigner(null);};
            }
            bridgeWire.Label="Bridge";bridgeWire.DrawingHint="Bridge · Click and drag across water, release to design · Esc cancels";
            bridgeWire.Arm();
        }
        BridgeInstance NewBridge(Vector2 start,Vector2 end) => new BridgeInstance
        {
            id=Guid.NewGuid().ToString("N"),styleId=bridgeStyle.id,district=DistrictAtWorld(new Vector3(start.x,0,start.y)),start=start,end=end,
            width=bridgeStyle.widths.Contains(8)?8:bridgeStyle.widths[0],clearance=Mathf.Max(4,bridgeStyle.minClearance)
        };
        void PreviewDraggedBridge(Vector2 start,Vector2 end)
        {
            if(Time.unscaledTime<nextBridgePreview || hasBridgePreview && (end-bridgePreviewEnd).sqrMagnitude<.0625f)return;
            nextBridgePreview=Time.unscaledTime+.08f;bridgePreviewEnd=end;hasBridgePreview=true;
            var draft=NewBridge(start,end);string error=BridgeAssembly.Fit(draft,bridgeStyle,BridgeGround,BridgeWater,true);
            if(draft.supports!=null){bridgeDraft=draft;ShowBridgeGhost(error==null);}
        }
        void ShowBridgeGhost(bool valid)
        {
            if(bridgeGhost){bridgeGhost.gameObject.SetActive(false);Destroy(bridgeGhost.gameObject);}
            if(bridgeDraft?.supports==null || bridgeStyle==null)return;
            var root=new GameObject("Bridge ghost · no committed earthwork");root.transform.SetParent(transform,false);
            bridgeGhost=root.AddComponent<BridgePresentation>();bridgeGhost.Build(bridgeDraft,bridgeStyle,true,valid,BridgeGround);
        }
        void OpenBridgeDesigner(BridgeInstance existing)
        {
            CloseTransportModal();
            if(existing!=null){CancelRoadPlacement();CancelLotPlacement();CloseTownSection();SelectDistrictLot(null);bridgeEditing=existing;bridgeDraft=existing.Copy();bridgeStyle=BridgeStyles.Find(existing.styleId);}
            if(bridgeDraft==null)return;
            if(bridgeWire)bridgeWire.Cancel();
            var panel=FounderRect(townToolbar.parent,"Bridge designer",new Vector2(365,0),new Vector2(650,640));bridgeDesigner=panel;
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(0,.5f);
            panel.anchoredPosition=new Vector2(24,0);panel.gameObject.AddComponent<GrowableModalFit>().Initialize(new Vector2(650,640));StyleTownPanel(panel,TownPanelColor);
            FounderText(panel,"Bridge designer",new Vector2(0,280),new Vector2(600,45),30).color=TownGold;
            var refreshLabels=new List<Action>();
            var styles=BridgeStyles.All.ToArray();
            var styleChoice=TownButton(panel,lotFont,(bridgeStyle?.label??bridgeDraft.styleId)+$" · {bridgeDraft.Length:0} m",25,-65,600,null);
            styleChoice.onClick.AddListener(()=>
            {
                if(styles.Length==0)return;
                int index=Array.FindIndex(styles,s=>s.id==bridgeDraft.styleId);
                bridgeStyle=styles[(index+1)%styles.Length];bridgeDraft.styleId=bridgeStyle.id;
                if(!bridgeStyle.widths.Contains(bridgeDraft.width))bridgeDraft.width=bridgeStyle.widths[0];
                styleChoice.GetComponentInChildren<Text>().text=bridgeStyle.label+$" · {bridgeDraft.Length:0} m";
                foreach(var refresh in refreshLabels)refresh();
                RefreshBridgeDraft();
            });
            int row=0;
            Step("Deck elevation",()=>bridgeDraft.deckHeight.ToString("0.0")+" m",delta=>bridgeDraft.deckHeight+=delta*.5f);
            Step("Carriageway",()=>bridgeDraft.width.ToString("0")+" m",delta=>
            {
                if(bridgeStyle==null)return;int at=Array.IndexOf(bridgeStyle.widths,bridgeDraft.width);
                bridgeDraft.width=bridgeStyle.widths[Mathf.Clamp(at+delta,0,bridgeStyle.widths.Length-1)];
            });
            Step("Lanes",()=>bridgeDraft.lanes.ToString(),delta=>bridgeDraft.lanes=Mathf.Clamp(bridgeDraft.lanes+delta,1,4));
            Step("Sidewalks (each)",()=>bridgeDraft.sidewalkWidth.ToString("0.00")+" m",delta=>bridgeDraft.sidewalkWidth=Mathf.Clamp(bridgeDraft.sidewalkWidth+delta*.25f,.75f,2));
            Step("Water clearance",()=>bridgeDraft.clearance.ToString("0.0")+" m",delta=>bridgeDraft.clearance=Mathf.Clamp(bridgeDraft.clearance+delta,1,30));
            Step("Pier-free opening",()=>bridgeDraft.channelWidth.ToString("0")+" m",delta=>bridgeDraft.channelWidth=Mathf.Clamp(bridgeDraft.channelWidth+delta*2,2,32));
            var earth=TownButton(panel,lotFont,"",25,-385,600,null);
            void EarthLabel()=>earth.GetComponentInChildren<Text>().text="Approach grading: "+(bridgeDraft.earthwork?"On (preview until confirmed)":"Off");
            earth.onClick.AddListener(()=>{bridgeDraft.earthwork=!bridgeDraft.earthwork;EarthLabel();RefreshBridgeDraft();});EarthLabel();
            bridgeFeedback=FounderText(panel,"",new Vector2(0,-150),new Vector2(600,100),18);bridgeFeedback.alignment=TextAnchor.UpperLeft;
            bridgeConfirm=TownButton(panel,lotFont,existing==null?"Build bridge":"Apply changes",25,-535,190,ConfirmBridge);
            TownButton(panel,lotFont,"Cancel",225,-535,190,CancelBridgeTool);
            if(existing!=null)TownButton(panel,lotFont,"Demolish",425,-535,190,()=>{RemoveBridge(bridgeEditing);CancelBridgeTool();});
            FounderText(panel,"Straight, fixed crossings · Esc cancels",new Vector2(0,-290),new Vector2(600,30),15);
            RefreshBridgeDraft();
            void Step(string label,Func<string> value,Action<int> change)
            {
                int y=-100-row++*44;
                var caption=FounderText(panel,label,Vector2.zero,new Vector2(275,36),18);
                caption.rectTransform.anchorMin=caption.rectTransform.anchorMax=caption.rectTransform.pivot=new Vector2(0,1);caption.rectTransform.anchoredPosition=new Vector2(25,y);caption.alignment=TextAnchor.MiddleLeft;
                var number=TownButton(panel,lotFont,value(),355,y,170,null);number.interactable=false;
                refreshLabels.Add(()=>number.GetComponentInChildren<Text>().text=value());
                TownButton(panel,lotFont,"−",310,y,36,()=>Change(-1));TownButton(panel,lotFont,"+",535,y,36,()=>Change(1));
                void Change(int delta){change(delta);number.GetComponentInChildren<Text>().text=value();RefreshBridgeDraft();}
            }
        }
        string BridgeSiteProblem(BridgeInstance b,BridgeInstance except=null)
        {
            var style=BridgeStyles.Find(b.styleId);if(style==null)return "Missing style: "+b.styleId+". Reinstall its JSON and parts; saved construction is preserved.";
            string error=b.DataProblem(GridSide*GridSide,MapSide*.5f);if(error!=null)return error;
            error=BridgeAssembly.Validate(b,style,BridgeGround,BridgeWater);if(error!=null)return error;
            int year=CityForgeTimeline.Find(DistrictAppearanceEra(SelectedDistrict))?.LastYear??TownYear(SelectedDistrict);var era=CityForgeTimeline.Eras.First(e=>e.id==style.firstEraId);
            if(year<era.FirstYear)return "This bridge style is available from "+era.name+".";
            if(!InActiveDistrict(b.start) || !InActiveDistrict(b.end))return "Keep both bridge connections inside the active district.";
            if(ApprovedDistrict)
            {
                float reach=b.TotalWidth*.5f+b.earthworkFeather;
                for(float offset=-reach;offset<=reach;offset+=1)
                    if(!ApprovedDistrict.TimberRouteClear(b.start+b.Right*offset,b.end+b.Right*offset))return "Clear trees from the bridge and approach corridor first.";
            }
            var outline=BridgeOutline(b,b.earthworkFeather);
            if(!PointWireGeometry.IsSimple(outline))return "Invalid bridge footprint.";
            foreach(var lot in PlacedLots)
            {
                var footprint=new DistrictLotFootprint(lot);
                for(float at=0;at<=b.Length;at+=1)
                    if(footprint.OverlapsCircle(b.Point(at),b.TotalWidth*.5f+b.earthworkFeather))return "Keep the bridge and approach grading clear of lots.";
            }
            for(float at=0;at<=b.Length;at+=1)
            {
                var p=b.Point(at);
                foreach(int side in new[]{-1,0,1})
                {
                    var edge=p+b.Right*(side*(b.TotalWidth*.5f+b.earthworkFeather));
                    if(!InActiveDistrict(edge))return "Keep the bridge and grading width inside the active district.";
                    if(InCampClearing(edge))return "Keep bridge construction clear of camps.";
                }
                foreach(var road in districtRoadMeshes.Keys)
                {
                    float distance=Vector2.Distance(ClosestRoadPoint(road,p),p);
                    if(distance>road.width*.5f+b.TotalWidth*.5f+b.earthworkFeather)continue;
                    if(at>road.width*.5f+b.TotalWidth*.5f+b.earthworkFeather+1 && at<b.Length-road.width*.5f-b.TotalWidth*.5f-b.earthworkFeather-1)return "The prototype cannot cross or grade an existing road midway. Connect at its endpoints.";
                }
                foreach(var other in bridges)
                    if(other!=except && other.Project(p,-b.TotalWidth*.5f-b.earthworkFeather,out _))return "Keep this crossing clear of other bridges.";
            }
            return null;
        }
        static Vector2[] BridgeOutline(BridgeInstance b,float margin)
        {
            var right=b.Right*(b.TotalWidth*.5f+margin);
            return new[]{b.start-right,b.end-right,b.end+right,b.start+right};
        }
        void RefreshBridgeDraft()
        {
            string error;
            if(bridgeStyle==null)error="Missing style: "+bridgeDraft.styleId+". Reinstall it to repair this crossing.";
            else
            {
                error=BridgeAssembly.Fit(bridgeDraft,bridgeStyle,BridgeGround,BridgeWater,false);
                error??=BridgeSiteProblem(bridgeDraft,bridgeEditing);
            }
            bridgeFeedback.text=error??$"Ready · {bridgeDraft.supports.Length} bents · Deck {bridgeDraft.deckHeight:0.0} m\nApproach grades and geometry apply only on confirmation.";
            bridgeFeedback.color=error==null?new Color(.6f,.9f,.73f):new Color(1,.7f,.45f);
            bridgeConfirm.interactable=error==null;ShowBridgeGhost(error==null);
        }
        void ConfirmBridge()
        {
            if(!CanEditRoad || bridgeDraft==null)return;
            string problem=BridgeSiteProblem(bridgeDraft,bridgeEditing);
            if(problem!=null){bridgeFeedback.text=problem;return;}
            if(bridges.Count>=256 && bridgeEditing==null){bridgeFeedback.text="This region has reached its bridge limit.";return;}
            if(ApprovedDistrict && ApprovedDistrict.GetComponent<HarborVesselVisits>() is { } traffic && traffic.VesselOccupiesBridge(bridgeDraft))
            {bridgeFeedback.text="Wait for the vessel to clear this crossing, then confirm again.";return;}
            RestoreBridgeEarthwork();
            if(bridgeEditing!=null)bridges.Remove(bridgeEditing);
            bridges.Add(bridgeDraft.Copy());
            RebuildBridgePresentation();CancelBridgeTool();
            ShowDistrictMessage("Bridge constructed. Save District to keep the crossing.");
        }
        void RemoveBridge(BridgeInstance bridge)
        {
            if(bridge==null || !CanEditRoad)return;
            RestoreBridgeEarthwork();bridges.Remove(bridge);RebuildBridgePresentation();
            ShowDistrictMessage("Bridge removed.");
        }
        bool UpdateBridgeTool()
        {
            if(!BridgeInputActive)return false;
            var nature=GetComponent<NatureToolbar>();
            if(!CanEditRoad || nature && nature.Selected!=NatureTool.None || Input.GetKeyDown(KeyCode.Escape))CancelBridgeTool();
            return true;
        }
        void CancelBridgeTool()
        {
            if(bridgeWire)bridgeWire.Cancel();
            if(bridgeDesigner){bridgeDesigner.gameObject.SetActive(false);Destroy(bridgeDesigner.gameObject);bridgeDesigner=null;}
            if(bridgeGhost){bridgeGhost.gameObject.SetActive(false);Destroy(bridgeGhost.gameObject);bridgeGhost=null;}
            bridgeDraft=null;bridgeEditing=null;bridgeStyle=null;hasBridgePreview=false;
        }
        bool TryOpenBridgeContext(Vector2 pointer)
        {
            var camera=GetComponentInChildren<CityForgeCameraController>().GetComponent<Camera>();
            var ray=camera.ScreenPointToRay(pointer);
            BridgeInstance hitBridge=null;float nearest=float.PositiveInfinity;
            foreach(var pair in bridgeViews)
                foreach(var collider in pair.Value.GetComponentsInChildren<MeshCollider>())
                    if(collider.Raycast(ray,out var hit,100000)&&hit.distance<nearest){nearest=hit.distance;hitBridge=bridges.Find(b=>b.id==pair.Key);}
            if(hitBridge!=null){OpenBridgeDesigner(hitBridge);return true;}
            if(!TryRoadGround(pointer,out var p))return false;
            var bridge=ActiveBridges.FirstOrDefault(b=>b.Project(p,-1,out _));
            if(bridge==null)return false;OpenBridgeDesigner(bridge);return true;
        }
        void RestoreBridgeEarthwork()
        {
            var ground=ApprovedDistrict?ApprovedDistrict.DisplayTerrain:null;
            if(ground && ground.TryGetComponent<BridgeEarthwork>(out var earth))earth.Restore();
        }
        void ApplyBridgeEarthwork()
        {
            var ground=ApprovedDistrict?ApprovedDistrict.DisplayTerrain:null;if(!ground || !UsableBridges.Any())return;
            var earth=ground.GetComponent<BridgeEarthwork>()??ground.gameObject.AddComponent<BridgeEarthwork>();
            foreach(var bridge in UsableBridges)earth.Apply(bridge,p=>InActiveDistrict(p)&&!BridgeWater(p).HasValue);
            var active=UsableBridges.ToArray();
            if(active.Length>0)
            {
                var bounds=active.Select(b=>BridgeOutline(b,b.earthworkFeather)).SelectMany(p=>p).ToArray();
                var area=Rect.MinMaxRect(bounds.Min(p=>p.x),bounds.Min(p=>p.y),bounds.Max(p=>p.x),bounds.Max(p=>p.y));
                InvalidateDistrictGridHeights(area);
                GetComponentInChildren<DistrictWeather>()?.RefreshGround(area);
            }
        }
        void RebuildBridgePresentation()
        {
            ClearBridgePresentation();
            if(SelectedDistrict<0)return;
            foreach(var bridge in ActiveBridges)
            {
                var style=BridgeStyles.Find(bridge.styleId);
                if(style==null){bridgeRepairs[bridge.id]="Missing style: "+bridge.styleId;continue;}
                var problem=bridge.DataProblem(GridSide*GridSide,MapSide*.5f);
                if(problem==null)problem=BridgeAssembly.Validate(bridge,style,BridgeGround,BridgeWater);
                if(problem!=null){bridgeRepairs[bridge.id]=problem;continue;}
                var root=new GameObject("Bridge · "+bridge.id);root.transform.SetParent(transform,false);
                var presentation=root.AddComponent<BridgePresentation>();presentation.Build(bridge,style,false);bridgeViews.Add(bridge.id,presentation);navigableBridges.Add(bridge);
            }
            ApplyBridgeEarthwork();
            FreightRoadRevision++;pedestrianRoutesDirty=true;MarkImmigrationSitesChanged();
            if(bridgeRepairs.Count>0)ShowDistrictMessage("A bridge needs repair. Open Transport → Bridges; saved construction is preserved.");
        }
        void ClearBridgePresentation()
        {
            foreach(var presentation in bridgeViews.Values)if(presentation){presentation.gameObject.SetActive(false);Destroy(presentation.gameObject);}
            bridgeViews.Clear();bridgeRepairs.Clear();navigableBridges.Clear();
        }
        internal bool BridgeVesselClear(Vector2 point,float radius,float airDraft,float waterLevel)
        {
            foreach(var b in bridges)
            {
                if(Array.IndexOf(ActiveDistrictMembers,b.district)<0)continue;
                var delta=point-b.start;float at=Vector2.Dot(delta,b.Direction);
                if(at< -radius || at>b.Length+radius || Mathf.Abs(Vector2.Dot(delta,b.Right))>b.TotalWidth*.5f+radius)continue;
                var style=BridgeStyles.Find(b.styleId);
                if(style==null || b.Height(at)-style.deckThickness-waterLevel<airDraft)return false;
                if(b.supports.Any(s=>Vector2.Distance(b.Point(s.station),point)<radius+b.TotalWidth*.5f+.5f))return false;
                if(Mathf.Abs(at-b.channelCenter)+radius>b.channelWidth*.5f)return false;
            }
            return true;
        }
        string BridgeRoadConflict(Vector2 p,float roadWidth)
        {
            if(bridges.Count==0)return null;
            foreach(var b in ActiveBridges)
            {
                if(!b.Project(p,-roadWidth*.5f,out float at))continue;
                if(at<4 || at>b.Length-4)continue;
                return "Connect roads at bridge endpoints; elevated intersections are not supported.";
            }
            return null;
        }
        sealed class BridgeWireSurface : PointWireSurface
        {
            readonly PrototypeRegionView view;
            internal BridgeWireSurface(PrototypeRegionView view){this.view=view;}
            public override bool Available=>view && view.CanEditRoad;
            public override bool CanEdit=>Available && !view.bridgeDesigner;
            public override Collider Collider=>view.RoadTerrain.GetComponent<TerrainCollider>();
            public override bool Contains(Vector2 p)=>view.InActiveDistrict(p);
            public override float Height(Vector2 p)=>view.TryBridgeSurface(p,0,out float height)?height:view.BridgeGround(p);
        }
    }
}
