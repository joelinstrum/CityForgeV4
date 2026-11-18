using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal sealed class DistrictHarass : MonoBehaviour
    {
        internal LotHarass behavior;
        internal Camera districtCamera;
        private void LateUpdate()
        {
            if (behavior != null && ActorSimulationVisibility.NearCamera(districtCamera, transform.position))
                behavior.Tick(Mathf.Min(Time.deltaTime, .1f));
        }
    }
}
