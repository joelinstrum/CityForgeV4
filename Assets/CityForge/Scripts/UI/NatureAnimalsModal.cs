using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using CityForge.World;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    public sealed partial class NatureToolPopup
    {
        RectTransform animalsModal;
        readonly HashSet<string> animalSelection = new();
        int animalGroup, animalPage, animalsPerSpecies = 1;
        bool randomAnimals;
        public event Action<string[], int, bool> AnimalsPlacementRequested;
        // Stable catalog IDs keep the domestic rabbit separate from the existing wild rabbit.
        static readonly (string id,string label,Rect crop)[] DomesticAnimalCards={
            ("bull","Bull",new Rect(0,0,500,362)),("chick","Chick",new Rect(500,0,240,362)),
            ("hen","Hen",new Rect(740,0,300,362)),("cow","Cow",new Rect(1040,0,460,362)),
            ("goose","Goose",new Rect(1500,0,250,362)),("horse","Horse",new Rect(1750,0,422,362)),
            ("pig","Pig",new Rect(0,362,410,362)),("domestic-rabbit","Rabbit",new Rect(410,362,290,362)),
            ("ram","Ram",new Rect(700,362,380,362)),("rooster","Rooster",new Rect(1080,362,350,362)),
            ("sheep","Sheep",new Rect(1430,362,340,362)),("turkey","Turkey",new Rect(1770,362,402,362))
        };
        static string AnimalLabel(string id) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
            id.Replace("bird-", "").Replace("-flight", " (flying)").Replace('_', ' ').Replace("goldFinch", "goldfinch").Replace("blueJay", "blue jay"));
        static int AnimalGroup(WildlifeSpecies animal) => animal.Domesticated?3:animal.habitat switch
        {
            WildlifeHabitat.WoodlandBird or WildlifeHabitat.FlyingBird => 1,
            WildlifeHabitat.Freshwater or WildlifeHabitat.River or WildlifeHabitat.FreshwaterBank or WildlifeHabitat.RiverBankShallows => 2,
            _ => 0
        };
        void CloseAnimalsModal()
        {
            if(!animalsModal)return;
            animalsModal.gameObject.SetActive(false);Destroy(animalsModal.gameObject);animalsModal=null;
        }
        void OpenAnimalsModal()
        {
            Option=0;OptionChanged?.Invoke(0);
            if(!waterFont)waterFont=Font.CreateDynamicFontFromOSFont(new[]{"Georgia","Times New Roman"},24);
            var library=Resources.Load<WildlifeLibrary>("Wildlife/TemperateWildlife");
            var all=library?library.species.Where(s=>s!=null&&s.prefab).GroupBy(s=>s.id).Select(g=>g.First()).ToArray():Array.Empty<WildlifeSpecies>();
            animalSelection.RemoveWhere(id=>!all.Any(s=>s.id==id));
            var available=animalGroup==3
                ?DomesticAnimalCards.Select(card=>all.FirstOrDefault(s=>s.id==card.id)??new WildlifeSpecies{id=card.id,disposition=AnimalDisposition.Domesticated})
                    .Concat(all.Where(s=>s.Domesticated&&!DomesticAnimalCards.Any(card=>card.id==s.id))).ToArray()
                :all.Where(s=>AnimalGroup(s)==animalGroup).ToArray();
            var placeable=available.Where(s=>s.prefab).ToArray();
            if(!randomAnimals&&animalSelection.Count==0&&placeable.Length>0)animalSelection.Add(placeable[0].id);
            const int pageSize=7;
            animalPage=Mathf.Clamp(animalPage,0,Mathf.Max(0,(available.Length-1)/pageSize));
            animalsModal=Center(GetComponentInParent<Canvas>().transform,"Animals modal",Vector2.zero,Vector2.zero);
            animalsModal.anchorMin=Vector2.zero;animalsModal.anchorMax=Vector2.one;animalsModal.offsetMin=animalsModal.offsetMax=Vector2.zero;
            animalsModal.gameObject.AddComponent<Image>().color=new Color(0,0,0,.62f);
            var panel=Center(animalsModal,"Add Animals",Vector2.zero,new Vector2(1500,790));
            var face=panel.gameObject.AddComponent<Image>();face.sprite=ZoneModalArtwork.Panel;face.type=Image.Type.Sliced;
            TextLabel(panel,"Add Animals",new Vector2(-390,329),new Vector2(650,62),44,true);
            Action(panel,"Close animals","×",new Vector2(705,335),new Vector2(45,45),()=>close());
            Rule(panel,new Vector2(0,279),new Vector2(1420,1));
            var concept=Resources.Load<Texture2D>("UI/Animals/Concept");
            if(concept)Picture(panel,"Woodland wildlife",concept,new Rect(180f/1589,1-320f/990,520f/1589,190f/990),new Vector2(-455,181),new Vector2(495,180));
            TextLabel(panel,"Populate Your Region",new Vector2(240,218),new Vector2(900,58),39,true);
            TextLabel(panel,"Bring your district to life with local wildlife.",new Vector2(240,163),new Vector2(900,42),25);
            TextLabel(panel,"Animals roam, forage and rest in their natural habitat.",new Vector2(240,120),new Vector2(900,36),21);
            Rule(panel,new Vector2(0,75),new Vector2(1420,1));
            TextLabel(panel,"Animal Group",new Vector2(-490,43),new Vector2(420,38),26,true);
            string[] groups={"Woodland Animals","Birds","Water & Banks","Domesticated"};
            for(int i=0;i<4;i++)
            {
                int group=i;
                var b=Action(panel,groups[i],groups[i],new Vector2(-537+i*358,-6),new Vector2(340,52),()=>{animalGroup=group;animalPage=0;animalSelection.Clear();ReopenAnimals();});
                b.image.sprite=i==animalGroup?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
            }
            TextLabel(panel,"Preview Species",new Vector2(-500,-70),new Vector2(410,40),27,true);
            TextLabel(panel,"Ctrl+click for multiple selections",new Vector2(267,-70),new Vector2(850,40),24).alignment=TextAnchor.MiddleRight;
            var sheet=Resources.Load<Texture2D>("UI/Animals/Wildlife");
            var domesticSheet=Resources.Load<Texture2D>("UI/Animals/Domesticated");
            var cards=new List<(Button button,string id)>();
            var selectionLabel=TextLabel(panel,"",new Vector2(-100,-275),new Vector2(990,48),22);
            var place=Action(panel,"Place animals","Place Animals",new Vector2(545,-337),new Vector2(310,56),()=>
            {
                var ids=(randomAnimals?placeable:all.Where(s=>animalSelection.Contains(s.id))).Select(s=>s.id).ToArray();
                if(ids.Length==0)return;
                CloseAnimalsModal();AnimalsPlacementRequested?.Invoke(ids,animalsPerSpecies,randomAnimals);
            });
            place.image.sprite=ZoneModalArtwork.Gold;
            Button randomButton=null;
            randomButton=Action(panel,"Random animals","Random",new Vector2(-602,-173),new Vector2(160,156),()=>{randomAnimals=true;animalSelection.Clear();RefreshAnimals();});
            TextLabel(randomButton.transform,"?",new Vector2(0,22),new Vector2(110,70),48,true).alignment=TextAnchor.MiddleCenter;
            randomButton.GetComponentInChildren<Text>().rectTransform.anchoredPosition=new Vector2(0,-53);
            foreach(var species in available.Skip(animalPage*pageSize).Take(pageSize))
            {
                var id=species.id;int i=cards.Count+1;
                var b=Action(panel,"Species "+id,"",new Vector2(-602+i*172,-173),new Vector2(160,156),()=>{SelectAnimal(id,Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl));RefreshAnimals();});
                string[] artIds={"deer","wolf","fox","bear","beaver","rabbit","raccoon","turkey"};
                int tile=Array.IndexOf(artIds,id);
                var domesticCard=DomesticAnimalCards.FirstOrDefault(card=>card.id==id);
                string caption=domesticCard.label??AnimalLabel(id);
                if(animalGroup==3&&domesticSheet&&domesticCard.id!=null)
                {
                    var crop=domesticCard.crop;
                    float ratio=crop.width/crop.height;var size=ratio>148f/106?new Vector2(148,148/ratio):new Vector2(106*ratio,106);
                    Picture(b.transform,caption+" illustration",domesticSheet,new Rect(crop.x/2172,1-(crop.y+crop.height)/724,crop.width/2172,crop.height/724),new Vector2(0,17),size);
                }
                else if(sheet&&tile>=0)Picture(b.transform,AnimalLabel(id)+" illustration",sheet,new Rect(new[]{0f,350,650,930,1240,1470,1680,1910}[tile]/2172f, 1-390f/724, new[]{350f,300,280,310,230,210,230,262}[tile]/2172f,390f/724),new Vector2(0,17),new Vector2(148,106));
                else TextLabel(b.transform,AnimalGroup(species)==1?"BIRD":AnimalGroup(species)==2?"WATER":species.Domesticated?"DOMESTIC":"WILDLIFE",new Vector2(0,20),new Vector2(145,65),23,true).alignment=TextAnchor.MiddleCenter;
                TextLabel(b.transform,caption,new Vector2(0,species.prefab?-53:-47),new Vector2(148,species.prefab?44:28),19,true).alignment=TextAnchor.MiddleCenter;
                if(!species.prefab)
                {
                    b.interactable=false;
                    TextLabel(b.transform,"Model pending",new Vector2(0,-68),new Vector2(148,19),13).alignment=TextAnchor.MiddleCenter;
                }
                cards.Add((b,id));
            }
            if(available.Length==0)TextLabel(panel,"No local animals are available in this group.",new Vector2(0,-173),new Vector2(1100,80),26);
            if(available.Length>pageSize)
            {
                TextLabel(panel,$"Page {animalPage+1} / {(available.Length+pageSize-1)/pageSize}",new Vector2(550,-275),new Vector2(210,36),19).alignment=TextAnchor.MiddleRight;
                Action(panel,"Previous species","‹",new Vector2(-688,-275),new Vector2(45,44),()=>{animalPage--;ReopenAnimals();}).interactable=animalPage>0;
                Action(panel,"Next species","›",new Vector2(688,-275),new Vector2(45,44),()=>{animalPage++;ReopenAnimals();}).interactable=(animalPage+1)*pageSize<available.Length;
            }
            TextLabel(panel,"Per species",new Vector2(-600,-337),new Vector2(220,42),23,true);
            var countLabel=TextLabel(panel,animalsPerSpecies.ToString(),new Vector2(-398,-337),new Vector2(60,42),24,true);countLabel.alignment=TextAnchor.MiddleCenter;
            Action(panel,"Fewer animals","−",new Vector2(-460,-337),new Vector2(45,44),()=>{animalsPerSpecies=Mathf.Max(1,animalsPerSpecies-1);countLabel.text=animalsPerSpecies.ToString();RefreshAnimals();});
            Action(panel,"More animals","+",new Vector2(-335,-337),new Vector2(45,44),()=>{animalsPerSpecies=Mathf.Min(20,animalsPerSpecies+1);countLabel.text=animalsPerSpecies.ToString();RefreshAnimals();});
            Action(panel,"Characters","Characters…",new Vector2(-115,-337),new Vector2(235,48),()=>{CloseAnimalsModal();gameObject.SetActive(true);SelectOption(2);});
            TextLabel(panel,"Click to place · Hold Shift: expand · Esc: cancel",new Vector2(150,-337),new Vector2(280,64),17);
            RefreshAnimals();
            void RefreshAnimals()
            {
                foreach(var card in cards)card.button.image.sprite=animalSelection.Contains(card.id)?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
                randomButton.image.sprite=randomAnimals?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
                randomButton.interactable=placeable.Length>0;
                place.interactable=randomAnimals?placeable.Length>0:animalSelection.Count>0;
                selectionLabel.text=randomAnimals?$"Random from {groups[animalGroup]} · {animalsPerSpecies} animals per click · 25 m radius":animalSelection.Count==0?"Choose at least one species.":$"{animalSelection.Count} species selected · {animalSelection.Count*animalsPerSpecies} animal{(animalSelection.Count*animalsPerSpecies==1?"":"s")} per click";
            }
        }
        internal void SelectAnimal(string id,bool multiple){randomAnimals=false;if(!multiple)animalSelection.Clear();if(!animalSelection.Add(id))animalSelection.Remove(id);}
        void ReopenAnimals(){CloseAnimalsModal();OpenAnimalsModal();}
    }
}
