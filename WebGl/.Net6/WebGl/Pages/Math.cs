using System;

namespace WebGl.Pages.Math{

public class Vector3{
    public float x;
    public float y;
    public float z;

public Vector3(float ux, float uy, float uz){
    x=ux;
    y=uy;
    z=uz;
}

public void  Add(Vector3 v){
    x += v.x;
    y += v.y;
    z += v.z;
}

public void Normalize(){
    double norma= System.Math.Sqrt(x*x+y*y+z*z);
    float fnorma=(float)norma;
    x=x/fnorma;
    y=y/fnorma;
    z=z/fnorma;
}
}

public class AffineMat4 {

public AffineMat4(){
m00=1.0f;
m01=0.0f;
m02=0.0f;
m03=0.0f;
m10=0.0f;
m11=1.0f;
m12=0.0f;
m13=0.0f;
m20=0.0f;
m21=0.0f;
m22=1.0f;
m23=0.0f;
m30=0.0f;
m31=0.0f;
m32=0.0f;
m33=1.0f;

matArray = new float[16];

}
public float m00,m01,m02,m03,m10,m11,m12,m13,m20,m21,m22,m23,m30,m31,m32,m33;
private float _m00,_m01,_m02,_m03,_m10,_m11,_m12,_m13,_m20,_m21,_m22,_m23,_m30,_m31,_m32,_m33;

public override string ToString(){
    var str_version = $"{m00} {m01} {m02} {m03}\n{m10} {m11} {m12} {m13}\n{m20} {m21} {m22} {m23}\n{m30} {m31} {m32} {m33}\n";
    return str_version;
}

private float[] matArray;

public float[] GetArray(){
    matArray[0]=m00;
    matArray[1]=m10;
    matArray[2]=m20;
    matArray[3]=m30;
    matArray[4]=m01;
    matArray[5]=m11;
    matArray[6]=m21;
    matArray[7]=m31;
    matArray[8]=m02;
    matArray[9]=m12;
    matArray[10]=m22;
    matArray[11]=m32;
    matArray[12]=m03;
    matArray[13]=m13;
    matArray[14]=m23;
    matArray[15]=m33;

    return matArray;


}
public void Transpose(){
_m01=m01;
_m02=m02;
_m03=m03;
_m12=m12;
_m13=m13;
_m23=m23;
m01=m10;
m02=m20;
m03=m30;
m10=_m01;
m12=m21;
m13=m31;
m20=_m02;
m21=_m12;
m23=m32;
m30=_m03;
m31=_m13;
m32=_m23;
}


public void Translate(Vector3 vector){

    m03 += vector.x;
    m13 += vector.y;
    m23 += vector.z;
}


public void Inverse(){
   store();
   m01=_m10;
   m02=_m20;
   m10=_m01;
   m12=_m21;
   m20=_m02;
   m21=_m12;
   m03=m00*_m03+m01*_m13+m02*_m23;
   m13=m10*_m03+m11*_m13+m12*_m23;
   m23=m20*_m03+m21*_m13+m22*_m23;

}

public void Perspective(float FOV,float r, float near, float far){

 float f = 1.0f / (float)System.Math.Tan(FOV/2.0f);
 
 m00= f/r;
 m10=0.0f;
 m20=0.0f;
 m30=0.0f;
 m01=0.0f;
 m11=f;
 m21=0.0f;
 m31=0.0f;
 m02=0.0f;
 m12=0.0f;
 float nf = 1.0f/(near-far);
 m22=(far+near)*nf;
 m32=-1.0f;
 m03=0.0f;
 m13=0.0f;
 m23=2*far*near*nf;
 m33=0.0f;


}
public void Rotation(float angle,Vector3 vector){
float rangle=angle*(float)System.Math.PI/180.0f;
float c =(float)System.Math.Cos(angle);
float s =(float)System.Math.Sin(angle);
float ic = 1-c;
float ux=vector.x;
float uy=vector.y;
float uz=vector.z;
m00=c+ux*ux*ic;
m01=ux*uy*ic-uz*s;
m02=ux*uz*ic+uy*s;
m10=uy*ux*ic+uz*s;
m11=c+uy*uy*ic;
m12=uy*uz*ic-ux*s;
m20=uz*ux*ic-uy*s;
m21=uz*uy*ic+ux*s;
m22=c+uz*uz*ic;


}

public void Scale(float s){
    m00 *= s;
    m11 *= s;
    m22 *= s;
    m33 *= s;
}

public void Translation(Vector3 vector){
    m03=vector.x;
    m13=vector.y;
    m23=vector.z;
}

private void store(){
_m00=m00;
_m10=m10;
_m20=m20;
_m30=m30;
_m01=m01;
_m11=m11;
_m21=m21;
_m31=m31;
_m02=m02;
_m12=m12;
_m22=m22;
_m32=m32;
_m03=m03;
_m13=m13;
_m23=m23;
_m33=m33;


}
private void load(){
m00=_m00;
m10=_m10;
m20=_m20;
m30=_m30;
m01=_m01;
m11=_m11;
m21=_m21;
m31=_m31;
m02=_m02;
m12=_m12;
m22=_m22;
m32=_m32;
m03=_m03;
m13=_m13;
m23=_m23;
m33=_m33;


}
}
}

