using UnityEngine;
using UnityEngine.EventSystems;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal const float AnimalPlacementRadius=25f;
        float CurrentAnimalPlacementRadius=>AnimalPlacementRadius*((Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))?2:1);
        GameObject animalPlacementCircle;LineRenderer animalPlacementRing;Material animalPlacementMaterial;
        TerrainCollider animalPlacementCollider;bool animalPlacementColliderEnabled;
        void OpenAnimalPlacementPreview()
        {
            if(ApprovedDistrict&&ApprovedDistrict.DisplayTerrain)
            {
                animalPlacementCollider=ApprovedDistrict.DisplayTerrain.GetComponent<TerrainCollider>();
                if(animalPlacementCollider){animalPlacementColliderEnabled=animalPlacementCollider.enabled;animalPlacementCollider.enabled=true;}
            }
            if(animalPlacementCircle)return;
            animalPlacementCircle=new GameObject("Animal placement radius",typeof(LineRenderer));animalPlacementCircle.transform.SetParent(transform,false);
            animalPlacementRing=animalPlacementCircle.GetComponent<LineRenderer>();animalPlacementRing.loop=true;animalPlacementRing.useWorldSpace=true;animalPlacementRing.positionCount=96;
            animalPlacementMaterial=new Material(Resources.Load<Shader>("UI/ForestRemovalCircle"));animalPlacementMaterial.SetColor("_Color",new Color(.25f,.12f,.045f,.95f));
            animalPlacementRing.sharedMaterial=animalPlacementMaterial;animalPlacementRing.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;animalPlacementRing.receiveShadows=false;
            animalPlacementCircle.SetActive(false);
        }
        void CloseAnimalPlacementPreview()
        {
            if(animalPlacementCollider)animalPlacementCollider.enabled=animalPlacementColliderEnabled;
            animalPlacementCollider=null;
            if(animalPlacementCircle){animalPlacementCircle.SetActive(false);Destroy(animalPlacementCircle);}
            if(animalPlacementMaterial)Destroy(animalPlacementMaterial);
            animalPlacementCircle=null;animalPlacementRing=null;animalPlacementMaterial=null;
        }
        void UpdateAnimalPlacementPreview()
        {
            if(!animalPlacementCircle)return;
            bool valid=armedDistrictActor!=null&&armedAnimalSpecies!=null&&SelectedDistrict>=0&&ApprovedDistrict&&!DistrictLoading&&!IsSaving&&!TownConstructionVisible&&Application.isFocused&&
                !(EventSystem.current&&EventSystem.current.IsPointerOverGameObject());
            Vector2 point=default;
            if(valid)valid=TryRoadGround(Input.mousePosition,out point)&&InActiveDistrict(point);
            animalPlacementCircle.SetActive(valid);if(!valid)return;
            DrawAnimalPlacementPreview(point);
        }
        void DrawAnimalPlacementPreview(Vector2 point)
        {
            animalPlacementCircle.SetActive(true);
            var camera=GetComponentInChildren<Camera>();
            animalPlacementRing.widthMultiplier=camera?Mathf.Clamp(camera.orthographicSize*.003f,.08f,1.2f):.15f;
            float radius=CurrentAnimalPlacementRadius;
            for(int i=0;i<96;i++)
            {
                float angle=i*Mathf.PI*2/96;var p=point+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                animalPlacementRing.SetPosition(i,new Vector3(p.x,ApprovedDistrict.Height(p)+.15f,p.y));
            }
        }
    }
}
