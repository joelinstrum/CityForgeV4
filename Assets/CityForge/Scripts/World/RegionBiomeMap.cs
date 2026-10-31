using System;
using UnityEngine;

namespace CityForge.World
{
    public enum RegionMoisture { Balanced, Dry, Wet }
    public enum RegionVegetation { Mixed, OpenMeadow, Woodland }
    public enum RegionBiome { Meadow, DryMeadow, Woodland, Riverside, Upland, Water }

    [Serializable] public sealed class RegionDistrictInfo
    {
        public int index;
        public string name;
        public Rect bounds;
        public RegionBiome biome;
        public float woodland, woodlandFraction, moisture, waterFraction;
    }

    public readonly struct RegionBiomeSample
    {
        public readonly float moisture, woodland, dryness;
        public readonly RegionBiome biome;
        public RegionBiomeSample(float moisture,float woodland,float dryness,RegionBiome biome)
        {this.moisture=moisture;this.woodland=woodland;this.dryness=dryness;this.biome=biome;}
    }

    /// <summary>One region-wide ecology field. District edges never restart its coordinates.</summary>
    public sealed class RegionBiomeMap
    {
        readonly RegionCreationSettings recipe;
        readonly PrototypeRegionData data;
        readonly Vector2 offset;
        public RegionBiomeMap(RegionCreationSettings recipe,PrototypeRegionData data)
        {
            recipe.Validate();this.recipe=recipe;this.data=data;
            var random=new System.Random(recipe.seed);offset=new Vector2(random.Next(100,10000),random.Next(100,10000));
        }
        static float Sample(float[,] field,Vector2 uv)
        {
            float x=Mathf.Clamp01(uv.x)*512,z=Mathf.Clamp01(uv.y)*512;
            int ix=Mathf.Min(511,(int)x),iz=Mathf.Min(511,(int)z);
            return Mathf.Lerp(Mathf.Lerp(field[iz,ix],field[iz,ix+1],x-ix),Mathf.Lerp(field[iz+1,ix],field[iz+1,ix+1],x-ix),z-iz);
        }
        public RegionBiomeSample At(Vector2 uv)
        {
            uv=new Vector2(Mathf.Clamp01(uv.x),Mathf.Clamp01(uv.y));
            float elevation=Sample(data.Heights,uv)-(TropicalOceanDepth.DeepRecipe(recipe)?(TropicalOceanDepth.Sea-12)/PrototypeRegionData.Height:0),bank=Sample(data.WaterDistance,uv)/PrototypeRegionData.Side;
            float regional=Mathf.PerlinNoise(offset.x+uv.x*4.3f,offset.y+uv.y*4.3f);
            float patch=Mathf.PerlinNoise(offset.x+81+uv.x*13,offset.y-23+uv.y*13);
            float river=data.WaterBodies!=null&&data.WaterBodies.Length>0?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.045f,bank)):0;
            float climate=recipe.moisture==RegionMoisture.Dry?-.24f:recipe.moisture==RegionMoisture.Wet?.24f:0;
            float moisture=Mathf.Clamp01(.25f+regional*.5f+river*.25f+climate);
            float vegetation=recipe.vegetation==RegionVegetation.OpenMeadow?-.38f:recipe.vegetation==RegionVegetation.Woodland?.32f:0;
            float forest=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.76f,regional*.4f+patch*.2f+moisture*.4f+vegetation));
            forest*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(recipe.version>=5?.32f:.17f,recipe.version>=5?.65f:.43f,elevation));
            float dry=Mathf.Clamp01((.55f-moisture)*1.8f);
            bool wet=elevation<12f/PrototypeRegionData.Height&&bank<=0;
            if(wet)forest=0;
            var kind=wet?RegionBiome.Water:elevation>.20f?RegionBiome.Upland:river>.65f?RegionBiome.Riverside:forest>.55f?RegionBiome.Woodland:dry>.35f?RegionBiome.DryMeadow:RegionBiome.Meadow;
            return new RegionBiomeSample(moisture,forest,dry,kind);
        }
        public static Rect DistrictBounds(int index,int grid=2)
        {
            if(index<0||index>=grid*grid)throw new ArgumentOutOfRangeException(nameof(index));
            return new Rect(index%grid/(float)grid,index/grid/(float)grid,1f/grid,1f/grid);
        }
        public RegionBiomeSample AtDistrict(int index,Vector2 local)
        {var bounds=DistrictBounds(index,recipe.GridSide);return At(bounds.min+Vector2.Scale(local,bounds.size));}
        public RegionDistrictInfo[] DescribeDistricts()
        {
            string[] names={"Southwest","Southeast","Northwest","Northeast"};var result=new RegionDistrictInfo[recipe.GridSide*recipe.GridSide];
            for(int i=0;i<result.Length;i++){
                var district=new RegionDistrictInfo{index=i,name=recipe.GridSide==2?names[i]:$"District {i%recipe.GridSide+1}–{i/recipe.GridSide+1}",bounds=DistrictBounds(i,recipe.GridSide)};var counts=new int[6];
                const int n=24;
                for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                    var sample=At(district.bounds.min+new Vector2((x+.5f)/n,(z+.5f)/n)/recipe.GridSide);
                    if(sample.woodland>.55f)district.woodlandFraction+=1;
                    district.woodland+=sample.woodland;district.moisture+=sample.moisture;counts[(int)sample.biome]++;
                }
                district.woodlandFraction/=n*n;district.woodland/=n*n;district.moisture/=n*n;district.waterFraction=counts[(int)RegionBiome.Water]/(float)(n*n);
                int dominant=0;for(int b=1;b<counts.Length;b++)if(counts[b]>counts[dominant])dominant=b;
                district.biome=(RegionBiome)dominant;result[i]=district;
            }
            return result;
        }
        public static string Label(RegionBiome biome)=>biome switch {
            RegionBiome.DryMeadow=>"Dry meadow",RegionBiome.Riverside=>"Riverside",RegionBiome.Upland=>"Upland",_=>biome.ToString()
        };
    }
}
