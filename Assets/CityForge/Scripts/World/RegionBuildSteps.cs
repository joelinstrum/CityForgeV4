using System.Collections;
namespace CityForge.World
{
    /// <summary>Keep immediate editor/test entry points on the same staged build implementation.</summary>
    public static class RegionBuildSteps
    {
        public static void Complete(IEnumerator routine)
        {
            try{while(routine.MoveNext())if(routine.Current is IEnumerator nested)Complete(nested);}
            finally{(routine as System.IDisposable)?.Dispose();}
        }
    }
}
