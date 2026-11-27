using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
namespace CityForge.CameraSystem
{
    [DisallowMultipleComponent,RequireComponent(typeof(CityForgeCameraController))]
    public sealed class DrivingCockpitOverlay : MonoBehaviour
    {
        [Tooltip("Optional sprite override; otherwise uses the privately imported vintage cockpit.")]
        public Sprite cockpitSprite;
        [Tooltip("Open this asset in the Inspector to configure or disable each effect independently.")]
        public DrivingCockpitSettings settings;
        CityForgeCameraController owner;
        PrototypeRegionView region;
        RectTransform canvasRect,rig,imageRect;
        RectTransform[] edges;
        Canvas cockpitCanvas,controlsCanvas;
        Image image;
        Image[] edgeImages;
        Text instructions;
        AudioSource engineSource;
        AudioClip defaultEngine;
        // The catalog's historical Art Deco era has the stable ID "modern".
        internal static bool UsesArtDecoCockpit(PrototypeRegionView world) => world&&world.SelectedDistrict>=0&&world.DistrictAppearanceEra(world.SelectedDistrict)=="modern";
        bool ownsSettings,sampled;
        float lastSpeed,lastGround,distancePhase,roll;
        int lastMph=int.MinValue;
        Vector2 offset;
        Color tint=Color.white;
        internal bool DrawsRideHud => isActiveAndEnabled&&controlsCanvas&&controlsCanvas.gameObject.activeSelf;
        internal void Attach(CityForgeCameraController driver,PrototypeRegionView world)
        {
            owner=driver;region=world;
            if(!UsesArtDecoCockpit(world)){Hide();return;}
            if(!settings){settings=Resources.Load<DrivingCockpitSettings>("Driving/Cockpit/Settings");if(!settings){settings=ScriptableObject.CreateInstance<DrivingCockpitSettings>();settings.hideFlags=HideFlags.DontSave;ownsSettings=true;}}
            if(!cockpitSprite)cockpitSprite=Resources.Load<Sprite>("Driving/Cockpit/VintageCockpit");
            if(cockpitSprite&&!cockpitCanvas)Build();
            if(!defaultEngine)defaultEngine=Resources.Load<AudioClip>("Audio/CitySounds/car-model-t");
            if(!engineSource){var audioObject=new GameObject("Art Deco driving engine");audioObject.transform.SetParent(transform,false);audioObject.hideFlags=HideFlags.DontSave;engineSource=audioObject.AddComponent<AudioSource>();engineSource.playOnAwake=false;engineSource.loop=true;engineSource.spatialBlend=0;engineSource.dopplerLevel=0;engineSource.volume=0;}
            sampled=false;offset=Vector2.zero;roll=0;distancePhase=0;
            SetVisible(isActiveAndEnabled&&driver.Riding);
            if(cockpitCanvas&&world)UpdateVisuals(0,Time.unscaledTime,driver.RideSpeed,driver.RideSteeringInput,driver.transform.position.y,world.CurrentClockHour);
        }
        void Build()
        {
            cockpitCanvas=Canvas("Driving cockpit overlay",-100);canvasRect=(RectTransform)cockpitCanvas.transform;
            rig=Rect(canvasRect,"Cockpit motion",Vector2.zero);rig.anchorMin=rig.anchorMax=rig.pivot=Vector2.one*.5f;
            edges=new RectTransform[4];edgeImages=new Image[4];
            for(int i=0;i<4;i++){edges[i]=Rect(rig,"Opaque edge extension "+i,Vector2.zero);edgeImages[i]=edges[i].gameObject.AddComponent<Image>();edgeImages[i].raycastTarget=false;}
            imageRect=Rect(rig,"Vintage cockpit",Vector2.zero);image=imageRect.gameObject.AddComponent<Image>();image.sprite=cockpitSprite;image.preserveAspect=true;image.raycastTarget=false;
            controlsCanvas=Canvas("Driving controls above cockpit",10);
            var panel=Rect(controlsCanvas.transform,"Existing ride instructions",new Vector2(470,48));panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(0,1);panel.anchoredPosition=new Vector2(16,-16);
            var background=panel.gameObject.AddComponent<Image>();background.color=new Color(0,0,0,.72f);background.raycastTarget=false;
            var label=Rect(panel,"Speed and controls",Vector2.zero);label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(10,3);label.offsetMax=new Vector2(-10,-3);
            instructions=label.gameObject.AddComponent<Text>();instructions.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");instructions.fontSize=16;instructions.color=Color.white;instructions.alignment=TextAnchor.MiddleLeft;instructions.raycastTarget=false;
        }
        static Canvas Canvas(string name,int order)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));root.hideFlags=HideFlags.DontSave;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1;
            var group=root.GetComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
            // No GraphicRaycaster or input-reading behaviours are attached to either canvas.
            return canvas;
        }
        static RectTransform Rect(Transform parent,string name,Vector2 size)
        {var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);rect.sizeDelta=size;return rect;}
        internal void Hide(){if(engineSource){engineSource.Stop();engineSource.volume=0;}SetVisible(false);sampled=false;offset=Vector2.zero;roll=0;}
        void SetVisible(bool visible){if(cockpitCanvas)cockpitCanvas.gameObject.SetActive(visible&&cockpitSprite);if(controlsCanvas)controlsCanvas.gameObject.SetActive(visible&&cockpitSprite);}
        void LateUpdate()
        {
            if(!owner)owner=GetComponent<CityForgeCameraController>();
            if(!owner||!owner.Riding||!UsesArtDecoCockpit(region)){Hide();return;}
            if(!cockpitCanvas){Attach(owner,region);if(!cockpitCanvas)return;}
            UpdateEngineAudio(Time.unscaledDeltaTime);
            SetVisible(true);
            UpdateVisuals(Time.unscaledDeltaTime,Time.unscaledTime,owner.RideSpeed,owner.RideSteeringInput,owner.transform.position.y,region.CurrentClockHour);
        }
        internal void UpdateEngineAudio(float delta)
        {
            if(!engineSource||!settings)return;
            var clip=settings.engineSound?settings.engineSound:defaultEngine;
            if(!isActiveAndEnabled||!owner||!owner.Riding||!UsesArtDecoCockpit(region)||!settings.engineAudio||!clip){engineSource.Stop();engineSource.volume=0;return;}
            if(engineSource.clip!=clip){engineSource.Stop();engineSource.clip=clip;engineSource.volume=0;}
            if(!engineSource.isPlaying&&!AudioListener.pause)engineSource.Play();
            engineSource.volume=Mathf.MoveTowards(engineSource.volume,Mathf.Clamp01(settings.engineVolume),Mathf.Max(0,delta)/Mathf.Max(.05f,settings.engineFadeSeconds));
        }
        internal void UpdateVisuals(float delta,float time,float speed,float steering,float ground,float hour)
        {
            if(!cockpitCanvas||!settings)return;
            bool first=!sampled;var s=settings;float dt=Mathf.Clamp(delta,0,.1f);float acceleration=sampled&&dt>.00001f?(speed-lastSpeed)/dt:0;float heightRate=sampled&&dt>.00001f?(ground-lastGround)/dt:0;
            lastSpeed=speed;lastGround=ground;sampled=true;distancePhase+=Mathf.Abs(speed)*dt;
            float moving=Mathf.Clamp01(Mathf.Abs(speed)/3.13f);var target=Vector2.zero;float targetRoll=0;
            if(s.engineVibration){target.x+=Mathf.Sin(time*s.engineFrequency*6.283185f)*s.engineIntensity*.4f;target.y+=Mathf.Sin(time*s.engineFrequency*6.283185f+1.1f)*s.engineIntensity;}
            if(s.turningSway){target.x-=Mathf.Clamp(steering,-1,1)*moving*s.turningIntensity;targetRoll=-Mathf.Clamp(steering,-1,1)*moving*s.turningRoll;}
            if(s.roadVibration)target.y+=(Mathf.Sin(distancePhase*s.roadFrequency*2f)*.65f+Mathf.Sin(distancePhase*s.roadFrequency*3.17f)*.35f)*moving*s.roadIntensity+Mathf.Clamp(heightRate,-1,1)*moving*s.groundResponse;
            if(s.inertia)target.y-=Mathf.Clamp(acceleration/5f,-1,1)*s.inertiaIntensity;
            target=Vector2.ClampMagnitude(target,Mathf.Max(0,s.maximumOffset));targetRoll=Mathf.Clamp(targetRoll,-s.maximumRoll,s.maximumRoll);
            float blend=1-Mathf.Exp(-Mathf.Max(1,s.response)*dt);offset=Vector2.Lerp(offset,target,blend);roll=Mathf.Lerp(roll,targetRoll,blend);
            if(!s.engineVibration&&!s.turningSway&&!s.roadVibration&&!s.inertia){offset=Vector2.zero;roll=0;}
            rig.anchoredPosition=offset;rig.localRotation=Quaternion.Euler(0,0,roll);
            var targetTint=InteriorTint(s,hour);tint=first?targetTint:Color.Lerp(tint,targetTint,1-Mathf.Exp(-4f*dt));
            if(!s.timeOfDayLighting)tint=Color.white;
            image.color=tint;image.sprite=cockpitSprite;
            foreach(var edge in edgeImages)edge.color=new Color(s.edgeColor.r*tint.r,s.edgeColor.g*tint.g,s.edgeColor.b*tint.b,1);
            Fit(canvasRect.rect.size,cockpitSprite.rect.size);
            int mph=Mathf.RoundToInt(speed/.44704f);
            if(instructions&&mph!=lastMph){lastMph=mph;instructions.text=$"City ride · {mph} mph\n↑ Accelerate   ↓ Brake   ← → Steer   Esc Return";}
        }
        internal void Fit(Vector2 viewport,Vector2 source)
        {
            float scale=Mathf.Min(viewport.x/source.x,viewport.y/source.y)*settings.imageScale;var size=source*scale;imageRect.sizeDelta=size;
            float inset=Mathf.Min(settings.edgeInsetSourcePixels*scale,Mathf.Min(size.x,size.y)*.04f),extent=viewport.magnitude*3f;
            edges[0].sizeDelta=edges[1].sizeDelta=new Vector2(extent+inset,extent*4);
            edges[0].anchoredPosition=new Vector2(-size.x*.5f-extent*.5f+inset*.5f,0);edges[1].anchoredPosition=new Vector2(size.x*.5f+extent*.5f-inset*.5f,0);
            edges[2].sizeDelta=edges[3].sizeDelta=new Vector2(extent*4,extent+inset);
            edges[2].anchoredPosition=new Vector2(0,size.y*.5f+extent*.5f-inset*.5f);edges[3].anchoredPosition=new Vector2(0,-size.y*.5f-extent*.5f+inset*.5f);
        }
        internal static Color InteriorTint(DrivingCockpitSettings s,float hour)
        {
            if(!s.timeOfDayLighting)return Color.white;
            float h=Mathf.Repeat(hour,24);if(h<6)h+=24;float brightness;
            if(h<12)brightness=Mathf.Lerp(s.morningBrightness,s.dayBrightness,(h-6)/6);
            else if(h<15)brightness=s.dayBrightness;
            else if(h<20)brightness=Mathf.Lerp(s.dayBrightness,s.eveningBrightness,(h-15)/5);
            else if(h<22)brightness=Mathf.Lerp(s.eveningBrightness,s.nightBrightness,(h-20)/2);
            else brightness=Mathf.Lerp(s.nightBrightness,s.morningBrightness,(h-22)/8);
            Color color=Color.Lerp(s.nightTint,s.daylightTint,Mathf.InverseLerp(s.nightBrightness,s.dayBrightness,brightness))*brightness;color.a=1;return color;
        }
        void OnDisable()=>Hide();
        void OnDestroy(){if(engineSource)Destroy(engineSource.gameObject);if(cockpitCanvas)Destroy(cockpitCanvas.gameObject);if(controlsCanvas)Destroy(controlsCanvas.gameObject);if(ownsSettings&&settings)Destroy(settings);}
    }
}
