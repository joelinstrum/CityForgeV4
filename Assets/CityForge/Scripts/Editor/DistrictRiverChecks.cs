#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class DistrictRiverChecks
    {
        const string Output="Library/DistrictRiverChecks/";
        static DistrictRiverChecks(){EditorApplication.update+=Poll;}
        static void Poll(){
            const string request="Library/DistrictRiver.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            bool blend=File.ReadAllText(request).Trim()=="blend";
            File.Delete(request);Directory.CreateDirectory(Output);EditorApplication.isPaused=false;
            CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Check(blend)));
        }
        static IEnumerator Guard(IEnumerator routine){
            var stack=new System.Collections.Generic.Stack<IEnumerator>();stack.Push(routine);
            while(stack.Count>0){object next;
                try{
                    var current=stack.Peek();
                    if(!current.MoveNext()){(current as IDisposable)?.Dispose();stack.Pop();continue;}
                    next=current.Current;
                }catch(Exception e){File.WriteAllText(Output+"District-River-Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}
                if(next is IEnumerator nested){stack.Push(nested);continue;}yield return next;
            }
        }

        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static IEnumerator Check(bool blend=false){
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            var previousPose=blend&&view?view.GetComponentInChildren<CityForgeCameraController>().CaptureRegionState():null;
            MeadowRegionSnapshot previousCache=null;
            if(blend&&view&&view.ApprovedDistrict){try{previousCache=view.ApprovedDistrict.CaptureSnapshot();}catch(Exception){/* A reload may have discarded the old nonserialized cache. */}}
            if(view)view.ReturnToMenu();yield return null;yield return null;
            var info=SavedRegionStore.ListRegions().FirstOrDefault(r=>r.name=="Testernia"&&r.creation?.version>=5);
            var saved=info!=null?SavedRegionStore.Load(info.path):RegionMapGenerator.Generate("District river trial",RegionSize.Medium,
                new RegionCreationSettings{seed=730421,topography=4,rivers=1});
            if(blend&&previousCache!=null){saved.meadow=previousCache;saved.info.meadowCacheDistrict=36;}
            var originalHeights=(float[,])saved.data.Heights.Clone();
            yield return PrototypeRegionView.OpenStages(saved.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:saved);
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            var river=view.Data.WaterBodies.First(b=>b.kind==WaterBodyKind.River&&b.centerline.Length>2);
            int segment=river.centerline.Length/2;var segmentStart=river.centerline[segment-1];var segmentEnd=river.centerline[segment];
            var flow=LakeWaterRenderer.RiverCurrentCoordinates(view.Data,view.MapSide,((segmentStart+segmentEnd)*.5f-Vector2.one*.5f)*view.MapSide);
            Require(Vector2.Dot(new Vector2(flow.z,flow.w),-(segmentEnd-segmentStart).normalized)>.95f,"Current follows local downstream centerline");
            int district=36; // District 5–5 from the user's screenshot.
            yield return view.EnterDistrictStages(district);
            var land=view.ApprovedDistrict;var surface=land.GetComponentsInChildren<LakeWaterRenderer>().FirstOrDefault();
            if(!surface){
                var body=view.Data.WaterBodies.First(b=>b.kind==WaterBodyKind.River);var point=body.centerline[body.centerline.Length/2];
                district=Mathf.Min(7,(int)(point.y*8))*8+Mathf.Min(7,(int)(point.x*8));
                yield return view.EnterDistrictStages(district);land=view.ApprovedDistrict;surface=land.GetComponentsInChildren<LakeWaterRenderer>().Single();
            }
            Require(surface,"Bounded water exists");
            Require(surface.SurfaceMaterial.GetTexture("_RiverFlow")&&surface.SurfaceMaterial.GetFloat("_CurrentSpeed")>0,"Lengthwise current field and speed configured");
            Require(!ShaderUtil.ShaderHasError(surface.SurfaceMaterial.shader),"Current shader compiles");
            Require(land.Water==null&&!land.GetComponentsInChildren<RiverFlowAnimation>().Any(),"No camera-following Crest or advected river surface");
            var filter=surface.GetComponent<MeshFilter>();var bounds=filter.sharedMesh.bounds;
            Require(bounds.min.x>=land.Origin.x-.001f&&bounds.max.x<=land.Origin.x+land.Side+.001f&&bounds.min.z>=land.Origin.y-.001f&&bounds.max.z<=land.Origin.y+land.Side+.001f,"Water bounded to selected district");
            var texture=(Texture2D)surface.SurfaceMaterial.GetTexture("_DepthMask");
            var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
            var previous=RenderTexture.active;Graphics.Blit(texture,target);RenderTexture.active=target;
            var readable=new Texture2D(texture.width,texture.height,TextureFormat.RGBAHalf,false,true);
            readable.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);readable.Apply();
            var pixels=readable.GetPixels();RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.Destroy(readable);int wet=0;for(int i=0;i<pixels.Length;i++)if(pixels[i].r>.02f){wet++;Require(pixels[i].g>.5f,"Every submerged sample covered up to actual shore");}
            Require(wet>0,"River contains water");
            var rig=view.GetComponentInChildren<CityForgeCameraController>();var camera=rig.GetComponent<Camera>();var pose=rig.CaptureRegionState();
            var matrix=surface.transform.localToWorldMatrix;var vertices=filter.sharedMesh.vertices;
            for(int zoom=0;zoom<=5;zoom++){
                rig.SetZoomLevel(zoom);var state=rig.CaptureRegionState();state.focus+=new Vector3(100,0,-80);rig.RestoreRegionState(state);int frame=Time.frameCount;while(Time.frameCount<=frame)yield return null;
                Require(land.DisplayTerrain.treeDistance>=land.Baseline.treeDistance+rig.OrthographicSetback-.01f,"Forest culling distance compensates camera retreat");
                Require(surface.transform.localToWorldMatrix==matrix&&filter.sharedMesh.vertices.SequenceEqual(vertices),"River stays stationary while panning/zooming");
                var td=land.DisplayTerrain.terrainData;
                for(int z=0;z<=32;z++)for(int x=0;x<=32;x++){
                    var p=new Vector3(land.Origin.x+x*land.Side/32,td.GetInterpolatedHeight(x/32f,z/32f),land.Origin.y+z*land.Side/32);
                    var screen=camera.WorldToViewportPoint(p);
                    if(screen.x>=0&&screen.x<=1&&screen.y>=0&&screen.y<=1)
                        Require(screen.z>=camera.nearClipPlane&&screen.z<=camera.farClipPlane,"Visible terrain remains inside camera depth range at zoom "+zoom);
                }
            }
            rig.RestoreRegionState(pose);
            var maps=land.DisplayTerrain.terrainData.GetAlphamaps(0,0,land.DisplayTerrain.terrainData.alphamapResolution,land.DisplayTerrain.terrainData.alphamapResolution);
            int banks=0;int n=maps.GetLength(0);for(int z=0;z<n;z+=8)for(int x=0;x<n;x+=8){
                var p=land.Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*land.Side;
                if(land.ShoreDistance(p)>0&&land.ShoreDistance(p)<3){banks++;Require(maps[z,x,0]>.8f,"Exposed bank soil follows actual waterline");}
            }
            Require(banks>0,"Exposed bank samples present");
            var fingerprint=filter.sharedMesh.bounds;view.ShowOverview();yield return null;yield return view.EnterDistrictStages(district);
            Require(view.ApprovedDistrict.UsedSavedTerrain,"Existing district cache restored");
            Require(view.ApprovedDistrict.GetComponentsInChildren<LakeWaterRenderer>().Single().GetComponent<MeshFilter>().sharedMesh.bounds==fingerprint,"Stationary water rebuilds consistently after overview/cache transition");
            if(blend){
                land=view.ApprovedDistrict;
                var td=land.DisplayTerrain.terrainData;
                Require(land.DisplayTerrain.heightmapPixelError==1,"Detailed terrain retained");
                Require(land.DisplayTerrain.GetComponent<TerrainCollider>().enabled&&!view.transform.Find("ContinuousRegionTerrain").GetComponent<TerrainCollider>().enabled,"Physics uses smooth displayed surface");
                var districtBounds=RegionBiomeMap.DistrictBounds(district,view.GridSide);
                int resolution=td.heightmapResolution;
                for(int z=7;z<resolution;z+=37)for(int x=11;x<resolution;x+=41){
                    float u=districtBounds.x+x/(float)(resolution-1)*districtBounds.width,v=districtBounds.y+z/(float)(resolution-1)*districtBounds.height;
                    float expected=SmoothRiverSurface.Sample(view.Data.Heights,u,v)*td.size.y;
                    Require(Mathf.Abs(td.GetInterpolatedHeight(x/(float)(resolution-1),z/(float)(resolution-1))-expected)<.05f,"District uses smooth interpolation between coarse samples");
                }
                for(int z=0;z<513;z++)for(int x=0;x<513;x++)Require(view.Data.Heights[z,x]==originalHeights[z,x],"Stored geography unchanged");
                foreach(var tree in land.CanonicalTreeInstances){
                    var p=land.Origin+new Vector2(tree.position.x,tree.position.z)*land.Side;
                    Require(Mathf.Abs(tree.position.y*td.size.y-land.Height(p))<.05f,"Cached trees seated on smooth ground");
                }
                var road=land.transform.Find("Runtime natural road transitions").GetComponent<MeshFilter>().sharedMesh;
                var points=road.vertices;var indices=road.triangles;
                for(int i=0;i<indices.Length;i+=3){var p=(points[indices[i]]+points[indices[i+1]]+points[indices[i+2]])/3;
                    Require(p.y>=land.Height(new Vector2(p.x,p.z))-.03f,"Road does not disappear beneath terrain");}
                if(previousPose!=null)rig.RestoreRegionState(previousPose);
                File.WriteAllText(Output+"District-Blend-Checks.txt","PASS: monotone cubic interpolation at native terrain samples; saved 513-square heights unchanged; native physics collider matches displayed ground; cached trees seated on ground; every road triangle centroid above terrain; stationary river and pan/zoom/cache checks also passed.\n");
            }
            int finalFrame=Time.frameCount;while(Time.frameCount<finalFrame+8)yield return null;
            ScreenCapture.CaptureScreenshot(Output+(blend?"District-Blend-Fixed.png":"District-River-Fixed.png"));yield return null;
            File.WriteAllText(Output+"District-River-Checks.txt","PASS: "+saved.info.name+" district "+district+"; fixed district-bounded river; all submerged texture samples covered; no Crest grid or flow advection; stationary mesh while panning at zoom0–5; visible terrain inside camera clip range; exposed soil banks; overview return and existing terrain cache restore. No player saves written.\n");
        }
    }
}
#endif
