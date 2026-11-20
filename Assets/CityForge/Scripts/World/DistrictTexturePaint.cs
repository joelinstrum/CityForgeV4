using System;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private bool tearingDownPaint;
        private GroundPaintData districtGroundPaint = new();
        private TexturePaintTool districtTexturePaint;
        private GroundPaintRenderer districtPaintRendering;
        internal GroundPaintData CaptureGroundPaint()
        {
            if(districtTexturePaint)districtTexturePaint.FinishStroke();
            return districtGroundPaint.Copy();
        }
        private void RestoreGroundPaint(GroundPaintData data) => districtGroundPaint = data ?? new GroundPaintData();
        private void CancelTexturePaint(){if(districtTexturePaint)districtTexturePaint.Close();}
        private void RebuildGroundPaint()
        {
            if(tearingDownPaint)return;
            if(districtTexturePaint)return; // Presentation refresh must not silently relinquish the active paint tool.
            if(districtPaintRendering){districtPaintRendering.gameObject.SetActive(false);Destroy(districtPaintRendering.gameObject);}
            if(SelectedDistrict<0||districtGroundPaint.tiles.Count==0)return;
            var go=new GameObject("District texture paint");go.transform.SetParent(transform,false);
            districtPaintRendering=go.AddComponent<GroundPaintRenderer>();
            districtPaintRendering.Initialize(districtGroundPaint,LotHeight,ActiveDistrictBounds,InActiveDistrict);
        }
        private bool PaintHitsBuilding(Vector2 point)
        {
            foreach(var building in roadBuildings)if(building.Contains(point,0))return true;
            return false;
        }
        private void OpenTexturize()
        {
            if(!CanBuildTown||IsSaving)return;
            CancelTexturePaint();CancelZoneDrag();CancelZonedBlock(false);zoneToolArmed=false;CloseRoadContext();SetRoadPaths(false);
            CloseTownSection();CancelRoadPlacement();CancelLotPlacement();CancelDistrictActor();SelectDistrictLot(null);
            if(districtPaintRendering)districtPaintRendering.gameObject.SetActive(false);
            var camera=GetComponentInChildren<CameraSystem.CityForgeCameraController>().GetComponent<Camera>();
            var go=new GameObject("District Texturize tool");go.transform.SetParent(transform,false);
            districtTexturePaint=go.AddComponent<TexturePaintTool>();
            districtTexturePaint.Initialize(townToolbar.parent,districtGroundPaint,Pointer,Project,LotHeight,
                point=>InActiveDistrict(point)&&!PaintHitsBuilding(point),
                ()=>CanBuildTown&&TownConstructionVisible&&!IsSaving&&!DistrictLoading,
                data=>districtGroundPaint=data,()=>{districtTexturePaint=null;RebuildGroundPaint();},ActiveDistrictBounds);
            Vector2? Pointer()
            {
                if(EventSystem.current&&EventSystem.current.IsPointerOverGameObject())return null;
                var collider=RoadTerrain?RoadTerrain.GetComponent<TerrainCollider>():null;
                if(!collider)return null;
                bool enabled=collider.enabled;collider.enabled=true;
                bool hit=collider.Raycast(camera.ScreenPointToRay(Input.mousePosition),out var picked,100000f);
                collider.enabled=enabled;
                return hit?new Vector2(picked.point.x,picked.point.z):null;
            }
            Vector2 Project(Vector2 p){var s=camera.WorldToScreenPoint(new Vector3(p.x,LotHeight(p),p.y));return new Vector2(s.x,s.y);}
        }
    }
}
