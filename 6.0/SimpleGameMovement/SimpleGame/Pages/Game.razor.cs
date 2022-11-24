using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Microsoft.JSInterop; //Interop for game loop rendering through Javascript
using System.Net.Http;
using System.Net.Http.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazor.Extensions;
using Blazor.Extensions.Canvas;
using Blazor.Extensions.Canvas.WebGL;

using SimpleGame.Math;
using SimpleGame.Shared;
using SimpleGame;
using SimpleGame.GameFramework;

namespace SimpleGame.Pages {
public partial class Game : ComponentBase {

 
    // Just for debugging purposes
    private int currentCount = 0;
 

    // Injected services

    [Inject]
    private  IJSRuntime JSRuntime {get; set;}

    [Inject]
    private HttpClient HttpClient {get; set;}

    [CascadingParameter]
    protected Controller PawnController {get; set;}
 

// Assets Container

    public Dictionary<string,RetrievedMesh> AssetsCollection {get; set;} = new Dictionary<string,RetrievedMesh>();
    // Retrieved Level

    public GameFramework.Level ActiveLevel {get; set;}


    // Game state:  Geometry
    
    private float lastTimeStamp =0.0f;

    public readonly Vector3 Up = new Vector3(0.0f,1.0f,0.0f);

    private AffineMat4 ModelMat= new AffineMat4();

    private AffineMat4 CameraMat= new AffineMat4();
    private AffineMat4 ModelViewMat = new AffineMat4();
    private AffineMat4 ProyMat = new AffineMat4();
    private AffineMat4 NormalTransform = new AffineMat4();

    // Game state: User Interaction
 
    private double currentMouseX, currentMouseY;


    // Rendering state

    protected BECanvasComponent _canvasReference;

    private WebGLContext _context;
    private RetrievedMesh retMesh;

    
    private const string vsSource=@"
    uniform mat4 uModelViewMatrix;
    uniform mat4 uProjectionMatrix;
    uniform mat4 uNormalTransformMatrix;
    attribute vec3 aVertexPosition;
    attribute vec3 aVertexNormal;
    attribute vec4 aVertexColor;
    varying vec4 vVertexPosition;
    varying vec4 vVertexNormal;
    varying vec4 vVertexColor;
    void main(void){
    vVertexPosition = uProjectionMatrix*uModelViewMatrix*vec4(0.5*aVertexPosition,1.0);
    vVertexNormal = uNormalTransformMatrix * vec4(aVertexNormal,0.0);
    vVertexColor=aVertexColor;
    gl_Position = vVertexPosition;
    }";


    private const string fsSource=@"
    precision mediump float;
    varying vec4 vVertexColor;
    varying vec4 vVertexNormal;
    void main(){
    vec4 la = vec4(0.1,0.8,0.1,1.0);
    vec4 ld = vec4(1.0,1.0,1.0,1.0);
    vec3 ldir = vec3(1.0,1.0,1.5);
    float cl = max(dot(ldir,vVertexNormal.xyz),0.0);
    vec4 newcolor = la*vVertexColor+vec4(cl*(ld.rgb*vVertexColor.rgb),ld.a*vVertexColor.a);
    //gl_FragColor=newcolor;
    //gl_FragColor=vVertexColor;
    gl_FragColor=vec4(max(vVertexNormal.x,0.0),max(vVertexNormal.y,0.0),max(vVertexNormal.z,0.0),1.0);
    }"; 


    private WebGLShader vertexShader;
    private WebGLShader fragmentShader;
    private WebGLProgram program;

    
    private Dictionary<string,MeshBuffers> BufferCollection  = new Dictionary<string,MeshBuffers>();


    private int positionAttribLocation;
    private int normalAttribLocation;
    private int colorAttribLocation;
    private WebGLUniformLocation projectionUniformLocation;
    private WebGLUniformLocation modelViewUniformLocation;
    private WebGLUniformLocation normalTransformUniformLocation;

   

    ////////////////////////////////////////////////////////////////////////////
    // WebGL related methods
    ////////////////////////////////////////////////////////////////////////////
    
    private async Task<WebGLShader> GetShader(string code, ShaderType stype ){

        WebGLShader shader = await this._context.CreateShaderAsync(stype);
        await this._context.ShaderSourceAsync(shader,code);
        await this._context.CompileShaderAsync(shader);
        if (!await this._context.GetShaderParameterAsync<bool>(shader, ShaderParameter.COMPILE_STATUS))
                {
                    string info = await this._context.GetShaderInfoLogAsync(shader);
                    await this._context.DeleteShaderAsync(shader);
                    throw new Exception("An error occured while compiling the shader: " + info);
                }

        return shader;

    }

