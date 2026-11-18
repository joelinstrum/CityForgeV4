#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using CityForge.World;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace CityForge.Editor
{
    public static class VehicleBehaviorPreview
    {
        public static void Render()
        {
            EditorApplication.ExecuteMenuItem("CityForge/Lots/Prepare vehicle behavior artwork");
            var uiFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var titleFont=AssetDatabase.LoadAssetAtPath<Font>("Assets/CityForge/Fonts/Cinzel-Regular.ttf");
            const string characters="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 —?.,/:()●○▾$";
            foreach(var font in new[]{uiFont,titleFont})if(font)foreach(int size in new[]{15,16,17,18,19,20,21,23,24,25,27,30})font.RequestCharactersInTexture(characters,size);
            var back=LotEditorWorkspace.Create(titleFont?titleFont:uiFont,()=>{});
            var canvas=back.GetComponentInParent<Canvas>();
            var panel=canvas.GetComponent<LotEditorPanel>();
            var lot=new LotDefinition{name="Lumber Camp",category="industrial",subcategory="extraction",wealth=LotWealth.Low,width=8,length=8};
            var item=new LotObjectDefinition{assetId="v3-vehicle-lumber-wagon",position=new Vector2(5,-5),vehicleScript=new LotVehicleScript{
                configured=true,enabled=true,behavior=LotVehicleBehavior.ResourceDelivery,category=VehicleTaskCategory.Resource,
                resource=VehicleResource.Timber,when=RegionTimeOfDay.Morning,capacity=4,handlingSeconds=4}};
            lot.objects.Add(item);
            lot.objects.Add(new LotObjectDefinition{assetId="v3-building-work-tent",position=new Vector2(-5,2)});
            // The UI under test uses the same controls and private artwork as the live editor.
            var open=typeof(LotEditorPanel).GetMethod("OpenLot",BindingFlags.Instance|BindingFlags.NonPublic);
            try{open.Invoke(panel,new object[]{lot});}
            catch(TargetInvocationException){lot.objects.RemoveAt(1);open.Invoke(panel,new object[]{lot});}
            typeof(LotEditorPanel).GetMethod("OpenVehicleScript",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(panel,new object[]{item});
            var camera=new GameObject("Vehicle behavior preview camera").AddComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=10;camera.transform.position=new Vector3(0,0,-10);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.06f,.08f);
            var target=new RenderTexture(1672,941,24);camera.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            canvas.GetComponent<CanvasScaler>().enabled=false;canvas.scaleFactor=1;
            Canvas.ForceUpdateCanvases();
            var viewport=canvas.GetComponentInChildren<LotEditorViewport>();
            typeof(LotEditorViewport).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(viewport,null);
            foreach(var previewCamera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(previewCamera!=camera&&previewCamera.targetTexture)previewCamera.Render();
            Directory.CreateDirectory("Library/VehicleBehaviorPreview");
            var previous=RenderTexture.active;
            foreach(var page in new[]{"What","When","Where"})
            {
                foreach(var button in canvas.GetComponentsInChildren<Button>(true))if(button.gameObject.name==page)button.onClick.Invoke();
                for(int settle=0;settle<3;settle++){Canvas.ForceUpdateCanvases();camera.Render();}
                RenderTexture.active=target;
                var image=new Texture2D(1672,941,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1672,941),0,0);image.Apply();
                File.WriteAllBytes("Library/VehicleBehaviorPreview/"+page+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
            foreach(var button in canvas.GetComponentsInChildren<Button>(true))if(button.gameObject.name=="What")button.onClick.Invoke();
            var resources=canvas.GetComponentsInChildren<Dropdown>(true).First(menu=>menu.gameObject.name=="Behavior Timber");
            typeof(Dropdown).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(resources,null);
            resources.alphaFadeSpeed=0;resources.Show();for(int settle=0;settle<3;settle++){Canvas.ForceUpdateCanvases();camera.Render();}RenderTexture.active=target;
            var listImage=new Texture2D(1672,941,TextureFormat.RGB24,false);listImage.ReadPixels(new Rect(0,0,1672,941),0,0);listImage.Apply();
            File.WriteAllBytes("Library/VehicleBehaviorPreview/Resources.png",listImage.EncodeToPNG());UnityEngine.Object.DestroyImmediate(listImage);
            RenderTexture.active=previous;
            Debug.Log("VEHICLE BEHAVIOR PREVIEW rendered What/When/Where from actual editor UI. No lot or map saved.");
        }
    }
}
#endif
