using System;
using UnityEngine;

namespace CityForge.Lots
{
    public enum SignMountKind { FrontFacing=1, ProjectingBlade=2 }
    [Serializable] public sealed class BuildingSignMount
    {
        public string id, label, mountingInterface, defaultSignId;
        public string originalSignRootPath;
        public string[] retainedHardwarePaths=Array.Empty<string>();
        public SignMountKind kind;
        public Vector3 position, eulerAngles, wallOutward=Vector3.forward;
        // Native Unity sign-local metres: face width X, height Y, thickness Z, hardware included.
        public Vector3 maxSignSize;
        public float maxProjection, heightAboveWalkway;
        public bool clearanceReviewed;
        public string[] allowedSignIds=Array.Empty<string>();
    }
    // Prepared on each native tier. Missing profiles keep the approved original fixed sign.
    [DisallowMultipleComponent]
    public sealed class BuildingSignMountProfile : MonoBehaviour
    {
        public int contractVersion=1;
        public bool mediumTier;
        public BuildingSignMount[] mounts=Array.Empty<BuildingSignMount>();
    }
}
