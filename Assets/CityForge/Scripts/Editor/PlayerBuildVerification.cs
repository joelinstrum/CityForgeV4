using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CityForge.Editor
{
    public static class PlayerBuildVerification
    {
        [MenuItem("CityForge/Verify macOS Player Build")]
        public static void Run()
        {
            RegionPbrKitBuilder.BuildGraph();
            AssetDatabase.SaveAssets();
            var output = Path.GetFullPath("Builds/Verification/CityForge.app");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });
            Debug.Log($"CityForge player verification: {report.summary.result}; errors={report.summary.totalErrors}; warnings={report.summary.totalWarnings}; output={output}");
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("CityForge player verification failed.");
        }
    }
}
