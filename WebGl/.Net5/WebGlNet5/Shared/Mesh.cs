namespace WebGlNet5.Mesh
{
public class RetrievedMesh
{
    public int nvertices {get; set;}
    public int nindices {get; set;}

    public float[] vertices {get; set;}
    public float[] normals {get; set;}
    public float[] colors {get; set;}
    public int[] indices {get; set;}

    public ushort[] usindices {get; set;}
}
}