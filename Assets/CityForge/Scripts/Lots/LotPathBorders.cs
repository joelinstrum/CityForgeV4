using UnityEngine;
namespace CityForge.Lots
{
    internal static class LotPathBorders
    {
        public static readonly LotPathBorder[] All={LotPathBorder.None,LotPathBorder.Curb,LotPathBorder.Brick,LotPathBorder.Fieldstone,LotPathBorder.Timber,LotPathBorder.Limestone,LotPathBorder.Earth};
        public static string Label(LotPathBorder border)=>border switch {LotPathBorder.Curb=>"Gray stone",LotPathBorder.Brick=>"Red brick",LotPathBorder.Fieldstone=>"Fieldstone",LotPathBorder.Timber=>"Timber",LotPathBorder.Limestone=>"Limestone",LotPathBorder.Earth=>"Earth",_=>"None"};
        public static Texture2D Texture(LotPathBorder border)=>Resources.Load<Texture2D>("Lots/PathBorders/"+(border switch {LotPathBorder.Curb=>"stone",LotPathBorder.Brick=>"brick",LotPathBorder.Fieldstone=>"fieldstone",LotPathBorder.Timber=>"timber",LotPathBorder.Limestone=>"limestone",LotPathBorder.Earth=>"earth",_=>"none"}));
    }
}
