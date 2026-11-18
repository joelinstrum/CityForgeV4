using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class HooliganHarassSetup
{
    internal static void Prepare()
    {
        const string root = "Assets/CityForge/Resources/Lots/V3Characters/Hooligan/";
        const string path = root + "HarassUnarmed.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) return;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Model.fbx");
        var renderer = model ? model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
        if (!renderer || !File.Exists(root + "Color.jpg")) return;
        int hand = System.Array.FindIndex(renderer.bones, bone => bone.name == "R_Hand");
        if (hand < 0) return;
        var texture = new Texture2D(2, 2);
        try
        {
            texture.LoadImage(File.ReadAllBytes(root + "Color.jpg"));
            var source = renderer.sharedMesh;
            var mesh = Object.Instantiate(source);
            mesh.name = source.name + " Harass Unarmed";
            var weights = source.boneWeights;
            var uv = source.uv;
            float HandWeight(int i)
            {
                var w = weights[i];
                return (w.boneIndex0 == hand ? w.weight0 : 0) + (w.boneIndex1 == hand ? w.weight1 : 0) +
                    (w.boneIndex2 == hand ? w.weight2 : 0) + (w.boneIndex3 == hand ? w.weight3 : 0);
            }
            bool Weapon(int a, int b, int c)
            {
                if ((HandWeight(a) + HandWeight(b) + HandWeight(c)) / 3 < .5f) return false;
                var p = (uv[a] + uv[b] + uv[c]) / 3;
                var color = texture.GetPixelBilinear(p.x, p.y);
                // This source's handheld prop is dark neutral metal/wood; exposed skin is warm.
                return color.r < .40f && color.r - color.b < .13f;
            }
            int removed = 0;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var indices = source.GetTriangles(sub);
                var kept = new System.Collections.Generic.List<int>();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    if (Weapon(indices[i], indices[i + 1], indices[i + 2])) { removed++; continue; }
                    kept.Add(indices[i]); kept.Add(indices[i + 1]); kept.Add(indices[i + 2]);
                }
                // Remove tiny prop fragments disconnected by the cut, joining UV-seam duplicates by position.
                var parent = Enumerable.Range(0, source.vertexCount).ToArray();
                int Root(int v) { while (parent[v] != v) { parent[v] = parent[parent[v]]; v = parent[v]; } return v; }
                void Join(int a, int b) { parent[Root(a)] = Root(b); }
                var positions = source.vertices;
                var welded = new System.Collections.Generic.Dictionary<Vector3Int, int>();
                for (int i = 0; i < positions.Length; i++)
                {
                    var key = Vector3Int.RoundToInt(positions[i] * 100000);
                    if (welded.TryGetValue(key, out int other)) Join(i, other); else welded[key] = i;
                }
                for (int i = 0; i < kept.Count; i += 3) { Join(kept[i], kept[i + 1]); Join(kept[i], kept[i + 2]); }
                var counts = new System.Collections.Generic.Dictionary<int, int>();
                for (int i = 0; i < kept.Count; i += 3)
                { int key = Root(kept[i]); counts[key] = (counts.TryGetValue(key, out int count) ? count : 0) + 1; }
                var clean = new System.Collections.Generic.List<int>();
                for (int i = 0; i < kept.Count; i += 3)
                {
                    if (counts[Root(kept[i])] < 32 && HandWeight(kept[i]) > .35f) { removed++; continue; }
                    clean.Add(kept[i]); clean.Add(kept[i + 1]); clean.Add(kept[i + 2]);
                }
                mesh.SetTriangles(clean, sub);
            }
            AssetDatabase.CreateAsset(mesh, path);
            Debug.Log("Prepared unarmed Harass mesh: removed " + removed + " handheld-prop triangles.");
        }
        finally { Object.DestroyImmediate(texture); }
    }
}
