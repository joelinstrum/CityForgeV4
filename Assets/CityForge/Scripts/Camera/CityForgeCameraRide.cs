using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.CameraSystem
{
    public sealed partial class CityForgeCameraController
    {
        private const float MetersPerSecondPerMph = .44704f;
        private const float MaximumRideSpeed = 7f * MetersPerSecondPerMph;
        public bool Riding { get; private set; }
        private World.RegionCameraState rideReturnState;
        private Vector3 ridePosition;
        private float rideHeading, rideSpeed, rideNear, rideFov;
        private int rideDistrict;
        internal float RideSpeed => rideSpeed;
        internal float RideSteeringInput { get; private set; }
        private DrivingCockpitOverlay rideCockpitOverlay;

        internal bool HandleRideInput(World.PrototypeRegionView region)
        {
            if (Riding)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || region.SelectedDistrict != rideDistrict || region.DistrictLoading)
                    EndRide();
                return true;
            }
            bool command = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
                Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!command || !Input.GetKeyDown(KeyCode.D) || region.SelectedDistrict < 0 ||
                region.DistrictLoading || region.IsSaving || region.BlocksCameraZoom || flyingIntoDistrict) return false;
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected && selected.TryGetComponent<InputField>(out var field) && field.isFocused) return false;
            if (BeginRide(region)) region.PrepareRideTools();
            return true;
        }

        internal bool BeginRide(World.PrototypeRegionView region)
        {
            if (Riding || !region || region.SelectedDistrict < 0 || region.DistrictLoading) return false;
            if (!region.TryStartRide(focus, yaw, out var start, out var heading)) return false;
            rideReturnState = CaptureRegionState();
            rideNear = viewCamera.nearClipPlane;
            rideFov = viewCamera.fieldOfView;
            ridePosition = start;
            rideHeading = heading;
            rideDistrict = region.SelectedDistrict;
            rideSpeed = 0f;
            Riding = true;
            RideSteeringInput = 0;
            lowerView = true;
            ZoomLevel = 0;
            ApplyRidePose();
            ZoomChanged?.Invoke(ZoomLevel);
            rideCockpitOverlay=GetComponent<DrivingCockpitOverlay>();
            if(!rideCockpitOverlay)rideCockpitOverlay=gameObject.AddComponent<DrivingCockpitOverlay>();
            rideCockpitOverlay.Attach(this,region);
            return true;
        }

        public void EndRide()
        {
            if (!Riding) return;
            Riding = false;
            RideSteeringInput = 0;
            if(rideCockpitOverlay)rideCockpitOverlay.Hide();
            rideSpeed = 0f;
            viewCamera.nearClipPlane = rideNear;
            viewCamera.fieldOfView = rideFov;
            RestoreRegionState(rideReturnState);
        }

        private void AdvanceRide(float delta, World.PrototypeRegionView region)
        {
            if (!region || region.SelectedDistrict != rideDistrict || region.DistrictLoading) { EndRide(); return; }
            float steer = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            StepRide(region, Mathf.Min(delta, .1f), steer, Input.GetKey(KeyCode.UpArrow), Input.GetKey(KeyCode.DownArrow));
        }

        internal void StepRide(World.PrototypeRegionView region, float delta, float steer, bool accelerate, bool brake)
        {
            RideSteeringInput = Mathf.Clamp(steer,-1,1);
            float remaining = Mathf.Max(0f, delta);
            while (remaining > 0f)
            {
                float dt = Mathf.Min(remaining, .02f);
                remaining -= dt;
                rideHeading = Mathf.Repeat(rideHeading + steer * 65f * dt, 360f);
                rideSpeed = Mathf.MoveTowards(rideSpeed, brake ? 0f : accelerate ? MaximumRideSpeed : 0f,
                    (brake ? 8f : accelerate ? 3f : .8f) * dt);
                var forward = Quaternion.Euler(0f, rideHeading, 0f) * Vector3.forward;
                var next = ridePosition + forward * (rideSpeed * dt);
                if (region.TryRideGround(new Vector2(next.x, next.z), out float height) &&
                    Mathf.Abs(height + 1.7f - ridePosition.y) <= .5f)
                    ridePosition = new Vector3(next.x, height + 1.7f, next.z);
                else rideSpeed = 0f;
            }
            ApplyRidePose();
        }

        private void ApplyRidePose()
        {
            viewCamera.orthographic = false;
            viewCamera.fieldOfView = 65f;
            viewCamera.nearClipPlane = .08f;
            viewCamera.farClipPlane = Mathf.Max(2000f, regionSide * 2f);
            transform.SetPositionAndRotation(ridePosition, Quaternion.Euler(0f, rideHeading, 0f));
        }

        private void OnGUI()
        {
            if (!Riding || (rideCockpitOverlay&&rideCockpitOverlay.DrawsRideHud)) return;
            GUI.Box(new Rect(16f, 16f, 470f, 48f),
                $"City ride · {rideSpeed / MetersPerSecondPerMph:0} mph\n↑ Accelerate   ↓ Brake   ← → Steer   Esc Return");
        }
    }
}
