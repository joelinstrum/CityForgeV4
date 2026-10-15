using UnityEngine;

namespace CityForge.Environment
{
    /// <summary>Finite same-elevation-water prototype field, derived from actual heights.
    /// R encodes signed horizontal distance in metres: (distance + 32) / 64.
    /// Positive is dry land. No lake ellipse or shoreline ring geometry is assumed.</summary>
    public static class NaturalShoreDistanceField
    {
        public static Texture2D Build(float[,] heights, float waterLevel, float cellSize)
        {
            int nz = heights.GetLength(0), nx = heights.GetLength(1);
            var wet = new bool[nz, nx];
            var distance = new float[nz, nx];
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++) wet[z, x] = heights[z, x] <= waterLevel;
            for (int z = 0; z < nz; z++)
                for (int x = 0; x < nx; x++)
                {
                    bool edge = (x > 0 && wet[z,x] != wet[z,x-1])
                        || (z > 0 && wet[z,x] != wet[z-1,x])
                        || (x+1 < nx && wet[z,x] != wet[z,x+1])
                        || (z+1 < nz && wet[z,x] != wet[z+1,x]);
                    distance[z,x] = edge ? cellSize*.5f : 1e6f;
                }
            // Eight-neighbour chamfer transform: deterministic O(N), approximate distance.
            for (int sweep=0; sweep<2; sweep++)
            {
                for (int z=0; z<nz; z++) for (int x=0; x<nx; x++)
                {
                    float d=distance[z,x];
                    if(x>0)d=Mathf.Min(d,distance[z,x-1]+cellSize);
                    if(z>0){d=Mathf.Min(d,distance[z-1,x]+cellSize);
                        if(x>0)d=Mathf.Min(d,distance[z-1,x-1]+cellSize*1.414214f);
                        if(x+1<nx)d=Mathf.Min(d,distance[z-1,x+1]+cellSize*1.414214f);}
                    distance[z,x]=d;
                }
                for (int z=nz-1; z>=0; z--) for (int x=nx-1; x>=0; x--)
                {
                    float d=distance[z,x];
                    if(x+1<nx)d=Mathf.Min(d,distance[z,x+1]+cellSize);
                    if(z+1<nz){d=Mathf.Min(d,distance[z+1,x]+cellSize);
                        if(x>0)d=Mathf.Min(d,distance[z+1,x-1]+cellSize*1.414214f);
                        if(x+1<nx)d=Mathf.Min(d,distance[z+1,x+1]+cellSize*1.414214f);}
                    distance[z,x]=d;
                }
            }
            var pixels=new Color[nx*nz];
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                float signed=Mathf.Min(32,distance[z,x])*(wet[z,x]?-1:1);
                pixels[z*nx+x]=new Color((signed+32)/64,0,0,1);
            }
            var texture=new Texture2D(nx,nz,TextureFormat.RHalf,false,true)
                {name="Height-derived signed shoreline distance",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            texture.SetPixels(pixels);texture.Apply(false,false);return texture;
        }
    }
}
