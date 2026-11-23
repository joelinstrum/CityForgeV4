using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using CityForge.World;
namespace CityForge.UI
{
    public sealed partial class CityForgeModal
    {
        TerrainImportData activeImport;
        RectTransform importRoot;
        InputField importMetadata,importHeight,importWater;
        bool importPresetMode,importRefreshQueued;
        Toggle importRepair;Dropdown importRotation,importRelief;
        Text importStatus,importSummary,importElevations,importWarnings,importLegend,importProvenance;RawImage importImage;Button importPreviewButton,importUseButton;

        Texture2D importPreviewImage;
        string autoImportHeight,autoImportWater,importValidatedKey,importPendingKey;

        CancellationTokenSource importCancellation;
        static Sprite importRoundedSprite;
        static Sprite ImportRoundedSprite()
        {
            if(importRoundedSprite)return importRoundedSprite;
            const int n=32;const float radius=7;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Import UI rounded card",hideFlags=HideFlags.HideAndDontSave};var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){float dx=Mathf.Max(radius-x,x-(n-1-radius));float dy=Mathf.Max(radius-y,y-(n-1-radius));float distance=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;pixels[y*n+x]=new Color(1,1,1,Mathf.Clamp01(radius-distance+.5f));}
            texture.SetPixels(pixels);texture.Apply();importRoundedSprite=Sprite.Create(texture,new Rect(0,0,n,n),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));importRoundedSprite.hideFlags=HideFlags.HideAndDontSave;return importRoundedSprite;
        }
        readonly Dictionary<InputField,Text> importFileChecks=new Dictionary<InputField,Text>();
        readonly Dictionary<InputField,string> importPaths=new Dictionary<InputField,string>();
        string ImportPath(InputField field)=>field&&importPaths.TryGetValue(field,out var path)&&field.text==Path.GetFileName(path)?path:field?field.text:"";
        void SetImportPath(InputField field,string path){importPaths[field]=path;field.text=Path.GetFileName(path);InvalidateImport();if(field==importMetadata)FillImportPaths();}
        Image ImportCard(Transform parent,string name,Vector2 at,Vector2 size,Color color)
        {
            var image=ImportRect(parent,name,at,size).gameObject.AddComponent<Image>();image.color=color;if(size.x>4&&size.y>4){image.sprite=ImportRoundedSprite();image.type=Image.Type.Sliced;}
            var border=image.gameObject.AddComponent<Outline>();border.effectColor=new Color(.48f,.48f,.35f,.55f);border.effectDistance=new Vector2(1,-1);return image;
        }
        void ShowImportSummary()
        {
            var s=activeImport.Settings;
            foreach(var pair in importFileChecks)pair.Value.text=string.IsNullOrWhiteSpace(ImportPath(pair.Key))?"":"✓";
            importSummary.text=$"Image size: {s.widthPixels:N0} × {s.heightPixels:N0} pixels\nImported area: {s.widthMeters/1000:0.###} × {s.heightMeters/1000:0.###} km\nPadded region: {s.mapSide/1000:0.###} × {s.mapSide/1000:0.###} km";
            importElevations.text=$"Elevation: {s.actualMin:0.00} to {s.actualMax:0.00} m\nEncoding: {s.blackMeters:0.##} to {s.whiteMeters:0.##} m\nSea level: {s.sourceSea:0.##} m   ·   Game offset: +{s.verticalOffset:0.##} m";
            importWarnings.text=$"{s.waterAboveSea:N0} water pixels above sea level\n{s.landBelowSea:N0} land pixels below sea level\n"+(s.repairShoreline?"Repair ON · derived shoreline only":"Repair OFF · original data preserved");
            importLegend.text=$"N ↑   <color=#F5AF47>□</color> Imported area ({s.widthMeters/1000:0.###} × {s.heightMeters/1000:0.###} km)     <color=#92958B>■</color> Generated padding";
            importProvenance.text=s.dataset+" · "+s.provenance;
            importStatus.text="Preview ready. Review the warnings, then create your region.";
        }
        void ConfigureImportOption()
        {
            var button=ImportButton(terrainShapePanel.transform,"Import terrain files",new Vector2(0,-370),new Vector2(350,48),()=>{if(terrainShapeChoice.value==TerrainShapes.Length)OpenPrebuiltMaps();else OpenTerrainImport();});
            button.gameObject.SetActive(false);terrainShapeChoice.onValueChanged.AddListener(i=>{button.gameObject.SetActive(i>=TerrainShapes.Length);button.GetComponentInChildren<Text>().text=i==TerrainShapes.Length?"Choose a pre-built map":"Import terrain files";});
        }
        static RectTransform ImportRect(Transform parent,string name,Vector2 at,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=at;r.sizeDelta=size;return r;
        }
        Text ImportText(Transform parent,string value,Vector2 at,Vector2 size,int fontSize=22)
        {
            var t=ImportRect(parent,value,at,size).gameObject.AddComponent<Text>();t.text=value;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.color=new Color(.95f,.89f,.72f);t.raycastTarget=false;t.alignment=TextAnchor.UpperLeft;return t;
        }
        Button ImportButton(Transform parent,string label,Vector2 at,Vector2 size,Action action)
        {
            var r=ImportRect(parent,label,at,size);r.gameObject.AddComponent<Image>().color=new Color(.25f,.22f,.13f);var border=r.gameObject.AddComponent<Outline>();border.effectColor=new Color(.60f,.49f,.28f,.65f);border.effectDistance=new Vector2(1,-1);r.GetComponent<Image>().sprite=ImportRoundedSprite();r.GetComponent<Image>().type=Image.Type.Sliced;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();ImportText(r,label,new Vector2(12,-8),size-new Vector2(24,12)).alignment=TextAnchor.MiddleCenter;b.onClick.AddListener(()=>action());return b;
        }
        public void OpenTerrainImport()=>OpenTerrainImport(false);
        public void OpenPrebuiltMaps()=>OpenTerrainImport(true);
        void OpenTerrainImport(bool prebuilt)
        {
            CloseTerrainImport();importPresetMode=prebuilt;importRefreshQueued=false;importPaths.Clear();importFileChecks.Clear();var canvas=GetComponentInParent<Canvas>();importRoot=ImportRect(canvas.transform,"Terrain Import",Vector2.zero,Vector2.zero);importRoot.anchorMin=Vector2.zero;importRoot.anchorMax=Vector2.one;importRoot.offsetMin=importRoot.offsetMax=Vector2.zero;importRoot.gameObject.AddComponent<Image>().color=new Color(.035f,.055f,.052f,1);
            var panel=ImportRect(importRoot,"Import panel",Vector2.zero,new Vector2(1600,1000));panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one*.5f;panel.gameObject.AddComponent<GrowableModalFit>().Initialize(new Vector2(1600,1000));
            var title=ImportText(panel,prebuilt?"Pre-built Maps":"Import Terrain",new Vector2(40,-20),new Vector2(1300,62),46);title.font=terrainShapeChoice.captionText.font;
            ImportText(panel,prebuilt?"Choose a ready-made landscape for your new region":"Create a region from your own terrain data",new Vector2(40,-88),new Vector2(1500,36),24);
            var fileRows=ImportRect(panel,"Custom import files",Vector2.zero,new Vector2(740,500));
            InputField FileRow(string label,string icon,float y,string extension){
                ImportText(fileRows,icon,new Vector2(45,y-27),new Vector2(54,45),27);
                ImportText(fileRows,label,new Vector2(115,y),new Vector2(565,30),22);
                var field=ImportCard(fileRows,label+" path",new Vector2(115,y-38),new Vector2(440,48),new Color(.10f,.13f,.12f));var input=field.gameObject.AddComponent<InputField>();input.targetGraphic=field;input.textComponent=ImportText(field.transform,"",new Vector2(14,-8),new Vector2(375,32),20);input.textComponent.supportRichText=false;input.textComponent.horizontalOverflow=HorizontalWrapMode.Overflow;
                var placeholder=ImportText(field.transform,"Choose a file…",new Vector2(14,-8),new Vector2(412,32),20);placeholder.color=new Color(.59f,.63f,.58f);input.placeholder=placeholder;var check=ImportText(field.transform,"",new Vector2(403,-8),new Vector2(28,32),23);check.color=new Color(.71f,.85f,.44f);importFileChecks[input]=check;
                ImportButton(fileRows,"Browse…",new Vector2(575,y-38),new Vector2(155,48),()=>{
#if UNITY_EDITOR
                    string selected=UnityEditor.EditorUtility.OpenFilePanel(label,"",extension);if(!string.IsNullOrEmpty(selected))SetImportPath(input,selected);
#else
                    importStatus.text="Paste the full local file path into the field.";
#endif
                });input.onValueChanged.AddListener(_=>InvalidateImport());input.onEndEdit.AddListener(value=>{if(File.Exists(value))SetImportPath(input,value);});return input;
            }
            importMetadata=FileRow("Metadata (JSON)","{ }",-158,"json");importHeight=FileRow("Heightmap (16-bit PNG)","/\\",-270,"png");importWater=FileRow("Tidal Water Mask (PNG, optional)","≈",-382,"png");
            importMetadata.onEndEdit.AddListener(_=>FillImportPaths());importMetadata.onValueChanged.AddListener(_=>FillImportPaths());
            ImportCard(panel,"Options divider",new Vector2(40,-495),new Vector2(690,1),new Color(.32f,.32f,.24f));
            var toggleRect=ImportRect(panel,"Repair Shoreline",new Vector2(40,-518),new Vector2(690,38));var box=ImportCard(toggleRect,"Box",Vector2.zero,new Vector2(28,28),new Color(.05f,.08f,.07f));var mark=ImportText(toggleRect,"✓",new Vector2(3,2),new Vector2(26,30),24);mark.color=new Color(.72f,.85f,.45f);importRepair=toggleRect.gameObject.AddComponent<Toggle>();importRepair.targetGraphic=box;importRepair.graphic=mark;importRepair.isOn=false;mark.enabled=false;ImportText(toggleRect,"Repair Shoreline (optional)",new Vector2(48,0),new Vector2(540,32),22);importRepair.onValueChanged.AddListener(value=>{mark.enabled=value;ImportOptionsChanged();});
            ImportText(panel,"Corrects conflicting samples near the shore. Source files stay intact.",new Vector2(40,-560),new Vector2(700,32),17);
            ImportText(panel,"Orientation",new Vector2(40,-603),new Vector2(330,28),20);ImportText(panel,"Elevation Scale",new Vector2(395,-603),new Vector2(335,28),20);
            importRotation=ImportChoice(panel,"Rotation",new[]{"North Up","90° clockwise","180°","270° clockwise"},new Vector2(40,-640),()=>ImportOptionsChanged());
            importRelief=ImportChoice(panel,"Relief",new[]{"Original (1×)","Relief 2×","Relief 4×"},new Vector2(395,-640),()=>ImportOptionsChanged());
            var previewCard=ImportCard(panel,"Preview frame",new Vector2(775,-145),new Vector2(785,543),new Color(.075f,.10f,.09f));
            var imageRect=ImportRect(previewCard.transform,"Flat terrain preview",new Vector2(162.5f,-16),new Vector2(460,460));importImage=imageRect.gameObject.AddComponent<RawImage>();importImage.color=new Color(.08f,.11f,.10f);importImage.raycastTarget=false;
            importLegend=ImportText(previewCard.transform,"Your terrain preview will appear here",new Vector2(22,-492),new Vector2(745,36),18);
            var summary=ImportCard(panel,"Import details",new Vector2(40,-715),new Vector2(1520,165),new Color(.10f,.13f,.115f));
            ImportText(summary.transform,"Import Summary",new Vector2(24,-14),new Vector2(450,30),24);importSummary=ImportText(summary.transform,"Select your metadata and heightmap.\nThe water mask is optional.",new Vector2(24,-55),new Vector2(465,94),19);
            ImportCard(summary.transform,"Divider",new Vector2(510,-22),new Vector2(1,122),new Color(.28f,.32f,.28f));importElevations=ImportText(summary.transform,"Elevation and sea level details\nwill appear after preview.",new Vector2(540,-55),new Vector2(450,94),19);
            ImportCard(summary.transform,"Divider",new Vector2(1010,-22),new Vector2(1,122),new Color(.28f,.32f,.28f));ImportText(summary.transform,"Import Warnings",new Vector2(1040,-14),new Vector2(450,30),24).color=new Color(1,.78f,.32f);importWarnings=ImportText(summary.transform,"No files validated yet.",new Vector2(1040,-55),new Vector2(455,94),19);
            importProvenance=ImportText(panel,"",new Vector2(40,-889),new Vector2(1520,34),14);
            importStatus=ImportText(panel,"Choose your files, then select Preview.",new Vector2(40,-937),new Vector2(600,42),18);
            importPreviewButton=ImportButton(panel,"Preview",new Vector2(665,-932),new Vector2(200,54),()=>StartCoroutine(PreviewTerrainImport()));
            ImportButton(panel,"Back",new Vector2(885,-932),new Vector2(160,54),CloseTerrainImport);
            ImportButton(panel,"Cancel",new Vector2(1065,-932),new Vector2(160,54),CloseTerrainImport);
            importUseButton=ImportButton(panel,"Create Imported Region",new Vector2(1245,-932),new Vector2(315,54),()=>{
                if(activeImport==null)return;var recipe=ReadCreationSettings();recipe.terrainImport=activeImport.Settings;recipe.districtGrid=16;recipe.districtTreeCoverage=null;var controller=FindFirstObjectByType<MainMenuController>();CloseTerrainImport();if(controller)controller.CreatePrototypeRegion(RegionName,RegionSize.Large,recipe);
            });importUseButton.targetGraphic.color=new Color(.92f,.69f,.29f);importUseButton.GetComponentInChildren<Text>().color=new Color(.06f,.075f,.05f);importUseButton.interactable=false;
            fileRows.gameObject.SetActive(!prebuilt);
            ImportButton(panel,prebuilt?"Import your own files":"Pre-built maps",new Vector2(1280,-65),new Vector2(280,46),()=>{if(prebuilt)OpenTerrainImport();else OpenPrebuiltMaps();});
            if(prebuilt){
                var map=ImportCard(panel,"Manhattan preset",new Vector2(40,-158),new Vector2(690,320),new Color(.10f,.13f,.115f));
                ImportText(map.transform,"Lower Manhattan · circa 1609",new Vector2(26,-24),new Vector2(620,48),34);
                ImportText(map.transform,"12 × 12 km · natural shoreline schematic",new Vector2(26,-86),new Vector2(630,40),21);
                ImportText(map.transform,"Synthetic, historically inspired · NOT GIS",new Vector2(26,-136),new Vector2(630,36),21).color=new Color(.95f,.76f,.39f);
                ImportText(map.transform,"Hand-schematized coast; generated relief and seabed.\nNot a verified historical reconstruction.",new Vector2(26,-184),new Vector2(630,84),21);
                importUseButton.GetComponentInChildren<Text>().text="Create Region";
                importSummary.text="Lower Manhattan 1609 schematic\nPreparing your preview…";
                string folder=Path.Combine(Application.streamingAssetsPath,"TerrainPresets","Manhattan1609");
                SetImportPath(importHeight,Path.Combine(folder,"Manhattan_1609_Height.png"));SetImportPath(importWater,Path.Combine(folder,"Manhattan_1609_Water.png"));SetImportPath(importMetadata,Path.Combine(folder,"Manhattan_1609_CityForge.json"));
                if(File.Exists(ImportPath(importMetadata)))StartCoroutine(PreviewTerrainImport());else importStatus.text="Lower Manhattan 1609 preset files are not installed in this project.";
            }else SetImportPath(importMetadata,PlayerPrefs.GetString("CityForge.TerrainImport.Metadata",""));
        }
        Dropdown ImportChoice(Transform parent,string label,string[] options,Vector2 at,Action changed)
        {
            var d=Instantiate(terrainShapeChoice,parent);d.name=label;d.GetComponent<RectTransform>().anchoredPosition=at;d.GetComponent<RectTransform>().sizeDelta=new Vector2(335,46);d.onValueChanged=new Dropdown.DropdownEvent();d.ClearOptions();d.AddOptions(new List<string>(options));d.value=0;d.onValueChanged.AddListener(_=>changed());return d;
        }
        void ImportOptionsChanged()
        {
            string key=ImportInputsKey();if(key==importValidatedKey||key==importPendingKey)return;
            InvalidateImport();if(!importPresetMode||!importRoot||importRefreshQueued)return;
            importRefreshQueued=true;importStatus.text="Updating terrain preview…";StartCoroutine(RefreshImportOptions());
        }
        IEnumerator RefreshImportOptions(){yield return null;importRefreshQueued=false;if(importRoot)yield return PreviewTerrainImport();}
        void FillImportPaths()
        {
            if(!importMetadata||!File.Exists(ImportPath(importMetadata)))return;
            try{var meta=JsonUtility.FromJson<TerrainImportMetadata>(File.ReadAllText(ImportPath(importMetadata)));string folder=Path.GetDirectoryName(ImportPath(importMetadata));
                if((string.IsNullOrWhiteSpace(ImportPath(importHeight))||ImportPath(importHeight)==autoImportHeight)&&!string.IsNullOrEmpty(meta.heightmap))SetImportPath(importHeight,autoImportHeight=Path.Combine(folder,meta.heightmap));
                if((string.IsNullOrWhiteSpace(ImportPath(importWater))||ImportPath(importWater)==autoImportWater)&&!string.IsNullOrEmpty(meta.water_mask))SetImportPath(importWater,autoImportWater=Path.Combine(folder,meta.water_mask));
            }catch(Exception){/* Preview displays the validation error without interrupting path entry. */}
        }
        string ImportInputsKey()=>string.Join("\0",ImportPath(importMetadata),ImportPath(importHeight),ImportPath(importWater),importRepair?importRepair.isOn.ToString():"",importRotation?importRotation.value.ToString():"",importRelief?importRelief.value.ToString():"");
        void InvalidateImport(){string key=ImportInputsKey();if(key==importValidatedKey||key==importPendingKey)return;activeImport=null;foreach(var check in importFileChecks.Values)if(check)check.text="";importValidatedKey=importPendingKey=null;importCancellation?.Cancel();if(importUseButton)importUseButton.interactable=false;if(importStatus)importStatus.text="Files or options changed. Select Preview to validate.";if(importWarnings)importWarnings.text="Preview required before creation.";if(importImage&&importImage.texture)importImage.color=new Color(1,1,1,.35f);}
        IEnumerator PreviewTerrainImport()
        {
            if(!importRoot)yield break;foreach(var field in new[]{importMetadata,importHeight,importWater}){string path=ImportPath(field);if(File.Exists(path))SetImportPath(field,path);}importCancellation?.Cancel();var cancellation=new CancellationTokenSource();importCancellation=cancellation;var token=cancellation.Token;string inputsKey=ImportInputsKey();importPendingKey=inputsKey;
            string metadata=ImportPath(importMetadata),height=ImportPath(importHeight),water=ImportPath(importWater);bool repair=importRepair.isOn;int rotation=importRotation.value;float relief=importRelief.value==0?1:importRelief.value==1?2:4;
            string regionName=RegionName;var recipe=ReadCreationSettings();recipe.terrainImport=null;recipe.districtGrid=16;recipe.districtTreeCoverage=null;importPreviewButton.interactable=false;importUseButton.interactable=false;importStatus.text="Reading and validating terrain… Cancel remains available.";
            var job=Task.Run(()=>{var data=TerrainImportData.Read(metadata,height,water,repair,rotation,relief,token);var snapshot=data.Generate(regionName,recipe,token);return (snapshot,pixels:BuildFlatImportPixels(data,token));},token);
            while(!job.IsCompleted){if(!importRoot){cancellation.Cancel();yield break;}yield return null;}
            if(!importRoot||importCancellation!=cancellation)yield break;importPreviewButton.interactable=true;
            if(job.IsCanceled||token.IsCancellationRequested){importStatus.text="Preview cancelled. Original files were not changed.";yield break;}
            if(job.IsFaulted){importStatus.text=job.Exception.GetBaseException().Message;yield break;}
            activeImport=job.Result.snapshot.data.Import;importPendingKey=null;importValidatedKey=inputsKey;
            try{
                BuildImportPreview(job.Result.snapshot,job.Result.pixels);
                if(activeImport==null)throw new InvalidOperationException("Preview data was invalidated while rendering.");
                ShowImportSummary();
                importUseButton.interactable=true;
                PlayerPrefs.SetString("CityForge.TerrainImport.Metadata",metadata);
            }
            catch(Exception e){Debug.LogException(e);activeImport=null;importStatus.text="Preview could not be built: "+e.Message;}
        }
        const int FlatImportPreviewSize=768;
        static Color32[] BuildFlatImportPixels(TerrainImportData data,CancellationToken token)
        {
            int n=FlatImportPreviewSize;var result=new Color32[n*n];var settings=data.Settings;
            float span=Mathf.Max(settings.widthMeters,settings.heightMeters)*1.16f;
            for(int y=0;y<n;y++)
            {
                token.ThrowIfCancellationRequested();
                for(int x=0;x<n;x++)
                {
                    var point=new Vector2((x/(float)(n-1)-.5f)*span,(y/(float)(n-1)-.5f)*span);
                    var uv=point/settings.mapSide+Vector2.one*.5f;var source=data.SourceUV(uv);
                    bool inside=source.x>=0&&source.x<=1&&source.y>=0&&source.y<=1;
                    float height=data.HeightMeters(uv)-settings.verticalOffset;
                    Color color=data.TidalAt(uv)
                        ?Color.Lerp(new Color(.08f,.29f,.39f),new Color(.13f,.39f,.47f),Mathf.InverseLerp(-25,0,height))
                        :Color.Lerp(new Color(.39f,.49f,.30f),new Color(.68f,.66f,.43f),Mathf.InverseLerp(0,Mathf.Max(1,settings.actualMax),height));
                    if(!inside)color=Color.Lerp(color,new Color(.32f,.34f,.32f),.7f);
                    float edge=Mathf.Min(Mathf.Abs(source.x),Mathf.Abs(source.x-1),Mathf.Abs(source.y),Mathf.Abs(source.y-1));
                    if(source.x>=-.003f&&source.x<=1.003f&&source.y>=-.003f&&source.y<=1.003f&&edge<span/n/Mathf.Min(settings.widthMeters,settings.heightMeters)*1.5f)color=new Color(.96f,.63f,.22f);
                    result[y*n+x]=color;
                }
            }
            return result;
        }
        void BuildImportPreview(SavedRegionSnapshot snapshot,Color32[] pixels)
        {
            DisposeImportStage();
            importPreviewImage=new Texture2D(FlatImportPreviewSize,FlatImportPreviewSize,TextureFormat.RGBA32,false){name="Flat imported terrain preview"};
            importPreviewImage.SetPixels32(pixels);importPreviewImage.Apply();
            importImage.texture=importPreviewImage;importImage.color=Color.white;
        }
        void DisposeImportStage(){if(importPreviewImage)Destroy(importPreviewImage);importPreviewImage=null;}
        void OnDisable(){CloseTerrainImport();}
        void CloseTerrainImport(){importCancellation?.Cancel();DisposeImportStage();if(importRoot){importRoot.gameObject.SetActive(false);Destroy(importRoot.gameObject);}importRoot=null;}
    }
}
