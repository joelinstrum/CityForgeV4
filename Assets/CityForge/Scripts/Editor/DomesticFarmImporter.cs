#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using CityForge.World;
namespace CityForge.EditorTools
{
    /// <summary>Source-only importer; supplied models and all generated assets remain private.</summary>
    public static class DomesticFarmImporter
    {
        const string Source="Assets/CityForge/Art/Animals/DomesticatedFarm/Source/";
        const string Output="Assets/CityForge/Resources/Wildlife/DomesticatedFarm/";
        static readonly (string file,string id,float height,float speed)[] Animals={
            ("Bull","bull",1.6f,.5f),("Chick","chick",.18f,.2f),("Chicken","hen",.4f,.3f),
            ("Cow","cow",1.5f,.45f),("Goose","goose",.65f,.35f),("Horse","horse",1.7f,.7f),
            ("Pig","pig",.75f,.4f),("Rabbit","domestic-rabbit",.4f,.35f),("Ram","ram",1f,.4f),
            ("Rooster","rooster",.5f,.3f),("Sheep","sheep",.85f,.4f),("Turkey","turkey",.65f,.3f)
        };
        [MenuItem("CityForge/Animals/Import Local Domestic Farm Animals")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before importing domestic models.");
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            var library=AssetDatabase.LoadAssetAtPath<WildlifeLibrary>("Assets/CityForge/Resources/Wildlife/TemperateWildlife.asset");
            if(!library)throw new FileNotFoundException("Prepare the local wildlife library first.");
            var entries=library.species.Where(s=>s!=null&&!Animals.Any(a=>a.id==s.id)).ToList();
            var report=new StringBuilder("Domestic farm animal import\n");
            foreach(var animal in Animals)
            {
                string path=Source+animal.file+".fbx";
                var importer=AssetImporter.GetAtPath(path) as ModelImporter;
                if(!importer)throw new FileNotFoundException(path);
                importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                AnimationClip Find(string name)=>clips.FirstOrDefault(c=>c.name.Equals(name,StringComparison.OrdinalIgnoreCase)||c.name.EndsWith("|"+name,StringComparison.OrdinalIgnoreCase));
                var idle=Find("Idle");var walk=Find("Walk");var graze=Find("Eat Loop")??Find("Eat");
                if(!idle||!walk||!graze)throw new InvalidDataException(animal.file+" missing Idle/Walk/Eat clips: "+string.Join(",",clips.Select(c=>c.name)));
                var controller=Controller(animal.id,CopyClip(idle,animal.id+"-idle"),CopyClip(walk,animal.id+"-walk"),CopyClip(graze,animal.id+"-graze"));
                var body=UnityEngine.Object.Instantiate(source);var wrapper=new GameObject(animal.id);
                try
                {
                    foreach(var script in body.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
                    var animator=body.GetComponent<Animator>()??body.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                    var renderers=body.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new InvalidDataException(animal.file+" has no renderer");
                    foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>MaterialFor(m,animal.id)).ToArray();
                    var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                    if(bounds.size.y<.001f)throw new InvalidDataException(animal.file+" has empty bounds");
                    float scale=animal.height/bounds.size.y;
                    var head=body.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Head"||t.name=="Head1");
                    var spine=body.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Replace(" ","")=="Spine1"||t.name=="BackBone.1");
                    if(!head||!spine)throw new InvalidDataException(animal.file+" needs explicit forward-axis mapping");
                    var forward=head.position-spine.position;forward.y=0;
                    if(forward.sqrMagnitude<.00001f)throw new InvalidDataException(animal.file+" ambiguous forward axis");
                    float yaw=Vector3.SignedAngle(forward.normalized,Vector3.forward,Vector3.up);
                    var alignment=new GameObject("Model alignment");alignment.transform.SetParent(wrapper.transform,false);
                    body.name=animal.file;body.transform.SetParent(alignment.transform,false);body.transform.localScale*=scale;
                    body.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
                    alignment.transform.localRotation=Quaternion.Euler(0,yaw,0);
                    var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,Output+animal.id+".prefab");
                    entries.Add(new WildlifeSpecies{id=animal.id,prefab=prefab,habitat=WildlifeHabitat.WoodlandEdge,disposition=AnimalDisposition.Domesticated,weight=0,speed=animal.speed,grazingState="Graze"});
                    long triangles=0;foreach(var renderer in renderers){var mesh=renderer is SkinnedMeshRenderer skinned?skinned.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;if(mesh)for(int i=0;i<mesh.subMeshCount;i++)triangles+=(long)mesh.GetIndexCount(i)/3;}
                    int bones=renderers.OfType<SkinnedMeshRenderer>().Sum(r=>r.bones.Length);
                    report.AppendLine($"{animal.id}: triangles={triangles}, skin bones={bones}, target height={animal.height:F2}m; Idle={idle.name}, Walk={walk.name}, Graze={graze.name}; avatar={animator.avatar!=null}, forward correction={yaw:F1}deg");
                    if(bones==0)throw new InvalidDataException(animal.file+" lacks a skinned rig");
                }
                finally{UnityEngine.Object.DestroyImmediate(wrapper);if(body)UnityEngine.Object.DestroyImmediate(body);}
            }
            library.species=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            File.WriteAllText(Output+"Import-Report.txt",report.ToString());
        }
        static AnimationClip CopyClip(AnimationClip source,string name)
        {
            var clip=UnityEngine.Object.Instantiate(source);clip.name=name;AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
            string path=Output+name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);return existing;}
            AssetDatabase.CreateAsset(clip,path);return clip;
        }
        static AnimatorController Controller(string id,AnimationClip idle,AnimationClip walk,AnimationClip graze)
        {
            string path=Output+id+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path)??AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.parameters=Array.Empty<AnimatorControllerParameter>();controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
            var rest=machine.AddState("Idle");rest.motion=idle;var moving=machine.AddState("Walk");moving.motion=walk;var eating=machine.AddState("Graze");eating.motion=graze;machine.defaultState=rest;
            foreach(var state in new[]{rest,eating}){var t=state.AddTransition(moving);t.hasExitTime=false;t.duration=.2f;t.AddCondition(AnimatorConditionMode.If,0,"Moving");}
            var stop=moving.AddTransition(rest);stop.hasExitTime=false;stop.duration=.2f;stop.AddCondition(AnimatorConditionMode.IfNot,0,"Moving");EditorUtility.SetDirty(controller);return controller;
        }
        static Material MaterialFor(Material source,string id)
        {
            if(!source)throw new InvalidDataException(id+" missing material");
            string name=source.name.Replace('/','_');string path=Output+id+"-"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            Color color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_Color")?source.color:Color.white;
            material.SetColor("_BaseColor",color);material.SetTexture("_BaseMap",source.mainTexture);material.SetFloat("_Smoothness",.15f);material.SetFloat("_Metallic",0);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
        }
    }
}
#endif
