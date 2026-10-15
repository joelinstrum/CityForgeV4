using UnityEngine;

namespace CityForge.CameraSystem
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class MeadowShowcaseNavigation : MonoBehaviour
    {
        [SerializeField, Range(.01f, .25f)] float edgeFraction = .1f;
        [SerializeField] float panSpeed = 35f;
        [SerializeField] Vector2 zoomRange = new Vector2(45f, 800f);
        [SerializeField] float zoomStep = 1.35f;
        Camera view;
        Vector3 focus, initialFocus, initialPosition;
        Quaternion orientation;
        float distance, targetDistance, initialDistance;
        bool isometric, savedFog;
        Vector3 savedFocus, savedPosition;
        Quaternion savedOrientation;
        float savedDistance, savedTarget, savedOrthoSize;
        bool savedOrthographic;
        public bool Isometric => isometric;
        public float isometricSize = 70f;

        public void ToggleIsometric()
        {
            if (!isometric)
            {
                savedFocus = focus; savedPosition = transform.position; savedOrientation = orientation;
                savedDistance = distance; savedTarget = targetDistance;
                savedOrthographic = view.orthographic; savedOrthoSize = view.orthographicSize;
                savedFog = RenderSettings.fog;
                focus = new Vector3(335f, 12f, 300f);
                orientation = Quaternion.Euler(35.26439f, 315f, 0f);
                distance = targetDistance = isometricSize;
                view.orthographic = true;
                RenderSettings.fog = false;
                isometric = true;
                ApplyPose();
            }
            else
            {
                focus = savedFocus; orientation = savedOrientation;
                distance = savedDistance; targetDistance = savedTarget;
                view.orthographic = savedOrthographic; view.orthographicSize = savedOrthoSize;
                RenderSettings.fog = savedFog;
                isometric = false;
                transform.SetPositionAndRotation(savedPosition, orientation);
            }
        }

        void OnDisable()
        {
            if (isometric) ToggleIsometric();
        }

        void ApplyPose()
        {
            if (isometric) view.orthographicSize = distance;
            var position = focus - orientation * Vector3.forward * (isometric ? 280f : distance);
            var terrain = Terrain.activeTerrain;
            if (terrain && position.x >= 0f && position.x <= 512f && position.z >= 0f && position.z <= 512f)
                position.y = Mathf.Max(position.y, terrain.SampleHeight(position) + terrain.transform.position.y + 8f);
            transform.SetPositionAndRotation(position, orientation);
        }

        public void SetTargetDistance(float value)
        {
            targetDistance=distance=Mathf.Clamp(value,zoomRange.x,zoomRange.y);ApplyPose();
        }
        public float TargetDistance => targetDistance;
        public bool ClosestTwoZoomLevels => targetDistance < zoomRange.x * zoomStep * Mathf.Sqrt(zoomStep);

        public static Vector2 EdgeDirection(Vector2 uv, float fraction)
        {
            if (uv.x < 0f || uv.x > 1f || uv.y < 0f || uv.y > 1f) return Vector2.zero;
            return new Vector2(uv.x < fraction ? -1f : uv.x > 1f - fraction ? 1f : 0f,
                uv.y < fraction ? -1f : uv.y > 1f - fraction ? 1f : 0f);
        }

        void Awake()
        {
            view = GetComponent<Camera>();
            orientation = transform.rotation;
            initialPosition = transform.position;
            var ground = new Plane(Vector3.up, new Vector3(0f, 9f, 0f));
            var ray = new Ray(transform.position, transform.forward);
            distance = ground.Raycast(ray, out var hit) ? hit : 170f;
            distance = Mathf.Clamp(distance, zoomRange.x, zoomRange.y);
            focus = transform.position + transform.forward * distance;
            initialFocus = focus;
            initialDistance = targetDistance = distance;
        }

        void LateUpdate()
        {
            if (!Application.isFocused) return;
            if (Input.GetKeyDown(KeyCode.I)) { ToggleIsometric(); return; }
            var rect = view.pixelRect;
            var mouse = (Vector2)Input.mousePosition;
            Vector2 direction = Vector2.zero;
            if (Cursor.lockState == CursorLockMode.None && rect.Contains(mouse))
            {
                var uv = (mouse - rect.position) / rect.size;
                direction = EdgeDirection(uv, edgeFraction);
            }
            #if UNITY_EDITOR
            // Legacy mouse position can remain at its last location after leaving Game view.
            var hovered = UnityEditor.EditorWindow.mouseOverWindow;
            if (hovered == null || hovered.GetType().Name != "GameView") direction = Vector2.zero;
#endif
            var right = Vector3.ProjectOnPlane(orientation * Vector3.right, Vector3.up).normalized;
            var forward = Vector3.ProjectOnPlane(orientation * Vector3.forward, Vector3.up).normalized;
            var speed = panSpeed * Mathf.Clamp(distance / initialDistance, .35f, 4f);
            focus += (right * direction.x + forward * direction.y).normalized * speed * Time.unscaledDeltaTime;
            focus.x = Mathf.Clamp(focus.x, 0f, 512f);
            focus.z = Mathf.Clamp(focus.z, 0f, 512f);

            bool zoomIn = Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus)
                || Input.GetKeyDown(KeyCode.KeypadPlus);
            bool zoomOut = Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus);
            if (zoomIn != zoomOut)
                targetDistance = Mathf.Clamp(targetDistance * (zoomIn ? 1f / zoomStep : zoomStep), zoomRange.x, zoomRange.y);
            if (Input.GetKeyDown(KeyCode.Home)) { ResetView(); return; }
            distance = Mathf.Lerp(distance, targetDistance, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            ApplyPose();
        }
        public void ResetView()
        {
            if (isometric) ToggleIsometric();
            focus = initialFocus;
            targetDistance = distance = initialDistance;
            transform.SetPositionAndRotation(initialPosition, orientation);
        }
    }
}
