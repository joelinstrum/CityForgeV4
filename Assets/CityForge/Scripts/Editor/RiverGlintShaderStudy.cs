#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace CityForge.EditorTools
{
    /// <summary>Builds a local-only derivative from the installed licensed SDK, never SDK source in Git.</summary>
    [InitializeOnLoad]
    public static class RiverGlintShaderStudy
    {
        public const string ShaderPath = "Assets/CityForge/LocalWaterStudy/RiverGlint.shader";
        static RiverGlintShaderStudy() => EditorApplication.update += Poll;
        static void Poll()
        {
            const string request = "Library/RiverGlintShader.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try
            {
                var importer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.ShaderGraph.ShaderGraphImporter")).First(t => t != null);
                var method = importer.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(m => m.Name == "GetShaderText" && m.GetParameters().Length == 4 && m.GetParameters()[3].IsOut);
                object[] args = { "Packages/com.waveharmonic.crest/Runtime/Shaders/Surface/Water.shadergraph", null, null, null };
                string source = (string)method.Invoke(null, args);
                string forward = "#include \"Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl\"";
                if (!source.Contains(forward)) throw new InvalidOperationException("Installed URP forward-pass structure differs; no shader generated.");
                int title = source.IndexOf('"'), endTitle = source.IndexOf('"', title + 1);
                source = source.Substring(0, title + 1) + "Hidden/CityForge/LocalRiverGlintStudy" + source.Substring(endTitle);
                source = source.Replace(forward, "#include \"Assets/CityForge/Rendering/Water/RiverGlintResponse.hlsl\"");
                int properties = source.IndexOf('{', source.IndexOf("Properties", StringComparison.Ordinal));
                source = source.Insert(properties + 1, "\n_CityForgeGlintStart(\"Glint start\",Float)=0.1\n_CityForgeGlintEnd(\"Glint end\",Float)=0.5\n_CityForgeGlintGain(\"Glint gain\",Float)=1\n_CityForgeRippleEvolution(\"Ripple evolution\",Float)=0.018\n_CityForgeRippleEvolutionSpeed(\"Ripple evolution speed\",Float)=1\n");
                Directory.CreateDirectory(Path.GetDirectoryName(ShaderPath));
                File.WriteAllText(ShaderPath, source);
                AssetDatabase.ImportAsset(ShaderPath, ImportAssetOptions.ForceSynchronousImport);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Generated shader has errors; baseline untouched.");
                File.WriteAllText("Library/RiverGlintShader.txt", "BUILT: local generated derivative; SDK files unchanged. Runtime rendering must still be verified.");
            }
            catch (Exception e) { File.WriteAllText("Library/RiverGlintShader.txt", "FAIL: " + e); Debug.LogException(e); }
        }
    }
}
#endif
