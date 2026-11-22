using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        Button levelerButton;
        bool levelerArmed;
        int levelerReadyFrame,levelerTiles,levelerRotationSteps;
        Vector2Int? levelerCell;
        float nextLevelerPreview;
        GameObject levelerPreview;
        Mesh levelerMesh;
        Material levelerMaterial;
        LineRenderer levelerOutline;
        ZoneGroundPlane levelerStamp;
        string levelerProblem;
        const float LevelerSeaClearance=.2f;
        internal double LastLevelerMilliseconds {get;private set;}

        void BuildLevelerButton(Font font)
        {
            levelerButton=TownButton(townToolbar,font,"Level",1268,-14,64,()=>SetLeveler(!levelerArmed));
            var text=levelerButton.GetComponentInChildren<Text>();text.fontSize=13;text.alignment=TextAnchor.LowerCenter;
            var icon=FounderRect(levelerButton.transform,"Level ground icon",new Vector2(0,6),new Vector2(40,26));
            Bar(-12,5,8,4);Bar(-4,1,8,4);Bar(4,-3,8,4);Bar(0,-9,36,3);
            void Bar(float x,float y,float w,float h){var image=FounderRect(icon,"Earth level",new Vector2(x,y),new Vector2(w,h)).gameObject.AddComponent<Image>();image.color=TownGold;image.raycastTarget=false;}
            var trigger=levelerButton.gameObject.AddComponent<EventTrigger>();var entry=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};entry.callback.AddListener(_=>{if(!levelerArmed)ShowDistrictMessage("Level ground · 8 × 8 tiles · Shift: 16 × 16 · Tab: rotate 45°");});trigger.triggers.Add(entry);
        }
        void SetLeveler(bool armed)
        {
            if(armed){if(!CanUseLeveler)return;if(!levelerNature)CloseTownSection();CancelRoadPlacement();CancelLotPlacement();SetRoadPaths(false);SelectDistrictLot(null);levelerRotationSteps=0;}
            CancelLevelerStroke();levelerArmed=armed;levelerCell=null;levelerReadyFrame=Time.frameCount+1;nextLevelerPreview=0;
            if(levelerPreview)levelerPreview.SetActive(false);
            if(levelerButton)MarkTownButton(levelerButton,armed);
            if(!armed){if(terraformCollider)terraformCollider.enabled=terraformColliderEnabled;terraformCollider=null;levelerNature=false;}
            if(armed)ShowDistrictMessage(LevelerCaption(levelerNature?Mathf.RoundToInt(terraformSettings.Size/4):8));
        }
        bool UpdateLeveler()
        {
            if(!levelerArmed)return false;
            if(!CanUseLeveler||Input.GetKeyDown(KeyCode.Escape)){SetLeveler(false);return true;}
            bool rotated=Application.isFocused&&Input.GetKeyDown(KeyCode.Tab);
            if(rotated)RotateLevelerSelection();
            if(!Application.isFocused||(EventSystem.current&&EventSystem.current.IsPointerOverGameObject())||!TryZoneCell(out var cell))
            {CancelLevelerStroke();if(levelerPreview)levelerPreview.SetActive(false);return true;}
            bool large=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);int tiles=levelerNature?Mathf.RoundToInt(terraformSettings.Size/4)*(large?2:1):large?16:8;
            bool click=Input.GetMouseButtonDown(0)&&Time.frameCount>levelerReadyFrame;
            bool held=Input.GetMouseButton(0),released=Input.GetMouseButtonUp(0);
            bool resized=levelerTiles!=0&&tiles!=levelerTiles;
            if(rotated||resized){if(levelerDragging)ChangeLevelerStrokeShape(held);else CancelLevelerStroke();}
            if(click||rotated||tiles!=levelerTiles||Time.unscaledTime>=nextLevelerPreview&&levelerCell!=cell)
            {
                levelerCell=cell;levelerTiles=tiles;nextLevelerPreview=Time.unscaledTime+.125f;
                levelerStamp=LevelerPlane(cell,tiles);levelerProblem=LevelerSiteProblem(levelerStamp);
                DrawLevelerPreview(levelerStamp,tiles,levelerProblem==null);
                ShowDistrictMessage(levelerProblem??LevelerCaption(tiles));
            }
            if(levelerPreview)levelerPreview.SetActive(true);
            if(click)BeginLevelerStroke();
            if(levelerDragging&&held)QueueLevelerStroke(ZoneCorner(cell),tiles,false);
            if(levelerDragging&&released){QueueLevelerStroke(ZoneCorner(cell),tiles,true);levelerDragging=false;}
            ProcessLevelerStroke(tiles);
            return true; // Own world input until Escape, including invalid ground/UI clicks.
        }
        void RotateLevelerSelection()
        {
            levelerRotationSteps=(levelerRotationSteps+1)%8;
            levelerCell=null;
        }
        ZoneGroundPlane LevelerPlane(Vector2Int cell,int tiles)
        {
            var plane=new ZoneGroundPlane{center=ZoneCorner(cell),size=Vector2.one*(tiles*DistrictGridCellMeters),cornerRadius=DistrictGridCellMeters,rotation=levelerRotationSteps*45f,leveler=true,roughEdges=true,district=SelectedDistrict};ConfigureTerraformPlane(plane);return plane;
        }
        string LevelerSiteProblem(ZoneGroundPlane plane)
        {
            return null; // Destructive earthwork: no lot, slope, road or water preservation checks.
        }
        string CommitLeveler(ZoneGroundPlane plane)
        {
            if(ZoneGroundPlanes.Count>=10000)return "The terrain edit limit has been reached.";
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            var problem=LevelerSiteProblem(plane);if(problem!=null)return problem;
            var land=ApprovedDistrict;if(!land||!land.DisplayTerrain)return "Open a district to level the ground.";
            var grading=land.DisplayTerrain.GetComponent<DistrictLotGrading>()??land.DisplayTerrain.gameObject.AddComponent<DistrictLotGrading>();
            plane.height=levelerStrokeHeight??(plane.nature?TerraformTarget(plane,grading):Mathf.Max(SeaLevel+LevelerSeaClearance,grading.Minimum(plane.Bounds,p=>plane.Contains(p),float.PositiveInfinity,true)));
            var affected=plane.Bounds;float feather=GroundPlaneBlend(plane);affected.min-=Vector2.one*feather;affected.max+=Vector2.one*feather;
            DestroyLevelerContents(plane,affected,feather);
            var last=ZoneGroundPlanes.LastOrDefault();
            if(last==null||last.center!=plane.center||last.size!=plane.size||last.rotation!=plane.rotation||last.cornerRadius!=plane.cornerRadius||last.sweep!=plane.sweep||last.nature!=plane.nature||last.stroke!=plane.stroke||last.strength!=plane.strength||last.roughEdges!=plane.roughEdges||Mathf.Abs(last.height-plane.height)>.0001f)ZoneGroundPlanes.Add(plane);
            // Clear the bank as well as the flat center: no floating native or placed trees.
            land.ClearTreesUnderLot(p=>plane.Contains(p,feather)&&InActiveDistrict(p),true,affected);
            ApplyZoneGroundPlanes(affected);land.RefreshHarborStones();
            MergeLevelerSweep(plane,last,grading);
            LastLevelerMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            return null;
        }
        static List<Vector2> LevelerBoundary(ZoneGroundPlane plane)
        {
            var result=new List<Vector2>();var half=plane.size*.5f;float r=plane.cornerRadius;
            for(int corner=0;corner<4;corner++)
            {
                float start=corner*90;var center=new Vector2(corner==0||corner==3?half.x-r:-half.x+r,corner<2?half.y-r:-half.y+r);
                for(int i=0;i<=8;i++){float angle=(start+i*90f/8)*Mathf.Deg2Rad;result.Add(plane.World(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r));}
            }
            if(plane.sweep.sqrMagnitude<.0001f)return result;
            var points=result.SelectMany(p=>new[]{p-plane.sweep*.5f,p+plane.sweep*.5f}).OrderBy(p=>p.x).ThenBy(p=>p.y).ToArray();
            var hull=new List<Vector2>();float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
            foreach(var p in points){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            int lower=hull.Count;for(int i=points.Length-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}hull.RemoveAt(hull.Count-1);return hull;
        }
        void DrawLevelerPreview(ZoneGroundPlane plane,int tiles,bool valid)
        {
            if(!levelerPreview)
            {
                levelerPreview=new GameObject("Leveler tile selection",typeof(MeshFilter),typeof(MeshRenderer));levelerPreview.transform.SetParent(transform,false);
                levelerMesh=Own(new Mesh{name="Rounded leveler tile grid"});levelerPreview.GetComponent<MeshFilter>().sharedMesh=levelerMesh;
                levelerMaterial=Own(new Material(Resources.Load<Shader>("Region/LevelerPreview")));
                var renderer=levelerPreview.GetComponent<MeshRenderer>();renderer.sharedMaterial=levelerMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
                levelerOutline=new GameObject("Rounded leveler edge").AddComponent<LineRenderer>();levelerOutline.transform.SetParent(levelerPreview.transform,false);levelerOutline.sharedMaterial=Own(new Material(Resources.Load<Shader>("Region/LevelerPreview")));levelerOutline.loop=true;levelerOutline.useWorldSpace=true;levelerOutline.widthMultiplier=.12f;levelerOutline.shadowCastingMode=ShadowCastingMode.Off;
            }
            var color=valid?new Color(.48f,.27f,.12f,.48f):new Color(.7f,.12f,.08f,.45f);levelerMaterial.SetColor("_BaseColor",color);
            levelerOutline.sharedMaterial.SetColor("_BaseColor",valid?new Color(.83f,.57f,.28f):new Color(.9f,.18f,.1f));
            var vertices=new List<Vector3>();var triangles=new List<int>();var half=plane.size*.5f;var inner=half-Vector2.one*plane.cornerRadius;
            for(int z=0;z<tiles;z++)for(int x=0;x<tiles;x++)
            {
                var min=-half+new Vector2(x,z)*DistrictGridCellMeters+Vector2.one*.07f;var max=min+Vector2.one*(DistrictGridCellMeters-.14f);
                int n=vertices.Count;foreach(var local in new[]{min,new Vector2(min.x,max.y),max,new Vector2(max.x,min.y)})
                {
                    var p=local;var q=new Vector2(Mathf.Max(0,Mathf.Abs(p.x)-inner.x),Mathf.Max(0,Mathf.Abs(p.y)-inner.y));
                    if(q.magnitude>plane.cornerRadius){var rounded=q.normalized*plane.cornerRadius;p=new Vector2(Mathf.Sign(p.x)*(inner.x+rounded.x),Mathf.Sign(p.y)*(inner.y+rounded.y));}
                    var world=plane.World(p);vertices.Add(new Vector3(world.x,CurrentLotTerrainHeight(world)+.1f,world.y));
                }
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            levelerMesh.Clear();levelerMesh.SetVertices(vertices);levelerMesh.SetTriangles(triangles,0);levelerMesh.RecalculateBounds();
            var boundary=LevelerBoundary(plane);levelerOutline.positionCount=boundary.Count;
            for(int i=0;i<boundary.Count;i++)levelerOutline.SetPosition(i,new Vector3(boundary[i].x,CurrentLotTerrainHeight(boundary[i])+.15f,boundary[i].y));
        }
    }
}
