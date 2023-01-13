using System;
using System.Collections;
using System.Collections.Generic;

using SimpleGame;
using SimpleGame.Math;
using SimpleGame.GameFramework;

namespace SimpleGame{

public class Collision{

    public static bool CheckCollisionAABB(Actor actor1, Actor actor2){
        Vector3 v1 = actor1.Transform.GetTranslationVector();
        Vector3 v2 = actor2.Transform.GetTranslationVector();
        List<Vector3> actorBox1=actor1.GetCollisionBox();
        List<Vector3> actorBox2=actor2.GetCollisionBox();
        //Translate boxes
        List<Vector3> box1 = new List<Vector3>();
        List<Vector3> box2 = new List<Vector3>();
        foreach(var v in actorBox1){
            box1.Add(v+v1);
        }
        foreach(var v in actorBox2){
            box2.Add(v+v2); 
        }

        if ((box1[1] > box2[0]) && (box1[0] < box2[1]))
        {
           return true;
        }
        else
            return false;
    }
}


}