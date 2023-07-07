using System.Threading.Tasks;
using SimpleGame.Pages;
using Blazor.Extensions.Canvas.WebGL;


public abstract class Command 
{
    protected Game object_;
    public WebGLShader vertexShader;
    public WebGLShader fragmentShader; //para shader de fragmentos
    public WebGLProgram program; //shader de fragmentos solo pra pintar las sombras, según el color indicado en el atributo sombras del actor.



    public abstract  Task  Exec();

    public virtual async Task  Initialize()
    {

    }
}