    private async Task<WebGLProgram> BuildProgram(WebGLShader vShader, WebGLShader fShader){
        var prog = await this._context.CreateProgramAsync();
        await this._context.AttachShaderAsync(prog, vShader);
        await this._context.AttachShaderAsync(prog, fShader);
        await this._context.LinkProgramAsync(prog);


        if (!await this._context.GetProgramParameterAsync<bool>(prog, ProgramParameter.LINK_STATUS))
        {
                    string info = await this._context.GetProgramInfoLogAsync(prog);
                    throw new Exception("An error occured while linking the program: " + info);
        }

        return prog;
   
    }

    private async Task prepareBuffers(){

        List<string> activeMeshes = ActiveLevel.GetActiveMeshes();
        // Buffer creation
        foreach(string meshid in activeMeshes){
            MeshBuffers buffers = new MeshBuffers();
            buffers.VertexBuffer = await this._context.CreateBufferAsync();
            buffers.ColorBuffer = await this._context.CreateBufferAsync();
            buffers.NormalBuffer = await this._context.CreateBufferAsync();
            buffers.IndexBuffer = await this._context.CreateBufferAsync();
            RetrievedMesh retMesh = AssetsCollection[meshid];
            buffers.NumberOfIndices=retMesh.indices.Length;
            BufferCollection.Add(meshid,buffers);

        }
        // Data transfer
        foreach(KeyValuePair<string,MeshBuffers> keyval in BufferCollection){
            RetrievedMesh retMesh = AssetsCollection[keyval.Key];
            MeshBuffers buffers = keyval.Value;
            await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER,buffers.VertexBuffer);
            await this._context.BufferDataAsync(BufferType.ARRAY_BUFFER, retMesh.vertices, BufferUsageHint.STATIC_DRAW);

            await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER,buffers.ColorBuffer);
            await this._context.BufferDataAsync(BufferType.ARRAY_BUFFER, retMesh.colors, BufferUsageHint.STATIC_DRAW);

            await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER,buffers.NormalBuffer);
            await this._context.BufferDataAsync(BufferType.ARRAY_BUFFER, retMesh.normals, BufferUsageHint.STATIC_DRAW);

