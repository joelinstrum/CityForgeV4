using System;
using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class LotResourceVehicle : MonoBehaviour
    {
        private PrototypeRegionView view;
        private DistrictLotRecord camp, receiver;
        private LotObjectDefinition authored;
        private LotVehicleJobState job;
        private Camera districtCamera;
        private Vector2 home;
        private Quaternion homeRotation;
        private Vector2[] route;
        private int next;
        private float replan, idlePoll;
        private EstablishedRoute clearance, savedOutboundClearance, savedReturnClearance;
        private Vector2[] savedOutbound, savedReturn;
        private DistrictLotRecord savedReceiver;
        private Vector2 savedReceiverCenter, receiverCenter;
        private PrototypeRegionView.RouteRoad[] roads, savedRoads;
        private Func<Vector2, Vector2, bool> sectionAvailable;
        private float speed, cornerSpeed;
        private bool travelling;
        private Action<Vector2, float, float> advanceVehicle;
        private ActorSimulationCadence cadence;
        private VehicleCargoPresentation cargoView;
        private Vector2 Point => new(transform.position.x, transform.position.z);

        internal void Initialize(PrototypeRegionView region, DistrictLotRecord source, LotObjectDefinition item, Camera camera)
        {
            view = region; camp = source; authored = item; districtCamera = camera;
            sectionAvailable = (from, to) => view.RouteRoadAvailable((from + to) * .5f, roads) && view.FreightClear(from, to);
            home = Point; homeRotation = transform.rotation;
            camp.vehicleJobs ??= new List<LotVehicleJobState>();
            job = camp.vehicleJobs.Find(state => state.objectId == item.id);
            if (job == null) { job = new LotVehicleJobState { objectId = item.id }; camp.vehicleJobs.Add(job); }
            if (job.hasPosition && job.phase != LotFreightPhase.Waiting && view.ApprovedDistrict.Contains(job.position)) Place(job.position);
            else { job.position = home; job.hasPosition = true; }
            speed = VehicleProfile.Find(item.assetId)?.speed ?? 2.25f;
            advanceVehicle = view.FreightVehicleMotion(transform, item.assetId);
            cargoView = VehicleCargoPresentation.Attach(transform, VehicleProfile.Find(item.assetId)); UpdateCargo();
        }

        private void Update()
        {
            if (!view || job == null || view.DistrictLoading || view.IsSaving || !view.ApprovedDistrict || view.SelectedDistrict < 0) return;
            if (job.phase is LotFreightPhase.Waiting or LotFreightPhase.Loading or LotFreightPhase.Unloading)
            {
                idlePoll -= Time.deltaTime;
                if (idlePoll > 0) return;
                idlePoll = .5f;
                Tick(.5f);
                return;
            }
            float delta = cadence.Step(Mathf.Min(Time.deltaTime, .1f), ActorSimulationVisibility.NearCamera(districtCamera, transform.position), GetInstanceID());
            if (delta > 0) Tick(delta);
        }

        internal void Tick(float delta)
        {
            if (job == null || !view || view.IsSaving || view.DistrictLoading) return;
            var settings = authored.vehicleScript;
            if (settings.configured && view.TimeOfDay != settings.when) job.departedThisWindow = false;
            if (job.phase is LotFreightPhase.Waiting or LotFreightPhase.Loading)
            {
                SetTravelling(false);
                if (job.cargo > 0 && job.cargoResource != settings.resource)
                {
                    int stored = VehicleResources.Stored(camp, job.cargoResource);
                    if (stored > LotFreightValidation.StockLimit - job.cargo) return;
                    VehicleResources.Store(camp, job.cargoResource, stored + job.cargo);
                    job.cargo = 0; job.phase = LotFreightPhase.Waiting; job.handlingProgress = 0;
                    UpdateCargo();
                }
                var resource = settings.resource;
                if (job.cargo < settings.Capacity)
                {
                    if (VehicleResources.Stored(camp, resource) > 0)
                    {
                        float progress = job.handlingProgress + settings.Capacity * delta / settings.HandlingSeconds;
                        int moved = LotFreightLedger.LoadSome(camp, job, Mathf.FloorToInt(progress + .0001f), settings.Capacity, resource);
                        job.handlingProgress = Mathf.Clamp(progress - moved, 0, .9999f);
                        if (moved > 0) UpdateCargo();
                    }
                    if (job.cargo < settings.Capacity) return;
                }
                // Loading is independent of the departure window. A full wagon waits for its scheduled time.
                if (!VehicleSchedule.CanDepart(settings, view.TimeOfDay, job)) return;
                replan -= delta;
                if (replan > 0) return;
                if (savedOutbound != null && ReceiverValid(savedReceiver, settings.configured) && savedReceiver.center == savedReceiverCenter && (Point - savedOutbound[0]).sqrMagnitude < .01f)
                { receiver = savedReceiver; receiverCenter = savedReceiverCenter; route = savedOutbound; clearance = savedOutboundClearance; roads = savedRoads; }
                else
                {
                    if (!view.TryFreightDestination(camp, Point, null, out receiver, out route, settings.configured)) { replan = 5f; return; }
                    EstablishRoute(true);
                }
                if (!LotFreightLedger.Dispatch(job, settings.Capacity, receiver.id)) return;
                job.departedThisWindow = settings.configured;
                next = route.Length > 1 ? 1 : route.Length;
                UpdateCargo(); replan = 0;
            }
            if (job.phase == LotFreightPhase.Unloading)
            {
                SetTravelling(false);
                receiver ??= view.PlacedLots.Find(lot => lot.id == job.destinationLotId);
                if (receiver == null || !view.PlacedLots.Contains(receiver) || receiver.definition.DeliveryRole == LotDeliveryRole.None ||
                    (settings.configured && receiver.definition.DeliveryRole != LotDeliveryRole.TownCenter))
                { job.phase = LotFreightPhase.Outbound; job.handlingProgress = 0; route = null; replan = 0; return; }
                float progress = job.handlingProgress + settings.Capacity * delta / settings.HandlingSeconds;
                int moved = LotFreightLedger.UnloadSome(receiver, job, Mathf.FloorToInt(progress + .0001f));
                job.handlingProgress = job.cargo == 0 ? 0 : Mathf.Clamp(progress - moved, 0, .9999f);
                if (moved > 0) UpdateCargo();
                if (job.phase == LotFreightPhase.Returning) { route = null; replan = 0; }
                return;
            }
            if (job.phase == LotFreightPhase.Outbound && receiver != null &&
                (receiver.center != receiverCenter || !view.PlacedLots.Contains(receiver) || receiver.definition.DeliveryRole == LotDeliveryRole.None ||
                 (settings.configured && receiver.definition.DeliveryRole != LotDeliveryRole.TownCenter))) { ForgetRoutes(); replan = 0; }
            if (route == null)
            {
                replan -= delta;
                if (replan > 0) { SetTravelling(false); return; }
                replan = 5f;
                if (job.phase == LotFreightPhase.Outbound)
                {
                    if (!view.TryFreightDestination(camp, Point, job.destinationLotId, out receiver, out route, settings.configured)) { SetTravelling(false); return; }
                    job.destinationLotId = receiver.id;
                    EstablishRoute(true);
                }
                else if (savedReturn != null && (Point - savedReturn[0]).sqrMagnitude < .01f)
                { route = savedReturn; clearance = savedReturnClearance; roads = savedRoads; }
                else
                {
                    if (!view.FreightRoute(Point, home, 24f, 24f, true, out route)) { SetTravelling(false); return; }
                    EstablishRoute(false);
                }
                next = route.Length > 1 ? 1 : route.Length;
            }
            if (next < route.Length)
            {
                cornerSpeed = VehicleCornering.Speed(route, Point, next, 1, false, speed, cornerSpeed, delta);
                float budget = cornerSpeed * delta;
                SetTravelling(true);
                // Consume the remaining distance across sample boundaries instead of losing
                // part of each frame's movement on the short segments around a corner.
                for (int segment = 0; segment < route.Length && next < route.Length && budget > .00001f; segment++)
                {
                    var destination = Vector2.MoveTowards(Point, route[next], budget);
                    if (!clearance.CanTravel(next - 1, Point, destination, new NavigationState(view), sectionAvailable))
                    { ForgetRoutes(); replan = 2f; SetTravelling(false); return; }
                    float distance = Vector2.Distance(destination, Point);
                    if (distance > .00001f)
                        advanceVehicle(VehicleCornering.Heading(route, Point, next, 1, false), distance, distance / Mathf.Max(.1f, cornerSpeed));
                    Place(destination);
                    budget -= distance;
                    if ((Point - route[next]).sqrMagnitude < .0001f) next++;
                    else break;
                }
                return;
            }
            SetTravelling(false);
            if (job.phase == LotFreightPhase.Outbound)
            {
                job.phase = LotFreightPhase.Unloading;
                job.handlingProgress = 0; UpdateCargo();
            }
            else
            {
                job.phase = LotFreightPhase.Waiting; job.handlingProgress = 0; route = null;
                transform.rotation = homeRotation; UpdateCargo();
            }
        }

        private bool ReceiverValid(DistrictLotRecord lot, bool townCenterOnly) => lot != null && view.PlacedLots.Contains(lot) &&
            lot.definition.DeliveryRole != LotDeliveryRole.None && (!townCenterOnly || lot.definition.DeliveryRole == LotDeliveryRole.TownCenter);

        private void EstablishRoute(bool outbound)
        {
            var state = new NavigationState(view);
            if (outbound) receiverCenter = receiver.center;
            clearance = new EstablishedRoute(route, state);
            roads = view.CaptureRouteRoads();
            if (!outbound || (route[0] - home).sqrMagnitude >= .01f) return;
            savedOutbound = route; savedOutboundClearance = clearance; savedReceiver = receiver; savedReceiverCenter = receiver.center; savedRoads = roads;
            savedReturn = (Vector2[])route.Clone(); Array.Reverse(savedReturn);
            savedReturnClearance = new EstablishedRoute(savedReturn, state);
        }

        private void ForgetRoutes()
        {
            route = savedOutbound = savedReturn = null;
            clearance = savedOutboundClearance = savedReturnClearance = null;
            savedReceiver = null;
        }

        private void Place(Vector2 point)
        {
            transform.position = new Vector3(point.x, view.ApprovedDistrict.Height(point), point.y);
            job.position = point; job.hasPosition = true;
        }
        private void SetTravelling(bool value)
        {
            if (travelling == value) return;
            travelling = value;
            if (!value) cornerSpeed = 0f;
            PrototypeRegionView.SetVehicleMotion(transform, authored.assetId, value);
        }
        private void UpdateCargo()
        {
            cargoView?.SetLoad(job.cargoResource, job.cargo, authored.vehicleScript.Capacity);
            view.RefreshSelectedFreight(camp);
            if (receiver != null) view.RefreshSelectedFreight(receiver);
        }
        private void OnDisable()
        {
            if (authored != null) SetTravelling(false);
        }
    }
}
