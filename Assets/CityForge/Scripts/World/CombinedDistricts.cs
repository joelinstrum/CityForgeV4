using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace CityForge.World
{
    [Serializable] public sealed class CombinedDistrictGroup { public int[] cells; }
    public static class DistrictConnections
    {
        public static bool Valid(IEnumerable<int> input,int grid,int minimum=2)
        {
            var cells=input?.ToArray();if(cells==null||cells.Length<minimum||cells.Length>4||cells.Distinct().Count()!=cells.Length||cells.Any(i=>i<0||i>=grid*grid))return false;
            var seen=new HashSet<int>{cells[0]};bool added=true;while(added){added=false;foreach(int i in cells)if(!seen.Contains(i)&&seen.Any(j=>Math.Abs(i%grid-j%grid)+Math.Abs(i/grid-j/grid)==1)){seen.Add(i);added=true;break;}}return seen.Count==cells.Length;
        }
        public static void Validate(CombinedDistrictGroup[] groups,int grid)
        {
            if(groups==null)return;var used=new HashSet<int>();if(groups.Length>grid*grid/2)throw new ArgumentException("Too many combined districts.");
            foreach(var group in groups){if(group==null||!Valid(group.cells,grid)||group.cells.Any(i=>!used.Add(i)))throw new ArgumentException("Combined districts must contain two to four unique edge-connected cells without overlaps.");}
        }
    }
    public sealed partial class PrototypeRegionView
    {
        public CombinedDistrictGroup[] CombinedDistricts {get;private set;}=Array.Empty<CombinedDistrictGroup>();
        public int[] DistrictMembers(int cell)=>CombinedDistricts.FirstOrDefault(g=>g.cells.Contains(cell))?.cells??new[]{cell};
        public int DistrictAnchor(int cell)=>DistrictMembers(cell).Min();
        int memberAnchor=-2;int[] activeMembers;HashSet<int> activeSet;
        public int[] ActiveDistrictMembers {get {if(memberAnchor!=SelectedDistrict){memberAnchor=SelectedDistrict;activeMembers=SelectedDistrict<0?Array.Empty<int>():DistrictMembers(SelectedDistrict);activeSet=new HashSet<int>(activeMembers);}return activeMembers;}}
        public bool IsCombinedDistrict=>ActiveDistrictMembers.Length>1;
        public Rect ActiveDistrictBounds {
            get {var cells=ActiveDistrictMembers;return Rect.MinMaxRect(cells.Min(i=>DistrictOrigin(i).x),cells.Min(i=>DistrictOrigin(i).y),cells.Max(i=>DistrictOrigin(i).x)+DistrictSide,cells.Max(i=>DistrictOrigin(i).y)+DistrictSide);}
        }
        public bool InActiveDistrict(Vector2 p){if(SelectedDistrict<0)return true;int cell=DistrictAtWorld(new Vector3(p.x,0,p.y));var members=ActiveDistrictMembers;return cell>=0&&activeSet.Contains(cell);}
        public void CombineDistrictCells(int[] cells)
        {
            if(SelectedDistrict>=0||districtLoading||!DistrictConnections.Valid(cells,GridSide))throw new ArgumentException("Select two to four edge-connected districts from the region view.");
            if(CombinedDistricts.Any(g=>g.cells.Length==cells.Length&&g.cells.All(cells.Contains)))return;
            var selection=new HashSet<int>(cells);foreach(var group in CombinedDistricts)if(group.cells.Any(selection.Contains)&&!group.cells.All(selection.Contains))throw new ArgumentException("Select an entire existing combined district.");
            CombinedDistricts=CombinedDistricts.Where(g=>!g.cells.Any(selection.Contains)).Concat(new[]{new CombinedDistrictGroup{cells=cells.OrderBy(i=>i).ToArray()}}).ToArray();
            memberAnchor=-2;
            foreach(int cell in cells)sessionMeadows.Remove(cell);if(cells.Contains(LoadedMeadowDistrict)){LoadedMeadow=null;LoadedMeadowDistrict=-1;}
            GetComponent<DistrictCombiner>()?.RefreshBorders();
        }
    }
    public sealed class DistrictCombiner : MonoBehaviour
    {
        public bool Active {get;private set;}
        public int[] Selection=>selected.OrderBy(i=>i).ToArray();
        readonly HashSet<int> selected=new();PrototypeRegionView view;GameObject panel;Button toggle,apply;Text hint;Texture2D ids;
        public void Initialize(PrototypeRegionView source)
        {
            view=source;var hud=view.transform.Find("RegionHud");panel=new GameObject("CombineDistricts",typeof(RectTransform));panel.transform.SetParent(hud,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-25,-135);rect.sizeDelta=new Vector2(260,150);
            toggle=MakeButton("COMBINE DISTRICTS",0,()=>SetActive(!Active));apply=MakeButton("APPLY",48,Apply);
            var text=new GameObject("Hint",typeof(RectTransform),typeof(Text));text.transform.SetParent(panel.transform,false);hint=text.GetComponent<Text>();hint.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");hint.fontSize=15;hint.color=new Color(.95f,.83f,.46f);hint.alignment=TextAnchor.UpperRight;hint.raycastTarget=false;var hr=text.GetComponent<RectTransform>();hr.anchorMin=hr.anchorMax=hr.pivot=Vector2.one;hr.anchoredPosition=new Vector2(0,-98);hr.sizeDelta=new Vector2(380,60);
            ids=new Texture2D(view.GridSide,view.GridSide,TextureFormat.RGBAFloat,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,name="District grouping and selection"};RefreshBorders();
        }
        Button MakeButton(string caption,float y,UnityEngine.Events.UnityAction click)
        {
            var go=new GameObject(caption,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(panel.transform,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.one;r.anchoredPosition=new Vector2(0,-y);r.sizeDelta=new Vector2(260,42);var button=go.GetComponent<Button>();button.onClick.AddListener(click);
            var label=new GameObject("Caption",typeof(RectTransform),typeof(Text));label.transform.SetParent(go.transform,false);var t=label.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=caption;t.fontSize=18;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;var tr=t.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;return button;
        }
        public void SetActive(bool active){Active=active;selected.Clear();hint.text=active?"Select 2–4 districts sharing an edge":"";RefreshBorders();}
        public bool Select(int cell)
        {
            if(!Active||cell<0||cell>=view.DistrictCount)return false;
            var next=new HashSet<int>(selected);var members=view.DistrictMembers(cell);bool remove=members.All(next.Contains);foreach(int i in members){if(remove)next.Remove(i);else next.Add(i);}
            if(next.Count>4){hint.text="Maximum: 4 original districts.";return false;}
            if(next.Count>0&&!DistrictConnections.Valid(next,view.GridSide,1)){hint.text="Districts must share edges and stay connected.";return false;}
            selected.Clear();selected.UnionWith(next);hint.text=selected.Count+" / 4 selected";RefreshBorders();return true;
        }
        public void Apply(){if(!DistrictConnections.Valid(selected,view.GridSide))return;view.CombineDistrictCells(Selection);Active=false;selected.Clear();hint.text="Combined. Click the district to open it. Save Region to keep it.";RefreshBorders();}
        public void RefreshBorders()
        {
            if(!ids)return;var colors=new Color[view.DistrictCount];for(int i=0;i<colors.Length;i++){var group=view.DistrictMembers(i);colors[i]=new Color((group.Min()+1f)/(view.DistrictCount+1),selected.Contains(i)?1:0,group.Length>1?1:0,1);}ids.SetPixels(colors);ids.Apply();
            var relief=view.GetComponentInChildren<RegionReliefMap>(true);if(relief)relief.SetDistrictGroups(ids);
            if(view.SelectedDistrict<0){int count=view.DistrictCount-view.CombinedDistricts.Sum(g=>g.cells.Length-1);var title=view.transform.Find("RegionHud/RegionTitle");if(title)title.GetComponent<Text>().text=view.RegionName+" / "+count+" DISTRICTS";var heading=view.transform.Find("RegionHud/DistrictSelection/Heading");if(heading)heading.GetComponent<Text>().text=count+" DISTRICTS · CLICK THE MAP TO ENTER";}
            toggle.GetComponent<Image>().color=Active?new Color(.64f,.43f,.10f):new Color(.13f,.15f,.13f,.96f);toggle.GetComponentInChildren<Text>().text=Active?"● COMBINE DISTRICTS":"COMBINE DISTRICTS";apply.gameObject.SetActive(Active);apply.interactable=DistrictConnections.Valid(selected,view.GridSide);apply.GetComponent<Image>().color=new Color(.35f,.31f,.18f);
        }
        void Update(){if(panel)panel.SetActive(view.SelectedDistrict<0&&!view.DistrictLoading);}
        void OnDestroy(){if(ids)Destroy(ids);}
    }
}
