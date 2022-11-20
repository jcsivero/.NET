using Microsoft.JSInterop;
using WebGl.Pages.Math;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Blazor.Extensions;
using Blazor.Extensions.Canvas;
using Blazor.Extensions.Canvas.WebGL;

namespace WebGl.Pages {
public partial class Game : ComponentBase {
    [Inject]
    private  IJSRuntime JSRuntime {get; set;}

    private int currentCount = 0;
    private WebGLContext _context;
    protected BECanvasComponent _canvasReference;

    	private static readonly float[] cubeVertices =  {
        -1.0f,-1.0f,-1.0f,
        -1.0f,1.0f,-1.0f,
        1.0f,1.0f,-1.0f,
        1.0f,-1.0f,-1.0f,
        -1.0f,-1.0f,1.0f,
        -1.0f,1.0f,1.0f,
        1.0f,1.0f,1.0f,
        1.0f,-1.0f,1.0f
    };

    private static readonly int[] intCubeIndices =  {
        2,1,0,
        3,2,0,
        6,2,3,
        7,6,3,
        1,4,0,
        5,4,1,
        5,7,4,
        6,7,5,
        2,5,1,
        2,6,5,
        4,3,0,
        7,3,2
    };

    private float[] cubeColors= new [] {
        1.0f,0.0f,0.0f,1.0f,
        1.0f,0.0f,0.0f,1.0f,
        1.0f,0.0f,0.0f,1.0f,
        1.0f,0.0f,0.0f,1.0f,
        0.0f,1.0f,0.0f,1.0f,
        0.0f,1.0f,0.0f,1.0f,
        0.0f,1.0f,0.0f,1.0f,
        0.0f,1.0f,0.0f,1.0f
    };


    private static readonly ushort[] cubeIndices = Array.ConvertAll(intCubeIndices, val=>checked((ushort) val));

    private const string vsSource=@"
    uniform mat4 uModelViewMatrix;
    uniform mat4 uProjectionMatrix;
    attribute vec3 aVertexPosition;
    attribute vec4 aVertexColor;
    varying vec4 vVertexPosition;
    varying vec4 vVertexColor;
    void main(void){
    vVertexPosition = uProjectionMatrix*uModelViewMatrix*vec4(0.5*aVertexPosition,1.0);
    vVertexColor=aVertexColor;
    gl_Position = vVertexPosition;
    }";


    private const string fsSource=@"
    precision mediump float;
    varying vec4 vVertexColor;
    void main(){
    gl_FragColor=vVertexColor;
    }"; 

    private WebGLShader vertexShader;
    private WebGLShader fragmentShader;
    private WebGLProgram program;

    private int positionAttribLocation;
    private int colorAttribLocation;
    private WebGLUniformLocation projectionUniformLocation;
    private WebGLUniformLocation modelViewUniformLocation;

    private WebGLBuffer vertexBuffer;
    private WebGLBuffer colorBuffer;
    private WebGLBuffer indexBuffer;
    public AffineMat4 ModelViewMat;
    public AffineMat4 ProyMat;

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
        this.vertexBuffer = await this._context.CreateBufferAsync();
        await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER,this.vertexBuffer);
        await this._context.BufferDataAsync(BufferType.ARRAY_BUFFER, cubeVertices, BufferUsageHint.STATIC_DRAW);

