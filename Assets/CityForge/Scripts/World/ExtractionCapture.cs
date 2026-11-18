using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private GameObject extractionSphere;
        private Material extractionFill, extractionRingMaterial;
        private LineRenderer extractionRing;
        private LotDefinition captureDefinition;
        private Vector2 captureCenter;
        private int captureRotation, captureRevision=-1, captureDiscoveries=-1;
        private RegionSeason captureSeason;
        private LotRequiredResource capturedResource;
        private bool captureCached, captureReachable;
        private int captureTrees;

        private bool EvaluateExtraction(DistrictLotRecord lot, bool force=false)
        {
            if(!LotExtraction.Applies(lot.definition))return true;
            var resource=LotExtraction.Resource(lot.definition);
            if(!force&&captureCached&&captureDefinition==lot.definition&&captureCenter==lot.center&&captureRotation==lot.FacingDegrees&&
                captureRevision==WoodlandRevision&&captureDiscoveries==RegionalResources.DiscoveryCount&&captureSeason==VisualSeason&&capturedResource==resource)return captureReachable;
            captureCached=true;captureDefinition=lot.definition;captureCenter=lot.center;captureRotation=lot.FacingDegrees;
            captureRevision=WoodlandRevision;captureDiscoveries=RegionalResources.DiscoveryCount;captureSeason=VisualSeason;capturedResource=resource;captureTrees=0;
            if(resource==LotRequiredResource.Timber)
            {
                var footprint=new DistrictLotFootprint(lot);
                if(ApprovedDistrict&&ApprovedDistrict.DisplayTerrain)
                    captureTrees=ApprovedDistrict.CountCaptureTrees(new Vector3(lot.center.x,LotHeight(lot.center),lot.center.y),LotExtraction.CaptureRadius,
                        point => footprint.Contains(point),LotExtraction.MinimumTrees);
                captureReachable=captureTrees>=LotExtraction.MinimumTrees;
            }
            else captureReachable=RegionalResources!=null&&RegionalResources.InCapture(LotExtraction.Key(resource),lot.center/MapSide+Vector2.one*.5f,LotExtraction.CaptureRadius/MapSide,point=>
            {
                var world=(point-Vector2.one*.5f)*MapSide;
                if(!InActiveDistrict(world))return false;
                float rise=LotHeight(world)-LotHeight(lot.center);
                return (world-lot.center).sqrMagnitude+rise*rise<=LotExtraction.CaptureRadius*LotExtraction.CaptureRadius+.01f;
            });
            return captureReachable;
        }

        private string ExtractionSiteProblem(DistrictLotRecord lot, bool force=false)
        {
            if(!LotExtraction.Applies(lot.definition)||EvaluateExtraction(lot,force))return null;
            return capturedResource==LotRequiredResource.Timber
                ? $"You must be within reach of the resource.\nTimber: {captureTrees}/{LotExtraction.MinimumTrees} trees within {LotExtraction.CaptureRadius:0} m."
                : $"You must be within reach of the resource.\n{capturedResource} must be within {LotExtraction.CaptureRadius:0} m.";
        }

        private void UpdateExtractionSphere(DistrictLotRecord lot)
        {
            if(!LotExtraction.Applies(lot.definition))return;
            bool valid=EvaluateExtraction(lot);
            if(!extractionSphere)
            {
                extractionSphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);extractionSphere.name="Extraction resource capture sphere";
                extractionSphere.transform.SetParent(transform,false);
                var collider=extractionSphere.GetComponent<Collider>();if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);
                var renderer=extractionSphere.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                extractionFill=Own(new Material(Resources.Load<Shader>("UI/ExtractionCaptureSphere")));renderer.sharedMaterial=extractionFill;
                extractionRing=new GameObject("Resource capture boundary").AddComponent<LineRenderer>();extractionRing.transform.SetParent(extractionSphere.transform,false);
                extractionRing.loop=true;extractionRing.useWorldSpace=true;extractionRing.positionCount=64;extractionRing.widthMultiplier=.3f;
                extractionRing.shadowCastingMode=ShadowCastingMode.Off;extractionRing.receiveShadows=false;
                extractionRingMaterial=Own(new Material(extractionFill));extractionRingMaterial.SetFloat("_Fill",1);extractionRingMaterial.SetFloat("_Rim",0);
                extractionRing.sharedMaterial=extractionRingMaterial;
            }
            extractionSphere.SetActive(true);
            var position=new Vector3(lot.center.x,LotHeight(lot.center),lot.center.y);
            bool moved=extractionSphere.transform.position!=position;
            extractionSphere.transform.position=position;extractionSphere.transform.localScale=Vector3.one*(LotExtraction.CaptureRadius*2);
            extractionRing.transform.localScale=Vector3.one/(LotExtraction.CaptureRadius*2);
            var color=valid?new Color(.12f,.9f,.3f,.20f):new Color(.98f,.12f,.08f,.20f);
            extractionFill.SetColor("_Color",color);color.a=.75f;extractionRingMaterial.SetColor("_Color",color);
            var camera=lotPlacementCamera?lotPlacementCamera:GetComponentInChildren<Camera>();
            if(camera){extractionFill.SetVector("_Viewer",camera.transform.position);extractionRingMaterial.SetVector("_Viewer",camera.transform.position);}
            if(moved||extractionRing.GetPosition(0)==Vector3.zero)for(int i=0;i<64;i++)
            {
                float angle=i*Mathf.PI*2/64;var point=lot.center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*LotExtraction.CaptureRadius;
                float height=ApprovedDistrict&&ApprovedDistrict.Contains(point)?LotHeight(point):position.y;
                extractionRing.SetPosition(i,new Vector3(point.x,height+.2f,point.y));
            }
        }
    }
}
