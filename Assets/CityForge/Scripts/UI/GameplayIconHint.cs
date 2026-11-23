using UnityEngine;
using UnityEngine.EventSystems;
namespace CityForge.UI
{
    internal sealed class GameplayIconHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        private GameObject hint;
        internal void Initialize(GameObject label){hint=label;hint.SetActive(false);}
        public void OnPointerEnter(PointerEventData data){if(hint)hint.SetActive(true);}
        public void OnPointerExit(PointerEventData data){if(hint)hint.SetActive(false);}
        public void OnSelect(BaseEventData data){if(hint)hint.SetActive(true);}
        public void OnDeselect(BaseEventData data){if(hint)hint.SetActive(false);}
        private void OnDisable(){if(hint)hint.SetActive(false);}
    }
}
