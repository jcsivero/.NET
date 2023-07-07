using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop; //Interop for game loop rendering through Javascript
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazor.Extensions;
using Blazor.Extensions.Canvas.WebGL;
using SimpleGame.Math;
using SimpleGame.Shared;

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
 

    public readonly int NumberOfDirectionalLights = 2;

    // Game state:  Geometry

    // Assets Container

    public Dictionary<string,RetrievedMesh> AssetsCollection {get; set;}
    // Retrieved Level

    public GameFramework.Level ActiveLevel {get; set;}
    private float lastTimeStamp =0.0f;

    public readonly Vector3 Up = new Vector3(0.0f,1.0f,0.0f);

    private AffineMat4 ModelMat= new AffineMat4();

    private AffineMat4 CameraMat= new AffineMat4();
    private AffineMat4 ModelViewMat = new AffineMat4();
    public AffineMat4 ProyMat = new AffineMat4();
    private AffineMat4 NormalTransform = new AffineMat4();

    // Game state: User Interaction
 
    private double currentMouseX, currentMouseY;
    private Vector3 LastDisplacementLocal=new Vector3();
    private Vector3 LastDisplacementWorld=new Vector3();


    // Rendering state

    protected BECanvasComponent _canvasReference;

    public WebGLContext _context;

    public Dictionary<string,MeshBuffers> BufferCollection;

    public int positionAttribLocation;
    public int normalAttribLocation;
    public int colorAttribLocation;

    public WebGLUniformLocation projectionUniformLocation;
    public WebGLUniformLocation modelViewUniformLocation;
    public WebGLUniformLocation normalTransformUniformLocation;
    public WebGLUniformLocation baseColorLocation;
    public WebGLUniformLocation ambientLightLocation;
    // Uniform for directional lights

    public WebGLUniformLocation[] dirLightDirectionLocation;
    public WebGLUniformLocation[] dirLightDiffuseLocation;

    private List<AffineMat4> ShadowMatrix = new List<AffineMat4>();

    ////////////////////////////////////////////////////////////////////////////
    // WebGL related methods
    ////////////////////////////////////////////////////////////////////////////
    
    public async Task<WebGLShader> GetShader(string code, ShaderType stype ){

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

    public async Task<WebGLProgram> BuildProgram(WebGLShader vShader, WebGLShader fShader){
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

    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // Update stage related methods
    ///////////////////////////////////////////////////////////////////////////////////////////////////

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
        

        pawn.SetTransform(pawn_position,pawn_axis,pawn_angle,pawn.Scale);
        ActiveLevel.ActorCollection["pawn"]=pawn;

        updateCamera();
        updatePawn();
        calculateModelView();

    }

private void calculateModelView(){
        // Calculate Shadow Matrix for each light
        this.ShadowMatrix.Clear();
        foreach(var keyval in ActiveLevel.ActorCollection){
            if(!keyval.Value.Enabled)
                continue;
            if(keyval.Value.Type==GameFramework.ActorType.Light){
                GameFramework.Actor light = keyval.Value;
                Vector4 zunit = new Vector4(0.0f,0.0f,1.0f,0.0f);
                light.Direction=light.Transform.TransformVector(zunit); 
                AffineMat4 sm = new AffineMat4();
                sm.ShadowMatrix(light.Direction,ActiveLevel.ShadowPlaneNormal, ActiveLevel.ShadowPlanePoint);
                this.ShadowMatrix.Add(sm);
            }
        }

        foreach(var keyval in ActiveLevel.ActorCollection){

            if(!keyval.Value.Enabled)
                continue;
            if(keyval.Value.Type==SimpleGame.GameFramework.ActorType.StaticMesh){
            keyval.Value.ModelView.Copy(keyval.Value.Transform);
            keyval.Value.ModelView.LeftMProduct(this.CameraMat);
            keyval.Value.NormalTransform.Copy(keyval.Value.Transform);
            keyval.Value.NormalTransform.GeneralInverse();
            keyval.Value.NormalTransform.Transpose();
            int nLights = this.ShadowMatrix.Count;
            keyval.Value.ModelViewShadow.Clear();
            if(keyval.Value.Shadow){
            foreach(var sm in this.ShadowMatrix){
                AffineMat4 mv = new AffineMat4();
                mv.Copy(keyval.Value.Transform);
                mv.LeftMProduct(sm);
                mv.LeftMProduct(this.CameraMat);
                keyval.Value.ModelViewShadow.Add(mv);
            }
            }


        }
        }

    }


    Angles2D angles = new Angles2D();

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

        Vector3 displacement = this.PawnController.GetMovement(); // This displacement is pointing correctly in the world reference system 
        if(displacement.Norm()>0){
            this.LastDisplacementLocal=displacement; // Debugging purposes
            ActiveLevel.ActorCollection["apawn"].Transform.ForwardTo(displacement,this.Up);
            ActiveLevel.ActorCollection["apawn"].Transform.Translate(displacement);
            ActiveLevel.ActorCollection["apawn"].Transform.Scale(ActiveLevel.ActorCollection["apawn"].Scale);
        }
    }
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
    ///////////////////////////////////////////////////////////////////////////////////*

    public async Task Draw(Command command){
        


        await command.Exec();

        
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////
    ////////        GAME LOOP                                                               /////////
    /////////////////////////////////////////////////////////////////////////////////////////////////

    [JSInvokable]
    public async void GameLoop(float timeStamp ){

            this.Update(timeStamp);

            
             await this.Draw(commandDrawProgram_);
             await this.Draw(commandDrawProgramShadow_);
    }

        ///////////////////////////////////////////////////////////////////////////////////
        // On After Render Method: all the things that happen after the Blazor component has
        // been redendered: initializations
        //////////////////////////////////////////////////////////////////////////////////
        private int windowHeight {get; set;}
        private int windowWidth {get; set;}

        Command commandDrawProgram_;
        Command commandDrawProgramShadow_;
    
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {            
            var dimension = await JSRuntime.InvokeAsync<WindowDimension>("getWindowDimensions");
            this.windowHeight = dimension.Height;
            this.windowWidth = dimension.Width;
            if(!firstRender)
                return;
            

            dirLightDirectionLocation = new WebGLUniformLocation[this.NumberOfDirectionalLights];
            dirLightDiffuseLocation = new WebGLUniformLocation[this.NumberOfDirectionalLights];

            // Initialize Controller
            PawnController.WindowWidth=this.windowWidth;
            PawnController.WindowHeight=this.windowHeight;
            PawnController.MouseEffect=400.0;
            PawnController.BoomRate=1.0;
            this.uiInteraction.MouseEffect=400.0;
            this.uiInteraction.BoomRate=1.0;
            PawnController.GamePlaying=true;



            // Initialize Assets Container

            AssetsCollection = new Dictionary<string,RetrievedMesh>();
            BufferCollection = new Dictionary<string,MeshBuffers>();
            // Retrieve a level

            ActiveLevel = new GameFramework.Level(HttpClient,"assets/level.json");

            await ActiveLevel.RetrieveLevel(AssetsCollection);

  
            // Getting the WebGL context
            this._context = await this._canvasReference.CreateWebGLAsync();
            commandDrawProgram_ = new CommandDrawProgram(this);
            commandDrawProgramShadow_ = new CommandDrawShadow(this);
            await commandDrawProgram_.Initialize();
            await commandDrawProgramShadow_.Initialize();
                        

            // Getting the pipeline buffers a part of the pipeline state

            await this.prepareBuffers();


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
public class MeshBuffers{

    public WebGLBuffer VertexBuffer {get; set;}
    public WebGLBuffer ColorBuffer {get; set;}
    public WebGLBuffer NormalBuffer {get; set;}

    public WebGLBuffer IndexBuffer {get; set;}

    public int NumberOfIndices {get;set;}
}
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
}