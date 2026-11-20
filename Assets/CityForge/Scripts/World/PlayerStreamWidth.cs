using UnityEngine;

namespace CityForge.World
{
    public enum StreamWidthVariation { Uniform, Gentle, Natural }
    internal static class PlayerStreamWidth
    {
        // Input/output are normalized region units, independent of district seams.
        internal static float AtStation(RegionWaterBody body, float station)
        {
            if (!body.playerChannel || !body.isStream || body.playerStreamWidthVersion != 1) return body.halfWidthNormalized;
            if (body.playerStreamWidthVariation == 0) return body.halfWidthNormalized * .75f;
            if (!body.streamWidthPhaseReady)
            {
                uint hash = 2166136261;
                foreach (char c in body.id ?? "") hash = unchecked((hash ^ c) * 16777619);
                body.streamWidthPhase = hash / (float)uint.MaxValue * Mathf.PI * 2;
                body.streamWidthPhaseReady = true;
            }
            float tiles = station / Mathf.Max(.0000001f, body.halfWidthNormalized);
            float phase = body.streamWidthPhase;
            float wave = Mathf.Sin(tiles * .43f + phase) * .7f + Mathf.Sin(tiles * .17f + phase * 2.7f) * .3f;
            float amplitude = body.playerStreamWidthVariation == 1 ? .125f : .25f;
            return body.halfWidthNormalized * (.75f + amplitude * wave);
        }
        internal static float AtPoint(RegionWaterBody body, Vector2 point, float side)
        {
            PlayerChannelGeometry.Sampler(body,side).Query(point,out _,out var current);
            return AtStation(body,current.x/side)*side;
        }
    }
}
