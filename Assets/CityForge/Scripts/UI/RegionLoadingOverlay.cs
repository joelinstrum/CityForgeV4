using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
namespace CityForge.UI
{
    /// <summary>Independent canvas/runner stays alive when the menu canvas is hidden.</summary>
    public sealed class RegionLoadingOverlay : MonoBehaviour
    {
        public float Progress {get;private set;}
        public string Stage {get;private set;}
        public bool IsLoading {get;private set;}
        public long LongestStepMilliseconds {get;private set;}
        public string LongestStepStage {get;private set;}
        Text stageLabel;RectTransform fill;
        Action completed;Action<Exception> failed;
        readonly Stack<IEnumerator> steps=new();
        bool finished;
        public static RegionLoadingOverlay Show(string regionName,Font font,bool district=false)
        {
            var root=new GameObject("RegionLoadingOverlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var loader=root.AddComponent<RegionLoadingOverlay>();
            font=font?font:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var shade=Box(root.transform,"Backdrop",new Color(.035f,.05f,.04f,.98f));Stretch(shade);
            var panel=Box(root.transform,"Panel",new Color(.08f,.1f,.085f,1));panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(850,290);
            var border=panel.gameObject.AddComponent<Outline>();border.effectColor=new Color(.62f,.52f,.32f,.8f);border.effectDistance=Vector2.one;
            Label(panel,"Title","OPENING “"+regionName+"”",font,30,new Vector2(35,-28),new Vector2(780,76));
            loader.stageLabel=Label(panel,"Stage",district?"Preparing to open district":"Preparing to open region",font,23,new Vector2(35,-114),new Vector2(780,40));
            var track=Box(panel,"Track",new Color(.025f,.04f,.03f,1));track.anchorMin=track.anchorMax=track.pivot=new Vector2(0,1);track.anchoredPosition=new Vector2(35,-173);track.sizeDelta=new Vector2(780,20);
            loader.fill=Box(track,"Fill",new Color(.58f,.65f,.38f,1));Stretch(loader.fill);loader.fill.anchorMax=new Vector2(0,1);
            Label(panel,"Hint",district?"Please wait while your district is prepared.":"Please wait while your region is prepared.",font,18,new Vector2(35,-219),new Vector2(780,35));
            return loader;
        }
        static RectTransform Box(Transform parent,string name,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return go.GetComponent<RectTransform>();}
        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static Text Label(Transform parent,string name,string value,Font font,int size,Vector2 position,Vector2 dimensions){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();text.font=font;text.fontSize=size;text.text=value;text.color=new Color(.89f,.87f,.74f);text.supportRichText=false;text.raycastTarget=false;var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=dimensions;return text;}
        public void Begin(SavedRegionInfo info,Canvas menu,Action onComplete,Action<Exception> onFailure)
        {
            if(IsLoading)return;IsLoading=true;completed=onComplete;failed=onFailure;StartCoroutine(Load(info,menu));
        }
        void Advance(string stage,float progress){Stage=stage;Progress=Mathf.Max(Progress,Mathf.Clamp01(progress));fill.anchorMax=new Vector2(Progress,1);stageLabel.text=stage;}
        void Update(){if(IsLoading&&stageLabel)stageLabel.text=Stage+new string('.',(int)(Time.unscaledTime*2)%4);}
        IEnumerator Load(SavedRegionInfo info,Canvas menu)
        {
            Advance("Reading saved region",.04f);
            // Allow the overlay to be presented before starting file work.
            yield return null;yield return null;
            string path=info.path;var read=Task.Run(()=>SavedRegionStore.Load(path));
            while(!read.IsCompleted)yield return null;
            if(read.IsFaulted){Finish(read.Exception.GetBaseException());yield break;}
            if(read.IsCanceled){Finish(new OperationCanceledException("Region loading was canceled."));yield break;}
            var saved=read.Result;steps.Push(PrototypeRegionView.OpenStages(saved.info.name,menu,snapshot:saved,progress:Advance));
            yield return RunQueuedSteps();
        }
        public void BeginDistrict(IEnumerator work,Action onComplete,Action<Exception> onFailure,string initialStage="Preparing district")
        {
            if(IsLoading)return;IsLoading=true;completed=onComplete;failed=onFailure;
            StartCoroutine(LoadDistrict(work,initialStage));
        }
        IEnumerator LoadDistrict(IEnumerator work,string initialStage)
        {
            Advance(initialStage,.02f);
            // Present the overlay before touching meshes, textures or vegetation.
            yield return null;yield return null;
            steps.Push(work);yield return RunQueuedSteps();
        }
        public void Report(string stage,float progress)=>Advance(stage,progress);
        IEnumerator RunQueuedSteps()
        {
            Exception error=null;
            while(steps.Count>0){
                var current=steps.Peek();bool more=false;object yielded=null;var timer=System.Diagnostics.Stopwatch.StartNew();string stage=Stage;
                try{more=current.MoveNext();if(more)yielded=current.Current;}
                catch(Exception e){error=e;break;}
                finally{if(timer.ElapsedMilliseconds>LongestStepMilliseconds){LongestStepMilliseconds=timer.ElapsedMilliseconds;LongestStepStage=stage;}}
                if(!more){steps.Pop();(current as IDisposable)?.Dispose();continue;}
                if(yielded is IEnumerator nested){steps.Push(nested);continue;}
                yield return yielded;
            }
            if(error!=null){DisposeSteps();Finish(error);yield break;}
            Advance("Ready",1);yield return null;yield return null;Finish(null);
        }
        void DisposeSteps(){while(steps.Count>0){try{(steps.Pop() as IDisposable)?.Dispose();}catch(Exception error){Debug.LogException(error);}}}
        void Finish(Exception error){IsLoading=false;finished=true;completed?.Invoke();if(error!=null)failed?.Invoke(error);Destroy(gameObject);}
        void OnDestroy(){DisposeSteps();if(!finished)completed?.Invoke();}
    }
}
