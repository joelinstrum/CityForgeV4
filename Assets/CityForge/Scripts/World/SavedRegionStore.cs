using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CityForge.World
{
    [Serializable] public sealed class RegionCameraState
    {
        public Vector3 focus;public float pitch,yaw,distance,size;public int zoom;public bool lowerView;public bool hasViewTilt;public float viewTilt;
    }
    [Serializable] public sealed class SavedRegionInfo
    {
        public int schema=1,seed,size,district=3,baselineVersion;public int season;public bool meadow=true;public bool waterless;public bool coastline;public int coastVersion;public bool minimalRiverCrest;public bool stillWater;public int riverBedVersion;
        // Missing/zero values in older saves migrate to the accepted lake/pond renderer.
        public int lakeWaterVersion=1;
        public bool animals;
        // Also supports climate on legacy coastal maps without inventing a creation recipe.
        public RegionClimate climate;
        public RegionCreationSettings creation;
        public RegionDistrictInfo[] districts;
        public NativeCampRecord[] nativeCamps;
        public CombinedDistrictGroup[] combinedDistricts;
        public int meadowCacheDistrict=-1;
        public float terrainHeightRange;
        public RegionWaterBody[] waterBodies;
        public string id,name,savedUtc;public RegionCameraState camera;
        [NonSerialized] public string path;
    }
    public sealed class MeadowRegionSnapshot
    {
        public int version;public Vector2[] road;public TreeInstance[] trees;public float[,,] alphamaps;
    }
    public sealed class SavedRegionSnapshot
    {
        public SavedRegionInfo info;public PrototypeRegionData data;public MeadowRegionSnapshot meadow;
    }
    /// <summary>Versioned map snapshots. Generated GPU resources are rebuilt, not serialized.</summary>
    public static class SavedRegionStore
    {
        const string Magic="CITYFORGE_REGION_1";
        public const string TestId="meadow-test-valley";
        public static string DirectoryPath=>Path.Combine(Application.persistentDataPath,"Regions");
        public static void EnsureTestRegion()
        {
            Directory.CreateDirectory(DirectoryPath);
            var path=Path.Combine(DirectoryPath,TestId+".cfregion");
            if(File.Exists(path)||File.Exists(Path.Combine(DirectoryPath,"Deleted",TestId+".removed")))return;
            var preset=Resources.Load<TextAsset>("Regions/MeadowTestValley");
            if(preset){var temp=path+".tmp";File.WriteAllBytes(temp,preset.bytes);File.Move(temp,path);}
        }
        public static List<SavedRegionInfo> ListRegions()
        {
            EnsureTestRegion();var result=new List<SavedRegionInfo>();
            foreach(var path in Directory.GetFiles(DirectoryPath,"*.cfregion")){
                try{using var stream=File.OpenRead(path);using var gzip=new GZipStream(stream,CompressionMode.Decompress);using var reader=new BinaryReader(gzip);var info=ReadInfo(reader);info.path=path;result.Add(info);}
                catch(Exception e) when(e is IOException||e is InvalidDataException||e is ArgumentException){Debug.LogWarning("Skipped unreadable saved region: "+Path.GetFileName(path));}
            }
            return result.OrderByDescending(x=>x.savedUtc).ToList();
        }
        /// <summary>Remove a listed save, preserving its file and backup outside the active list.</summary>
        public static void Delete(SavedRegionInfo region)
        {
            if(region==null||string.IsNullOrEmpty(region.path))throw new ArgumentException("Select a saved region.");
            var path=Path.GetFullPath(region.path);
            if(Path.GetDirectoryName(path)!=Path.GetFullPath(DirectoryPath)||Path.GetExtension(path)!=".cfregion")
                throw new ArgumentException("The region is outside the saved region folder.");
            if(!File.Exists(path))throw new FileNotFoundException("This region has already been removed.",path);
            var archive=Path.Combine(DirectoryPath,"Deleted",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(archive);
            var destination=Path.Combine(archive,Path.GetFileName(path));bool movedBackup=false;
            try{
                File.Move(path,destination);
                if(File.Exists(path+".bak")){File.Move(path+".bak",destination+".bak");movedBackup=true;}
                // Do not silently reinstall the bundled test region after the user deletes it.
                if(Path.GetFileNameWithoutExtension(path)==TestId)File.WriteAllText(Path.Combine(DirectoryPath,"Deleted",TestId+".removed"),"");
            }catch{
                if(movedBackup)File.Move(destination+".bak",path+".bak");
                if(File.Exists(destination))File.Move(destination,path);
                throw;
            }
        }
        public static SavedRegionSnapshot Capture(PrototypeRegionView view,string id=null,string name=null)
        {
            var baseline=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
            var info=new SavedRegionInfo{animals=view.AnimalsEnabled,id=id??view.SavedRegionId??Guid.NewGuid().ToString("N"),name=name??view.RegionName,season=(int)view.Season,terrainHeightRange=view.TerrainHeightRange,seed=view.Data.Seed,size=(int)view.Size,district=view.SelectedDistrict,meadow=view.PrefersMeadow,waterBodies=view.Data.WaterBodies,waterless=view.Waterless,coastline=view.Coastline,coastVersion=view.CoastVersion,minimalRiverCrest=view.MinimalRiverCrest,stillWater=false,lakeWaterVersion=LakeWaterRenderer.UsesCalmWater(view)?1:-1,riverBedVersion=view.RiverBedVersion,savedUtc=DateTime.UtcNow.ToString("O"),baselineVersion=baseline?baseline.generationVersion:0,camera=view.GetComponentInChildren<CameraSystem.CityForgeCameraController>().CaptureRegionState()};
            info.climate=view.Climate;info.creation=view.CreationSettings;info.districts=view.Districts;info.combinedDistricts=view.CombinedDistricts;info.nativeCamps=view.NativeCamps;
            var pilot=view.ApprovedDistrict;info.meadowCacheDistrict=pilot?pilot.DistrictIndex:view.LoadedMeadowDistrict;
            return new SavedRegionSnapshot{info=info,data=view.Data,meadow=pilot?pilot.CaptureSnapshot():view.LoadedMeadow};
        }
        public static string Save(SavedRegionSnapshot snapshot)
        {
            Directory.CreateDirectory(DirectoryPath);
            // IDs are names, never paths supplied by a save file.
            if(string.IsNullOrEmpty(snapshot.info.id)||snapshot.info.id.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'))throw new InvalidDataException("Invalid region ID.");
            var path=Path.Combine(DirectoryPath,snapshot.info.id+".cfregion");Write(snapshot,path);return path;
        }
        public static void Write(SavedRegionSnapshot snapshot,string path)
        {
            DistrictConnections.Validate(snapshot.info.combinedDistricts,snapshot.info.creation?.GridSide??2);
            NativeCampPlanner.Validate(snapshot.info.nativeCamps,snapshot.info.creation?.GridSide??2,NativeCampPlanner.MapSide(snapshot.info.size,snapshot.info.creation?.GridSide??2));
            string temp=path+".tmp";Directory.CreateDirectory(Path.GetDirectoryName(path));
            try{
                using(var stream=File.Create(temp))using(var gzip=new GZipStream(stream,System.IO.Compression.CompressionLevel.Fastest))using(var writer=new BinaryWriter(gzip)){
                    writer.Write(Magic);var metadata=JsonUtility.ToJson(snapshot.info);
                    // Unity serializes a null inline class as a default object. Preserve legacy
                    // maps' absent recipe so saving climate cannot change their grid or scale.
                    if(snapshot.info.creation==null)metadata=System.Text.RegularExpressions.Regex.Replace(metadata,"\"creation\"\\s*:\\s*\\{[^{}]*\\}","\"creation\":null");
                    var json=Encoding.UTF8.GetBytes(metadata);if(json.Length>1048576)throw new InvalidDataException("Region metadata exceeds the supported size.");writer.Write(json.Length);writer.Write(json);
                    WriteFloats(writer,snapshot.data.Heights);WriteFloats(writer,snapshot.data.WaterDistance);
                    writer.Write(snapshot.meadow!=null);
                    if(snapshot.meadow!=null){var m=snapshot.meadow;writer.Write(m.version);writer.Write(m.road.Length);foreach(var p in m.road){writer.Write(p.x);writer.Write(p.y);}
                        writer.Write(m.trees.Length);foreach(var t in m.trees){writer.Write(t.position.x);writer.Write(t.position.y);writer.Write(t.position.z);writer.Write(t.widthScale);writer.Write(t.heightScale);writer.Write(t.rotation);writer.Write(t.prototypeIndex);WriteColor(writer,t.color);WriteColor(writer,t.lightmapColor);}
                        writer.Write(m.alphamaps.GetLength(0));writer.Write(m.alphamaps.GetLength(2));WriteFloats(writer,m.alphamaps);
                    }
                }
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            }finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public static SavedRegionSnapshot Load(string path)
        {
            using var stream=File.OpenRead(path);using var gzip=new GZipStream(stream,CompressionMode.Decompress);using var reader=new BinaryReader(gzip);
            var info=ReadInfo(reader);info.path=path;
            var heights=new float[513,513];var water=new float[513,513];ReadFloats(reader,heights);ReadFloats(reader,water);
            foreach(float h in heights)if(float.IsNaN(h)||float.IsInfinity(h)||h<0||h>1)throw new InvalidDataException("Invalid terrain heights.");
            foreach(float d in water)if(float.IsNaN(d)||float.IsInfinity(d))throw new InvalidDataException("Invalid water distances.");
            var result=new SavedRegionSnapshot{info=info,data=new PrototypeRegionData(info.seed,heights,water){WaterBodies=info.waterBodies}};
            if(reader.ReadBoolean()){
                var m=new MeadowRegionSnapshot{version=reader.ReadInt32()};int roads=Count(reader,2,1024);m.road=new Vector2[roads];for(int i=0;i<roads;i++){m.road[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());if(!Finite(m.road[i].x)||!Finite(m.road[i].y)||(i>0&&m.road[i]==m.road[i-1]))throw new InvalidDataException("Invalid saved road.");}
                int trees=Count(reader,0,200000);m.trees=new TreeInstance[trees];for(int i=0;i<trees;i++){
                    var t=new TreeInstance{position=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()),widthScale=reader.ReadSingle(),heightScale=reader.ReadSingle(),rotation=reader.ReadSingle(),prototypeIndex=reader.ReadInt32(),color=ReadColor(reader),lightmapColor=ReadColor(reader)};
                    if(!Finite(t.position.x)||!Finite(t.position.y)||!Finite(t.position.z)||t.position.x<0||t.position.x>1||t.position.z<0||t.position.z>1||!Finite(t.widthScale)||!Finite(t.heightScale)||!Finite(t.rotation)||t.widthScale<=0||t.heightScale<=0||!ForestTreeMix.IsValidPrototype(t.prototypeIndex,info.id==HillsMountainTestRegion.Id))throw new InvalidDataException("Invalid saved tree.");m.trees[i]=t;
                }
                int n=Count(reader,32,4096),layers=Count(reader,1,8);m.alphamaps=new float[n,n,layers];ReadFloats(reader,m.alphamaps);foreach(float value in m.alphamaps)if(!Finite(value)||value<0||value>1)throw new InvalidDataException("Invalid saved ground cover.");result.meadow=m;
            }
            return result;
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static SavedRegionInfo ReadInfo(BinaryReader reader)
        {
            if(reader.ReadString()!=Magic)throw new InvalidDataException("Unsupported saved region.");int length=Count(reader,2,1048576);var bytes=reader.ReadBytes(length);if(bytes.Length!=length)throw new EndOfStreamException();
            var json=Encoding.UTF8.GetString(bytes);var info=JsonUtility.FromJson<SavedRegionInfo>(json);
            // JsonUtility can instantiate a missing nested recipe with current defaults.
            // An old save without a recipe must retain its original grid/vertical scale.
            if(info!=null&&(!json.Contains("\"creation\"")||System.Text.RegularExpressions.Regex.IsMatch(json,"\"creation\"\\s*:\\s*null")))info.creation=null;
            if(info!=null&&!json.Contains("\"meadowCacheDistrict\""))info.meadowCacheDistrict=info.district>=0?info.district:3;if(info==null||info.schema!=1||(int)info.climate<0||(int)info.climate>1||info.season<0||info.season>2||info.size<0||info.size>2||!Finite(info.terrainHeightRange)||info.terrainHeightRange<0||info.terrainHeightRange>10000||info.district< -1||info.district>=(info.creation?.GridSide??2)*(info.creation?.GridSide??2)||string.IsNullOrWhiteSpace(info.name))throw new InvalidDataException("Unsupported region metadata.");if(info.creation!=null)info.creation.Validate();DistrictConnections.Validate(info.combinedDistricts,info.creation?.GridSide??2);NativeCampPlanner.Validate(info.nativeCamps,info.creation?.GridSide??2,NativeCampPlanner.MapSide(info.size,info.creation?.GridSide??2));return info;
        }
        static int Count(BinaryReader reader,int min,int max){int count=reader.ReadInt32();if(count<min||count>max)throw new InvalidDataException("Invalid snapshot dimensions.");return count;}
        static void WriteFloats(BinaryWriter writer,Array values){var buffer=new byte[Buffer.ByteLength(values)];Buffer.BlockCopy(values,0,buffer,0,buffer.Length);writer.Write(buffer);}
        static void ReadFloats(BinaryReader reader,Array values){var bytes=reader.ReadBytes(Buffer.ByteLength(values));if(bytes.Length!=Buffer.ByteLength(values))throw new EndOfStreamException();Buffer.BlockCopy(bytes,0,values,0,bytes.Length);}
        static void WriteColor(BinaryWriter w,Color32 c){w.Write(c.r);w.Write(c.g);w.Write(c.b);w.Write(c.a);}
        static Color32 ReadColor(BinaryReader r)=>new Color32(r.ReadByte(),r.ReadByte(),r.ReadByte(),r.ReadByte());
    }
}
