using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp; //Textures
using SixLabors.ImageSharp.Formats.Jpeg; //Textures
using SixLabors.ImageSharp.PixelFormats; //Textures
using System.Runtime.InteropServices; //MemoryMarshal




using SimpleGame;
using SimpleGame.Math;

namespace SimpleGame.GameFramework {

public enum ActorType : byte {

    StaticMesh,
    Light,
    Generic

}

public enum MaterialType: byte{

    BaseColor,
    Texture
}

public enum CollisionType : byte {
    NoCollision,
    Box,

    Sphere
}


public class Actor{


   public bool Enabled {get; set;}

   public ActorType Type {get; set;}

    public string StaticMeshId {get; set;}

    public string TextureId{get;set;}

    public MaterialType MaterialType{get;set;}

    public bool Shadow {get; set;}

    public AffineMat4  Transform= new AffineMat4();

    public AffineMat4 ModelView=new AffineMat4();

    public AffineMat4 NormalTransform = new AffineMat4();

    public List<AffineMat4> ModelViewShadow = new List<AffineMat4>();

    public Vector3 Scale;

    public Vector4 BaseColor;

    public Vector3 Direction; 

    public CollisionType CollisionType;
    public List<Vector3> CollisionData;


    public Actor(){
        this.Enabled=false;
        this.StaticMeshId="";
        this.CollisionType=CollisionType.NoCollision;
        this.CollisionData=new List<Vector3>();
    }

    public void SetTransform(Vector3 positionVector, Vector3 axisVector, double angle, Vector3 scale){
        this.Scale = scale;
        Transform.Rotation((float)angle,axisVector);
        Transform.Scale(scale);
        if(CollisionType==CollisionType.Box){
        List<Vector3> newCollisionData=new List<Vector3>();
        foreach(var vec in CollisionData){
            newCollisionData.Add(scale*vec);
        }
        CollisionData=newCollisionData;
        }
        Transform.Translation(positionVector);
    }

    public void GenerateCollisionData(CollisionType collisionType,RetrievedMesh mesh){
        if(Type!=ActorType.StaticMesh)
            return;
        CollisionData = new List<Vector3>();
        List<Vector3> vertices = Vector3.GenerateList(mesh.vertices);

        if(collisionType==CollisionType.Box){
            CollisionData.Add(Vector3.Min(vertices));
            CollisionData.Add(Vector3.Max(vertices));
        }
        return;
    }

