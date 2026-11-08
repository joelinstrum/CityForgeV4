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
        // Rows contain only managed sampling and write to disjoint output slots.
        internal static IEnumerator ComputeRows(int count, System.Action<int> fill)
        {
            using var cancellation=new System.Threading.CancellationTokenSource();
            var options=new System.Threading.Tasks.ParallelOptions{
                MaxDegreeOfParallelism=System.Math.Clamp(System.Environment.ProcessorCount-2,1,8),
                CancellationToken=cancellation.Token
            };
            var task=System.Threading.Tasks.Task.Run(()=>System.Threading.Tasks.Parallel.For(0,count,options,fill));
            try{
                while(!task.IsCompleted)yield return null;
                task.GetAwaiter().GetResult();
            }
            finally{
                if(!task.IsCompleted)cancellation.Cancel();
            }
        }
    }
}