        this.colorBuffer = await this._context.CreateBufferAsync();
        await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER,this.colorBuffer);
        await this._context.BufferDataAsync(BufferType.ARRAY_BUFFER, this.cubeColors, BufferUsageHint.STATIC_DRAW);

        this.indexBuffer = await this._context.CreateBufferAsync();
        await this._context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER,this.indexBuffer);
        await this._context.BufferDataAsync(BufferType.ELEMENT_ARRAY_BUFFER, cubeIndices, BufferUsageHint.STATIC_DRAW);

        // Disconect buffers
        await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER,null);
        await this._context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER,null);
    }


	 private async Task getAttributeLocations(){    

        this.positionAttribLocation = await this._context.GetAttribLocationAsync(this.program,"aVertexPosition");
        this.colorAttribLocation = await this._context.GetAttribLocationAsync(this.program,"aVertexColor");
        this.projectionUniformLocation=await this._context.GetUniformLocationAsync(this.program,"uProjectionMatrix");
        this.modelViewUniformLocation = await this._context.GetUniformLocationAsync(this.program,"uModelViewMatrix");

    }
	
	/*protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        this._context = await this._canvasReference.CreateWebGLAsync();
        this.vertexShader=await this.GetShader(vsSource,ShaderType.VERTEX_SHADER);
        this.fragmentShader=await this.GetShader(fsSource,ShaderType.FRAGMENT_SHADER);

        this.program= await this.BuildProgram(this.vertexShader,this.fragmentShader);
        await this._context.DeleteShaderAsync(this.vertexShader);
        await this._context.DeleteShaderAsync(this.fragmentShader);
        await this.prepareBuffers();
        await this.getAttributeLocations();

        await this._context.ClearColorAsync(1, 0, 0, 1);
        await this._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT);
    }*/

    
    /*protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        this._context = await this._canvasReference.CreateWebGLAsync();
    
        await this._context.ClearColorAsync(1, 0, 0, 1);
        await this._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT);
    }*/
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        this._context = await this._canvasReference.CreateWebGLAsync();
        this.vertexShader=await this.GetShader(vsSource,ShaderType.VERTEX_SHADER);
        this.fragmentShader=await this.GetShader(fsSource,ShaderType.FRAGMENT_SHADER);

        this.program= await this.BuildProgram(this.vertexShader,this.fragmentShader);
        await this._context.DeleteShaderAsync(this.vertexShader);
        await this._context.DeleteShaderAsync(this.fragmentShader);
        await this.prepareBuffers();
        await this.getAttributeLocations();

        await this._context.ClearColorAsync(1, 0, 0, 1);
        await this._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT);

        InitializeModel();
        Console.WriteLine("Starting Game Loop");
        await JSRuntime.InvokeAsync<object>("initRenderJS",DotNetObjectReference.Create(this));

    }  

private float rotation_angle = 0.0f;
private float lastTimeStamp =0.0f;

public void Update(float timeStamp){
const float vel = 0.001f;
float delta;
double FOV = 45.0* System.Math.PI / 180.0f;
double r = this._context.DrawingBufferWidth / this._context.DrawingBufferHeight;
double near = 0.1;
double far = 100.0f;

this.ProyMat.Perspective((float)FOV,(float)r,(float)near,(float)far);

Vector3 axis = new Vector3(0.0f,1.0f,1.0f);
axis.Normalize();
delta = timeStamp-this.lastTimeStamp;
this.lastTimeStamp = timeStamp;
//this.rotation_angle += (delta*vel*360.0f)%360.0f;
this.rotation_angle += vel*delta;
this.ModelViewMat.Rotation(rotation_angle,axis);
}


[JSInvokable]
public async void GameLoop(float timeStamp ){

        this.Update(timeStamp);

        await this.Draw();
}



public async Task Draw(){
    await this._context.BeginBatchAsync();
	await this._context.UseProgramAsync(this.program);
    await this._context.UniformMatrixAsync(this.projectionUniformLocation,false,this.ProyMat.GetArray());
    await this._context.UniformMatrixAsync(this.modelViewUniformLocation,false,this.ModelViewMat.GetArray());
    await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER, this.vertexBuffer);
    await this._context.EnableVertexAttribArrayAsync((uint)this.positionAttribLocation);
    await this._context.VertexAttribPointerAsync((uint)this.positionAttribLocation,3, DataType.FLOAT, false, 0, 0L);

    await this._context.BindBufferAsync(BufferType.ARRAY_BUFFER, this.colorBuffer);
    await this._context.EnableVertexAttribArrayAsync((uint)this.colorAttribLocation);
    await this._context.VertexAttribPointerAsync((uint)this.colorAttribLocation,4, DataType.FLOAT, false, 0, 0L);
    
    await this._context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, this.indexBuffer);

    await this._context.ClearColorAsync(0, 0, 1, 1);
    await this._context.ClearDepthAsync(1.0f);
    await this._context.DepthFuncAsync(CompareFunction.LEQUAL);
    await this._context.EnableAsync(EnableCap.DEPTH_TEST);
    await this._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);
    await this._context.ViewportAsync(0,0,this._context.DrawingBufferWidth,this._context.DrawingBufferHeight);
    await this._context.DrawElementsAsync(Primitive.TRIANGLES,cubeIndices.Length,DataType.UNSIGNED_SHORT, 0);
    await this._context.EndBatchAsync();
 

}

    public void InitializeModel(){
        Vector3 initial_translate= new Vector3(0.0f,0.0f,-3.0f);
        this.ModelViewMat = new AffineMat4();
        this.ProyMat=new AffineMat4();
        this.ModelViewMat.Translate(initial_translate);
}

     private void IncrementCount()
    {
        currentCount++;
        Console.WriteLine($"El valor del contador ahora es {currentCount}");
    }  
  }


 }


