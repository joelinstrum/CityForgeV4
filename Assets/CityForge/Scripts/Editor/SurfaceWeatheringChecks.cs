#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CityForge.World;
using CityForge.Lots;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class SurfaceWeatheringChecks
    {
        static SurfaceWeatheringChecks() { EditorApplication.update += Poll; }
        static void Poll()
        {
            const string request = "Library/SurfaceWeathering.request";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
            string output = File.ReadAllText(request).Trim(); File.Delete(request); Directory.CreateDirectory(output);
            bool asyncCompilation=ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation=false;
            try { Run(output); }
            catch (Exception e) { File.WriteAllText(output + "/checks.txt", "FAIL: " + e); Debug.LogException(e); }
            finally { ShaderUtil.allowAsyncCompilation=asyncCompilation; }
        }
        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Run(string output)
        {
            var log = new List<string>();
            log.Add("Play session preserved: " + EditorApplication.isPlaying);
            foreach (string name in new[] { "GrimeMask", "GutterSediment", "OrganicDebris" })
            {
                string path = "Assets/CityForge/Resources/SurfaceWeathering/Textures/" + name + ".png";
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Require(t && t.width == 1024 && t.height == 1024, "Texture size " + name);
                Require(importer.sRGBTexture == (name != "GrimeMask"), "Color space " + name);
                Require(importer.mipmapEnabled && importer.wrapMode == TextureWrapMode.Repeat, "Repeat/mips " + name);
                Require(name != "OrganicDebris" || importer.alphaSource == TextureImporterAlphaSource.FromInput, "Debris alpha");
            }
            var shader = Resources.Load<Shader>("Region/LotPathBlend");
            Require(shader && shader.isSupported, "Surface shader unsupported");
            var junctionShader = Resources.Load<Shader>("UI/StreetJunction");
            Require(junctionShader && junctionShader.isSupported, "Junction shader unsupported");
            var scene = EditorSceneManager.NewPreviewScene();
            var assets = new List<UnityEngine.Object>();
            try
            {
                var cameraObject = new GameObject("Weathering QA camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene;
                camera.transform.position = new Vector3(13,22,-22); camera.transform.LookAt(Vector3.zero);
                camera.orthographic = true; camera.orthographicSize = 13; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.16f,.19f,.16f); camera.allowHDR = false;
                var lightObject = new GameObject("QA sunlight"); SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(50,-35,0);
                var rt = new RenderTexture(1024,768,24,RenderTextureFormat.ARGB32); assets.Add(rt); rt.Create(); camera.targetTexture = rt;
                var material = LotPathMaterial.Find("cobblestone-road-9").CreateMaterial(); assets.Add(material);
                LotPathMaterial.ApplyEdgeTreatment(material, new LotPathDefinition { width=4 },0,0);
                var profile = UnityEngine.Object.Instantiate(Resources.Load<SurfaceWeatheringProfile>("SurfaceWeathering/Road")); assets.Add(profile);
                Require(profile && profile.grimeMask && profile.gutterSediment && profile.organicDebris, "Profile references");
                var path = new LotPathDefinition { width=4, curved=true, points = new Vector2[] { new(-10,-7),new(-5,-4),new(-3,2),new(2,5),new(10,6) } };
                var build = typeof(LotPathDefinition).Assembly.GetType("CityForge.Lots.LotPathSurface").GetMethod("BuildMesh",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
                var args = build.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : null).ToArray();
                args[0]=path;args[1]=new Vector2(2,2);args[2]=0f;args[3]=(Func<Vector2,float>)(p=>0);
                var roadMesh = (Mesh)build.Invoke(null,args); assets.Add(roadMesh);
                var uv = new List<Vector4>(); roadMesh.GetUVs(1,uv);
                Require(uv.Count == roadMesh.vertexCount && uv.All(v=>v.y>0), "UV1 ribbon metadata");
                var road = new GameObject("Curved road",typeof(MeshFilter),typeof(MeshRenderer)); SceneManager.MoveGameObjectToScene(road,scene);
                road.GetComponent<MeshFilter>().sharedMesh=roadMesh; road.GetComponent<MeshRenderer>().sharedMaterial=material;
                byte[] Render(string name)
                {
                    camera.Render(); camera.Render(); camera.Render(); var previous=RenderTexture.active; RenderTexture.active=rt;
                    var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false,true);
                    image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();RenderTexture.active=previous;
                    var pixels=image.GetRawTextureData<byte>().ToArray();File.WriteAllBytes(output+"/"+name+".png",image.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(image);return pixels;
                }
                var oldShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/CityForge/SurfaceWeatheringQA/OriginalLotPath.shader");
                Require(oldShader,"Original shader fixture missing");
                var originalMaterial=new Material(oldShader);assets.Add(originalMaterial);originalMaterial.CopyPropertiesFromMaterial(material);
                var savedKeywords=material.shaderKeywords;
                road.GetComponent<MeshRenderer>().sharedMaterial=originalMaterial;originalMaterial.shaderKeywords=savedKeywords;var baseline=Render("00-original");
                road.GetComponent<MeshRenderer>().sharedMaterial=material;material.shaderKeywords=savedKeywords;material.SetFloat("_WeatherIntensity",0);
                profile.intensity=0;profile.Apply(material,false);var zero=Render("01-zero");
                Require(baseline.SequenceEqual(zero), "Zero intensity does not reproduce original pixels");
                profile.grimeStrength=profile.moistureVariation=profile.sedimentStrength=profile.organicDebrisStrength=0;
                profile.intensity=.45f;profile.Apply(material,false);var gutter=Render("02-gutter");
                Require(!zero.SequenceEqual(gutter), "Weathering not visible");
                profile.syntheticCamberStrength=.08f;profile.contactShading=.4f;material.SetFloat("_RecessBorder",1);
                profile.Apply(material,false);var contact=Render("03-camber-contact");
                Require(!contact.SequenceEqual(gutter),"Camber/contact not visible");
                profile.grimeStrength=.3f;profile.moistureVariation=.15f;profile.Apply(material,false);
                var grime=Render("04-world-grime");Require(!grime.SequenceEqual(contact),"Grime not visible");
                profile.sedimentStrength=.3f;profile.Apply(material,false);
                var sediment=Render("05-sediment");Require(!sediment.SequenceEqual(grime),"Sediment not visible");
                profile.organicDebrisStrength=.5f;profile.Apply(material,false);
                var debris=Render("06-debris");Require(!debris.SequenceEqual(sediment),"Debris not visible");
                float scatter=profile.debrisScatter,breakup=profile.edgeBreakup;
                profile.debrisScatter=0;profile.Apply(material,false);var singleDebris=Render("06a-single-debris");
                Require(!debris.SequenceEqual(singleDebris),"Offset leaf scatter not visible");
                profile.debrisScatter=scatter;profile.edgeBreakup=0;profile.Apply(material,false);var uniformEdges=Render("06b-uniform-edges");
                profile.edgeBreakup=breakup;profile.Apply(material,false);var brokenEdges=Render("06c-broken-edges");
                Require(!uniformEdges.SequenceEqual(brokenEdges),"Edge breakup not visible");
                profile.removeRepetition=false;profile.Apply(material,false);var tiledWeather=Render("06d-repeating-weather");
                profile.removeRepetition=true;profile.Apply(material,false);var variedWeather=Render("06e-varied-weather");
                Require(!tiledWeather.SequenceEqual(variedWeather),"Nonrepeating native weathering toggle not visible");
                log.Add("PASS: nonrepeating native weathering enable/disable changes rendered pixels.");
                log.Add("PASS: independently disabled offset leaf scatter and irregular world-space edge pockets produce render deltas.");
                profile.sidewalkEdgeWeathering=.8f;profile.Apply(material,true);
                Require(material.GetFloat("_WeatherCamber")==0,"Sidewalk inherited road camber");
                var sidewalk=Render("07-sidewalk");Require(!sidewalk.SequenceEqual(debris),"Independent sidewalk not visible");
                var settingsObject = new GameObject("Weathering settings QA");SceneManager.MoveGameObjectToScene(settingsObject,scene);
                var settings=settingsObject.AddComponent<SurfaceWeatheringSettings>();settings.roadProfile=profile;
                var library=Resources.Load<SurfaceWeatheringLibrary>("SurfaceWeathering/Library");
                Require(library && library.roadProfile && library.sidewalkProfile,"Persistent library references");
                var libraryCopy=UnityEngine.Object.Instantiate(library);assets.Add(libraryCopy);settings.library=libraryCopy;
                libraryCopy.overrides.Add(new SurfaceWeatheringSettings.Override { materialId="library-check",eraId="modern",profile=null });
                settings.Bind(material,"library-check","modern",false);Require(material.GetFloat("_WeatherIntensity")==0,"Persistent library material/era override");
                settings.Bind(material,"cobblestone","modern",false);
                settings.overrides.Add(new SurfaceWeatheringSettings.Override {materialId="cobblestone",eraId="modern",profile=null});
                typeof(SurfaceWeatheringSettings).GetMethod("OnValidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(settings,null);typeof(SurfaceWeatheringSettings).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(settings,null);
                Require(material.GetFloat("_WeatherIntensity")==0,"Material/era null override did not disable");
                settings.overrides.Clear();profile.intensity=.8f;typeof(SurfaceWeatheringSettings).GetMethod("OnValidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(settings,null);typeof(SurfaceWeatheringSettings).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(settings,null);
                Require(Mathf.Abs(material.GetFloat("_WeatherIntensity")-.8f)<.001f,"Inspector refresh failed");
                var originalShader=Shader.Find("Universal Render Pipeline/Lit");
                settings.sidewalkProfile=profile;settings.Bind(material,"concrete","modern",true,originalShader);
                profile.intensity=0;typeof(SurfaceWeatheringSettings).GetMethod("OnValidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(settings,null);typeof(SurfaceWeatheringSettings).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(settings,null);
                Require(material.shader==originalShader,"Disabled sidewalk must restore its original shader");
                profile.intensity=.45f;typeof(SurfaceWeatheringSettings).GetMethod("OnValidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(settings,null);typeof(SurfaceWeatheringSettings).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(settings,null);
                Require(material.shader==shader,"Sidewalk re-enable failed");
                Resources.Load<SurfaceWeatheringProfile>("SurfaceWeathering/Road").Apply(material,false);Render("08-default-road");
                var multi=UnityEngine.Object.Instantiate(Resources.Load<SurfaceWeatheringProfile>("SurfaceWeathering/Road"));assets.Add(multi);
                multi.multiScaleWeathering=false;multi.Apply(material,false);var priorLayers=Render("11-prior-layers");
                multi.multiScaleWeathering=true;multi.Apply(material,false);var newLayers=Render("12-multiscale");Require(!priorLayers.SequenceEqual(newLayers),"Multiscale toggle not visible");
                var baseTexture=material.GetTexture("_BaseMap");var normalTexture=material.GetTexture("_BumpMap");
                float microStrength=multi.microIntensity,mediumStrength=multi.mediumIntensity,macroStrength=multi.macroIntensity;
                multi.microIntensity=0;multi.Apply(material,false);Require(!newLayers.SequenceEqual(Render("13-no-micro")),"Micro intensity does not affect rendering");multi.microIntensity=microStrength;
                multi.mediumIntensity=0;multi.Apply(material,false);Require(!newLayers.SequenceEqual(Render("14-no-medium")),"Medium intensity does not affect rendering");multi.mediumIntensity=mediumStrength;
                multi.macroIntensity=0;multi.Apply(material,false);Require(!newLayers.SequenceEqual(Render("15-no-macro")),"Macro intensity does not affect rendering");multi.macroIntensity=macroStrength;
                var debugImages=new List<byte[]>();foreach(SurfaceWeatheringDebug debug in Enum.GetValues(typeof(SurfaceWeatheringDebug))){if(debug==SurfaceWeatheringDebug.None)continue;multi.debugView=debug;multi.Apply(material,false);var debugPixels=Render("Debug-"+debug);Require(!debugImages.Any(previous=>previous.SequenceEqual(debugPixels)),"Duplicate debug visualization: "+debug);debugImages.Add(debugPixels);}
                multi.debugView=SurfaceWeatheringDebug.Macro;multi.Apply(material,false);var stable=Render("16-stable-world");Require(stable.SequenceEqual(Render("17-repeat-frame")),"Weathering changes across repeated frames");
                var rebuilt=(Mesh)build.Invoke(null,args);assets.Add(rebuilt);road.GetComponent<MeshFilter>().sharedMesh=rebuilt;Require(stable.SequenceEqual(Render("18-rebuilt-mesh")),"Mesh rebuild changes weathering");
                string saved=JsonUtility.ToJson(path);var loaded=JsonUtility.FromJson<LotPathDefinition>(saved);var previousArg=args[0];args[0]=loaded;var reloadedMesh=(Mesh)build.Invoke(null,args);assets.Add(reloadedMesh);road.GetComponent<MeshFilter>().sharedMesh=reloadedMesh;Require(stable.SequenceEqual(Render("19-json-road-reload")),"Road JSON roundtrip changes weathering");args[0]=previousArg;
                var cameraPosition=camera.transform.position;float pixelWidth=2*camera.orthographicSize*((float)rt.width/rt.height)/rt.width;camera.transform.position+=camera.transform.right*pixelWidth*16;var translated=Render("20-camera-translated");double translationDifference=0;for(int y=0;y<rt.height;y++)for(int x=0;x<rt.width-16;x++)for(int c=0;c<3;c++)translationDifference+=Math.Abs(stable[(y*rt.width+x+16)*4+c]-translated[(y*rt.width+x)*4+c]);translationDifference/=rt.height*(rt.width-16)*3;Require(translationDifference<.1,"Camera translation moves world weathering: "+translationDifference);camera.transform.position=cameraPosition;
                multi.debugView=SurfaceWeatheringDebug.Micro;float oldSize=camera.orthographicSize;camera.orthographicSize=100;multi.Apply(material,false);var farMicro=Render("21-far-micro");multi.variationSeed+=83;multi.Apply(material,false);Require(farMicro.SequenceEqual(Render("22-far-other-seed")),"Subpixel micro noise does not fade at distance");multi.variationSeed-=83;camera.orthographicSize=oldSize;multi.debugView=SurfaceWeatheringDebug.None;
                Require(baseTexture==material.GetTexture("_BaseMap")&&normalTexture==material.GetTexture("_BumpMap"),"Base masonry textures changed");
                road.GetComponent<MeshFilter>().sharedMesh=roadMesh;
                log.Add("PASS: independent micro/medium/macro intensity deltas; six distinct debug views; original masonry maps retained; same-frame/rebuilt/JSON-restored road pixels identical; camera-translation debug MAD="+translationDifference+"; far micro detail fades exactly.");
                // Exercise the real footprint union, not ribbon-local edge distances at an intersection.
                var maskType=typeof(PrototypeRegionView).Assembly.GetType("CityForge.World.StreetSurfaceMask");
                var mask=Activator.CreateInstance(maskType,true);
                var add=maskType.GetMethod("AddTriangle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                void Rectangle(float x0,float y0,float x1,float y1)
                {
                    add.Invoke(mask,new object[]{new Vector2(x0,y0),new Vector2(x1,y0),new Vector2(x1,y1),null});
                    add.Invoke(mask,new object[]{new Vector2(x0,y0),new Vector2(x1,y1),new Vector2(x0,y1),null});
                }
                Rectangle(-10,-2,10,2);Rectangle(-2,-10,2,10);
                var edgeMethod=maskType.GetMethod("BoundaryEdges",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                var edges=(List<(Vector2 A,Vector2 B)>)edgeMethod.Invoke(mask,null);
                float Distance(Vector2 point)
                {
                    float d=2;
                    foreach(var e in edges){var delta=e.B-e.A;d=Mathf.Min(d,Vector2.Distance(point,e.A+delta*Mathf.Clamp01(Vector2.Dot(point-e.A,delta)/delta.sqrMagnitude)));}
                    return d;
                }
                Require(Distance(Vector2.zero)>1.9f,"False gutter across X junction center");
                Require(Distance(new Vector2(0,2))>1.9f,"False gutter across connected road edge");
                Require(Distance(new Vector2(5,2))<.001f,"Exposed gutter missing");
                var field=new Texture2D(256,256,TextureFormat.RFloat,false,true){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};assets.Add(field);
                var contains=maskType.GetMethod("Contains",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                var data=new float[256*256];
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {var point=new Vector2(-16+(x+.5f)/8,-16+(y+.5f)/8);data[y*256+x]=Distance(point)*((bool)contains.Invoke(mask,new object[]{point})?1:-1);}
                field.SetPixelData(data,0);field.Apply();
                road.SetActive(false);
                foreach(bool vertical in new[]{false,true})
                {
                    var straight=new LotPathDefinition{width=4,points=vertical?new[]{new Vector2(0,-10),new Vector2(0,10)}:new[]{new Vector2(-10,0),new Vector2(10,0)}};
                    args[0]=straight;var mesh=(Mesh)build.Invoke(null,args);assets.Add(mesh);
                    var crossing=new GameObject("Crossing QA",typeof(MeshFilter),typeof(MeshRenderer));SceneManager.MoveGameObjectToScene(crossing,scene);
                    crossing.GetComponent<MeshFilter>().sharedMesh=mesh;crossing.GetComponent<MeshRenderer>().sharedMaterial=material;
                }
                profile.grimeStrength=profile.moistureVariation=profile.sedimentStrength=profile.organicDebrisStrength=profile.contactShading=profile.syntheticCamberStrength=0;
                profile.intensity=.8f;profile.Apply(material,false);material.SetFloat("_RecessUseFootprint",1);material.SetTexture("_RecessFootprint",field);material.SetVector("_RecessFootprintRect",new Vector4(-16,-16,1f/32,1f/32));
                Render("09-X-junction-boundary");
                var junctionMat=new Material(junctionShader);assets.Add(junctionMat);
                for(int layer=0;layer<4;layer++){junctionMat.SetTexture("_Layer"+layer,material.GetTexture("_BaseMap"));junctionMat.SetColor("_Tint"+layer,material.GetColor("_BaseColor"));}
                profile.Apply(junctionMat,false);junctionMat.SetFloat("_RecessUseFootprint",1);junctionMat.SetTexture("_RecessFootprint",field);junctionMat.SetVector("_RecessFootprintRect",new Vector4(-16,-16,1f/32,1f/32));
                var centerMesh=new Mesh{vertices=new[]{new Vector3(-2,.002f,-2),new Vector3(-2,.002f,2),new Vector3(2,.002f,2),new Vector3(2,.002f,-2)},triangles=new[]{0,1,2,0,2,3},colors=new[]{Color.red,Color.red,Color.red,Color.red}};assets.Add(centerMesh);
                centerMesh.RecalculateNormals();
                var centerUV=new List<Vector2>{new(0,0),new(0,2),new(2,2),new(2,0)};
                for(int channel=0;channel<4;channel++)centerMesh.SetUVs(channel,centerUV);
                centerMesh.SetUVs(4,new List<Vector2>{Vector2.one,Vector2.one,Vector2.one,Vector2.one});
                var centerObject=new GameObject("Junction center QA",typeof(MeshFilter),typeof(MeshRenderer));SceneManager.MoveGameObjectToScene(centerObject,scene);
                centerObject.GetComponent<MeshFilter>().sharedMesh=centerMesh;centerObject.GetComponent<MeshRenderer>().sharedMaterial=junctionMat;
                Render("10-native-junction-shader");
                Require(!ShaderUtil.GetShaderMessages(junctionShader).Any(m=>m.severity.ToString()=="Error"),"Junction shader compile");
                log.Add("PASS: native StreetSurfaceMask X-union contains no internal edge; exposed gutter remains; native union field rendered on both road ribbons.");
                log.Add("PASS: camber/contact, world grime, sediment, debris and independent sidewalk render deltas; material/era disable override; live Inspector rebind; original sidewalk shader restoration and re-enable.");
                Require(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"), "Surface shader compile error");
                log.Add("PASS: 1024px imports/color spaces/repeat/mips/alpha; profile references; existing curved mesh UV1; zero-intensity exact rendered pixel equality; gutter changes pixels; shader compilation.");
                foreach(var method in typeof(ShaderUtil).GetMethods(System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).Where(m=>m.Name=="GetSRPBatcherCompatibilityCode"))
                {
                    var parameters=method.GetParameters();
                    if(parameters.Length==2){foreach(var checkedShader in new[]{shader,junctionShader})
                        {var code=method.Invoke(null,new object[]{checkedShader,0});log.Add(checkedShader.name+" SRP batcher compatibility code="+code);Require(Convert.ToInt32(code)==0,"SRP batcher compatibility lost");}}
                }
                log.Add("Original mesh vertices="+roadMesh.vertexCount+", triangles="+roadMesh.triangles.Length/3+", material passes="+material.passCount);
                // A T union must likewise exclude its connected seam and retain its closed end.
                mask=Activator.CreateInstance(maskType,true);Rectangle(-10,-2,10,2);Rectangle(-2,0,2,10);
                edges=(List<(Vector2 A,Vector2 B)>)edgeMethod.Invoke(mask,null);
                Require(Distance(new Vector2(0,2))>1.9f && Distance(new Vector2(0,-2))<.001f,"T-junction boundary");
                log.Add("PASS: native T-junction union suppresses its connected seam and retains the exposed end.");
                File.WriteAllLines(output+"/checks.txt",log);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); foreach(var asset in assets) if(asset) UnityEngine.Object.DestroyImmediate(asset); }
        }
    }
}
#endif
