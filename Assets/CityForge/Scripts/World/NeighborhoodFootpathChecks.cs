#if UNITY_EDITOR
using System;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;
namespace CityForge.Editor
{
    public static class NeighborhoodFootpathChecks
    {
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run()
        {
            var lot = new LotDefinition { width = 3, length = 3, zoningLayout = new LotZoningLayout { zone = TownZone.Residential2, wealth = LotWealth.Low } };
            Require(NeighborhoodFootpaths.Eligible(lot), "Low R2 excluded");
            lot.zoningLayout.zone = TownZone.Residential3; Require(NeighborhoodFootpaths.Eligible(lot), "Low R3 excluded");
            lot.zoningLayout.wealth = LotWealth.Medium; Require(!NeighborhoodFootpaths.Eligible(lot), "Medium wealth changed");
            lot.zoningLayout.wealth = LotWealth.Low; lot.zoningLayout.zone = TownZone.Residential1;
            Require(NeighborhoodFootpaths.Eligible(lot), "Low R1 excluded"); lot.zoningLayout.zone = TownZone.Residential2;
            for (int facing = 0; facing < 4; facing++)
            for (int rotation = 0; rotation < 8; rotation++)
            {
                lot.zoningLayout.frontage = (LotFrontage)facing;
                var record = new DistrictLotRecord { definition = lot, center = new Vector2(20, 30), RotationSteps = rotation };
                var edges = NeighborhoodFootpaths.Collect(new[] { record }); var edge = edges[0];
                var point = (edge.A + edge.B) * .5f + edge.Outward * .5f;
                Require(NeighborhoodFootpaths.Coverage(point, -edge.Outward, edges) > .99f, "Rotated frontage lost path");
                Require(NeighborhoodFootpaths.Coverage(point, edge.Outward, edges) == 0, "Path appeared on opposite road side");
                var direction = (edge.B - edge.A).normalized;
                float boundary = NeighborhoodFootpaths.Coverage(edge.A + edge.Outward * .5f, -edge.Outward, edges);
                Require(boundary > .1f && boundary < .9f && NeighborhoodFootpaths.Coverage(edge.A - direction + edge.Outward * .5f, -edge.Outward, edges) == 0,
                    "Path frontage end did not feather");
            }
            lot.zoningLayout.frontage = LotFrontage.South;
            var adjacent = NeighborhoodFootpaths.Collect(new[] { new DistrictLotRecord { definition = lot },
                new DistrictLotRecord { definition = lot, center = Vector2.right * 12 } });
            Require(NeighborhoodFootpaths.Coverage(new Vector2(6, -6.5f), Vector2.up, adjacent) > .99f, "Adjacent lots left a seam");
            lot.streetFrontage = LotStreetFrontage.LeftCorner;
            var corner = NeighborhoodFootpaths.Collect(new[] { new DistrictLotRecord { definition = lot } });
            Require(corner.Count == 2 && Mathf.Abs(Vector2.Dot(corner[0].Outward, corner[1].Outward)) < .01f, "Corner lot lost its side-street frontage");
            var style = LotPathMaterial.Find(LotPathMaterial.ResidentialFootpath);
            Require(style.Texture, "Existing dirt texture is missing");
            var material = style.CreateMaterial();
            try
            {
                LotPathMaterial.ApplyEdgeTreatment(material, new LotPathDefinition { materialId = style.Id, width = NeighborhoodFootpaths.Width }, 40, 0);
                Require(material.GetFloat("_EdgeBlend") > .3f && material.GetFloat("_EdgeBlend") < .4f && material.GetFloat("_PathOpacity") < 1,
                    "Footpath lost soft edges or ground blending");
                Require(material.HasProperty("_VertexOpacity") && material.HasProperty("_NaturalFootpath"), "Missing neighborhood transition controls");
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
            Debug.Log("NEIGHBORHOOD FOOTPATH CHECKS PASS: low R1/R2/R3; four frontages and eight rotations; correct road side; soft ends; continuous adjoining lots; existing dirt texture and feathered shader.");
        }
    }
}
#endif
