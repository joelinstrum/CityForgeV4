using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal sealed class StartupLoadingScreen : MonoBehaviour
    {
        private static StartupLoadingScreen active;
        private bool menuReady;
        internal static bool IsLoading => active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (SceneManager.GetActiveScene().name != "Startup") return;
            var screen = new GameObject("CityForge startup", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(StartupLoadingScreen));
            DontDestroyOnLoad(screen);
            active = screen.GetComponent<StartupLoadingScreen>();
            active.StartCoroutine(active.LoadMenu());
        }

        internal static void NotifyMenuReady()
        {
            if (active) active.menuReady = true;
        }

        private IEnumerator LoadMenu()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;

            var background = Add<RawImage>("Harbor", transform);
            Stretch(background.rectTransform);
            var art = Resources.Load<Texture2D>("Startup/Harbor");
            background.texture = art;
            if (art)
            {
                var aspect = background.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspect.aspectRatio = (float)art.width / art.height;
            }
            else background.color = new Color(.055f, .08f, .11f);

            var border = Add<Image>("Progress frame", transform);
            Dock(border.rectTransform, 174, 590, 22);
            border.color = new Color(.63f, .43f, .21f);
            var track = Add<Image>("Progress track", border.transform);
            Stretch(track.rectTransform);
            track.rectTransform.offsetMin = Vector2.one * 3;
            track.rectTransform.offsetMax = -Vector2.one * 3;
            track.color = new Color(.06f, .043f, .033f);
            var fill = Add<Image>("Loading progress", track.transform);
            Stretch(fill.rectTransform);
            fill.color = new Color(.96f, .71f, .35f);
            fill.rectTransform.anchorMax = Vector2.up;

            var label = Add<Text>("Status", transform);
            Dock(label.rectTransform, 137, 590, 32);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, .91f, .73f);
            label.text = "Loading menu…";
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(1, -2);

            // Present the lightweight scene before starting menu asset loading.
            yield return new WaitForEndOfFrame();
            yield return null;
            var load = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            while (!load.isDone)
            {
                fill.rectTransform.anchorMax = new Vector2(Mathf.Min(load.progress, .9f), 1);
                yield return null;
            }
            label.text = "Preparing menu…";
            while (!menuReady) yield return null;
            fill.rectTransform.anchorMax = Vector2.one;
            yield return new WaitForEndOfFrame();
            active = null;
            Destroy(gameObject);
        }

        private static T Add<T>(string name, Transform parent) where T : Graphic
        {
            var element = new GameObject(name, typeof(RectTransform), typeof(T)).GetComponent<T>();
            element.transform.SetParent(parent, false);
            element.raycastTarget = false;
            return element;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Dock(RectTransform rect, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
