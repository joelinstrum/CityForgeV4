using System;using System.IO;using System.Linq;using CityForge.Lots;using CityForge.UI;using UnityEngine;using UnityEngine.UI;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private static Font parksSerif;
        private void BuildParksOverview(bool agriculture=false)
        {
            if(townPanel){townPanel.gameObject.SetActive(false);Destroy(townPanel.gameObject);}
            townPanel=FounderRect(townToolbar.parent,"Parks overview",new Vector2(0,80),new Vector2(1280,620));
            var frame=townPanel;var skin=frame.gameObject.AddComponent<Image>();skin.sprite=ZoneModalArtwork.Panel;skin.type=Image.Type.Sliced;frame.gameObject.AddComponent<ParksOverviewFit>();
            var artwork=Resources.Load<Texture2D>("UI/OrnateParksConcept");
            if(!parksSerif)parksSerif=Font.CreateDynamicFontFromOSFont("Georgia",20);var serif=parksSerif?parksSerif:lotFont;
            RectTransform R(Transform parent,string name,float x,float y,float w,float h){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
            Text T(Transform parent,string text,float x,float y,float w,float h,int size=21){var t=R(parent,text,x,y,w,h).gameObject.AddComponent<Text>();t.font=serif;t.fontSize=size;t.text=text;t.color=TownGold;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
            Rect UV(float x,float y,float w,float h)=>new Rect(x/1853f,1-(y+h)/849f,w/1853f,h/849f);
            Button B(Transform parent,string title,float x,float y,float w,float h,Action action,bool active=false){var r=R(parent,title,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=active?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;im.type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;T(r,title,4,0,w-8,h,23);b.onClick.AddListener(()=>action());return b;}
            if(artwork){var banner=R(frame,"Park landscape banner",14,12,1252,156).gameObject.AddComponent<RawImage>();banner.texture=artwork;banner.uvRect=UV(300,49,1254,153);banner.raycastTarget=false;}else{T(frame,"Parks",100,25,1080,64,40);T(frame,"Add greenery and shape the public spaces of your city.",80,88,1120,40);}
            var close=B(frame,"×",1212,20,42,42,CloseTownSection);if(artwork){close.image.color=Color.clear;close.GetComponentInChildren<Text>().enabled=false;}
            B(frame,"Agriculture",106,179,260,58,()=>BuildParksOverview(true),agriculture);
            B(frame,"Parks",374,179,260,58,()=>BuildParksOverview(),!agriculture);
            B(frame,"Paths",642,179,260,58,()=>{CloseTownSection();OpenParkPaths();});
            B(frame,"Texture",910,179,260,58,()=>{CloseTownSection();OpenTexturize();});
            if(!agriculture)
            {
                Card("Add Trees",48,()=>ArmParkTreeBrush(false),UV(337,297,384,247));
                Card("Place a Tree",450,OpenParkTreePicker,UV(743,297,384,247));
                Card("Remove Trees",852,()=>ArmParkTreeBrush(true),UV(1147,297,384,247));
            }
            else T(frame,"Gardens & agricultural lots",70,258,1140,48,28);
            var lots=ReadParkLots(agriculture);float top=agriculture?320:555;
            if(lots.Length==0)T(frame,agriculture?"Save a Civics → Agriculture lot in the Lot Editor.":"Save a Civics → Parks lot in the Lot Editor.",80,top,1120,38,21);
            else
            {
                var window=R(frame,"Saved park lots",52,top,1176,agriculture?246:52);window.gameObject.AddComponent<RectMask2D>();var scroll=window.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.viewport=window;scroll.movementType=ScrollRect.MovementType.Clamped;
                var content=R(window,"Lot choices",0,0,1176,Mathf.Ceil(lots.Length/3f)*56);scroll.content=content;var catalog=LotAssetCatalog.Load();
                for(int i=0;i<lots.Length;i++){var lot=lots[i];var b=B(content,$"{lot.name} · {lot.width} × {lot.length}",i%3*392,i/3*56,382,48,()=>{if(!CanBuildTown||IsSaving)return;CloseTownSection();BeginLotPlacement(lot);});b.GetComponentInChildren<Text>().fontSize=17;b.interactable=lot.objects.Count>0&&LotAssetProblem(lot,catalog)==null;}
            }
            void Card(string title,float x,Action action,Rect uv)
            {
                var r=R(frame,title,x,262,380,246);var image=r.gameObject.AddComponent<RawImage>();image.texture=artwork;image.uvRect=uv;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action());
                if(!artwork){image.color=new Color(.1f,.15f,.16f);T(r,title,8,145,364,55,27);ParkTreeIcon(r,new Vector2(190,-80),90,TownGold);}
            }
        }
        private LotDefinition[] ReadParkLots(bool agriculture)
        {
            LotDefinition[] lots;try{lots=LotLibrary.Local.List().Select(e=>e.Lot).Where(l=>LotClassification.FindCategory(l.category)?.id=="civics"&&!IsFoundersLot(l)&&(agriculture?l.subcategory=="agriculture":l.subcategory is "parks" or "Parks")).ToArray();}
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){ShowDistrictMessage("Could not read the saved park lot library.");return Array.Empty<LotDefinition>();}
            if(!agriculture)return lots;var catalog=LotAssetCatalog.Load();if(!catalog)return lots;
            return new[]{"corn-garden","wheat-garden"}.Select(catalog.Find).Where(a=>a!=null&&a.prefab).Select(a=>{var lot=new LotDefinition{name=a.label,category="civics",subcategory="agriculture",width=1,length=1};lot.objects.Add(new LotObjectDefinition{assetId=a.id});return lot;}).Concat(lots).ToArray();
        }
    }
    internal sealed class ParksOverviewFit:MonoBehaviour
    {
        void LateUpdate(){var r=(RectTransform)transform;var p=(RectTransform)r.parent;float scale=Mathf.Min(1,(p.rect.width-36)/1280,(p.rect.height-240)/620);r.localScale=Vector3.one*Mathf.Max(.1f,scale);}
    }
}
