using CityForge.World;

namespace CityForge.Lots
{
    internal static class VehicleSchedule
    {
        internal static bool CanDepart(LotVehicleScript settings, RegionTimeOfDay phase, LotVehicleJobState job) =>
            !settings.configured || (settings.ResourceTask && phase == settings.when && !job.departedThisWindow);
    }
}
