using UnityEngine;
namespace CityForge.UI
{
    internal sealed class GovernmentModalFit:MonoBehaviour
    {
        private RectTransform panel;
        internal void Initialize(RectTransform value){panel=value;Fit();}
        private void OnRectTransformDimensionsChange()=>Fit();
        private void Fit()
        {
            if(!panel)return;var bounds=(RectTransform)transform;
            if(bounds.rect.width<=0||bounds.rect.height<=0)return;
            float scale=Mathf.Min(1,(bounds.rect.width-36)/1600,(bounds.rect.height-36)/900);
            panel.localScale=Vector3.one*Mathf.Max(.1f,scale);
        }
    }
}
