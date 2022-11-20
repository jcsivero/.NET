using System;
namespace WebGLCube.Math{

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

public static Vector3 operator+(Vector3 v1,Vector3 v2){
    Vector3 v=new Vector3(v1.x,v1.y,v1.z);
   v.Add(v2);
   return v;
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

// Matrix elements: mij is the ith row and jth column
public float m00,m01,m02,m03,m10,m11,m12,m13,m20,m21,m22,m23,m30,m31,m32,m33;
// Backup for matrix elements, see store and load methods
private float _m00,_m01,_m02,_m03,_m10,_m11,_m12,_m13,_m20,_m21,_m22,_m23,_m30,_m31,_m32,_m33;

// Array used as origin for possible copying operations. See GetArray method
// Matrix elements are stored in column major order.
private float[] matArray;


// Default constructor gives the identity matrix
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

public AffineMat4(AffineMat4 Source){

    // Copy Constructor
    matArray=new float[16];
    Copy(Source);    
}

public void Copy(AffineMat4 mat){

    m00=mat.m00; m01=mat.m01; m02=mat.m02; m03=mat.m03;
    m10=mat.m10; m11=mat.m11; m12=mat.m12; m13=mat.m13;
    m20=mat.m20; m21=mat.m21; m22=mat.m22; m23=mat.m23;
    m30=mat.m30; m31=mat.m31; m32=mat.m32; m33=mat.m33;



}

public override string ToString(){
    var str_version = $"{m00} {m01} {m02} {m03}\n{m10} {m11} {m12} {m13}\n{m20} {m21} {m22} {m23}\n{m30} {m31} {m32} {m33}\n";
    return str_version;
}


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

public void Translate(Vector3 vector){

    m03 += vector.x;
    m13 += vector.y;
    m23 += vector.z;
}


public void AffineInverse(){
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

// Camera operations

// Perspective obtaines the projection 4x4 matrix for perspective
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

// LookAt gives the World Matrix Transformation to rotate making the minus z axis
// points towards a target.
public void  TargetTo(Vector3 eye, Vector3 target, Vector3 up){
    // Using GL-Matrix
    double zx=eye.x-target.x;
    double zy=eye.y-target.y;
    double zz=eye.z-target.z;
    double znorm=zx*zx+zy*zy+zz*zz;
    double inv=1.0;
    if(znorm>0.0){
        inv=1.0/System.Math.Sqrt(znorm);
        zx *= inv;
        zy *= inv;
        zz *= inv;
    }
    else{
        zx=0.0; zy=0.0; zz=0.0;
    }
    double upx=up.x;
    double upy=up.y;
    double upz=up.z;
    double  unorm=upx*upx+upy*upy+upz*upz;
    if(unorm>0.0){
        inv=1.0/System.Math.Sqrt(unorm);
        upx *= inv;
        upy *= inv;
        upz *= inv;


    }
    else{
        upx=0.0f; upy=0.0; upz=0.0;
    }
    double xx=upy*zz-upz*zy;
    double xy=upz*zx-upx*zz;
    double xz=upx*zy-upy*zx;

    double yx=xz*zy-xy*zz;
    double yy=xx*zz-xz*zx;
    double yz=xy*zx-xx*zy;

    m00=(float)xx;
    m10=(float)xy;
    m20=(float)xz;
    m30=0.0f;
    m01=(float)yx;
    m11=(float)yy;
    m21=(float)yz;
    m31=0.0f;
    m02=(float)zx;
    m12=(float)zy;
    m22=(float)zz;
    m32=0.0f;
    m03=eye.x;
    m13=eye.y;
    m23=eye.z;
    m33=1.0f;


    
}
public void  LookAt(Vector3 eye, Vector3 target, Vector3 up){
    // Using GL-Matrix
    double zx=eye.x-target.x;
    double zy=eye.y-target.y;
    double zz=eye.z-target.z;
    double znorm=zx*zx+zy*zy+zz*zz;
    double inv=1.0;
    if(znorm>0.0){
        inv=1.0/System.Math.Sqrt(znorm);
        zx *= inv;
        zy *= inv;
        zz *= inv;
    }
    else{
        zx=0.0; zy=0.0; zz=0.0;
    }
    double xx=up.y*zz-up.z*zy;
    double xy=up.z*zx-up.x*zz;
    double xz=up.x*zy-up.y*zx;


    double  xnorm=xx*xx+xy*xy+xz*xz;
    if(xnorm>0.0){
        inv=1.0/System.Math.Sqrt(xnorm);
        xx *= inv;
        xy *= inv;
        xz *= inv;


    }
    else{
        xx=0.0f; xy=0.0; xz=0.0;
    }
    double yx=xz*zy-xy*zz;
    double yy=xx*zz-xz*zx;
    double yz=xy*zx-xx*zy;

    m00=(float)xx;
    m01=(float)xy;
    m02=(float)xz;
    m30=0.0f;
    m10=(float)yx;
    m11=(float)yy;
    m12=(float)yz;
    m31=0.0f;
    m20=(float)zx;
    m21=(float)zy;
    m22=(float)zz;
    m32=0.0f;
    m03=-(float)(xx*eye.x+xy*eye.y+xz*eye.z);
    m13=-(float)(yx*eye.x+yy*eye.y+yz*eye.z);
    m23=-(float)(zx*eye.x+zy*eye.y+zz*eye.z);
    m33=1.0f;


    
}


// General Operations for a 4x4 Matrix
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


public void GeneralInverse(){
// Extracted from gl matrix
 float b00 = m00*m11-m10*m01;
  float b01=m00*m21-m20*m01;
  float b02=m00*m31 - m30*m01;
  float b03=m10*m21-m20*m11;
  float b04=m10*m31-m30*m11;
  float b05=m20*m31-m30*m21;
  float b06=m02*m13-m12*m03;
  float b07=m02*m23-m22*m03;
  float b08=m02*m33-m32*m03;
  float b09=m12*m23-m22*m13;
  float b10=m12*m33-m32*m13;
  float b11=m22*m33-m32*m23;

  float det =  b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;

  float tol=1e-8f;
  if (System.Math.Abs(det)<tol) {
    return;
  }
  det = 1.0f / det;

  store();
  m00 = (_m11*b11-_m21*b10+_m31*b09)*det;
  m10 = (_m20*b10-_m10*b11-_m30*b09)*det;
  m20 = (_m13*b05-_m23*b04+_m33*b03)*det;
  m30 = (_m22*b04-_m12*b05-_m32*b03)*det;
  m01 = (_m21*b08-_m01*b11 -_m31*b07)*det;
  m11 = (_m00*b11-_m20*b08+_m30*b07)*det;
  m21 = (_m23*b02-_m03*b05-_m33*b01)*det;
  m31 = (_m02*b05-_m22*b02+_m32*b01)*det;
  m02 = (_m01*b10-_m11*b08+_m31*b06)*det;
  m12 = (_m10*b08-_m00*b10-_m30*b06)*det;
  m22 = (_m03*b04-_m13*b02+_m33*b00)*det;
  m32 = (_m12*b02-_m02*b04 - _m32*b00)*det;
  m03 = (_m11*b07-_m01*b09-_m21*b06)*det;
  m13 = (_m00*b09-_m10*b07+_m20*b06)*det;
  m23 = (_m13*b01-_m03*b03-_m23*b00)*det;
  m33 = (_m02*b03-_m12*b01+_m22*b00)*det;

}


public void LeftMProduct(AffineMat4 Moperand){
    _m00 = Moperand.m00*m00+Moperand.m01*m10+Moperand.m02*m20+Moperand.m03*m30;
    _m01 = Moperand.m00*m01+Moperand.m01*m11+Moperand.m02*m21+Moperand.m03*m31;
    _m02 = Moperand.m00*m02+Moperand.m01*m12+Moperand.m02*m22+Moperand.m03*m32;
    _m03 = Moperand.m00*m03+Moperand.m01*m13+Moperand.m02*m23+Moperand.m03*m33;
    _m10 = Moperand.m10*m00+Moperand.m11*m10+Moperand.m12*m20+Moperand.m13*m30;
    _m11 = Moperand.m10*m01+Moperand.m11*m11+Moperand.m12*m21+Moperand.m13*m31;
    _m12 = Moperand.m10*m02+Moperand.m11*m12+Moperand.m12*m22+Moperand.m13*m32;
    _m13 = Moperand.m10*m03+Moperand.m11*m13+Moperand.m12*m23+Moperand.m13*m33;
    _m20 = Moperand.m20*m00+Moperand.m21*m10+Moperand.m22*m20+Moperand.m23*m30;
    _m21 = Moperand.m20*m01+Moperand.m21*m11+Moperand.m22*m21+Moperand.m23*m31;
    _m22 = Moperand.m20*m02+Moperand.m21*m12+Moperand.m22*m22+Moperand.m23*m32;
    _m23 = Moperand.m20*m03+Moperand.m21*m13+Moperand.m22*m23+Moperand.m23*m33;
    _m30 = Moperand.m30*m00+Moperand.m31*m10+Moperand.m32*m20+Moperand.m33*m30;
    _m31 = Moperand.m30*m01+Moperand.m31*m11+Moperand.m32*m21+Moperand.m33*m31;
    _m32 = Moperand.m30*m02+Moperand.m31*m12+Moperand.m32*m22+Moperand.m33*m32;
    _m33 = Moperand.m30*m03+Moperand.m31*m13+Moperand.m32*m23+Moperand.m33*m33;
    this.load();
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
