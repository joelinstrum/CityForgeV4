using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        // Retain scene objects, materials, paint textures, routes and animation state.
        // Capture only terrain-conforming geometry near this edit, never imported model meshes.
        sealed class LevelerUpdates
        {
            readonly PrototypeRegionView view;
            readonly Rect bounds;
            readonly ZoneGroundPlane plane;
            readonly Dictionary<Mesh,MeshUpdate> meshes=new();
            readonly List<(Transform Body,Vector2 Point,float Old,Func<Vector2,float> Next)> objects=new();
            readonly Dictionary<DistrictLotRecord,Func<Vector2,float>> lotHeights=new();
            internal readonly List<DistrictLotRecord> Lots=new();
            sealed class MeshUpdate
            {
                internal Mesh Mesh;internal Transform Transform;internal Vector3[] Vertices;internal List<Vector3> Backing;
                internal readonly List<(int Index,Vector2 Point,float Old,Func<Vector2,float> Next)> Points=new();
                internal void Apply()
                {
                    if(!Mesh||!Transform)return;bool changed=false;var inverse=Transform.worldToLocalMatrix;
                    foreach(var p in Points)
                    {
                        float delta=p.Next(p.Point)-p.Old;if(Mathf.Abs(delta)<.0001f)continue;
                        Vertices[p.Index]+=inverse.MultiplyVector(Vector3.up*delta);
                        if(Backing!=null)Backing[p.Index]=Vertices[p.Index];changed=true;
                    }
                    if(!changed)return;
                    Mesh.SetVertices(Vertices);Mesh.RecalculateNormals();Mesh.RecalculateBounds();
                    if(Mesh.HasVertexAttribute(VertexAttribute.Tangent))Mesh.RecalculateTangents();
                }
            }
            internal LevelerUpdates(PrototypeRegionView view,ZoneGroundPlane plane,Rect bounds)
            {
                this.view=view;this.plane=plane;this.bounds=bounds;
                foreach(var lot in view.PlacedLots)
                {
                    if(!plane.Contains(lot.center)||!view.districtLotRoots.TryGetValue(lot,out var root)||!root)continue;
                    Lots.Add(lot);
                    var footprint=new DistrictLotFootprint(lot);bool flat=lot.cutToLowest||lot.definition.levelLot;
                    float old=lot.cutToLowest?lot.cutGroundHeight:lot.definition.levelLot?
                        DistrictLotFoundation.SampleLevel(lot.definition.Size,footprint.WorldPoint,view.NaturalLotHeight):0;
                    Func<Vector2,float> oldSite=p=>flat?old:view.NaturalLotHeight(p);
                    Func<Vector2,float> newSite=p=>plane.height;lotHeights.Add(lot,oldSite);
                    foreach(Transform child in root)
                    {
                        if(child.GetComponent<MeshFilter>()||child.GetComponent<GroundPaintRenderer>())continue;
                        var catalog=LotAssetCatalog.Load();
                        if(!lot.definition.objects.Any(item=>catalog.Find(item.assetId)?.label==child.name||Vector2.Distance(item.position,new Vector2(child.localPosition.x,child.localPosition.z))<.001f))continue;
                        CaptureObject(child,oldSite,newSite);
                    }
                    var surfaces=root.GetComponent<DistrictLotSurfaces>();
                    if(surfaces)foreach(var mesh in surfaces.GradingMeshes.Distinct())Capture(mesh,root,oldSite,newSite);
                    foreach(var paint in root.GetComponentsInChildren<GroundPaintRenderer>())CaptureRoot(paint.transform,oldSite,newSite);
                }
                if(view.lotInstances)
                {
                    var ground=view.lotInstances.GetComponent<ResidentialGroundBatches>();
                    if(ground)foreach(var batch in ground.GradingMeshes)foreach(var range in batch.Lots)
                    {
                        var lot=Lots.FirstOrDefault(l=>l.id==range.Id);if(lot==null)continue;
                        Capture(batch.Mesh,ground.transform,lotHeights[lot],p=>plane.height,batch.Vertices,range.Start,range.Count);
                    }
                }
                CaptureRoot(view.districtRoadRoot,view.LotHeight,view.LotHeight);
                if(view.districtPaintRendering)CaptureRoot(view.districtPaintRendering.transform,view.LotHeight,view.LotHeight);
                if(view.parkPathRoot)CaptureRoot(view.parkPathRoot.transform,view.LotHeight,view.LotHeight);
                if(view.zoneRenderer)Capture(view.zoneMesh,view.zoneRenderer.transform,view.LotHeight,view.LotHeight);
                foreach(var actor in view.pedestrians)if(!actor.EagleCarried)CaptureObject(actor.Body,view.LotHeight,view.LotHeight);
                foreach(var entry in view.streetscapeViews)if(entry.Value)CaptureObject(entry.Value.transform,view.LotHeight,view.LotHeight);
                foreach(var entry in view.placedTreeViews)if(entry.Value)CaptureObject(entry.Value.transform,view.LotHeight,view.LotHeight);
                if(view.placedChildrenRoot)foreach(Transform child in view.placedChildrenRoot)CaptureObject(child,view.LotHeight,view.LotHeight);
            }
            void CaptureObject(Transform body,Func<Vector2,float> old,Func<Vector2,float> next)
            {
                if(!body)return;var p=new Vector2(body.position.x,body.position.z);if(bounds.Contains(p))objects.Add((body,p,old(p),next));
            }
            void CaptureRoot(Transform root,Func<Vector2,float> old,Func<Vector2,float> next)
            {
                if(!root)return;
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<Renderer>();
                    if(renderer){var b=renderer.bounds;if(!bounds.Overlaps(Rect.MinMaxRect(b.min.x,b.min.z,b.max.x,b.max.z)))continue;}
                    Capture(filter.sharedMesh,filter.transform,old,next);
                }
            }
            void Capture(Mesh mesh,Transform transform,Func<Vector2,float> old,Func<Vector2,float> next,List<Vector3> backing=null,int start=0,int count=-1)
            {
                if(!mesh)return;
                if(!meshes.TryGetValue(mesh,out var update))meshes[mesh]=update=new MeshUpdate{Mesh=mesh,Transform=transform,Vertices=mesh.vertices,Backing=backing};
                else if(backing==null)return; // Source and clipped path may share a mesh.
                var matrix=transform.localToWorldMatrix;int end=count<0?update.Vertices.Length:start+count;
                for(int i=start;i<end;i++){var world=matrix.MultiplyPoint3x4(update.Vertices[i]);var p=new Vector2(world.x,world.z);if(bounds.Contains(p))update.Points.Add((i,p,old(p),next));}
            }
            internal void Apply()
            {
                foreach(var mesh in meshes.Values)mesh.Apply();
                foreach(var item in objects)if(item.Body)item.Body.position+=Vector3.up*(item.Next(item.Point)-item.Old);
                foreach(var lot in Lots)if(view.districtLotRoots.TryGetValue(lot,out var root)&&root)
                {
                    var foundation=root.GetComponent<DistrictLotFoundation>()??root.gameObject.AddComponent<DistrictLotFoundation>();
                    foundation.Regrade(lot.definition,plane.height,lot.definition.elevation+lot.elevation,view.CurrentLotTerrainHeight);
                }
            }
        }
        LevelerUpdates CaptureLevelerUpdates(ZoneGroundPlane plane,Rect bounds)=>new(this,plane,bounds);
    }
}
