using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CityForge.UI
{
    internal sealed class BrassHudHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Outline glow;
        public void OnPointerEnter(PointerEventData data)
        {
            if (!GetComponent<Button>().IsInteractable()) return;
            if (!glow)
            {
                glow = gameObject.AddComponent<Outline>();
                glow.effectColor = new Color(1f, .69f, .23f, .8f);
                glow.effectDistance = new Vector2(2, -2);
            }
            glow.enabled = true;
            transform.localScale = Vector3.one * 1.06f;
        }
        public void OnPointerExit(PointerEventData data) => ResetHover();
        private void OnDisable() => ResetHover();
        private void ResetHover()
        {
            if (glow) glow.enabled = false;
            transform.localScale = Vector3.one;
        }
    }
}
