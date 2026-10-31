using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CityForge.World
{
    /// <summary>Temporary district weather. Never modifies canonical terrain, vegetation or saved maps.</summary>
    public sealed class DistrictWeather : MonoBehaviour
    {
        public enum WeatherKind { Clear, Rain, Snow }
        public WeatherKind Current { get; private set; }
        public float SnowCoverage { get; private set; }
        public float FogAmount { get; private set; }
        public float PrecipitationStrength { get; private set; }
        public bool IsClearing => Current==WeatherKind.Clear&&(SnowCoverage>0||FogAmount>0||PrecipitationStrength>0);
        WeatherKind fallingKind;
        public bool Winter => landscape && landscape.View.VisualSeason == RegionSeason.Winter;
        MeadowDistrictLandscape landscape;
        Camera cameraView;
        ParticleSystem precipitation;
        Material flakes, snow;
        Mesh snowMesh;
        Texture2D landMask;
        GameObject snowSurface, menu;
        Button rainButton, snowButton;
        Text status;
        float fogDistance=300;
        bool savedFog;
        FogMode savedMode;
        Color savedColor;
        float savedDensity, savedStart, savedEnd;
        bool initialized;
        public void Initialize(MeadowDistrictLandscape district)
        {
            landscape=district;cameraView=district.View.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().GetComponent<Camera>();
            savedFog=RenderSettings.fog;savedMode=RenderSettings.fogMode;savedColor=RenderSettings.fogColor;
            savedDensity=RenderSettings.fogDensity;savedStart=RenderSettings.fogStartDistance;savedEnd=RenderSettings.fogEndDistance;
            flakes=new Material(Resources.Load<Shader>("Weather/Precipitation"));
            var emitter=new GameObject("Camera precipitation");emitter.transform.SetParent(transform,false);
            precipitation=emitter.AddComponent<ParticleSystem>();precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=precipitation.main;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=18000;main.startSpeed=0;main.startLifetime=5;
            var renderer=precipitation.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=flakes;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.minParticleSize=.0015f;renderer.maxParticleSize=.025f;
            var emission=precipitation.emission;emission.enabled=true;emission.rateOverTime=1600;
            initialized=true;BuildMenu();RefreshMenu();
        }
        void BuildMenu()
        {
            var hud=landscape.View.transform.Find("RegionHud");if(!hud)return;
            menu=new GameObject("TemporaryWeather",typeof(RectTransform));menu.transform.SetParent(hud,false);
            var rect=menu.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-25,-292);rect.sizeDelta=new Vector2(205,178);
            status=Label(menu.transform,"TEMP WEATHER",16);status.rectTransform.anchorMin=new Vector2(0,1);status.rectTransform.anchorMax=Vector2.one;status.rectTransform.pivot=Vector2.up;status.rectTransform.anchoredPosition=Vector2.zero;status.rectTransform.sizeDelta=new Vector2(0,28);
            rainButton=MakeButton("Rain",34,()=>SetWeather(WeatherKind.Rain));snowButton=MakeButton("Snow",80,()=>SetWeather(WeatherKind.Snow));MakeButton("Clear",126,()=>SetWeather(WeatherKind.Clear));
        }
        static Text Label(Transform parent,string text,int size)
        {
            var go=new GameObject("Caption",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var t=go.GetComponent<Text>();t.text=text;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=Color.white;t.raycastTarget=false;return t;
        }
        Button MakeButton(string name,float y,UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(menu.transform,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.up;rect.anchoredPosition=new Vector2(0,-y);rect.sizeDelta=new Vector2(205,40);
            go.GetComponent<Image>().color=new Color(.13f,.15f,.13f,.95f);var b=go.GetComponent<Button>();b.onClick.AddListener(action);var t=Label(go.transform,name.ToUpperInvariant(),19);t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;return b;
        }
        public void SetWeather(WeatherKind kind)
        {
            if(!initialized || (kind==WeatherKind.Snow&&!Winter)||(kind==WeatherKind.Rain&&Winter))return;
            if(Current==kind)return;
            Current=kind;
            if(kind!=WeatherKind.Clear){
                fallingKind=kind;PrecipitationStrength=1;flakes.SetColor("_BaseColor",Color.white);
                precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                if(kind==WeatherKind.Snow){if(!snowSurface)BuildSnow();snow.SetFloat("_Coverage",SnowCoverage);snowSurface.SetActive(true);}
                ConfigurePrecipitation();precipitation.Play();
            }
            RefreshMenu();
        }
        void RefreshMenu()
        {
            if(!menu)return;rainButton.interactable=!Winter;snowButton.interactable=Winter;
            foreach(var b in menu.GetComponentsInChildren<Button>()){
                bool active=b.name==Current.ToString();var colors=b.colors;colors.normalColor=active?new Color(.4f,.65f,.7f):Color.white;colors.disabledColor=new Color(.38f,.38f,.38f,.55f);b.colors=colors;
                b.GetComponentInChildren<Text>().color=b.interactable?Color.white:new Color(.55f,.55f,.55f);
            }
            status.text=IsClearing?"TEMP WEATHER · CLEARING":"TEMP WEATHER · "+Current.ToString().ToUpperInvariant();
        }
        void ConfigurePrecipitation()
        {
            bool isSnow=Current==WeatherKind.Snow;var main=precipitation.main;
            main.startLifetime=isSnow?8:2;main.startSize3D=true;main.startSizeX=isSnow?new ParticleSystem.MinMaxCurve(.13f,.32f):new ParticleSystem.MinMaxCurve(.1f,.18f);main.startSizeY=isSnow?new ParticleSystem.MinMaxCurve(.13f,.32f):new ParticleSystem.MinMaxCurve(2,3.5f);main.startSizeZ=.15f;
            main.startColor=isSnow?new Color(.94f,.97f,1,.85f):new Color(.72f,.82f,.9f,.7f);
            var velocity=precipitation.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=isSnow?.65f:1.2f;velocity.y=isSnow?-5f:-42f;velocity.z=isSnow?.3f:.5f;
            var noise=precipitation.noise;noise.enabled=isSnow;noise.strength=.65f;noise.frequency=.22f;noise.scrollSpeed=.25f;
            var renderer=precipitation.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;
            FollowCamera();
        }
        void FollowCamera()
        {
            // Emit across the visible map footprint, including wider orthographic zoom levels.
            var center=cameraView.transform.position;var ray=cameraView.ViewportPointToRay(new Vector3(.5f,.5f));
            if(new Plane(Vector3.up,new Vector3(0,landscape.View.SeaLevel,0)).Raycast(ray,out float distance))center=ray.GetPoint(distance);
            fogDistance=Vector3.Distance(cameraView.transform.position,center);
            float extent=Mathf.Clamp(cameraView.orthographicSize*1.4f,35,650);
            float ground=landscape.Height(new Vector2(center.x,center.z));
            precipitation.transform.position=new Vector3(center.x,ground+(fallingKind==WeatherKind.Snow?34:60),center.z);
            var shape=precipitation.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(extent*cameraView.aspect*2,10,extent*2);
            var emission=precipitation.emission;emission.rateOverTime=Mathf.Clamp(extent*extent*.45f,1100,7000)*PrecipitationStrength;
        }
        void Update()
        {
            if(!initialized)return;
            if((Current==WeatherKind.Snow&&!Winter)||(Current==WeatherKind.Rain&&Winter))SetWeather(WeatherKind.Clear);
            RefreshMenu();if(Current==WeatherKind.Clear&&!IsClearing)return;
            float dt=Time.deltaTime;
            PrecipitationStrength=Mathf.MoveTowards(PrecipitationStrength,Current==WeatherKind.Clear?0:1,dt/4);
            SnowCoverage=Mathf.MoveTowards(SnowCoverage,Current==WeatherKind.Snow?1:0,dt/10);
            FogAmount=Mathf.MoveTowards(FogAmount,Current==WeatherKind.Snow?1:0,dt/4);
            FollowCamera();
            // Fade existing particles as well as new emissions so a shower tapers away naturally.
            flakes.SetColor("_BaseColor",new Color(1,1,1,Mathf.SmoothStep(0,1,PrecipitationStrength)));
            if(PrecipitationStrength==0&&precipitation.isPlaying)precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(snow){snow.SetFloat("_Coverage",SnowCoverage);snowSurface.SetActive(SnowCoverage>0||Current==WeatherKind.Snow);}
            if(FogAmount>0){
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.68f,.74f,.8f);RenderSettings.fogDensity=.6f/Mathf.Max(150,fogDistance)*Mathf.SmoothStep(0,1,FogAmount);
            }else RestoreFog();
        }
        void BuildSnow()
        {
            var terrain=landscape.DisplayTerrain;var data=terrain.terrainData;int n=data.heightmapResolution;
            var heights=data.GetHeights(0,0,n,n);var vertices=new Vector3[n*n];var uvs=new Vector2[n*n];var colors=new Color[n*n];var triangles=new List<int>((n-1)*(n-1)*6);
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){
                int i=z*n+x;float u=x/(float)(n-1),v=z/(float)(n-1);var p=landscape.Origin+new Vector2(u,v)*landscape.Side;
                vertices[i]=new Vector3(u*landscape.Side,heights[z,x]*data.size.y+.035f,v*landscape.Side);uvs[i]=new Vector2(u,v);
                // Both canonical water footprints and actual submerged shores must be dry.
                bool dry=landscape.Contains(p)&&landscape.WaterDistance(p)>1.2f&&landscape.ShoreDistance(p)>.6f;colors[i]=dry?Color.white:Color.black;
                if(x<n-1&&z<n-1){triangles.Add(i);triangles.Add(i+n);triangles.Add(i+1);triangles.Add(i+1);triangles.Add(i+n);triangles.Add(i+n+1);}
            }
            landMask=new Texture2D(n,n,TextureFormat.R8,false,true){name="Temporary snow dry-land mask",wrapMode=TextureWrapMode.Clamp};landMask.SetPixels(colors);landMask.Apply();
            snowMesh=new Mesh{name="Temporary terrain-conforming snow",indexFormat=IndexFormat.UInt32};snowMesh.vertices=vertices;snowMesh.uv=uvs;snowMesh.SetTriangles(triangles,0);snowMesh.RecalculateNormals();snowMesh.RecalculateBounds();
            snow=new Material(Resources.Load<Shader>("Weather/DistrictSnow"));snow.SetTexture("_Land",landMask);
            snowSurface=new GameObject("Accumulated snow",typeof(MeshFilter),typeof(MeshRenderer));snowSurface.transform.SetParent(transform,false);snowSurface.transform.position=terrain.transform.position;snowSurface.GetComponent<MeshFilter>().sharedMesh=snowMesh;
            var renderer=snowSurface.GetComponent<MeshRenderer>();renderer.sharedMaterial=snow;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
        }
        void RestoreFog(){if(!initialized)return;RenderSettings.fog=savedFog;RenderSettings.fogMode=savedMode;RenderSettings.fogColor=savedColor;RenderSettings.fogDensity=savedDensity;RenderSettings.fogStartDistance=savedStart;RenderSettings.fogEndDistance=savedEnd;}
        void OnDisable(){RestoreFog();if(menu)menu.SetActive(false);if(precipitation)precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        void OnDestroy(){RestoreFog();if(menu)Destroy(menu);if(snowMesh)Destroy(snowMesh);if(snow)Destroy(snow);if(flakes)Destroy(flakes);if(landMask)Destroy(landMask);}
    }
}
