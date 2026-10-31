using System;
using UnityEngine;
using MapMagic.Nodes;
using MapMagic.Nodes.MatrixGenerators;
using MapMagic.Products;
using MapMagic.Core;
using MapMagic.Terrains;
using Den.Tools;
using Den.Tools.Matrices;
namespace CityForge.World
{
    /// <summary>One bounded MapMagic graph, sampled once per region. No scene tiles or vegetation.</summary>
    public sealed class MapMagicRegionGeography : IDisposable
    {
        public Graph Graph {get;}
        readonly Erosion200 erosion;
        readonly bool tallerSummits;
        readonly TileData data;
        public MapMagicRegionGeography(RegionCreationSettings recipe,RegionSize size)
        {
            recipe.Validate();tallerSummits=recipe.version>=10&&recipe.terrainShape==RegionTerrainShape.MountainBorder;
            Graph=ScriptableObject.CreateInstance<Graph>();Graph.name="CityForge regional geography v1";Graph.random=new Noise(recipe.seed,32768);
            float side=1024f*Mathf.Pow(2,(int)size)*(recipe.GridSide/2f);
            var noise=(Noise200)Generator.Create(typeof(Noise200));noise.seed=173;noise.type=Noise200.Type.Perlin;noise.size=side*(recipe.version>=5?.09f:.32f);noise.detail=recipe.version>=5?.62f:.48f;noise.turbulence=recipe.version>=5?.35f:.18f;Graph.Add(noise);
            var shaping=(Levels200)Generator.Create(typeof(Levels200));shaping.inMin=.23f;shaping.inMax=.78f;shaping.gamma=recipe.topography<=1?1f:1.8f;
            shaping.outMin=0;shaping.outMax=recipe.topography switch{0=>0f,1=>.085f,2=>.18f,3=>.37f,_=>.58f};Graph.Add(shaping);Graph.Link(shaping,noise);
            erosion=(Erosion200)Generator.Create(typeof(Erosion200));erosion.iterations=recipe.topography==0?0:recipe.version>=5?2:4;erosion.terrainDurability=.82f;erosion.erosionAmount=.65f;erosion.sedimentAmount=.55f;erosion.fluidityIterations=3;Graph.Add(erosion);
            if(recipe.version>=5){var landforms=(RegionalLandformNode)Generator.Create(typeof(RegionalLandformNode));landforms.seed=recipe.seed;landforms.topography=recipe.topography;landforms.side=side;landforms.variedMountainBorder=recipe.version>=11;landforms.compactMountainBorder=recipe.version>=9;landforms.naturalMountainBorder=recipe.version>=8;landforms.ruggedMountainBorder=recipe.version==7;landforms.mountainBorder=recipe.version>=6&&recipe.terrainShape==RegionTerrainShape.MountainBorder;Graph.Add(landforms);Graph.Link(landforms,noise);Graph.Link((IInlet<MatrixWorld>)erosion,(IOutlet<MatrixWorld>)landforms);}
            else Graph.Link((IInlet<MatrixWorld>)erosion,(IOutlet<MatrixWorld>)shaping);
            var output=(HeightOutput200)Generator.Create(typeof(HeightOutput200));Graph.Add(output);Graph.Link(output,erosion);
            data=new TileData{area=new Area(new Vector2D(0,0),new Vector2D(side,side),513,16),globals=new Globals{height=PrototypeRegionData.Height},random=Graph.random,isDraft=false,isPreview=false};
        }
        // Graph construction/disposal belong on the main thread. MapMagic's matrix pipeline can run on a worker.
        public float[,] Generate()
        {
            Graph.Generate(data,new StopToken());var field=data.ReadOutletProduct((IOutlet<MatrixWorld>)erosion);
            if(field==null)throw new InvalidOperationException("MapMagic did not produce regional heights.");
            var result=new float[513,513];var offset=data.area.active.rect.offset;
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){
                float value=field[x+offset.x,z+offset.z];if(!float.IsFinite(value))throw new InvalidOperationException("MapMagic produced non-finite terrain.");if(tallerSummits)value+=.30f*Mathf.Max(0,value-.13f);
                result[z,x]=Mathf.Clamp01(value);
            }
            return result;
        }
        public void Dispose(){if(Graph){if(Application.isPlaying)UnityEngine.Object.Destroy(Graph);else UnityEngine.Object.DestroyImmediate(Graph);}}
    }
}