    public List<Vector3> GetCollisionBox(){
        return CollisionData.GetRange(0,2);
    }
 
}
public class Level{

public Dictionary<string,Actor> ActorCollection {get; set;}

public Vector3 PlayerStartPosition {get; set;}

public double PlayerStartRotationAngle {get;set;}

public Vector3 PlayerStartRotationAxis {get;set;}

public Vector4 AmbientLight {get;set;}

public Vector3 ShadowPlaneNormal{get; set;}
public Vector3 ShadowPlanePoint{get; set;}
public Level(){

ActorCollection=new Dictionary<string,Actor>();
PlayerStartPosition=new Vector3(0.0f,0.0f,0.0f);
PlayerStartRotationAngle=0;
PlayerStartRotationAxis=new Vector3(0.0f,1.0f,0.0f);
AmbientLight = new Vector4(0.0f,0.0f,0.0f,1.0f);
ShadowPlaneNormal = new Vector3(0.0f,1.0f,0.0f);
ShadowPlanePoint = new Vector3(0.0f,0.0f,0.0f);
}

public Level(HttpClient httpClient, string path){
_httpClient = httpClient;
_levelPath = path;
ActorCollection=new Dictionary<string,Actor>();
PlayerStartPosition=new Vector3(0.0f,0.0f,0.0f);
PlayerStartRotationAngle=0;
PlayerStartRotationAxis=new Vector3(0.0f,1.0f,0.0f);

}

public List<string> GetActiveMeshes(){
    List<string> activeIds=new List<string>();
    foreach (var keyval in ActorCollection){
        if(keyval.Value.Enabled && keyval.Value.Type==ActorType.StaticMesh){
            if(!activeIds.Contains(keyval.Value.StaticMeshId))
                activeIds.Add(keyval.Value.StaticMeshId);
        }
    }
    return activeIds;
}
public List<string> GetActiveTextures(){
    List<string> activeIds=new List<string>();
    foreach (var keyval in ActorCollection){
        if(keyval.Value.Enabled && keyval.Value.Type==ActorType.StaticMesh && keyval.Value.MaterialType==MaterialType.Texture){
            if(!activeIds.Contains(keyval.Value.TextureId))
                activeIds.Add(keyval.Value.TextureId);
        }
    }
    return activeIds;
}



private void  setTexture(RetrievedTexture texture, byte[] loadedData){
    var configuration = new Configuration(new JpegConfigurationModule());

    Image<Rgba32> decodedImage = Image.Load<Rgba32>(configuration,loadedData);

    var pixels = new byte[4 * decodedImage.Width * decodedImage.Height];
    decodedImage.CopyPixelDataTo(pixels);
    texture.Rgba32DecodedImage=pixels;    // Span to array of bytes
    texture.Width=decodedImage.Width;
    texture.Height=decodedImage.Height;

}
public  async Task RetrieveLevel(Dictionary<string,RetrievedAsset> AssetCollection){

_retrievedLevel = await _httpClient.GetFromJsonAsync<RetrievedLevel>(_levelPath);

for(int i=0;i<_retrievedLevel.mesh_list.Length;i++){

    RetrievedMeshMeta meshMeta=_retrievedLevel.mesh_list[i];
        
    RetrievedMesh retMesh=await _httpClient.GetFromJsonAsync<RetrievedMesh>(meshMeta.file);
    retMesh.usindices = new ushort[retMesh.indices.Length];
    for(int j=0; j<retMesh.indices.Length;j++)
        retMesh.usindices[j]=(ushort)j;
    //retMesh.usindices = Array.ConvertAll<int,ushort>(retMesh.indices,delegate(int val){return (ushort)val;});
    retMesh.AssetType = AssetType.StaticMesh;    
    // Create arrays based on indices
    int nindices=retMesh.indices.Length;
    retMesh.vertexarray=new float[3*nindices];
    retMesh.colorarray=new float[4*nindices];
    retMesh.normalarray=new float[3*nindices];
    int ind;
    for(int j=0;j<retMesh.indices.Length;j++){
        ind=retMesh.indices[j];
        retMesh.vertexarray[j*3]=retMesh.vertices[ind*3];
        retMesh.vertexarray[j*3+1]=retMesh.vertices[ind*3+1];
        retMesh.vertexarray[j*3+2]=retMesh.vertices[ind*3+2];
    }
    for(int j=0;j<retMesh.indices.Length;j++){
        ind=retMesh.indices[j];
        retMesh.colorarray[j*4]=retMesh.colors[ind*4];
        retMesh.colorarray[j*4+1]=retMesh.colors[ind*4+1];
        retMesh.colorarray[j*4+2]=retMesh.colors[ind*4+2];
        retMesh.colorarray[j*4+3]=retMesh.colors[ind*4+3];

    }
    for(int j=0;j<retMesh.indices.Length;j++){
        ind=retMesh.indices[j];
        retMesh.normalarray[j*3]=retMesh.normals[ind*3];
        retMesh.normalarray[j*3+1]=retMesh.normals[ind*3+1];
        retMesh.normalarray[j*3+2]=retMesh.normals[ind*3+2];

    }
 

    AssetCollection.Add(meshMeta.id,retMesh);

}
for(int i=0;i<_retrievedLevel.texture_list.Length;i++){

    RetrievedTextureMeta texMeta=_retrievedLevel.texture_list[i];
    RetrievedTexture texture = new RetrievedTexture();
    byte[] rawData = await _httpClient.GetByteArrayAsync(texMeta.file);
    setTexture(texture,rawData);
    AssetCollection.Add(texMeta.id,texture);

}


for(int i=0;i<_retrievedLevel.actor_list.Length;i++){
    RetrievedActor retActor=_retrievedLevel.actor_list[i];
    Actor actor = new Actor();
    actor.Enabled=retActor.enabled;
    actor.Shadow = retActor.shadow;
    ActorType type;
    switch(retActor.type){
        case "staticmesh":
            type = ActorType.StaticMesh;
            break;
        case "dirlight":
            type = ActorType.Light;
            break;
        default:
            type = ActorType.Generic;
            break;

    }
    actor.Type = type;
    if(actor.Type==ActorType.StaticMesh){
        actor.StaticMeshId=retActor.sm;
        if(retActor.collisionbox){
            actor.CollisionType=CollisionType.Box;
            actor.GenerateCollisionData(CollisionType.Box,(RetrievedMesh)AssetCollection[retActor.sm]);
        }
    }
    actor.SetTransform(new Vector3(retActor.position),new Vector3(retActor.orientation.axis),retActor.orientation.angle, new Vector3(retActor.scale));
    //Console.WriteLine(retActor.id);
    actor.BaseColor = new Vector4(retActor.basecolor);
    if(retActor.texture!=""){
        actor.MaterialType=MaterialType.Texture;
        actor.TextureId=retActor.texture;
    }
    else
    {
        actor.MaterialType=MaterialType.BaseColor;
    }
     ActorCollection.Add(retActor.id,actor);

}

PlayerStartPosition = new Vector3(_retrievedLevel.playerstartposition);
PlayerStartRotationAngle = _retrievedLevel.playerstartrotationangle;
PlayerStartRotationAxis= new Vector3(_retrievedLevel.playerstartrotationaxis); 
AmbientLight = new Vector4(_retrievedLevel.ambientlight);
ShadowPlaneNormal = new Vector3(_retrievedLevel.shadowplanenormal);
ShadowPlanePoint = new Vector3(_retrievedLevel.shadowplanepoint);

}


private string _levelPath;
private HttpClient _httpClient;
private RetrievedLevel _retrievedLevel;


}
}