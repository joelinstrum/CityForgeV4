using System.Collections.Generic;
using UnityEngine;
namespace CityForge.UI
{
    public abstract class PointWireSurface
    {
        public abstract bool Available {get;}
        public abstract bool CanEdit {get;}
        public abstract Collider Collider {get;}
        public abstract bool Contains(Vector2 point);
        public abstract float Height(Vector2 point);
    }
    /// <summary>World-XZ guide points, independent of cameras, UI and consumer geometry.</summary>
    public sealed class PointWirePath
    {
        readonly List<Vector2> points=new();
        readonly System.Collections.ObjectModel.ReadOnlyCollection<Vector2> readOnly;
        public PointWirePath(){readOnly=points.AsReadOnly();}
        public IReadOnlyList<Vector2> Points=>readOnly;
        public bool Closed {get;private set;}
        public bool Finished {get;private set;}
        public bool TryAdd(Vector2 point){if(Finished||points.Count>=256||!float.IsFinite(point.x)||!float.IsFinite(point.y))return false;foreach(var p in points)if((p-point).sqrMagnitude<1)return false;points.Add(point);return true;}
        public bool TryClose(){if(Finished||points.Count<3)return false;Finished=true;Closed=true;return true;}
        public bool TryFinishOpen(){if(Finished||points.Count<2)return false;Finished=true;return true;}
        public void Undo(){if(Finished){Finished=false;Closed=false;}else if(points.Count>0)points.RemoveAt(points.Count-1);}
        public void Clear(){points.Clear();Closed=false;Finished=false;}
    }
}
