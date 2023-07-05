using System;

namespace SimpleGame{
    public class RetrievedMesh{
        public float[] vertices {get; set;}
        public int[] indices {get; set;}
        public float[] colors {get; set;}
        public  int nvertices {get; set;}
        public int nindices {get; set;}
        public ushort[] usindices {get; set;}
        public float[] normals {get; set;}
    }

    public class RetrievedMeshMeta{
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
        public double[] shadowColor{get; set;} //agregado para color de sombras
        public string type {get; set;}

        public double[] position {get;set;}

        public RetrievedOrientation orientation {get; set;}

        public double[] scale {get; set;}

        public double[] basecolor{get; set;}
        
    }


      public class RetrievedLevel{
            public RetrievedMeshMeta[] mesh_list {get; set;}
            public RetrievedActor[] actor_list {get; set;}    

            public double[] playerstartposition{get; set;}
            public double playerstartrotationangle{get;set;}
            public double[] playerstartrotationaxis{get;set;}

            public double[] ambientlight{get; set;}

            public double[] shadowplanenormal{get; set;}
            public double[] shadowplanepoint{get; set;}

    }

}