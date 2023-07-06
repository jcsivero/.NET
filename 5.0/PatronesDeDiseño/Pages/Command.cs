using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Microsoft.JSInterop; //Interop for game loop rendering through Javascript
using System.Net.Http;
using System.Net.Http.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazor.Extensions;
using Blazor.Extensions.Canvas;
using Blazor.Extensions.Canvas.WebGL;
public abstract class Command 
{

    public abstract  Task  Exec();
    //public abstract void SetFragmentShader(WebGLShader shader);
}