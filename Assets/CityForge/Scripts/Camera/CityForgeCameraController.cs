using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.CameraSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CityForgeCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 focus = Vector3.zero;
        [SerializeField, Range(15f, 85f)] private float pitch = 50f;
        [SerializeField] private float yaw = 45f;
        [SerializeField, Min(5f)] private float distance = 65f;
        [SerializeField, Min(3f)] private float orthographicSize = 32f;
        [SerializeField, Min(0.1f)] private float moveSpeed = 22f;
        [SerializeField, Min(1f)] private float rotationSpeed = 90f;

        private bool lowerView;
        public bool LowerView => lowerView;
        public bool FitShadowDepth {get;set;}=true;
        private bool meadowFraming;private float meadowStartSize;
        private Camera viewCamera;
        private Vector3 framedCenter;
        [SerializeField] private float regionSide, regionHeight = 1200f, farthestSize;
        [field: SerializeField] public int ZoomLevel { get; private set; } = 5;
        public event System.Action<int> ZoomChanged;

        public void SetZoomLevel(int level)
        {
            ZoomLevel = Mathf.Clamp(level, 0, 5);
            if (regionSide > 0f) orthographicSize = farthestSize / Mathf.Pow(2f, 5-ZoomLevel);
            ApplyPose(); ZoomChanged?.Invoke(ZoomLevel);
        }

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            if (!TryGetComponent<CameraMotionQuality>(out _)) gameObject.AddComponent<CameraMotionQuality>();
            ApplyPose();
        }

        public float PanSpeedMultiplier
        {
            get
            {
                if (regionSide <= 0f) return 1f;
                float multiplier = ZoomLevel <= 1 ? 1.69f : 1f;
                // Close navigation: the original 2x boost, plus the requested further 25%.
                return ZoomLevel >= 1 && ZoomLevel <= 3 ? multiplier * 2.5f : multiplier;
            }
        }
        public void StepZoom(bool closer)=>SetZoomLevel(ZoomLevel+(closer?-1:1));
        private void LateUpdate()
        {
            var dt = Time.unscaledDeltaTime;
            var movement = Vector2.zero;
            if ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))) movement.x -= 1f;
            if ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))) movement.x += 1f;
            if ((Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))) movement.y -= 1f;
            if ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))) movement.y += 1f;

            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var viewScale = Mathf.Clamp(orthographicSize / 32f, 0.25f, 100f);
            var speed = moveSpeed * viewScale * PanSpeedMultiplier * (Input.GetKey(KeyCode.LeftShift) ? 2f : 1f);
            focus += rotation * new Vector3(movement.x, 0f, movement.y).normalized * speed * dt;

            if (Input.GetMouseButton(2))
            {
                var dragScale = orthographicSize * 0.035f * PanSpeedMultiplier;
                focus += rotation * new Vector3(-Input.GetAxisRaw("Mouse X"), 0f,
                    -Input.GetAxisRaw("Mouse Y")) * dragScale;
            }

            if (regionSide > 0f)
            {
                if (Input.GetKeyDown(KeyCode.Q)) yaw -= 90f;
                if (Input.GetKeyDown(KeyCode.E)) yaw += 90f;
            }
            else
            {
            if (Input.GetKey(KeyCode.Q)) yaw -= rotationSpeed * dt;
            if (Input.GetKey(KeyCode.E)) yaw += rotationSpeed * dt;
            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxisRaw("Mouse X") * 4f;
                pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 4f, 15f, 85f);
            }

            }

            if (regionSide > 0f)
            {
                // The main keyboard + shares the = key; also support numeric keypads.
                var zoomIn = Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus)
                    || Input.GetKeyDown(KeyCode.KeypadPlus);
                var zoomOut = Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus);
                if (zoomIn != zoomOut) StepZoom(zoomIn);
            }

            var zoom = Input.mouseScrollDelta.y;
            if (Mathf.Abs(zoom) > 0.01f)
            {
                if (regionSide > 0f) StepZoom(zoom>0);
                else {
                var factor = Mathf.Pow(0.85f, zoom);
                orthographicSize = Mathf.Clamp(orthographicSize * factor, 3f, regionSide > 0f ? regionSide : 5000f);
                distance = Mathf.Clamp(distance * factor, 5f, regionSide > 0f ? regionSide * 4f : 12000f);
                }
            }

            if (Input.GetKeyDown(KeyCode.I)) ToggleViewAngle();
            if (regionSide == 0f && Input.GetKeyDown(KeyCode.P)) ToggleProjection();
            if (Input.GetKeyDown(KeyCode.Home)) ResetView();
            ApplyPose();
        }

        public World.RegionCameraState CaptureRegionState()=>new World.RegionCameraState{focus=focus,pitch=pitch,yaw=yaw,distance=distance,size=orthographicSize,zoom=ZoomLevel,lowerView=lowerView};
        public void RestoreRegionState(World.RegionCameraState state)
        {
            if(state==null)return;
            if(!float.IsFinite(state.focus.x)||!float.IsFinite(state.focus.y)||!float.IsFinite(state.focus.z)||!float.IsFinite(state.pitch)||!float.IsFinite(state.yaw)||!float.IsFinite(state.distance)||!float.IsFinite(state.size)||state.size<3||state.distance<5)throw new System.IO.InvalidDataException("Invalid saved camera.");
            lowerView=state.lowerView;viewCamera=GetComponent<Camera>();viewCamera.orthographic=!lowerView;
            focus=state.focus;pitch=state.pitch;yaw=state.yaw;distance=state.distance;orthographicSize=state.size;ZoomLevel=Mathf.Clamp(state.zoom,0,5);ApplyPose();ZoomChanged?.Invoke(ZoomLevel);
        }

        public void ToggleViewAngle()
        {
            lowerView = !lowerView;
            viewCamera = GetComponent<Camera>();
            viewCamera.orthographic = !lowerView;
            // Both views share the same focus, heading and six zoom steps.
            ApplyPose();
        }

        public void ResetView()
        {
            if (regionSide > 0f)
            {
                if(meadowFraming)FrameMeadowComparison(regionSide,regionHeight,framedCenter,meadowStartSize);else FrameArea(regionSide, regionHeight, framedCenter);
                return;
            }
            lowerView=false;viewCamera.orthographic=true;
            focus = Vector3.zero;
            pitch = 50f;
            yaw = 45f;
            distance = 65f;
            orthographicSize = 32f;
            ApplyPose();
        }

        // Prepared terrain studies use the same zoom steps as the region camera.
        public void ConfigureTerrainStudy(Vector3 center, float levelOneSize, int level)
        {
            regionSide = 256f; regionHeight = 40f; focus = center;
            distance = 300f; farthestSize = levelOneSize * 16f;
            SetZoomLevel(level);
        }

        public void ApplyPose()
        {
            if (viewCamera == null) viewCamera = GetComponent<Camera>();
            if(viewCamera.orthographicSize!=orthographicSize)viewCamera.orthographicSize=orthographicSize;
            var posePitch = lowerView ? 12f : pitch;
            var poseDistance = lowerView ? orthographicSize / Mathf.Tan(viewCamera.fieldOfView * .5f * Mathf.Deg2Rad) : distance;
            var orientation=Quaternion.Euler(posePitch,yaw,0f);
            var position=focus-orientation*Vector3.forward*poseDistance;
            if(lowerView)
            {
                // Back away along the viewing ray when a mountain occupies the camera position.
                // This retains the focus in the center of the image.
                for(int attempt=0;attempt<16;attempt++)
                {
                    float ground=float.NegativeInfinity;
                    foreach(var terrain in Terrain.activeTerrains)
                    {
                        var local=position-terrain.transform.position;var size=terrain.terrainData.size;
                        if(local.x>=0&&local.z>=0&&local.x<=size.x&&local.z<=size.z)
                            ground=Mathf.Max(ground,terrain.SampleHeight(position)+terrain.transform.position.y);
                    }
                    if(position.y>=ground+8f)break;
                    poseDistance+=(ground+8f-position.y)/Mathf.Sin(posePitch*Mathf.Deg2Rad);
                    position=focus-orientation*Vector3.forward*poseDistance;
                }
            }
            float near=lowerView ? .5f : viewCamera.nearClipPlane,far=Mathf.Max(1500f,poseDistance*2f+500f);
            if (regionSide > 0f)
            {
                // Fit the depth range to the terrain currently on screen. Huge empty
                // foreground/background ranges waste directional shadow texels.
                var depthMargin = orthographicSize * 2f + regionHeight;
                near=lowerView ? .5f : Mathf.Max(1f,poseDistance-depthMargin);far=poseDistance+depthMargin;
                if(FitShadowDepth&&!lowerView)
                {
                    // Orthographic screen Y and world height bound depth exactly:
                    // depth = distance + screenY*cot(pitch) - (height-focusY)/sin(pitch).
                    // Include roots below ground and 80m of canopy/building clearance.
                    var sin=Mathf.Sin(posePitch*Mathf.Deg2Rad);
                    var screenDepth=orthographicSize/Mathf.Tan(posePitch*Mathf.Deg2Rad);
                    near=Mathf.Max(1f,poseDistance-screenDepth-(regionHeight+80f-focus.y)/sin-20f);
                    far=Mathf.Max(near+10f,poseDistance+screenDepth+(focus.y+10f)/sin+20f);
                }
                if(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline&&pipeline.shadowDistance!=far)
                    pipeline.shadowDistance=far;
            }
            if(viewCamera.nearClipPlane!=near)viewCamera.nearClipPlane=near;
            if(viewCamera.farClipPlane!=far)viewCamera.farClipPlane=far;
            if(transform.position!=position||transform.rotation!=orientation)transform.SetPositionAndRotation(position,orientation);
        }

        public void FrameMeadowComparison(float side,float height,Vector3 center,float size=40)
        {
            lowerView=false;
            meadowFraming=true;meadowStartSize=size;
            regionSide=side;regionHeight=height;framedCenter=center;focus=center;
            pitch=35.26439f;yaw=315;distance=280;farthestSize=size*16;
            viewCamera=GetComponent<Camera>();viewCamera.orthographic=true;SetZoomLevel(1);
        }

        public void FrameRegion(float side, float height = 1200f)
        {
            FrameArea(side, height, Vector3.zero);
            var map=GetComponentInParent<World.PrototypeRegionView>()?.GetComponentInChildren<World.RegionReliefMap>(true);
            if(!map)return;
            var bounds=map.GetComponent<Renderer>().bounds;
            foreach(var renderer in map.GetComponentsInChildren<Renderer>())bounds.Encapsulate(renderer.bounds);
            focus=bounds.center;
            var inverse=Quaternion.Inverse(Quaternion.Euler(pitch,yaw,0));
            float horizontal=0,vertical=0;
            for(int i=0;i<8;i++){
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var projected=inverse*(corner-focus);horizontal=Mathf.Max(horizontal,Mathf.Abs(projected.x));vertical=Mathf.Max(vertical,Mathf.Abs(projected.y));
            }
            // Fit the actual relief bounds, with room above/below for the HUD.
            orthographicSize=1.25f*Mathf.Max(vertical,horizontal/Mathf.Max(.1f,viewCamera.aspect));
            farthestSize=orthographicSize;ApplyPose();
        }

        public void FrameArea(float side, float height, Vector3 center)
        {
            lowerView=false;
            meadowFraming=false;
            regionSide = side; regionHeight = height; framedCenter = center;
            focus = center + new Vector3(0f, height * .0375f, 0f);
            pitch = 35.264f;
            yaw = 45f;
            distance = side * 1.6f;
            if (viewCamera == null) viewCamera = GetComponent<Camera>();
            // Project the square into camera space and fit both axes with 12% margin.
            // Include the existing terrain's vertical range so hills cannot clip the frame.
            var projectedSpan = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad))
                + Mathf.Abs(Mathf.Cos(yaw * Mathf.Deg2Rad));
            var halfWidth = side * .5f * projectedSpan;
            var halfHeight = halfWidth * Mathf.Sin(pitch * Mathf.Deg2Rad)
                + height * .5f * Mathf.Cos(pitch * Mathf.Deg2Rad);
            orthographicSize = 1.12f * Mathf.Max(halfHeight, halfWidth / Mathf.Max(.1f, viewCamera.aspect));
            farthestSize = orthographicSize; ZoomLevel = 5; ZoomChanged?.Invoke(ZoomLevel);
            viewCamera.orthographic = true;
            ApplyPose();
        }

        private void ToggleProjection()
        {
            if (viewCamera == null) viewCamera = GetComponent<Camera>();
            var halfFov = viewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            if (viewCamera.orthographic)
                distance = Mathf.Clamp(orthographicSize / Mathf.Tan(halfFov), 5f, regionSide > 0f ? regionSide * 4f : 12000f);
            else
                orthographicSize = Mathf.Clamp(distance * Mathf.Tan(halfFov), 3f, regionSide > 0f ? regionSide : 5000f);
            viewCamera.orthographic = !viewCamera.orthographic;
        }
    }
}
