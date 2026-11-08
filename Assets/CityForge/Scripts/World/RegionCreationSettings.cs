using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace CityForge.World
{
    public enum ResourceAbundance { Normal, Sparse, Abundant, VeryAbundant }
    public enum ResourceDistribution { DistinctDistricts, Balanced, HighlySpecialized }
    public enum ResourceDiscovery { HistoricalProgressive, RevealedAtStart }

    public enum RegionClimate { Temperate, Tropical }

    /// <summary>Versioned recipe; saved samples remain authoritative when opening a region.</summary>
    [Serializable] public sealed class RegionCreationSettings
    {
        public int version=12, seed=PrototypeRegionData.DefaultSeed, topography, rivers, streams;
        public int districtGrid=8;
        public ResourceAbundance resourceAbundance;
        public ResourceDistribution resourceDistribution;
        public ResourceDiscovery resourceDiscovery;
        // Missing from older recipes: no retroactive settlements.
        public int nativeCampVersion;
        // Zero preserves the seeded 2–3 target for earlier enabled recipes.
        public int nativeCampCount;
        // New wizard recipes opt in; missing/zero keeps existing saved forests.
        public int forestAppearanceVersion;
        // Missing/zero preserves legacy ecology and saved planting.
        public int treeCoverageVersion,treeCoveragePercent=50;
        public int[] districtTreeCoverage;
        public int TreeCoverage(int district)=>districtTreeCoverage!=null&&district<districtTreeCoverage.Length&&district>=0?districtTreeCoverage[district]:treeCoveragePercent;
        static bool ValidCoverage(int percent)=>percent==25||percent==50||percent==75;
        // Opt-in only: old recipes retain their connected, broad waterways.
        public int waterwayVersion;
        public int GridSide=>version>=3?districtGrid:2;
        public RegionClimate climate;
        public RegionTerrainShape terrainShape;
        // Zero preserves the original River & Bay plan in existing recipes.
        public int riverBayVariant;
        // Absent in older recipes: preserve their seeded peninsula orientation. New wizard maps opt in.
        public bool peninsulaNorthToSouth;
        public RegionMoisture moisture;
        public RegionVegetation vegetation;
        public string era, period;
        public bool animals;
        public void Validate()
        {
            if((version!=1&&version!=2&&version!=3&&version!=4&&version!=5&&version!=6&&version!=7&&version!=8&&version!=9&&version!=10&&version!=11&&version!=12)||(int)terrainShape<0||(int)terrainShape>8||topography<0||topography>4||rivers<0||rivers>3||streams<0||streams>2||(int)climate<0||(int)climate>1||(int)moisture<0||(int)moisture>2||(int)vegetation<0||(int)vegetation>2)
                throw new ArgumentException("Unsupported region creation settings.");
            if ((uint)riverBayVariant > RiverBayLayout.RemakeVariantCount)
                throw new ArgumentException("Unsupported River & Bay layout variant.");
            if (terrainShape == RegionTerrainShape.RiverAndBay && version < 6)
                throw new ArgumentException("River & Bay requires a region shape recipe.");
            if ((uint)resourceAbundance > (uint)ResourceAbundance.VeryAbundant ||
                (uint)resourceDistribution > (uint)ResourceDistribution.HighlySpecialized ||
                (uint)resourceDiscovery > (uint)ResourceDiscovery.RevealedAtStart)
                throw new ArgumentException("Unsupported regional resource settings.");
            if(treeCoverageVersion<0||treeCoverageVersion>1)throw new ArgumentException("Unsupported tree coverage version.");
            if(treeCoverageVersion==1){
                if(!ValidCoverage(treeCoveragePercent))throw new ArgumentException("Tree coverage must be 25%, 50% or 75%.");
                if(districtTreeCoverage!=null&&districtTreeCoverage.Length>0){
                    if(districtTreeCoverage.Length!=GridSide*GridSide)throw new ArgumentException("Tree coverage needs one value per original district.");
                    foreach(int coverage in districtTreeCoverage)if(!ValidCoverage(coverage))throw new ArgumentException("Invalid district tree coverage.");
                }
            }
            if(waterwayVersion<0||waterwayVersion>6)throw new ArgumentException("Unsupported waterway version.");
            if(forestAppearanceVersion<0||forestAppearanceVersion>1)throw new ArgumentException("Unsupported forest appearance version.");
            if(nativeCampVersion<0||nativeCampVersion>1||(nativeCampCount!=0&&nativeCampCount!=2&&nativeCampCount!=3))throw new ArgumentException("Unsupported settlement content version.");
            if(version>=3&&(districtGrid<2||districtGrid>16||512%districtGrid!=0))throw new ArgumentException("Unsupported district grid.");
        }
    }

    public static class RegionMapGenerator
    {
        public static IEnumerator GenerateStages(string name,RegionSize size,RegionCreationSettings settings,Action<SavedRegionSnapshot> complete,Action<string,float> progress)
        {
            var generationTimer=System.Diagnostics.Stopwatch.StartNew();settings.Validate();var recipe=JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(settings));float[,] geography=null;
            if(recipe.version>=4){
                progress?.Invoke("Preparing MapMagic geography graph",.04f);yield return null;
                using(var source=new MapMagicRegionGeography(recipe,size)){
                    var task=Task.Run(source.Generate);
                    while(!task.IsCompleted){progress?.Invoke("Generating MapMagic terrain and erosion",.10f);yield return null;}
                    if(task.IsFaulted)throw task.Exception.GetBaseException();geography=task.Result;
                }
            }
            Debug.Log("Region generation profile: MapMagic="+generationTimer.Elapsed.TotalMilliseconds.ToString("F1")+"ms");generationTimer.Restart();
            progress?.Invoke("Connecting waterways and biomes",.13f);yield return null;
            var composition=Task.Run(()=>Generate(name,size,recipe,geography));
            while(!composition.IsCompleted)yield return null;
            if(composition.IsFaulted)throw composition.Exception.GetBaseException();Debug.Log("Region generation profile: composition="+generationTimer.Elapsed.TotalMilliseconds.ToString("F1")+"ms");complete(composition.Result);
        }
        public static SavedRegionSnapshot Generate(string name,RegionSize size,RegionCreationSettings settings,float[,] geography=null)
        {
            settings.Validate();
            if((int)size<0||(int)size>2||string.IsNullOrWhiteSpace(name))throw new ArgumentException("Invalid region name or size.");
            // Copy the recipe so later wizard edits cannot change the region's provenance.
            var recipe=JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(settings));
            
            if(recipe.version>=4&&geography==null){using var source=new MapMagicRegionGeography(recipe,size);geography=source.Generate();}
            var heights=new float[513,513];var distances=new float[513,513];
            var bodies=new List<RegionWaterBody>();var rng=new System.Random(recipe.seed);
            float offset=rng.Next(100,10000);
            int count=recipe.rivers+(recipe.streams==0?0:recipe.streams==1?2:5);
            for(int i=0;i<count;i++){
                bool stream=i>=recipe.rivers;
                float center=(i+1f)/(count+1), phase=(float)rng.NextDouble()*6.28f;
                var points=new Vector2[129];
                for(int j=0;j<points.Length;j++){float v=j/128f;points[j]=new Vector2(center+.025f*Mathf.Sin(v*9+phase),v);}
                bodies.Add(new RegionWaterBody{id=(stream?"stream-":"river-")+i,kind=WaterBodyKind.River,centerline=points,
                    halfWidthNormalized=stream?.004f:.012f,flowSpeed=.12f,appearanceProfile="calm-river",hasSurfaceLevel=true,surfaceLevelNormalized=12f/PrototypeRegionData.Height});
            }
            var layout=recipe.version>=6?new RegionTerrainLayout(recipe):null;
            float side=NativeCampPlanner.MapSide((int)size,recipe.GridSide);
            if (layout?.RiverBay != null)
            {
                bodies.Clear();
                layout.RiverBay.AddWaterways(recipe, side, bodies);
            }
            else if(recipe.version>=5)bodies=recipe.waterwayVersion>=1?RegionalRiverNetwork.CreateSeparated(recipe,side):RegionalRiverNetwork.Create(recipe);
            var ocean=layout?.CreateOcean();bool deepOcean=TropicalOceanDepth.DeepRecipe(recipe);
            void FillTerrainRow(int z)
            {
            for(int x=0;x<513;x++){
                float u=x/512f,v=z/512f;
                float h=35,bank=float.PositiveInfinity,riverBank=float.PositiveInfinity;
                if(recipe.version>=4)h+=geography[z,x]*PrototypeRegionData.Height;
                else{
                    float broad=Mathf.PerlinNoise(offset+u*3,offset+v*3);
                    float detail=Mathf.PerlinNoise(offset+u*8,offset+v*8);
                    h+=recipe.topography switch {
                        0=>0,
                        1=>broad*90,
                        2=>Mathf.Pow(Mathf.Max(0,(detail-.45f)/.55f),2)*150,
                        3=>broad*75+Mathf.Pow(Mathf.Max(0,(detail-.5f)*2),2)*300,
                        _=>broad*80+Mathf.Pow(Mathf.Clamp01((u-.45f)/.55f),2)*(220+detail*350)
                    };
                }
                foreach(var body in bodies){
                    if(recipe.version>=5){
                        float distance=RegionalRiverNetwork.BankDistance(body,u,v);
                        if(body.id=="tidal-river")riverBank=distance;
                        bank=Mathf.Min(bank,distance);
                        continue;
                    }
                    float row=v*128;int j=Mathf.Min(127,(int)row);
                    float cx=Mathf.Lerp(body.centerline[j].x,body.centerline[j+1].x,row-j);
                    float d=Mathf.Abs(u-cx)-body.halfWidthNormalized;
                    bank=Mathf.Min(bank,d);
                }
                if (layout?.RiverBay != null)
                {
                    float plain = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .105f, riverBank));
                    h = Mathf.Lerp(Mathf.Min(h, 19f), h, plain);
                }
                if(bodies.Count>0&&recipe.waterwayVersion==0&&layout?.RiverBay==null){float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(recipe.version>=5?0:-.003f,recipe.version>=5?.065f:.025f,bank));h=Mathf.Lerp(5,h,blend);}
                float shore=ocean!=null?layout.LandDistance(new Vector2(u,v)):1;
                if(ocean!=null){
                    h=shore>=0?Mathf.Lerp(12,h,Mathf.SmoothStep(0,1,shore/.055f)):Mathf.Lerp(12,2,Mathf.SmoothStep(0,1,-shore/.045f));
                    bank=Mathf.Min(bank,shore);
                }
                // Reserve vertical room below sea without changing the relative land relief.
                if(deepOcean){
                    h+=TropicalOceanDepth.Sea-12;
                    if(shore<0)h=TropicalOceanDepth.Sea-TropicalOceanDepth.Depth(-shore);
                }
                heights[z,x]=h/PrototypeRegionData.Height;
                distances[z,x]=bodies.Count==0&&ocean==null?PrototypeRegionData.Side:bank*PrototypeRegionData.Side;
            }
            }
            System.Threading.Tasks.Parallel.For(0,513,new System.Threading.Tasks.ParallelOptions{
                MaxDegreeOfParallelism=System.Math.Clamp(System.Environment.ProcessorCount-2,1,8)
            },FillTerrainRow);
            if(ocean!=null)bodies.Add(ocean);
            if(deepOcean)foreach(var body in bodies)if(body.hasSurfaceLevel)body.surfaceLevelNormalized+=(TropicalOceanDepth.Sea-12)/PrototypeRegionData.Height;
            var data=new PrototypeRegionData(recipe.seed,heights,distances){WaterBodies=bodies.ToArray()};
            if(recipe.version>=2)data.Biomes=new RegionBiomeMap(recipe,data);
            return new SavedRegionSnapshot{data=data,info=new SavedRegionInfo{id=Guid.NewGuid().ToString("N"),name=name.Trim(),seed=recipe.seed,size=(int)size,
                resources=ResourceSurvey.Generate(data,recipe,(int)size,layout),district=-1,animals=recipe.animals,terrainHeightRange=recipe.version>=5?PrototypeRegionData.Height:0,districts=data.Biomes?.DescribeDistricts(),nativeCamps=NativeCampPlanner.Create(data,recipe,(int)size),meadow=true,waterless=bodies.Count==0,waterBodies=data.WaterBodies,creation=recipe,savedUtc=DateTime.UtcNow.ToString("O")}};
        }
    }
}
