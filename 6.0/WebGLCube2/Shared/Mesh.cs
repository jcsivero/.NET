using System;

namespace WebGLCube{
    public class RetrievedMesh{
        public float[] vertices {get; set;}
        public int[] indices {get; set;}
        public float[] colors {get; set;}
        public  int nvertices {get; set;}
        public int nindices {get; set;}
        public ushort[] usindices {get; set;}
        public float[] normals {get; set;}
    }
}