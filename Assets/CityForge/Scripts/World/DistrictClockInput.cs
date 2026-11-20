using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CityForge.World
{
    internal sealed class DistrictClockInput:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        internal Func<float> Hour;
        internal Action<float> SetHour;
        float previous,unwrapped;
        public void OnPointerDown(PointerEventData e){unwrapped=Hour();previous=Mathf.Repeat(unwrapped,12);Apply(e);}
        public void OnDrag(PointerEventData e)=>Apply(e);
        public void OnPointerUp(PointerEventData e)=>Apply(e);
        void Apply(PointerEventData e){if(!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var p)||p.sqrMagnitude<9)return;
            float dial=Mathf.Repeat(Mathf.Atan2(p.x,p.y)*6/Mathf.PI,12);
            unwrapped+=Mathf.DeltaAngle(previous*30,dial*30)/30;previous=dial;SetHour(Mathf.Round(unwrapped));}
    }
}
