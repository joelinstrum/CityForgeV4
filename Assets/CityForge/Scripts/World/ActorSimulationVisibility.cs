using System.Runtime.CompilerServices;
using UnityEngine;

namespace CityForge.World
{
    internal static class ActorSimulationVisibility
    {
        internal const float ViewMargin = 18f;
        private static readonly ConditionalWeakTable<Camera, CameraFrustum> frusta = new();

        internal static bool NearCamera(Camera camera, Vector3 position)
        {
            if (!camera) return true;
            var frustum = frusta.GetValue(camera, _ => new CameraFrustum());
            if (frustum.frame != Time.frameCount)
            {
                GeometryUtility.CalculateFrustumPlanes(camera, frustum.planes);
                frustum.frame = Time.frameCount;
            }
            return GeometryUtility.TestPlanesAABB(frustum.planes,
                new Bounds(position, Vector3.one * (2f * ViewMargin)));
        }

        private sealed class CameraFrustum
        {
            internal readonly Plane[] planes = new Plane[6];
            internal int frame = -1;
        }
    }

    internal struct ActorSimulationCadence
    {
        private float pending, remaining;
        private bool offscreen;

        internal float Step(float delta, bool nearCamera, int instanceId)
        {
            pending += delta;
            if (nearCamera) offscreen = false;
            else
            {
                if (!offscreen)
                {
                    offscreen = true;
                    remaining = .15f + (instanceId & 15) * (.35f / 15f);
                }
                remaining -= delta;
                if (remaining > 0f) return 0f;
                remaining += .5f;
            }
            float elapsed = pending;
            pending = 0f;
            return elapsed;
        }
    }
}
