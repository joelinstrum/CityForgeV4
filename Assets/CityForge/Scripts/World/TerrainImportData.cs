using System;
using System.IO;
using System.Threading;
using UnityEngine;
namespace CityForge.World
{
    [Serializable] public sealed class TerrainImportSettings
    {
        public int version,widthPixels,heightPixels,quarterTurns,waterAboveSea,landBelowSea;
        public float widthMeters,heightMeters,mapSide,blackMeters,whiteMeters,sourceSea,verticalOffset,heightRange,actualMin,actualMax,exaggeration=1;
        public bool repairShoreline;
        public string dataset,provenance,sourceHeightName,sourceWaterName,sourceMetadataJson;
        [NonSerialized] public TerrainImportData data;
        public bool Active=>version==1;
        public float GameSea=>sourceSea+verticalOffset;
        public Rect Footprint=>new Rect(-widthMeters*.5f,-heightMeters*.5f,widthMeters,heightMeters);
        public void Validate()
        {
            if(!Active||widthPixels<2||heightPixels<2||widthPixels>4096||heightPixels>4096||
                !float.IsFinite(widthMeters)||!float.IsFinite(heightMeters)||widthMeters<1||heightMeters<1||Mathf.Max(widthMeters,heightMeters)>32768||
                !float.IsFinite(blackMeters)||!float.IsFinite(whiteMeters)||whiteMeters<=blackMeters||!float.IsFinite(sourceSea)||
                !float.IsFinite(exaggeration)||exaggeration<.25f||exaggeration>4||quarterTurns<0||quarterTurns>3||
                !float.IsFinite(verticalOffset)||verticalOffset<0||GameSea<0||GameSea>=heightRange||!float.IsFinite(heightRange)||heightRange<1||heightRange>10000||mapSide!=32768)
                throw new InvalidDataException("Invalid imported terrain dimensions, elevation range or settings.");
        }
    }
    [Serializable] sealed class TerrainImportMetadata
    {
        public string schema,dataset_name,data_provenance,heightmap,water_mask,orientation,coordinate_system;
        public int pixel_width,pixel_height;
        public Extent approximate_extent_m;public Encoding elevation_encoding;public Palette water_colors;
        [Serializable] public sealed class Extent {public float width,height;}
        [Serializable] public sealed class Encoding {public float black_m,white_m,sea_level_m;}
        [Serializable] public sealed class Palette {public string land,tidal,lake,river;}
    }
    /// <summary>Original north-up U16 heights and categorical labels. Derived sampling never mutates them.</summary>
    public sealed class TerrainImportData
    {
        public readonly TerrainImportSettings Settings;
        public readonly ushort[] Heights;
        public readonly byte[] Water;
        public TerrainImportData(TerrainImportSettings settings,ushort[] heights,byte[] water)
        {
            settings.Validate();if(heights==null||water==null||heights.Length!=(long)settings.widthPixels*settings.heightPixels||water.Length!=heights.Length)throw new InvalidDataException("Imported raster payload size mismatch.");
            Settings=settings;Heights=heights;Water=water;settings.data=this;
        }
        public static TerrainImportData Read(string metadataPath,string heightPath=null,string waterPath=null,bool repair=false,int turns=0,float exaggeration=1,CancellationToken cancel=default)
        {
            if(!File.Exists(metadataPath)||new FileInfo(metadataPath).Length>65536)throw new InvalidDataException("Choose a valid metadata JSON (up to64 KB).");
            string metadataJson=File.ReadAllText(metadataPath);TerrainImportMetadata m;try{m=JsonUtility.FromJson<TerrainImportMetadata>(metadataJson);}catch(Exception e){throw new InvalidDataException("Metadata JSON could not be read.",e);}
            if(m==null||m.schema!="cityforge.terrain-import.v1"||m.approximate_extent_m==null||m.elevation_encoding==null||m.orientation!="north-up")throw new InvalidDataException("Expected cityforge.terrain-import.v1 metadata with north-up orientation, extent and elevation encoding.");
            if(m.water_colors!=null&&(m.water_colors.land!="#000000"||m.water_colors.tidal!="#0000FF"||m.water_colors.lake!="#00FFFF"||m.water_colors.river!="#00FF00"))throw new InvalidDataException("Metadata water palette does not match the v1 contract.");
            string folder=Path.GetDirectoryName(Path.GetFullPath(metadataPath));
            if(heightPath==null)heightPath=Path.Combine(folder,m.heightmap??"");if(waterPath==null)waterPath=string.IsNullOrEmpty(m.water_mask)?"":Path.Combine(folder,m.water_mask);
            byte[] ReadBytes(string path){cancel.ThrowIfCancellationRequested();if(!File.Exists(path)||new FileInfo(path).Length>64*1024*1024)throw new InvalidDataException("Input file missing or larger than64 MB: "+Path.GetFileName(path));return File.ReadAllBytes(path);}
            var h=TerrainImportPng.Decode(ReadBytes(heightPath),true,cancel);
            var s=new TerrainImportSettings{version=1,widthPixels=h.width,heightPixels=h.height,widthMeters=m.approximate_extent_m.width,heightMeters=m.approximate_extent_m.height,mapSide=32768,
                blackMeters=m.elevation_encoding.black_m,whiteMeters=m.elevation_encoding.white_m,sourceSea=m.elevation_encoding.sea_level_m,repairShoreline=repair,quarterTurns=turns,exaggeration=exaggeration,dataset=m.dataset_name,provenance=m.data_provenance,sourceMetadataJson=metadataJson,sourceHeightName=Path.GetFileName(heightPath),sourceWaterName=Path.GetFileName(waterPath)};
            s.verticalOffset=Mathf.Max(0,1-Mathf.Min(s.sourceSea,s.sourceSea+(s.blackMeters-s.sourceSea)*exaggeration));s.heightRange=Mathf.Max(PrototypeRegionData.Height,Mathf.Max(s.sourceSea,s.sourceSea+(s.whiteMeters-s.sourceSea)*exaggeration)+s.verticalOffset+1);s.Validate();
            if(m.pixel_width!=h.width||m.pixel_height!=h.height)throw new InvalidDataException("Metadata pixel dimensions differ from the height PNG.");
            var raw=new ushort[h.width*h.height];var water=new byte[raw.Length];
            for(int i=0;i<raw.Length;i++)raw[i]=(ushort)(h.bytes[i*2]<<8|h.bytes[i*2+1]);
            if(!string.IsNullOrWhiteSpace(waterPath)){
                var w=TerrainImportPng.Decode(ReadBytes(waterPath),false,cancel);if(w.width!=h.width||w.height!=h.height)throw new InvalidDataException("Height and water PNG dimensions must match exactly.");
                for(int i=0;i<water.Length;i++){if((i&65535)==0)cancel.ThrowIfCancellationRequested();int j=i*3,r=w.bytes[j],g=w.bytes[j+1],b=w.bytes[j+2];
                    if(r==0&&g==0&&(b==0||b==255))water[i]=(byte)(b==255?1:0);
                    else if(r==0&&((g==255&&b==255)||(g==255&&b==0)))throw new InvalidDataException("Freshwater lakes/rivers are deferred in v1; use only black land and blue tidal water.");
                    else throw new InvalidDataException($"Unknown water color #{r:X2}{g:X2}{b:X2} at pixel({i%h.width},{i/h.width}). Use exact RGB without antialiasing.");}
            }
            s.actualMin=float.PositiveInfinity;s.actualMax=float.NegativeInfinity;
            for(int i=0;i<raw.Length;i++){float e=DecodeElevation(raw[i],s.blackMeters,s.whiteMeters);s.actualMin=Mathf.Min(s.actualMin,e);s.actualMax=Mathf.Max(s.actualMax,e);if(water[i]==1&&e>s.sourceSea)s.waterAboveSea++;if(water[i]==0&&e<s.sourceSea)s.landBelowSea++;}
            return new TerrainImportData(s,raw,water);
        }
        public Vector2 SourceToWorld(Vector2 sourceUV)
        {
            var p=new Vector2((sourceUV.x-.5f)*Settings.widthMeters,(.5f-sourceUV.y)*Settings.heightMeters);
            for(int i=0;i<Settings.quarterTurns;i++)p=new Vector2(p.y,-p.x);return p;
        }
        public Vector2 SourceUV(Vector2 regionUV)
        {
            var p=(regionUV-Vector2.one*.5f)*Settings.mapSide;
            for(int i=0;i<Settings.quarterTurns;i++)p=new Vector2(-p.y,p.x);
            return new Vector2(p.x/Settings.widthMeters+.5f,.5f-p.y/Settings.heightMeters);
        }
        public bool InFootprint(Vector2 uv){var p=SourceUV(uv);return p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1;}
        public bool TidalAt(Vector2 uv){var p=SourceUV(uv);int x=Mathf.Clamp(Mathf.FloorToInt(p.x*(Settings.widthPixels-1)+.5f),0,Settings.widthPixels-1),y=Mathf.Clamp(Mathf.FloorToInt(p.y*(Settings.heightPixels-1)+.5f),0,Settings.heightPixels-1);return Water[y*Settings.widthPixels+x]==1;}
        public static float DecodeElevation(ushort value,float black,float white)=>black+value/65535f*(white-black);
        float Pixel(int x,int y)
        {
            int width=Settings.widthPixels,index=y*width+x;float height=DecodeElevation(Heights[index],Settings.blackMeters,Settings.whiteMeters);
            if(Settings.repairShoreline&&((Water[index]==1&&height>=Settings.sourceSea)||(Water[index]==0&&height<=Settings.sourceSea))){
                bool edge=false;for(int dy=-2;dy<=2&&!edge;dy++)for(int dx=-2;dx<=2;dx++){if(Math.Abs(dx)+Math.Abs(dy)>2)continue;int px=x+dx,py=y+dy;if(px>=0&&py>=0&&px<width&&py<Settings.heightPixels&&Water[py*width+px]!=Water[index]){edge=true;break;}}
                if(edge)height=Water[index]==1?Settings.sourceSea-.02f:Settings.sourceSea+.02f;
            }
            return Settings.sourceSea+(height-Settings.sourceSea)*Settings.exaggeration+Settings.verticalOffset;
        }
        float Bilinear(float x,float y)
        {
            int a=Mathf.Min(Settings.widthPixels-2,Mathf.FloorToInt(x)),b=Mathf.Min(Settings.heightPixels-2,Mathf.FloorToInt(y));return Mathf.Lerp(Mathf.Lerp(Pixel(a,b),Pixel(a+1,b),x-a),Mathf.Lerp(Pixel(a,b+1),Pixel(a+1,b+1),x-a),y-b);
        }
        public float HeightMeters(Vector2 uv)
        {
            var p=SourceUV(uv);float gx=Mathf.Clamp01(p.x)*(Settings.widthPixels-1),gy=Mathf.Clamp01(p.y)*(Settings.heightPixels-1),h=Bilinear(gx,gy);
            // C1 edge continuation: match boundary slope, then ease to a flat apron over500m.
            float dx=(p.x-Mathf.Clamp01(p.x))*Settings.widthMeters,dy=(p.y-Mathf.Clamp01(p.y))*Settings.heightMeters;
            if(dx!=0){float slope=(Bilinear(Mathf.Min(gx+1,Settings.widthPixels-1),gy)-Bilinear(Mathf.Max(gx-1,0),gy))/(Settings.widthMeters/(Settings.widthPixels-1));h+=slope*Mathf.Sign(dx)*500*(1-Mathf.Exp(-Mathf.Abs(dx)/500));}
            if(dy!=0){float slope=(Bilinear(gx,Mathf.Min(gy+1,Settings.heightPixels-1))-Bilinear(gx,Mathf.Max(gy-1,0)))/(Settings.heightMeters/(Settings.heightPixels-1));h+=slope*Mathf.Sign(dy)*500*(1-Mathf.Exp(-Mathf.Abs(dy)/500));}
            return Mathf.Clamp(h,0,Settings.heightRange);
        }
        public string Summary=>$"{Settings.dataset}\n{Settings.widthPixels} × {Settings.heightPixels} pixels · {Settings.widthMeters/1000:0.###} × {Settings.heightMeters/1000:0.###} km footprint\n32.768 km square · 16 × 16 districts · orange outline = imported footprint\nTerrain {Settings.actualMin:0.##} to {Settings.actualMax:0.##} m (encoding {Settings.blackMeters:0.##}…{Settings.whiteMeters:0.##}) · sea {Settings.sourceSea:0.##} m · game offset +{Settings.verticalOffset:0.##} m\nWARNINGS: {Settings.waterAboveSea:N0} water pixels above sea; {Settings.landBelowSea:N0} land pixels below sea.\nRepair Shoreline {(Settings.repairShoreline?"ON (derived, two-pixel shoreline band)":"OFF: original data preserved")}\n{Settings.provenance}";
        public bool AnyIn(Rect region,bool shoreline=false)
        {
            // Conservative grid scan in source space; used only when preparing district water renderers.
            var a=SourceUV(region.min);var b=SourceUV(region.max);var c=SourceUV(new Vector2(region.xMin,region.yMax));var d=SourceUV(new Vector2(region.xMax,region.yMin));
            float minX=Mathf.Min(a.x,b.x,c.x,d.x),maxX=Mathf.Max(a.x,b.x,c.x,d.x),minY=Mathf.Min(a.y,b.y,c.y,d.y),maxY=Mathf.Max(a.y,b.y,c.y,d.y);
            int w=Settings.widthPixels,h=Settings.heightPixels;int x0=Mathf.Clamp(Mathf.FloorToInt(minX*(w-1)),0,w-1),x1=Mathf.Clamp(Mathf.CeilToInt(maxX*(w-1)),0,w-1),y0=Mathf.Clamp(Mathf.FloorToInt(minY*(h-1)),0,h-1),y1=Mathf.Clamp(Mathf.CeilToInt(maxY*(h-1)),0,h-1);
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){int i=y*w+x;if(!shoreline&&Water[i]==1)return true;if(shoreline&&((x+1<w&&Water[i]!=Water[i+1])||(y+1<h&&Water[i]!=Water[i+w])))return true;}return false;
        }
        public void Bind(PrototypeRegionData data)
        {
            data.Import=this;foreach(var body in data.WaterBodies??Array.Empty<RegionWaterBody>())if(body.id=="imported-tidal")body.importedTidal=this;
        }
        public SavedRegionSnapshot Generate(string name,RegionCreationSettings source,CancellationToken cancel=default)
        {
            var recipe=JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(source));recipe.terrainImport=Settings;recipe.districtGrid=16;recipe.rivers=recipe.streams=0;recipe.terrainShape=RegionTerrainShape.Land;
            var heights=new float[513,513];var dist=new float[513,513];var wet=new bool[513,513];bool hasWater=Array.IndexOf(Water,(byte)1)>=0;
            for(int z=0;z<513;z++){cancel.ThrowIfCancellationRequested();for(int x=0;x<513;x++){var uv=new Vector2(x/512f,z/512f);heights[z,x]=HeightMeters(uv)/Settings.heightRange;hasWater|=wet[z,x]=TidalAt(uv);dist[z,x]=PrototypeRegionData.Side;}}
            const float step=PrototypeRegionData.Side/512;
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)if((x>0&&wet[z,x]!=wet[z,x-1])||(z>0&&wet[z,x]!=wet[z-1,x]))dist[z,x]=0;
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){if(x>0)dist[z,x]=Mathf.Min(dist[z,x],dist[z,x-1]+step);if(z>0)dist[z,x]=Mathf.Min(dist[z,x],dist[z-1,x]+step);}
            for(int z=512;z>=0;z--)for(int x=512;x>=0;x--){if(x<512)dist[z,x]=Mathf.Min(dist[z,x],dist[z,x+1]+step);if(z<512)dist[z,x]=Mathf.Min(dist[z,x],dist[z+1,x]+step);}
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)if(wet[z,x])dist[z,x]=-dist[z,x];
            var bodies=hasWater?new[]{new RegionWaterBody{id="imported-tidal",kind=WaterBodyKind.Ocean,outline=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},hasSurfaceLevel=true,surfaceLevelNormalized=Settings.GameSea/Settings.heightRange,appearanceProfile="coastal-ocean",importedTidal=this}}:Array.Empty<RegionWaterBody>();
            var data=new PrototypeRegionData(recipe.seed,heights,dist){WaterBodies=bodies};Bind(data);data.Biomes=new RegionBiomeMap(recipe,data);
            return new SavedRegionSnapshot{data=data,info=new SavedRegionInfo{id=Guid.NewGuid().ToString("N"),name=name,seed=recipe.seed,size=2,district=-1,creation=recipe,terrainHeightRange=Settings.heightRange,waterBodies=bodies,waterless=!hasWater,meadow=true,animals=recipe.animals,districts=data.Biomes.DescribeDistricts(),resources=ResourceSurvey.Generate(data,recipe,2,null),nativeCamps=NativeCampPlanner.Create(data,recipe,2),savedUtc=DateTime.UtcNow.ToString("O")}};
        }
        public static void WritePayload(BinaryWriter writer,TerrainImportData data)
        {
            writer.Write(0x54494d31);writer.Write(data.Heights.Length);foreach(ushort h in data.Heights)writer.Write(h);writer.Write(data.Water);
        }
        public static void ReadPayload(BinaryReader reader,SavedRegionSnapshot snapshot)
        {
            var settings=snapshot.info.creation?.terrainImport;if(settings==null||!settings.Active)return;settings.Validate();
            if(reader.ReadInt32()!=0x54494d31)throw new InvalidDataException("Imported region payload is missing.");int count=reader.ReadInt32();if(count!=(long)settings.widthPixels*settings.heightPixels)throw new InvalidDataException("Invalid imported payload length.");
            var heights=new ushort[count];for(int i=0;i<count;i++)heights[i]=reader.ReadUInt16();var water=reader.ReadBytes(count);if(water.Length!=count)throw new EndOfStreamException();foreach(byte label in water)if(label>1)throw new InvalidDataException("Unsupported saved water label.");new TerrainImportData(settings,heights,water).Bind(snapshot.data);
        }
    }
}