            await this._context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER,buffers.IndexBuffer);
            await this._context.BufferDataAsync(BufferType.ELEMENT_ARRAY_BUFFER, retMesh.usindices, BufferUsageHint.STATIC_DRAW);
            
        }

        // Disconect buffers
        await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER,null);
        await this._context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER,null);
    }


    

    private async Task getAttributeLocations(){    

        this.positionAttribLocation = await this._context.GetAttribLocationAsync(this.program,"aVertexPosition");
        this.normalAttribLocation = await this._context.GetAttribLocationAsync(this.program,"aVertexNormal");

        this.colorAttribLocation = await this._context.GetAttribLocationAsync(this.program,"aVertexColor");
        this.projectionUniformLocation=await this._context.GetUniformLocationAsync(this.program,"uProjectionMatrix");
        this.modelViewUniformLocation = await this._context.GetUniformLocationAsync(this.program,"uModelViewMatrix");
        this.normalTransformUniformLocation = await this._context.GetUniformLocationAsync(this.program,"uNormalTransformMatrix");

    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // Update stage related methods
    ///////////////////////////////////////////////////////////////////////////////////////////////////

private void calculateModelView(){
        foreach(var keyval in ActiveLevel.ActorCollection){
            if(!keyval.Value.Enabled)
                continue;
            keyval.Value.ModelView.Copy(keyval.Value.Transform);
            keyval.Value.ModelView.LeftMProduct(this.CameraMat);
            keyval.Value.NormalTransform.Copy(keyval.Value.ModelView);
            keyval.Value.NormalTransform.GeneralInverse();
            keyval.Value.NormalTransform.Transpose();


        }

    }

    private Vector3 pawn_position {get; set;}

    public void InitializeGameState(){
        GameFramework.Actor pawn = GetPawn();

        if(pawn==null)
            Console.WriteLine("Warning, Not defined pawn in level");

        // Spawn transform for Pawn is extracted from Level definition
        Vector3 pawn_position = new Vector3(0.0f,0.0f,-3.0f);
        double pawn_angle=0.0;
        Vector3 pawn_axis=new Vector3(0.0f,1.0f,0.0f);
        if (ActiveLevel.PlayerStartPosition != null)
            pawn_position = ActiveLevel.PlayerStartPosition;
        
        pawn_angle=ActiveLevel.PlayerStartRotationAngle;
        
        if(ActiveLevel.PlayerStartRotationAxis != null)
            pawn_axis=ActiveLevel.PlayerStartRotationAxis;
        

        pawn.SetTransform(pawn_position,pawn_axis,pawn_angle);
        ActiveLevel.ActorCollection["pawn"]=pawn;

    }


private GameFramework.Actor GetActorById(string id){
        return ActiveLevel.ActorCollection[id];
    }

    private GameFramework.Actor GetPawn(string id="apawn"){
        if(ActiveLevel.ActorCollection.ContainsKey(id))
            return ActiveLevel.ActorCollection[id];
        else
            return null;
    }


private void updatePawn(){

        Vector3 displacement = this.PawnController.GetMovement();
        ActiveLevel.ActorCollection["apawn"].Transform.Translate(displacement);

    }


    Angles2D angles = new Angles2D();

    private void updateCamera(){

        double boomDistance=2.0;

        Angles2D boomAngles = this.PawnController.GetBoomAngles();
        this.angles.Yaw=boomAngles.Yaw;
        this.angles.Pitch = boomAngles.Pitch;
        double f= System.Math.PI/180.0;
        double cPitch = System.Math.Cos(boomAngles.Pitch*f);
        double x = boomDistance * cPitch * System.Math.Sin(boomAngles.Yaw*f);
        double z = boomDistance * cPitch * System.Math.Cos(boomAngles.Yaw*f);
        double y = boomDistance *  System.Math.Sin(boomAngles.Pitch*f);

        Vector3 camera_position = new Vector3((float)x,(float)y,(float)z);
        Vector3 pawn_position = GetPawn().Transform.GetTranslateVector();
        camera_position.Add(pawn_position);


        this.CameraMat.LookAt(camera_position,pawn_position,this.Up);
       

    }


    ///////////////////////////////////////////////////////////////////////
    ///////              UPDATE METHOD                         ///////////
    /////////////////////////////////////////////////////////////////////

    public void Update(float timeStamp){
        float delta;
        double FOV = 45.0* System.Math.PI / 180.0f;
        double r = this._context.DrawingBufferWidth / this._context.DrawingBufferHeight;
        double near = 0.1;
        double far = 100.0f;

        //Read User Interaction through Controller
        Coordinates2D mCoord = this.PawnController.GetMCoord();
        this.currentMouseX = mCoord.X;
        this.currentMouseY=mCoord.Y;

        //Update PawnController Parameters
        PawnController.MouseEffect=this.uiInteraction.MouseEffect;
        PawnController.BoomRate=this.uiInteraction.BoomRate;

        // Pawn update
        updatePawn();

        // Camera update
        updateCamera();

        // Proyection Matrix
        this.ProyMat.Perspective((float)FOV,(float)r,(float)near,(float)far);

        delta = timeStamp-this.lastTimeStamp;

        // ModelView and NormalTransform for all actors
        calculateModelView();
    }


    ////////////////////////////////////////////////////////////////////////////////////
    /////////////////     RENDERING METHODS                           /////////////////
    ///////////////////////////////////////////////////////////////////////////////////
    public async Task Draw(){
        await this._context.BeginBatchAsync();
        // Object independent operations
        await this._context.UseProgramAsync(this.program);
        await this._context.UniformMatrixAsync(this.projectionUniformLocation,false,this.ProyMat.GetArray());
        await this._context.ClearColorAsync(0, 0, 1, 1);
        await this._context.ClearDepthAsync(1.0f);
        await this._context.DepthFuncAsync(CompareFunction.LEQUAL);
        await this._context.EnableAsync(EnableCap.DEPTH_TEST);
        await this._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);
        await this._context.ViewportAsync(0,0,this._context.DrawingBufferWidth,this._context.DrawingBufferHeight);
         
        // Loop on objects
        foreach( var keyval in ActiveLevel.ActorCollection){
            GameFramework.Actor actor = keyval.Value;
            if(!actor.Enabled)
                continue;
            MeshBuffers mBuffers = BufferCollection[actor.StaticMeshId]; 


            // Update uniforms
            await this._context.UniformMatrixAsync(this.modelViewUniformLocation,false,actor.ModelView.GetArray());
            await this._context.UniformMatrixAsync(this.normalTransformUniformLocation,false,actor.NormalTransform.GetArray());

            // Buffers to attributes
            await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER, mBuffers.VertexBuffer);
            await this._context.EnableVertexAttribArrayAsync((uint)this.positionAttribLocation);
            await this._context.VertexAttribPointerAsync((uint)this.positionAttribLocation,3, DataType.FLOAT, false, 0, 0L);
        
            await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER, mBuffers.NormalBuffer);
            await this._context.EnableVertexAttribArrayAsync((uint)this.normalAttribLocation);
            await this._context.VertexAttribPointerAsync((uint)this.normalAttribLocation,3, DataType.FLOAT, false, 0, 0L);


            await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER, mBuffers.ColorBuffer);
            await this._context.EnableVertexAttribArrayAsync((uint)this.colorAttribLocation);
            await this._context.VertexAttribPointerAsync((uint)this.colorAttribLocation,4, DataType.FLOAT, false, 0, 0L);
        
            await this._context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, mBuffers.IndexBuffer);

           await this._context.DrawElementsAsync(Primitive.TRIANGLES,mBuffers.NumberOfIndices,DataType.UNSIGNED_SHORT, 0);
        }
        await this._context.EndBatchAsync();

    }



    ////////////////////////////////////////////////////////////////////////////////////////////////
    ////////        GAME LOOP                                                               /////////
    /////////////////////////////////////////////////////////////////////////////////////////////////

    [JSInvokable]
    public async void GameLoop(float timeStamp ){

            this.Update(timeStamp);

            await this.Draw();
    }

        ///////////////////////////////////////////////////////////////////////////////////
        // On After Render Method: all the things that happen after the Blazor component has
        // been redendered: initializations
        //////////////////////////////////////////////////////////////////////////////////
        private int windowHeight {get; set;}
        private int windowWidth {get; set;}

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            var dimension = await JSRuntime.InvokeAsync<WindowDimension>("getWindowDimensions");
            this.windowHeight = dimension.Height;
            this.windowWidth = dimension.Width;
            if(!firstRender)
                return;
             
             ActiveLevel = new GameFramework.Level(HttpClient,"assets/level.json");

              await ActiveLevel.RetrieveLevel(AssetsCollection);
            Console.WriteLine("Nivel Cargado: " + ActiveLevel);

            // Initialize Controller
            PawnController.WindowWidth=this.windowWidth;
            PawnController.WindowHeight=this.windowHeight;
            PawnController.MouseEffect=400.0;
            PawnController.BoomRate=1.0;
            this.uiInteraction.MouseEffect=400.0;
            this.uiInteraction.BoomRate=1.0;


            // Initialize Rendering State
            // Retrieving mesh
            retMesh = await HttpClient.GetFromJsonAsync<RetrievedMesh>("assets/mesh.json");
            Console.WriteLine($"Length normals:{retMesh.normals.Length}");
            //remMest.usindices = new ushort[retMesh.indices.Length]
            retMesh.usindices = Array.ConvertAll<int,ushort>(retMesh.indices,delegate(int val){return (ushort)val;});
            Console.WriteLine($"Nvertces:{retMesh.nvertices}");

            // Getting the WebGL context
            this._context = await this._canvasReference.CreateWebGLAsync();

            // Getting the program as part of the pipeline state
            this.vertexShader=await this.GetShader(vsSource,ShaderType.VERTEX_SHADER);
            this.fragmentShader=await this.GetShader(fsSource,ShaderType.FRAGMENT_SHADER);

            this.program= await this.BuildProgram(this.vertexShader,this.fragmentShader);
            await this._context.DeleteShaderAsync(this.vertexShader);
            await this._context.DeleteShaderAsync(this.fragmentShader);

            // Getting the pipeline buffers a part of the pipeline state

            await this.prepareBuffers();

            // Storing the attribute locations
            await this.getAttributeLocations();


            // Other pipele state initial configurations
            await this._context.ClearColorAsync(1, 0, 0, 1);
            await this._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT);

            // Initialie UI parameters

            // Initialize Game State
            InitializeGameState();

            // Launch Game Loop!
            Console.WriteLine("Starting Game Loop");
            await JSRuntime.InvokeAsync<object>("initRenderJS",DotNetObjectReference.Create(this));

        }
    
    /////////////////////////////////////////////////////////////////////////////////
    //// Events
    /////////////////////////////////////////////////////////////////////////////////

    private UIInteraction uiInteraction = new UIInteraction(1.0,1.0);

    //////////////////////////////////////////////////////////////////////////////////////////
    // Debugging related methods
    ////////////////////////////////////////////////////////////////////////////////////////////
    private void IncrementCount()
    {
        currentCount++;
        Console.WriteLine($"El valor del contador ahora es {currentCount}");
    }

}
// Helper classes
public class UIInteraction{

    public double MouseEffect {get; set;}
    public double BoomRate{get; set;}

    public string MouseEffectInput {get; set;}
    public string BoomRateInput{get; set;}

    public void Update(){
        Console.WriteLine("Updating UI Parameters");
        this.MouseEffect=Double.Parse(MouseEffectInput);
        this.BoomRate=Double.Parse(BoomRateInput);
    }
    public UIInteraction(double m,double b){
        MouseEffect=m;
        BoomRate=b;
    }
}
public class WindowDimension
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }


public class MeshBuffers{

    public WebGLBuffer VertexBuffer {get; set;}
    public WebGLBuffer ColorBuffer {get; set;}
    public WebGLBuffer NormalBuffer {get; set;}

    public WebGLBuffer IndexBuffer {get; set;}

    public int NumberOfIndices {get;set;}
}

}

