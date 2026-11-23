using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
namespace CityForge.World
{
    // Strict, bounded PNG reader for the explicit v1 contract. No texture/color-space conversions.
    public static class TerrainImportPng
    {
        public sealed class Raster { public int width,height,depth,type;public byte[] bytes; }
        static uint U32(byte[] b,int i)=>(uint)(b[i]<<24|b[i+1]<<16|b[i+2]<<8|b[i+3]);
        static uint Crc(byte[] b,int offset,int count){uint crc=0xffffffff;for(int i=offset;i<offset+count;i++){crc^=b[i];for(int bit=0;bit<8;bit++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}return ~crc;}
        public static Raster Decode(byte[] png,bool heightmap,CancellationToken cancel=default)
        {
            if(png==null||png.Length<33||png.Length>64*1024*1024)throw new InvalidDataException("PNG is missing, truncated or exceeds64 MB.");
            byte[] signature={137,80,78,71,13,10,26,10};for(int i=0;i<8;i++)if(png[i]!=signature[i])throw new InvalidDataException("Not a PNG file.");
            var result=new Raster();bool header=false,ended=false;int pos=8;
            using var compressed=new MemoryStream();
            while(pos+12<=png.Length){cancel.ThrowIfCancellationRequested();uint length=U32(png,pos);if(length>png.Length-pos-12)throw new InvalidDataException("Truncated PNG chunk.");int n=(int)length;string type=System.Text.Encoding.ASCII.GetString(png,pos+4,4);
                if(Crc(png,pos+4,n+4)!=U32(png,pos+8+n))throw new InvalidDataException("PNG checksum failed.");int p=pos+8;
                if(!header&&type!="IHDR")throw new InvalidDataException("PNG header must be first.");
                if(type=="IHDR"){
                    if(header||n!=13)throw new InvalidDataException("Invalid PNG header.");header=true;result.width=checked((int)U32(png,p));result.height=checked((int)U32(png,p+4));result.depth=png[p+8];result.type=png[p+9];
                    if(result.width<2||result.height<2||result.width>4096||result.height>4096||((long)result.width*result.height)>16777216)throw new InvalidDataException("Use a PNG between2 and4096 pixels per side.");
                    if(png[p+10]!=0||png[p+11]!=0||png[p+12]!=0)throw new InvalidDataException("Use a non-interlaced PNG with standard compression.");
                    if(heightmap?(result.depth!=16||result.type!=0):(result.depth!=8||result.type!=2))throw new InvalidDataException(heightmap?"Height PNG must be16-bit single-channel grayscale; 8-bit/RGB inputs lose the required elevation precision.":"Water PNG must be8-bit RGB with exact palette colors (no alpha).");
                }else if(type=="IDAT")compressed.Write(png,p,n);
                else if(type=="IEND"){if(n!=0)throw new InvalidDataException("Invalid PNG end.");ended=true;break;}
                else if((png[pos+4]&32)==0&&type!="PLTE")throw new InvalidDataException("Unsupported critical PNG chunk: "+type);
                pos+=n+12;
            }
            if(!ended||compressed.Length<6)throw new InvalidDataException("PNG is incomplete.");var zlib=compressed.ToArray();
            if((zlib[0]&15)!=8||((zlib[0]<<8)+zlib[1])%31!=0||(zlib[1]&32)!=0)throw new InvalidDataException("Unsupported PNG zlib stream.");
            int bpp=heightmap?2:3,stride=checked(result.width*bpp),expected=checked((stride+1)*result.height);var filtered=new byte[expected];
            using(var input=new MemoryStream(zlib,2,zlib.Length-6))using(var inflate=new DeflateStream(input,CompressionMode.Decompress)){
                int read=0;while(read<expected){cancel.ThrowIfCancellationRequested();int count=inflate.Read(filtered,read,Math.Min(expected-read,65536));if(count==0)throw new InvalidDataException("Incomplete PNG scanlines.");read+=count;}if(inflate.ReadByte()!=-1)throw new InvalidDataException("PNG scanline size does not match its header.");}
            uint a=1,b=0;foreach(byte value in filtered){a=(a+value)%65521;b=(b+a)%65521;}if((b<<16|a)!=U32(zlib,zlib.Length-4))throw new InvalidDataException("PNG data checksum failed.");
            result.bytes=new byte[stride*result.height];
            for(int y=0;y<result.height;y++){cancel.ThrowIfCancellationRequested();int start=y*(stride+1),filter=filtered[start];if(filter>4)throw new InvalidDataException("Unknown PNG row filter.");
                for(int x=0;x<stride;x++){int i=y*stride+x,left=x>=bpp?result.bytes[i-bpp]:0,up=y>0?result.bytes[i-stride]:0,diagonal=y>0&&x>=bpp?result.bytes[i-stride-bpp]:0;
                    int prediction=0;if(filter==1)prediction=left;else if(filter==2)prediction=up;else if(filter==3)prediction=(left+up)/2;else if(filter==4){int p=left+up-diagonal,pa=Math.Abs(p-left),pb=Math.Abs(p-up),pc=Math.Abs(p-diagonal);prediction=pa<=pb&&pa<=pc?left:pb<=pc?up:diagonal;}
                    result.bytes[i]=unchecked((byte)(filtered[start+1+x]+prediction));}
            }
            return result;
        }
    }
}
