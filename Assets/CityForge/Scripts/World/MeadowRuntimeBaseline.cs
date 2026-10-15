using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.World
{
    [Serializable] public sealed class MeadowPlantPiece { public Mesh mesh; public Material material; public int submesh; public Matrix4x4 local = Matrix4x4.identity; }
    [Serializable] public sealed class MeadowPlantType { public MeadowPlantPiece[] pieces; }
    [CreateAssetMenu(menuName="CityForge/Approved Meadow Runtime Baseline")]
    public sealed class MeadowRuntimeBaseline : ScriptableObject
    {
        public int generationVersion=2;
        public Material terrainMaterial, roadMaterial, flowMaterial;
        public TerrainLayer[] layers;
        public GameObject[] treePrefabs;
        public float[] treeWeights, treeWidths, treeHeights;
        public MeadowPlantType[] plants,forestFloorPlants;
        public float forestFloorScale=1;
        public GameObject waterTemplate, fftTemplate, windTemplate, sunTemplate;
        public GameObject shorelineReeds, shorelineGrass;
        public VolumeProfile volume;
        public GameObject cameraSettingsTemplate;
        public string cameraPipelineSettings;
        public Material skybox;
        public AmbientMode ambientMode;
        public Color ambientSky,ambientEquator,ambientGround,ambientLight;
        public float ambientIntensity,reflectionIntensity;
        public float[] ambientSH = new float[27];
        public float treeDistance,treeBillboardDistance,treeCrossFadeLength;
        public int treeMaximumFullLODCount;
        public float lod0Density=.65f,closestZoomDensity=.7f;
        public int referenceHeightResolution=1025;
        public float referenceTreeDensity=.026f;
        public float referenceViewSize=40,treeSpacing=2.5f,roadWidth=3;
        public float canopyRadius=5,shorelineWidth=3;
    }
}
