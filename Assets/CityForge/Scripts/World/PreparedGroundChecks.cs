#if UNITY_EDITOR
using System;
using System.Reflection;
using CityForge.World;
using UnityEngine;
namespace CityForge.Editor
{
    public static class PreparedGroundChecks
    {
        public static void Run()
        {
            var data=new TerrainData{heightmapResolution=129,size=new Vector3(128,64,128)};
            var heights=new float[129,129];
            for(int z=0;z<129;z++)for(int x=0;x<129;x++)heights[z,x]=(18f+Mathf.Sin(x*.12f)*2+Mathf.Cos(z*.09f))/64;
            data.SetHeights(0,0,heights);
            var root=Terrain.CreateTerrainGameObject(data);
            try
            {
                var grading=root.AddComponent<DistrictLotGrading>();
                typeof(DistrictLotGrading).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(grading,null);
                // Retain a different undo baseline, as occurs with previously prepared land.
                grading.Cut(new Rect(0,0,128,128),p=>true,13,p=>false,exact:true);grading.Finish();
                var before=data.GetHeights(0,0,129,129);
                foreach(float rotation in new[]{0f,45f})
                {
                    var plane=new ZoneGroundPlane{center=new Vector2(64,64),size=new Vector2(84,44),height=13,rotation=rotation};
                    grading.CutPlanes(new Rect(0,0,128,128),plane,p=>true);
                    if(grading.Finish())throw new Exception("No-op flat grade requested a heightmap sync");
                }
                var after=data.GetHeights(0,0,129,129);
                for(int z=0;z<129;z++)for(int x=0;x<129;x++)
                    if(before[z,x]!=after[z,x])throw new Exception($"Prepared plain changed at {x},{z}: {before[z,x]*64} -> {after[z,x]*64}");
                grading.Restore();
                if(Mathf.Abs(data.GetHeight(60,60)-heights[60,60]*64)>.002f)throw new Exception("Original undo baseline lost");
                Debug.Log("PREPARED GROUND CHECKS PASS: all16641 height samples unchanged by rectangular and diagonal zone grading on sea+1 plain; undo baseline retained.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(data);}
        }
    }
}
#endif
