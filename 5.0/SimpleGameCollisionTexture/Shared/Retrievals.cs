using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats; //Textures


namespace SimpleGame{

    public enum AssetType : byte {
        StaticMesh,
        Texture
    }
    public class RetrievedAsset {
       public AssetType AssetType {get; set;}


    };
    public class RetrievedMesh : RetrievedAsset {
        public float[] vertices {get; set;}
        public float[] vertexarray {get;set;}
        public int[] indices {get; set;}
        public float[] colors {get; set;}
        public float[] colorarray{get;set;}
        public  int nvertices {get; set;}
        public int nindices {get; set;}
        public ushort[] usindices {get; set;}
        public float[] normals {get; set;}
        public float[] normalarray {get; set;}
        public float[] uv{get; set;}
        };

    public struct RGBA{
        public byte  R;
        public byte G;
        public byte B;
        public byte A;
    }
    public struct ByteRGBA{
        public byte val;
    }
    public class RetrievedTexture : RetrievedAsset {
        public byte[] Rgba32DecodedImage; 
        public int Width{get; set;}
        public int Height{get; set;}
    }

    public class RetrievedTextureMeta {
        public string file {get; set;}
        public string id{get; set;}
 
    }

    public class RetrievedMeshMeta {
        public string file {get; set;}
        public string id{get; set;}
 

    }

    public class RetrievedOrientation{
        public double[] axis {get; set;}
        public double angle {get; set;}

    }

    public class RetrievedActor{
        public string id {get; set;}
        public string sm {get; set;}
        public bool enabled {get; set;}
        public bool shadow {get; set;}

        public string type {get; set;}

        public double[] position {get;set;}

        public RetrievedOrientation orientation {get; set;}

        public double[] scale {get; set;}

        public double[] basecolor{get; set;}
        public bool collisionbox{get; set;}

        public string texture{get;set;}
        
    }

    public class RetrievedLevel{
            public RetrievedMeshMeta[] mesh_list {get; set;}

            public RetrievedTextureMeta[] texture_list {get; set;}
            public RetrievedActor[] actor_list {get; set;}    

            public double[] playerstartposition{get; set;}
            public double playerstartrotationangle{get;set;}
            public double[] playerstartrotationaxis{get;set;}

            public double[] ambientlight{get; set;}

            public double[] shadowplanenormal{get; set;}
            public double[] shadowplanepoint{get; set;}

    }
}