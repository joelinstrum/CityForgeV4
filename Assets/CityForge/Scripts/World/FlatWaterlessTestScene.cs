using System.Collections;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Independent seeded diagnostic map using the approved terrain pipeline, with no water objects.</summary>
    public sealed class FlatWaterlessTestScene:MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null;
            const int seed=730422;var heights=new float[513,513];var water=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){
                // Small region is 1024 m across and 43.19 m high: approximately 1.3 m of gentle relief.
                heights[z,x]=.20f+.03f*Mathf.PerlinNoise(seed*.001f+x/180f,z/180f);
                water[z,x]=PrototypeRegionData.Side;
            }
            var snapshot=new SavedRegionSnapshot{info=new SavedRegionInfo{id="flat-waterless-test",name="Flat Waterless Test",seed=seed,size=(int)RegionSize.Small,district=3,meadow=true,waterless=true},data=new PrototypeRegionData(seed,heights,water)};
            PrototypeRegionView.Open(snapshot.info.name,FindFirstObjectByType<Canvas>(),snapshot:snapshot);
            var view=FindFirstObjectByType<PrototypeRegionView>();var pilot=view.ApprovedDistrict;
            pilot.SetSmallPlantsEnabled(false);pilot.SetTreesEnabled(false);
            view.GetComponentInChildren<CameraSystem.CityForgeCameraController>().FrameMeadowComparison(pilot.Side,pilot.DisplayTerrain.terrainData.size.y,new Vector3(256,0,256));
            var saved=SavedRegionStore.Save(SavedRegionStore.Capture(view));
            if(FindObjectsByType<WaveHarmonic.Crest.WaterRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length!=0||pilot.Water)throw new System.Exception("Waterless scene unexpectedly contains Crest water.");
            Debug.Log("PASS Flat Waterless Test: seeded gentle terrain, zero Crest WaterRenderers; saved to Open Region: "+saved);
        }
    }
}
