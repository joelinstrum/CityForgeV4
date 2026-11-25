using System;
using System.Linq;
using UnityEngine;

namespace CityForge.Lots
{
    public readonly struct SignEligibility
    {
        public readonly bool Eligible;
        public readonly string Reason;
        internal SignEligibility(bool eligible,string reason){Eligible=eligible;Reason=reason;}
    }
    public static class BuildingSignCompatibility
    {
        const float Tolerance=.001f;
        public static string KindLabel(SignMountKind kind)=>kind==SignMountKind.FrontFacing?"Front-facing":"Projecting blade";
        static bool Kind(SignMountKind k)=>k is SignMountKind.FrontFacing or SignMountKind.ProjectingBlade;
        static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
        static bool Positive(Vector3 v)=>Finite(v)&&v.x>0&&v.y>0&&v.z>0;
        static bool Id(string id)=>!string.IsNullOrEmpty(id)&&id.Length<=96&&id.All(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_');
        public static bool ValidMount(BuildingSignMount mount)
        {
            if(mount==null||!Id(mount.id)||!Id(mount.mountingInterface)||!Kind(mount.kind)||!Positive(mount.maxSignSize)||
                !Finite(mount.position)||!Finite(mount.eulerAngles)||!Finite(mount.wallOutward)||
                !float.IsFinite(mount.maxProjection)||mount.maxProjection<=0||!float.IsFinite(mount.heightAboveWalkway)||mount.heightAboveWalkway<0||
                mount.retainedHardwarePaths==null||mount.retainedHardwarePaths.Any(string.IsNullOrEmpty)||mount.allowedSignIds==null||mount.allowedSignIds.Any(s=>!Id(s))||mount.allowedSignIds.Distinct().Count()!=mount.allowedSignIds.Length||
                !string.IsNullOrEmpty(mount.defaultSignId)&&(!Id(mount.defaultSignId)||mount.allowedSignIds.Length>0&&!mount.allowedSignIds.Contains(mount.defaultSignId)))return false;
            if(Mathf.Abs(mount.wallOutward.magnitude-1)>.01f)return false;
            var outward=Quaternion.Euler(mount.eulerAngles)*(mount.kind==SignMountKind.FrontFacing?Vector3.forward:Vector3.right);
            return Vector3.Dot(outward,mount.wallOutward)>.99f && Mathf.Abs(mount.wallOutward.y)<.01f &&
                Vector3.Dot(Quaternion.Euler(mount.eulerAngles)*Vector3.up,Vector3.up)>.99f;
        }
        public static SignEligibility Evaluate(BuildingSignMount mount,BuildingSignAsset sign)
        {
            SignEligibility No(string reason)=>new(false,reason);
            if(!ValidMount(mount))return No("Attachment point metadata is incomplete or misaligned.");
            if(!mount.clearanceReviewed)return No("Attachment clearance has not been reviewed.");
            if(!sign||!Id(sign.id)||!Id(sign.mountingInterface)||!Kind(sign.kind)||!Positive(sign.size)||
                !float.IsFinite(sign.outwardProjection)||sign.outwardProjection<=0||!float.IsFinite(sign.minimumGroundClearance)||sign.minimumGroundClearance<0)
                return No("Sign metadata is incomplete.");
            if(sign.outwardProjection>(sign.kind==SignMountKind.ProjectingBlade?sign.size.x:sign.size.z)+Tolerance)
                return No("Sign projection exceeds its declared geometry envelope.");
            if(sign.kind!=mount.kind)return No("Requires a "+KindLabel(sign.kind).ToLowerInvariant()+" attachment point.");
            if(sign.kind==SignMountKind.ProjectingBlade&&!sign.doubleSided)return No("Blade signs must be readable on both sides.");
            if(sign.mountingInterface!=mount.mountingInterface)return No("Mounting hardware does not match.");
            if(mount.allowedSignIds.Length>0&&!mount.allowedSignIds.Contains(sign.id))return No("This attachment point is reserved for other signs.");
            if(sign.size.x>mount.maxSignSize.x+Tolerance||sign.size.y>mount.maxSignSize.y+Tolerance||sign.size.z>mount.maxSignSize.z+Tolerance)
                return No("Sign is larger than the available mounting space.");
            if(sign.outwardProjection>mount.maxProjection+Tolerance)return No("Sign projects beyond the reviewed clearance.");
            if(sign.minimumGroundClearance>mount.heightAboveWalkway+Tolerance)return No("Not enough clearance above the walkway.");
            if(!sign.nativeReviewed||!sign.nearPrefab||!sign.mediumPrefab)return No("Native sign preparation is not complete.");
            if(!sign.nearPrefab.GetComponentInChildren<Renderer>(true)||!sign.mediumPrefab.GetComponentInChildren<Renderer>(true))
                return No("Native sign geometry is missing.");
            if(sign.nearPrefab.GetComponentInChildren<LODGroup>(true)||sign.mediumPrefab.GetComponentInChildren<LODGroup>(true))
                return No("Sign tiers must use the building's detail control.");
            return new(true,"Compatible with this attachment point.");
        }
        public static bool TryProfile(LotAssetCatalogEntry asset,out BuildingSignMountProfile profile,out string reason)
        {
            profile=null;
            if(asset==null||asset.category!="Buildings"||!asset.prefab){reason="No building is selected.";return false;}
            var near=asset.prefab.GetComponentsInChildren<BuildingSignMountProfile>(true);
            if(near.Length!=1){reason="This building has no prepared sign attachment profile.";return false;}
            var p=near[0];
            if(p.mediumTier||!ValidProfile(p)){reason="The sign attachment profile needs preparation.";return false;}
            if(!asset.distantPrefab){reason="The reduced building tier needs matching sign mounts.";return false;}
            var medium=asset.distantPrefab.GetComponentsInChildren<BuildingSignMountProfile>(true);
            if(medium.Length!=1||!medium[0].mediumTier||!ValidProfile(medium[0])||!Match(p,medium[0])){reason="Sign mounts do not match between building detail levels.";return false;}
            profile=p;reason="";return true;
        }
        static bool ValidProfile(BuildingSignMountProfile p)=>p.contractVersion==1&&p.mounts!=null&&p.mounts.Length>0&&p.mounts.Length<=16&&
            p.mounts.All(ValidMount)&&p.mounts.Select(m=>m.id).Distinct().Count()==p.mounts.Length&&
            p.mounts.All(m=>string.IsNullOrEmpty(m.originalSignRootPath)?string.IsNullOrEmpty(m.defaultSignId):
                p.transform.Find(m.originalSignRootPath)&&p.transform.Find(m.originalSignRootPath).GetComponentInChildren<Renderer>(true)&&
                m.retainedHardwarePaths.All(path=>p.transform.Find(m.originalSignRootPath).Find(path)));
        static bool Match(BuildingSignMountProfile a,BuildingSignMountProfile b)
        {
            if(a.mounts.Length!=b.mounts.Length)return false;
            foreach(var x in a.mounts)
            {
                var y=b.mounts.FirstOrDefault(m=>m.id==x.id);
                if(y==null||x.kind!=y.kind||x.mountingInterface!=y.mountingInterface||(x.originalSignRootPath??"")!=(y.originalSignRootPath??"")||!x.retainedHardwarePaths.SequenceEqual(y.retainedHardwarePaths)||(x.defaultSignId??"")!=(y.defaultSignId??"")||x.clearanceReviewed!=y.clearanceReviewed||
                    Vector3.Distance(x.position,y.position)>Tolerance||Quaternion.Angle(Quaternion.Euler(x.eulerAngles),Quaternion.Euler(y.eulerAngles))>.1f||
                    Vector3.Distance(x.wallOutward,y.wallOutward)>Tolerance||Vector3.Distance(x.maxSignSize,y.maxSignSize)>Tolerance||
                    Mathf.Abs(x.maxProjection-y.maxProjection)>Tolerance||Mathf.Abs(x.heightAboveWalkway-y.heightAboveWalkway)>Tolerance||
                    !x.allowedSignIds.OrderBy(s=>s,StringComparer.Ordinal).SequenceEqual(y.allowedSignIds.OrderBy(s=>s,StringComparer.Ordinal)))return false;
            }
            return true;
        }
    }
}
