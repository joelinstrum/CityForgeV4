using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal static class VehicleBehaviorArtwork
    {
        private static readonly Dictionary<string, Sprite> sprites = new();
        internal static Sprite Get(string key)
        {
            if (sprites.TryGetValue(key, out var sprite)) return sprite;
            string file; Rect source; Vector2 size; bool sliced = false;
            switch (key)
            {
                case "nav-off": file="NavigationInactive";size=new(1536,1024);source=new(42,225,1452,566);sliced=true;break;
                case "nav-on": file="NavigationActive";size=new(1567,1004);source=new(38,162,1495,692);sliced=true;break;
                case "what": file="NavigationIcons";size=new(2172,724);source=new(145,42,620,640);break;
                case "when": file="NavigationIcons";size=new(2172,724);source=new(795,42,610,610);break;
                case "where": file="NavigationIcons";size=new(2172,724);source=new(1515,42,570,615);break;
                case "wide-off": file="InteriorAtlas";size=new(1536,1024);source=new(43,89,700,211);break;
                case "wide-on": file="InteriorAtlas";size=new(1536,1024);source=new(783,86,710,217);break;
                case "card-off": file="InteriorAtlas";size=new(1536,1024);source=new(410,344,310,255);sliced=true;break;
                case "card-on": file="InteriorAtlas";size=new(1536,1024);source=new(815,344,318,255);sliced=true;break;
                default:
                    file="InteriorAtlas";size=new(1536,1024);
                    source=key switch
                    {
                        "morning"=>new Rect(65,660,265,180), "noon"=>new Rect(410,660,185,180),
                        "afternoon"=>new Rect(692,660,190,180),"evening"=>new Rect(958,690,250,155),
                        _=>new Rect(1270,660,180,180)
                    };break;
            }
            var texture=Resources.Load<Texture2D>("UI/VehicleBehavior/"+file);
            if(!texture) return null;
            float sx=texture.width/size.x, sy=texture.height/size.y;
            var rect=new Rect(source.x*sx,(size.y-source.y-source.height)*sy,source.width*sx,source.height*sy);
            var border=sliced?new Vector4(Mathf.Min(70*sx,rect.width*.2f),Mathf.Min(60*sy,rect.height*.2f),Mathf.Min(70*sx,rect.width*.2f),Mathf.Min(60*sy,rect.height*.2f)):Vector4.zero;
            sprite=Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);
            sprite.name="Vehicle behavior "+key;sprites[key]=sprite;return sprite;
        }
        internal static void Style(Button button, bool active, bool card=false)
        {
            var image=button.GetComponent<Image>();
            bool compact=((RectTransform)button.transform).rect.height<75;
            image.sprite=Get(card?(active?"card-on":"card-off"):compact?(active?"wide-on":"wide-off"):(active?"nav-on":"nav-off"));
            image.type=Image.Type.Simple;image.preserveAspect=false;image.color=Color.white;
            button.transition=Selectable.Transition.None;
            var outline=button.GetComponent<Outline>();if(outline)outline.enabled=false;
        }
    }
}
