using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    [Serializable] public sealed class BridgeStyle
    {
        public int schemaVersion;
        public string id, label, firstEraId;
        public float[] widths;
        public float supportSpacing, maxSpan, maxSlope, minClearance, maxLength, maxSupportHeight;
        public float deckThickness, railingHeight;
        public string deckPart, railingPart, supportPart, bracePart;
        public string Problem()
        {
            if (schemaVersion != 1) return "Bridge style requires schemaVersion 1.";
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(label)) return "Bridge style needs an ID and label.";
            if (!CityForge.UI.CityForgeTimeline.Eras.Any(e => e.id == firstEraId)) return "Bridge style needs a recognized firstEraId.";
            if (widths == null || widths.Length == 0 || widths.Length > 8 || widths.Any(w => !float.IsFinite(w) || w < 4 || w > 16)) return "Supported carriageway widths must be 4–16 m.";
            if (!Positive(supportSpacing, 2, 24) || !Positive(maxSpan, supportSpacing, 48) || !Positive(maxSlope, .01f, .2f) ||
                !Positive(minClearance, 1, 30) || !Positive(maxLength, 16, 512) || !Positive(maxSupportHeight, 2, 60) ||
                !Positive(deckThickness, .15f, 2) || !Positive(railingHeight, .8f, 2)) return "Bridge span, slope, clearance or part dimensions are outside prototype limits.";
            foreach (var part in new[] { deckPart, railingPart, supportPart, bracePart })
                if (string.IsNullOrWhiteSpace(part) || part != "primitive:box" && !Resources.Load<GameObject>(part))
                    return "Missing bridge part: " + (part ?? "(empty)") + ". Install the style's referenced Resources prefab.";
            return null;
        }
        static bool Positive(float v,float min,float max) => float.IsFinite(v) && v >= min && v <= max;
    }

    public static class BridgeStyles
    {
        [Serializable] sealed class Catalog { public int schemaVersion; public BridgeStyle[] styles; }
        static BridgeStyle[] styles;
        static readonly List<string> errors = new();
        public static IReadOnlyList<string> Errors { get { Ensure(); return errors; } }
        public static IReadOnlyList<BridgeStyle> All { get { Ensure(); return styles; } }
        public static BridgeStyle Find(string id) { Ensure(); return styles.FirstOrDefault(s => s.id == id); }
        public static void Reload() { styles = null; Ensure(); }
        static void Ensure()
        {
            if (styles != null) return;
            errors.Clear(); var result = new List<BridgeStyle>();
            foreach (var asset in Resources.LoadAll<TextAsset>("Config/BridgeStyles").OrderBy(a=>a.name)) Read(asset.text, asset.name, result);
            string folder = Path.Combine(Application.persistentDataPath, "Mods", "BridgeStyles");
            if (Directory.Exists(folder)) foreach (var path in Directory.GetFiles(folder,"*.json").OrderBy(p=>p))
            { try { Read(File.ReadAllText(path), Path.GetFileName(path), result); } catch(Exception e) { errors.Add(Path.GetFileName(path)+": "+e.Message); } }
            styles = result.ToArray();
        }
        static void Read(string json,string source,List<BridgeStyle> result)
        {
            try
            {
                var catalog = JsonUtility.FromJson<Catalog>(json);
                if (catalog == null || catalog.schemaVersion != 1 || catalog.styles == null) { errors.Add(source+": expected schemaVersion 1 and styles array."); return; }
                foreach (var style in catalog.styles)
                {
                    var error = style == null ? "Empty style." : style.Problem();
                    if (error == null && result.Any(s=>s.id==style.id)) error = "Duplicate bridge style ID: "+style.id;
                    if (error != null) errors.Add(source+": "+error); else result.Add(style);
                }
            } catch(Exception e) { errors.Add(source+": "+e.Message); }
        }
    }

    [Serializable] public sealed class BridgeSupport
    {
        public float station;
        public float leftBed, rightBed;
    }

    [Serializable] public sealed class BridgeInstance
    {
        public int schemaVersion = 1, district;
        public string id, styleId;
        public Vector2 start, end;
        public float startHeight, endHeight, deckHeight, deckStart, deckEnd;
        public float width = 8, sidewalkWidth = 1, channelCenter, channelWidth = 16, clearance = 4;
        public int lanes = 2;
        public bool earthwork = true;
        public float earthworkFeather = 3;
        public BridgeSupport[] supports;
        public float Length => Vector2.Distance(start,end);
        public Vector2 Direction => (end-start).normalized;
        public Vector2 Right => new(-Direction.y,Direction.x);
        public float TotalWidth => width+2*sidewalkWidth;
        public Vector2 Point(float station) => start+Direction*station;
        public float Height(float station) => station < deckStart ? Mathf.Lerp(startHeight,deckHeight,Mathf.Clamp01(station/deckStart)) :
            station > deckEnd ? Mathf.Lerp(deckHeight,endHeight,Mathf.Clamp01((station-deckEnd)/(Length-deckEnd))) : deckHeight;
        public bool Project(Vector2 p,float inset,out float station)
        {
            var delta=p-start; station=Vector2.Dot(delta,Direction);
            return station>=0 && station<=Length && Mathf.Abs(Vector2.Dot(delta,Right))<=TotalWidth*.5f-inset;
        }
        public Vector2[] Route => new[]{ start,Point(deckStart),Point(deckEnd),end };
        public BridgeInstance Copy() => JsonUtility.FromJson<BridgeInstance>(JsonUtility.ToJson(this));
        // Structural validation is deliberately independent of installed styles. Missing mods retain their data.
        public string DataProblem(int districtCount,float halfMap)
        {
            if(schemaVersion!=1 || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(styleId) || id.Length>128 || styleId.Length>128 || district<0 || district>=districtCount) return "Invalid bridge identity/version/district.";
            var values=new[]{start.x,start.y,end.x,end.y,startHeight,endHeight,deckHeight,deckStart,deckEnd,width,sidewalkWidth,channelCenter,channelWidth,clearance,earthworkFeather};
            if(values.Any(v=>!float.IsFinite(v)) || Mathf.Abs(start.x)>halfMap || Mathf.Abs(start.y)>halfMap || Mathf.Abs(end.x)>halfMap || Mathf.Abs(end.y)>halfMap ||
                Length<8 || Length>512 || deckStart<1 || deckEnd<=deckStart || deckEnd>Length-1 || width<4 || width>16 || sidewalkWidth<0 || sidewalkWidth>2 || lanes<1 || lanes>4 ||
                channelWidth<0 || channelWidth>48 || channelCenter<deckStart || channelCenter>deckEnd || clearance<1 || clearance>30 || earthworkFeather<0 || earthworkFeather>8 ||
                Mathf.Abs(startHeight)>2048 || Mathf.Abs(endHeight)>2048 || Mathf.Abs(deckHeight)>2048 || supports==null || supports.Length<2 || supports.Length>256) return "Invalid bridge dimensions or alignment.";
            float previous=-1;
            foreach(var support in supports)
            {
                if(support==null || !float.IsFinite(support.station) || !float.IsFinite(support.leftBed) || !float.IsFinite(support.rightBed) ||
                    support.station<deckStart || support.station>deckEnd || support.station<=previous || Mathf.Abs(support.leftBed)>2048 || Mathf.Abs(support.rightBed)>2048) return "Invalid bridge supports.";
                previous=support.station;
            }
            return null;
        }
    }
}
