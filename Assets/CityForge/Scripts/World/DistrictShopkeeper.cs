using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class DistrictShopkeeper : MonoBehaviour
    {
        internal LotShopkeeper behavior;
        internal Camera districtCamera;
        private ActorSimulationCadence cadence;

        private void LateUpdate()
        {
            if(PrototypeRegionView.ZoningSimulationPaused)return;
            if (behavior == null) return;
            bool nearCamera = ActorSimulationVisibility.NearCamera(districtCamera, transform.position);
            float elapsed = cadence.Step(Mathf.Min(Time.deltaTime, .1f), nearCamera, GetInstanceID());
            if (elapsed > 0f) behavior.Tick(elapsed);
        }
    }
}
