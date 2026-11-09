using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        private readonly List<(Vector2 position, float radius, GameObject stone)> harborStones = new();

        private TerrainLayer CreateHarborRockLayer()
        {
            if (!hasDeepHarbor) return null;
            var source = Resources.Load<TerrainLayer>("Mountains/VariedCliffDark");
            if (!source) return null;

            var layer = Own(Instantiate(source));
            layer.name = "Deep Harbor wet rock";
            layer.tileSize = Vector2.one * 9f;
            layer.diffuseRemapMax = new Vector4(.57f, .60f, .59f, 1);
            layer.normalScale = .85f;
            layer.smoothness = .18f;
            return layer;
        }

        private TerrainLayer CreateHarborSoilLayer()
        {
            var source = Resources.Load<TerrainLayer>("RiverBeds/RichSoil");
            if (!source) return null;

            var layer = Own(Instantiate(source));
            layer.name = "Deep Harbor damp earth";
            layer.specular = new Color(.025f, .025f, .025f);
            layer.normalScale = .65f;
            layer.smoothness = 0;
            return layer;
        }

        private float HarborWetRock(Vector2 point, float distance)
        {
            float harbor = DeepHarborWeight(point);
            if (harbor <= 0 || distance < -12f) return 0;
            float reach = Mathf.Lerp(4f, 11f, Noise(point, 18, 810));
            float fade = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.6f, reach, -distance));
            return harbor * fade * Mathf.Lerp(.88f, 1f, Noise(point, 6, 811));
        }

        // Rock and soil replace portions of the existing meadow paint in the dry transition.
        private Vector2 HarborDryCover(Vector2 point, float distance)
        {
            float harbor = DeepHarborWeight(point);
            if (harbor <= 0 || distance > 10f) return Vector2.zero;
            float patch = Noise(point, 12, 812);
            float rockReach = Mathf.Lerp(1.3f, 5.4f, patch);
            float rock = (1 - Mathf.SmoothStep(0, 1, distance / rockReach)) * Mathf.Lerp(.72f, .97f, patch);
            float soilReach = Mathf.Lerp(4.5f, 10f, Noise(point, 20, 813));
            float soilPatch = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.38f, .72f, Noise(point, 7, 814)));
            float soil = (1 - rock) * (1 - Mathf.SmoothStep(0, 1, distance / soilReach)) * soilPatch * .55f;
            return new Vector2(rock, soil) * harbor;
        }

        private IEnumerator BuildHarborStones()
        {
            if (!hasDeepHarbor || coastalBeachDistances == null) yield break;
            var assets = Resources.Load<RiverBedRockAssets>("IndividualStreamRocks/Stones");
            if (!assets || assets.meshes.Length == 0 || assets.materials.Length == 0 || !assets.materials[0]) yield break;

            const int maxStones = 120;
            const float spacing = 4f;
            var material = Own(new Material(assets.materials[0]) { name = "Deep Harbor shore stones" });
            material.color *= new Color(.71f, .76f, .76f, 1f);
            var group = new GameObject("Deep Harbor shore stones").transform;
            group.SetParent(transform, false);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            int startX = Mathf.FloorToInt(Origin.x / spacing);
            int startZ = Mathf.FloorToInt(Origin.y / spacing);
            int endX = Mathf.CeilToInt((Origin.x + Side) / spacing);
            int endZ = Mathf.CeilToInt((Origin.y + Side) / spacing);
            for (int z = startZ; z < endZ && harborStones.Count < maxStones; z++)
            {
                for (int x = startX; x < endX && harborStones.Count < maxStones; x++)
                {
                    var center = new Vector2((x + .5f) * spacing, (z + .5f) * spacing);
                    if (!Contains(center) || Mathf.Abs(CoastalBeachDistance(center)) > 6f || DeepHarborWeight(center) < .75f)
                        continue;
                    var random = RandomFor(x, z, 815);
                    float Next() => (float)random.NextDouble();
                    var point = center + new Vector2(Next() - .5f, Next() - .5f) * spacing;
                    float distance = CoastalBeachDistance(point);
                    float patch = Noise(point, 14, 816);
                    if (!Contains(point) || distance < -1.8f || distance > 4.2f || Next() > Mathf.Lerp(.05f, .65f, patch) ||
                        View.InCampClearing(point, 2f) || RoadDistance(point) < Baseline.roadWidth + 1f)
                        continue;

                    float ground = Height(point);
                    if (ground < View.SeaLevel - 1.1f || ground > View.SeaLevel + 3f) continue;
                    var mesh = assets.meshes[random.Next(assets.meshes.Length)];
                    if (!mesh) continue;
                    float size = Mathf.Lerp(.55f, 2.4f, Next() * Next());
                    var extent = mesh.bounds.size;
                    float scale = size / Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z));
                    var rock = new GameObject("Shore stone", typeof(MeshFilter), typeof(MeshRenderer));
                    rock.transform.SetParent(group, false);
                    rock.transform.rotation = Quaternion.Euler((Next() - .5f) * 22f, Next() * 360f, (Next() - .5f) * 18f);
                    rock.transform.localScale = Vector3.one * scale;
                    rock.transform.position = new Vector3(point.x, ground + size * .12f, point.y) -
                        rock.transform.rotation * (mesh.bounds.center * scale);
                    rock.GetComponent<MeshFilter>().sharedMesh = mesh;
                    rock.GetComponent<MeshRenderer>().sharedMaterial = material;
                    harborStones.Add((point, size * .5f, rock));
                }
                if (timer.ElapsedMilliseconds > 10) { yield return null; timer.Restart(); }
            }
            RefreshHarborStones();
        }

        internal void RefreshHarborStones()
        {
            foreach (var (position, radius, stone) in harborStones)
                stone.SetActive(!View.HarborStoneOverlapsLot(position, radius));
        }
    }
}
