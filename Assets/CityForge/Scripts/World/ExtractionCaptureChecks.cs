#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CityForge.Lots;
using CityForge.World;
using UnityEditor;
using UnityEngine;

namespace CityForge.Editor
{
    public static class ExtractionCaptureChecks
    {
        private static void Require(bool value,string message){if(!value)throw new Exception(message);}
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
        private static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
        private sealed class Fixture
        {
            internal GameObject root;internal TerrainData data;internal PrototypeRegionView view;internal MeadowDistrictLandscape land;internal DistrictLotRecord lot;
            internal MeadowRuntimeBaseline baseline;
            internal Fixture()
            {
                root=new GameObject("Disposable extraction capture");view=root.AddComponent<PrototypeRegionView>();land=root.AddComponent<MeadowDistrictLandscape>();
                data=new TerrainData{heightmapResolution=33,size=new Vector3(200,40,200)};
                var heights=new float[33,33];for(int z=0;z<33;z++)for(int x=0;x<33;x++)heights[z,x]=.25f;data.SetHeights(0,0,heights);
                var terrain=root.AddComponent<Terrain>();terrain.terrainData=data;
                var map=new PrototypeRegionData(1,new float[513,513],new float[513,513]){WaterBodies=Array.Empty<RegionWaterBody>()};
                Set(view,"approvedDistrict",land);Set(view,"<SelectedDistrict>k__BackingField",3);Set(view,"<Data>k__BackingField",map);
                Set(land,"<View>k__BackingField",view);Set(land,"<DisplayTerrain>k__BackingField",terrain);Set(land,"<Side>k__BackingField",200f);Set(land,"terrainData",data);
                baseline=ScriptableObject.CreateInstance<MeadowRuntimeBaseline>();baseline.treePrefabs=new GameObject[11];Set(land,"<Baseline>k__BackingField",baseline);
                lot=new DistrictLotRecord{district=3,center=new Vector2(100,100),definition=new LotDefinition{category="industrial",subcategory="extraction",width=4,length=4}};
                Trees(10);
            }
            internal void Trees(int count)
            {
                var trees=new List<TreeInstance>();
                for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/10;var p=lot.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*35;
                    trees.Add(new TreeInstance{prototypeIndex=3,position=new Vector3(p.x/200,.25f,p.y/200),widthScale=1,heightScale=1});}
                // Neither trees cleared under the lot nor grass count toward the forest requirement.
                trees.Add(new TreeInstance{prototypeIndex=3,position=new Vector3(.5f,.25f,.5f)});
                trees.Add(new TreeInstance{prototypeIndex=0,position=new Vector3(.65f,.25f,.5f)});
                trees.Add(new TreeInstance{prototypeIndex=3,position=new Vector3(.95f,.25f,.5f)});
                Set(land,"fullTreeInstances",trees.ToArray());
            }
            internal void Dispose(){UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(data);UnityEngine.Object.DestroyImmediate(baseline);}
        }
        public static void Run()
        {
            CargoProgressChecks.Run();
            var f=new Fixture();
            try
            {
                Require(LotExtraction.Applies(f.lot.definition),"Extraction category not detected");
                f.Trees(9);string problem=(string)Call(f.view,"ExtractionSiteProblem",f.lot,true);
                Require(problem!=null&&problem.Contains("within reach")&&problem.Contains("9/10"),"Nine-tree placement did not fail with resource message");
                f.Trees(10);Require(Call(f.view,"ExtractionSiteProblem",f.lot,true)==null,"Ten-tree placement did not pass");
                Require(f.land.CountCaptureTrees(new Vector3(100,-100,100),80,_=>false,10)==0,"Sphere behaved as an infinite vertical cylinder");
                var mixed=f.root.AddComponent<MixedDistrictForest>();Set(mixed,"<Ready>k__BackingField",true);Set(mixed,"<PlacementSignature>k__BackingField","empty");
                Set(f.land,"<MixedForest>k__BackingField",mixed);
                Set(f.land,"fullTreeInstances",new[]{new TreeInstance{prototypeIndex=12,position=new Vector3(.65f,.25f,.5f)}});
                Require(f.land.CountCaptureTrees(new Vector3(100,10,100),80,_=>false,10)==1,"Native evergreen was excluded from mixed forest capture");
                Set(f.land,"<MixedForest>k__BackingField",null);UnityEngine.Object.DestroyImmediate(mixed);f.Trees(10);
                f.lot.definition.category="commercial";Require(Call(f.view,"ExtractionSiteProblem",f.lot,true)==null,"Non-extraction lot was restricted");
                f.lot.definition.category="industrial";f.lot.definition.requiredResources.Add(LotRequiredResource.Stone);
                Require(LotExtraction.Resource(f.lot.definition)==LotRequiredResource.Stone,"Existing Stone requirement did not select quarry resource");
                f.lot.definition.resourceCaptureConfigured=true;f.lot.definition.captureResource=LotRequiredResource.Coal;
                Require(LotDefinition.Parse(JsonUtility.ToJson(f.lot.definition)).captureResource==LotRequiredResource.Coal,"Capture resource did not survive save");
                var map=new RegionalResourceData{resolution=2,layers=new[]{new ResourceLayer{type="stone",samples=Convert.ToBase64String(new byte[]{255,255,255,255})}},
                    deposits=new[]{new MineralDeposit{id="iron-1",type="iron",position=new Vector2(.62f,.5f),radius=.03f,abundance=1}},discoveries=Array.Empty<string>()};
                var resources=new ResourceMap(map);
                Require(resources.InCapture("stone",new Vector2(.5f,.5f),.1f),"Surface resource in sphere not detected");
                Require(!resources.InCapture("iron",new Vector2(.5f,.5f),.1f),"Undiscovered deposit was revealed");
                resources.Discover("iron-1");Require(resources.InCapture("iron",new Vector2(.5f,.5f),.1f),"Reachable deposit boundary not detected");
                Require(!resources.InCapture("coal",new Vector2(.5f,.5f),.1f),"Missing resource accepted");
                Require(!resources.InCapture("stone",new Vector2(.5f,.5f),.1f,_=>false),"Resource field ignored 3D sphere filter");
                f.lot.definition.captureResource=LotRequiredResource.Timber;
                Call(f.view,"UpdateExtractionSphere",f.lot);
                var sphere=f.root.transform.Find("Extraction resource capture sphere");Require(sphere&&sphere.localScale==Vector3.one*160,"Capture sphere diameter incorrect");
                var fill=sphere.GetComponent<Renderer>().sharedMaterial;Require(fill.GetColor("_Color").g>fill.GetColor("_Color").r,"Valid capture sphere was not green");
                f.Trees(9);Call(f.view,"EvaluateExtraction",f.lot,true);Call(f.view,"UpdateExtractionSphere",f.lot);
                Require(fill.GetColor("_Color").r>fill.GetColor("_Color").g,"Invalid capture sphere was not red");
                Call(f.view,"CancelLotPlacement",false);Require(!sphere.gameObject.activeSelf,"Capture sphere remained after cancel");
                Debug.Log("EXTRACTION CAPTURE CHECKS PASS: nine/ten trees, footprint/grass/range exclusion, 3D sphere height, generic lot exemption, quarry resource selection, resource save/reload, discovered deposits, green/red preview and cancel cleanup.");
            }
            finally{f.Dispose();}
        }
        public static void Render()
        {
            var f=new Fixture();var owned=new List<GameObject>();var models=new List<GameObject>();
            var target=new RenderTexture(1200,800,24);var previous=RenderTexture.active;
            try
            {
                var camera=new GameObject("Extraction preview camera").AddComponent<Camera>();owned.Add(camera.gameObject);
                camera.orthographic=true;camera.orthographicSize=125;camera.transform.position=new Vector3(240,160,240);camera.transform.LookAt(new Vector3(100,25,100));
                camera.backgroundColor=new Color(.12f,.18f,.2f);camera.clearFlags=CameraClearFlags.SolidColor;camera.targetTexture=target;
                Set(f.view,"lotPlacementCamera",camera);
                var sun=new GameObject("Preview sunlight").AddComponent<Light>();owned.Add(sun.gameObject);sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(45,30,0);
                var grass=new TerrainLayer{diffuseTexture=LotGroundTexture.Find(false,"lush-grass").Texture,tileSize=Vector2.one*8};f.data.terrainLayers=new[]{grass};
                f.root.GetComponent<Terrain>().materialTemplate=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
                var template=MixedForestLibrary.Load().prefabs[0];
                for(int i=0;i<10;i++){float angle=i*Mathf.PI*2/10;var point=f.lot.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*35;
                    var tree=UnityEngine.Object.Instantiate(template,new Vector3(point.x,10,point.y),Quaternion.identity);models.Add(tree);}
                var asset=LotAssetCatalog.Load().Find("v3-building-work-tent");
                if(asset?.prefab){var tent=LotRuntimeLoader.InstantiateObject(new LotObjectDefinition{assetId=asset.id},asset,f.root.transform);tent.transform.position=new Vector3(100,10,100);}
                Directory.CreateDirectory("Library/ExtractionCapturePreview");
                foreach(int count in new[]{10,9})
                {
                    f.Trees(count);models[9].SetActive(count==10);Call(f.view,"EvaluateExtraction",f.lot,true);Call(f.view,"UpdateExtractionSphere",f.lot);
                    camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(1200,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,800),0,0);image.Apply();
                    File.WriteAllBytes("Library/ExtractionCapturePreview/"+(count==10?"Valid-Green":"Invalid-Red")+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                }
                Debug.Log("EXTRACTION SPHERE PREVIEWS rendered green and red with approved tree art on a synthetic flat QA district.");
            }
            finally{RenderTexture.active=previous;foreach(var go in models)UnityEngine.Object.DestroyImmediate(go);foreach(var go in owned)UnityEngine.Object.DestroyImmediate(go);target.Release();UnityEngine.Object.DestroyImmediate(target);f.Dispose();}
        }
    }
}
#endif
