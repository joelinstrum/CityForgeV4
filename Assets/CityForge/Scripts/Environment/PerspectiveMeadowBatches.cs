using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge
{
    // Collect after each tile has selected its existing density level; flush after all tiles.
    [DefaultExecutionOrder(32000)]
    internal sealed class PerspectiveMeadowBatches : MonoBehaviour
    {
        const int Capacity = 511;
        const float CellSide = 128;
        sealed class Group
        {
            public Mesh mesh; public Material material; public int submesh, count, frame;
            public Bounds bounds;
            public readonly List<Matrix4x4[]> chunks = new();
        }
        static PerspectiveMeadowBatches instance;
        readonly Dictionary<(Mesh, Material, int, int, int), Group> groups = new();
        readonly List<Group> active = new();
        readonly List<(Mesh, Material, int, int, int)> expired = new();
        int queuedFrame = -1;
        public static int SourceInstances { get; private set; }
        public static int DrawnInstances { get; private set; }
        public static int DrawCalls { get; private set; }
        public static double FlushMilliseconds { get; private set; }

        public static void Queue(NativeMeadowBatch batch, Matrix4x4[] matrices)
        {
            if (!instance) {
                var go = new GameObject("Perspective meadow submissions") { hideFlags = HideFlags.HideAndDontSave };
                instance = go.AddComponent<PerspectiveMeadowBatches>();
            }
            instance.Add(batch, matrices);
        }
        void Add(NativeMeadowBatch batch, Matrix4x4[] matrices)
        {
            int frame = Time.frameCount;
            if (queuedFrame != frame) { queuedFrame = frame; active.Clear(); SourceInstances = 0; }
            var key = (batch.mesh, batch.material, batch.submesh,
                Mathf.FloorToInt(batch.bounds.center.x / CellSide), Mathf.FloorToInt(batch.bounds.center.z / CellSide));
            if (!groups.TryGetValue(key, out var group)) {
                group = new Group { mesh = batch.mesh, material = batch.material, submesh = batch.submesh, frame = -1 };
                groups.Add(key, group);
            }
            if (group.frame != frame) {
                group.frame = frame; group.count = 0; group.bounds = batch.bounds; active.Add(group);
            } else group.bounds.Encapsulate(batch.bounds);
            int source = 0;
            while (source < matrices.Length) {
                int chunk = group.count / Capacity, offset = group.count % Capacity;
                if (chunk == group.chunks.Count) group.chunks.Add(new Matrix4x4[Capacity]);
                int count = Mathf.Min(Capacity - offset, matrices.Length - source);
                Array.Copy(matrices, source, group.chunks[chunk], offset, count);
                source += count; group.count += count;
            }
            SourceInstances += matrices.Length;
        }
        void LateUpdate()
        {
            DrawnInstances = DrawCalls = 0;
            if (queuedFrame != Time.frameCount) SourceInstances = 0;
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            if (queuedFrame == Time.frameCount) foreach (var group in active) {
                if (!group.mesh || !group.material) continue;
                var parameters = new RenderParams(group.material) { worldBounds = group.bounds,
                    shadowCastingMode = ShadowCastingMode.Off, receiveShadows = true };
                for (int start = 0; start < group.count; start += Capacity) {
                    int count = Mathf.Min(Capacity, group.count - start);
                    // No camera filter: reflection cameras retain the same submissions.
                    Graphics.RenderMeshInstanced(parameters, group.mesh, group.submesh,
                        group.chunks[start / Capacity], count);
                    DrawnInstances += count; DrawCalls++;
                }
            }
            FlushMilliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
            if (Time.frameCount % 300 == 0) {
                expired.Clear();
                foreach (var pair in groups) if (Time.frameCount - pair.Value.frame > 300) expired.Add(pair.Key);
                foreach (var key in expired) groups.Remove(key);
            }
        }
        void OnDestroy() { if (instance == this) { instance = null; SourceInstances = DrawnInstances = DrawCalls = 0; } }
    }
}
