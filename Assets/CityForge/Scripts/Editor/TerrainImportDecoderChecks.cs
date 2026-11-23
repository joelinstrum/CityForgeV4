#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.EditorTools
{
    // Tiny synthetic fixtures exercise every PNG row filter and retain single-U16 increments.
    public static class TerrainImportDecoderChecks
    {
        [MenuItem("CityForge/Checks/Terrain Import Decoder")]
        public static void Run()
        {
            string[] fixtures={
            "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACEAAAAADoj+WFAAAAFklEQVR4nGNgYGhg+P+fwYGh/j8DIwAc9wQ+i5sh2QAAAABJRU5ErkJggg==",
            "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACEAAAAADoj+WFAAAAFklEQVR4nGNkYGhgqP/P6MBg/7+RCQAYjwQCgAy/RQAAAABJRU5ErkJggg==",
            "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACEAAAAADoj+WFAAAAFUlEQVR4nGNiYGhg+P+fyQFIMDIBAB8kBMSm0bQAAAAAAElFTkSuQmCC",
            "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACEAAAAADoj+WFAAAAFklEQVR4nGNmYGhg2P+f2YFB/r8jEwAZ+QPmwYEo1QAAAABJRU5ErkJggg==",
            "iVBORw0KGgoAAAANSUhEUgAAAAMAAAACEAAAAADoj+WFAAAAFklEQVR4nGNhYGhgqP/P4sDw/z8jEwAazgRInt4vKwAAAABJRU5ErkJggg=="
            };
            ushort[] expected={0,32768,65535,16384,32767,1};
            for(int filter=0;filter<fixtures.Length;filter++){
                var bytes=Convert.FromBase64String(fixtures[filter]);var decoded=TerrainImportPng.Decode(bytes,true);
                if(decoded.width!=3||decoded.height!=2)throw new Exception("PNG fixture dimensions changed.");
                for(int i=0;i<expected.Length;i++)if((decoded.bytes[i*2]<<8|decoded.bytes[i*2+1])!=expected[i])throw new Exception("PNG16 precision/orientation/filter failure: "+filter);
            }
            if(TerrainImportData.DecodeElevation(0,-40,100)!=-40||TerrainImportData.DecodeElevation(65535,-40,100)!=100||Math.Abs(TerrainImportData.DecodeElevation(32768,-40,100)-30.001068f)>.00001f)throw new Exception("Elevation decoding failure.");
            var corrupt=Convert.FromBase64String(fixtures[0]);corrupt[corrupt.Length-15]^=1;bool rejected=false;try{TerrainImportPng.Decode(corrupt,true);}catch(InvalidDataException){rejected=true;}if(!rejected)throw new Exception("Corrupt PNG was accepted.");
            Debug.Log("PASS terrain import decoder: exact U16 values, north-up row order, filters0–4, elevation bounds/midpoint and CRC corruption rejection.");
        }
    }
}
#endif
