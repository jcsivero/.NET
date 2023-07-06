using SimpleGame.Pages;
using Blazor.Extensions.Canvas.WebGL;
using System.Threading.Tasks;

public  class CommandDrawProgram : Command
{
    private Game object_;
    public CommandDrawProgram(Game value)
    {
        object_ = value;
    }

    
    public override async Task  Exec()
    {
        await object_._context.ClearColorAsync(0, 0, 1, 1);
        await object_._context.ClearDepthAsync(1.0f);
        await object_._context.DepthFuncAsync(CompareFunction.LEQUAL);
        await object_._context.EnableAsync(EnableCap.DEPTH_TEST);        
        await object_._context.ViewportAsync(0,0,object_._context.DrawingBufferWidth,object_._context.DrawingBufferHeight);
         
        await object_.getAttributeLocations(object_.program);
        await object_._context.UseProgramAsync(object_.program);
        await object_._context.UniformMatrixAsync(object_.projectionUniformLocation,false,object_.ProyMat.GetArray());
        await object_._context.UniformAsync(object_.ambientLightLocation,object_.ActiveLevel.AmbientLight.GetArray());

        await object_._context.BeginBatchAsync();
        await object_._context.ClearAsync(BufferBits.COLOR_BUFFER_BIT | BufferBits.DEPTH_BUFFER_BIT);
        
         // Loop on lights for bindning uniforms
        // Note that this is assuming this lights cam be dynamic
        int counterProcessedLights=0;
        foreach(var keyval in object_.ActiveLevel.ActorCollection){
            if(counterProcessedLights==object_.NumberOfDirectionalLights)
                break;
            SimpleGame.GameFramework.Actor actor = keyval.Value;
            if(!actor.Enabled)
                continue;
            if(actor.Type==SimpleGame.GameFramework.ActorType.Light){
                await object_._context.UniformAsync(object_.dirLightDirectionLocation[counterProcessedLights],actor.Direction.GetArray());
                await object_._context.UniformAsync(object_.dirLightDiffuseLocation[counterProcessedLights],actor.BaseColor.GetArray());
                counterProcessedLights += 1;


            }

        }

        // Loop on objects
        foreach( var keyval in object_.ActiveLevel.ActorCollection){
            SimpleGame.GameFramework.Actor actor = keyval.Value;
            if(!actor.Enabled)
                continue;

            if(actor.Type==SimpleGame.GameFramework.ActorType.StaticMesh)
            {
            MeshBuffers mBuffers = object_.BufferCollection[actor.StaticMeshId]; 

            // Update uniforms
            await object_._context.UniformAsync(object_.baseColorLocation,actor.BaseColor.GetArray());
            await object_._context.UniformMatrixAsync(object_.modelViewUniformLocation,false,actor.ModelView.GetArray());
            await object_._context.UniformMatrixAsync(object_.normalTransformUniformLocation,false,actor.NormalTransform.GetArray());

            // Buffers to attributes
            await object_._context.BindBufferAsync(BufferType.ARRAY_BUFFER, mBuffers.VertexBuffer);
            await object_._context.EnableVertexAttribArrayAsync((uint)object_.positionAttribLocation);
            await object_._context.VertexAttribPointerAsync((uint)object_.positionAttribLocation,3, DataType.FLOAT, false, 0, 0L);
        
            await object_._context.BindBufferAsync(BufferType.ARRAY_BUFFER, mBuffers.NormalBuffer);
            await object_._context.EnableVertexAttribArrayAsync((uint)object_.normalAttribLocation);
            await object_._context.VertexAttribPointerAsync((uint)object_.normalAttribLocation,3, DataType.FLOAT, false, 0, 0L);


            await object_._context.BindBufferAsync(BufferType.ARRAY_BUFFER, mBuffers.ColorBuffer);
            await object_._context.EnableVertexAttribArrayAsync((uint)object_.colorAttribLocation);
            await object_._context.VertexAttribPointerAsync((uint)object_.colorAttribLocation,4, DataType.FLOAT, false, 0, 0L);
        
            await object_._context.BindBufferAsync(BufferType.ELEMENT_ARRAY_BUFFER, mBuffers.IndexBuffer);

            await object_._context.DrawElementsAsync(Primitive.TRIANGLES,mBuffers.NumberOfIndices,DataType.UNSIGNED_SHORT, 0);

            }
        }
    }   
}

  