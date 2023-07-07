using SimpleGame.Pages;
using Blazor.Extensions.Canvas.WebGL;

using System.Threading.Tasks;
public  class CommandDrawShadow : Command
{
   
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
    uniform vec4 uShadowColor;
    void main(){
    
    gl_FragColor=uShadowColor;
    
    }"; 
    public WebGLUniformLocation shadowColor; //para el atributo de sombra.
    public  CommandDrawShadow(Game value)
    {
        object_ = value;                
     
    }
  
    public override async Task  Initialize()
    {
           vertexShader=await object_.GetShader(vsSource,ShaderType.VERTEX_SHADER);
           fragmentShader= await object_.GetShader(fsSource,ShaderType.FRAGMENT_SHADER);
           program= await object_.BuildProgram(vertexShader,this.fragmentShader);
           await object_._context.DeleteShaderAsync(vertexShader);
           await object_._context.DeleteShaderAsync(this.fragmentShader);
    }

    public override async Task  Exec()
    {
        await object_._context.ClearColorAsync(0, 0, 1, 1);
        await object_._context.ClearDepthAsync(1.0f);
        await object_._context.DepthFuncAsync(CompareFunction.LEQUAL);
        await object_._context.EnableAsync(EnableCap.DEPTH_TEST);        
        await object_._context.ViewportAsync(0,0,object_._context.DrawingBufferWidth,object_._context.DrawingBufferHeight);
         
        await getAttributeLocations(program);
        await object_._context.UseProgramAsync(program);
        await object_._context.UniformMatrixAsync(object_.projectionUniformLocation,false,object_.ProyMat.GetArray());        
        await object_._context.BeginBatchAsync();      
        //await object_._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);                  
    
    // Loop on objects
        foreach( var keyval in object_.ActiveLevel.ActorCollection)
        {
            SimpleGame.GameFramework.Actor actor = keyval.Value;
            if(!actor.Enabled)
                continue;

            if(actor.Type==SimpleGame.GameFramework.ActorType.StaticMesh)
            {
                MeshBuffers mBuffers = object_.BufferCollection[actor.StaticMeshId]; 
            
                await object_._context.UniformAsync(shadowColor,actor.shadowColor.GetArray()); 

        // Buffers to attributes
                await object_._context.BindBufferAsync(BufferType.ARRAY_BUFFER, mBuffers.VertexBuffer);
                await object_._context.EnableVertexAttribArrayAsync((uint)object_.positionAttribLocation);
                await object_._context.VertexAttribPointerAsync((uint)object_.positionAttribLocation,3, DataType.FLOAT, false, 0, 0L);                            
                
                await object_._context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, mBuffers.IndexBuffer);
                
                foreach(var smv in actor.ModelViewShadow)
                {                    
//para establecer las sombras                                                                                                  
                    await object_._context.UniformMatrixAsync(object_.modelViewUniformLocation,false,smv.GetArray());                       
                    await object_._context.DrawElementsAsync(Primitive.TRIANGLES,mBuffers.NumberOfIndices,DataType.UNSIGNED_SHORT, 0);
                }

            }
        }
        await object_._context.EndBatchAsync();
    }
        public async Task getAttributeLocations(WebGLProgram p){    

        object_.positionAttribLocation = await object_._context.GetAttribLocationAsync(p,"aVertexPosition");
        object_.normalAttribLocation = await object_._context.GetAttribLocationAsync(p,"aVertexNormal");
        object_.colorAttribLocation = await object_._context.GetAttribLocationAsync(p,"aVertexColor");

        object_.projectionUniformLocation=await object_._context.GetUniformLocationAsync(p,"uProjectionMatrix");
        object_.modelViewUniformLocation = await object_._context.GetUniformLocationAsync(p,"uModelViewMatrix");
        object_.normalTransformUniformLocation = await object_._context.GetUniformLocationAsync(p,"uNormalTransformMatrix");
        object_.baseColorLocation=await object_._context.GetUniformLocationAsync(p,"uBaseColor");
        object_.ambientLightLocation=await object_._context.GetUniformLocationAsync(p,"uAmbientLight");
        object_.dirLightDiffuseLocation[0]=await object_._context.GetUniformLocationAsync(p,"uDirLight0Diffuse");
        object_.dirLightDiffuseLocation[1]=await object_._context.GetUniformLocationAsync(p,"uDirLight1Diffuse");
        object_.dirLightDirectionLocation[0]=await object_._context.GetUniformLocationAsync(p,"uDirLight0Direction");
        object_.dirLightDirectionLocation[1]=await object_._context.GetUniformLocationAsync(p,"uDirLight1Direction");
        shadowColor=await object_._context.GetUniformLocationAsync(p,"uShadowColor"); //variable uniform en programShadow


    }
}

  