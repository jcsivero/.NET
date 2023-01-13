using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using SimpleGame;
using SimpleGame.Math;

namespace SimpleGame.GameFramework {

public enum ActorType : byte {

    StaticMesh,
    Light,
    Generic

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

    public bool Shadow {get; set;}

    public AffineMat4  Transform= new AffineMat4();

    public AffineMat4 ModelView=new AffineMat4();

    public AffineMat4 NormalTransform = new AffineMat4();

    public List<AffineMat4> ModelViewShadow = new List<AffineMat4>();

    public Vector3 Scale;

    public Vector4 BaseColor;

    public Vector3 Direction; 

    public CollisionType CollisionType; // Marcamos el tipo de colisión
    public List<Vector3> CollisionData; // Datos necesarios en forma de lista de vectores


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
public  async Task RetrieveLevel(Dictionary<string,RetrievedMesh> AssetCollection){

_retrievedLevel = await _httpClient.GetFromJsonAsync<RetrievedLevel>(_levelPath);

for(int i=0;i<_retrievedLevel.mesh_list.Length;i++){

    RetrievedMeshMeta meshMeta=_retrievedLevel.mesh_list[i];
        
    RetrievedMesh retMesh=await _httpClient.GetFromJsonAsync<RetrievedMesh>(meshMeta.file);
    retMesh.usindices = new ushort[retMesh.indices.Length];
    retMesh.usindices = Array.ConvertAll<int,ushort>(retMesh.indices,delegate(int val){return (ushort)val;});

    AssetCollection.Add(meshMeta.id,retMesh);

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
    if(actor.Type==ActorType.StaticMesh)
    {
        actor.StaticMeshId=retActor.sm;
        if(retActor.collisionbox)
        {
            actor.CollisionType=CollisionType.Box;
            actor.GenerateCollisionData(CollisionType.Box,AssetCollection[retActor.sm]);
        }

    }
        
    actor.SetTransform(new Vector3(retActor.position),new Vector3(retActor.orientation.axis),retActor.orientation.angle, new Vector3(retActor.scale));
    actor.BaseColor = new Vector4(retActor.basecolor);
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