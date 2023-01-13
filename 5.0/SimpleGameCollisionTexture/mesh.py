import bpy
import os

dir_path=os.path.dirname(os.path.dirname(os.path.realpath(__file__)))
#new_path=os.path.dirname(os.path.join(dir_path,'..\..'))

new_path=os.path.join(dir_path,'mesh.json')

print(new_path)

F=open(new_path,"w")

sobjects=bpy.context.selected_objects;

object=sobjects[0];

object_data=object.data;

vertices=object_data.vertices;
F.write('{\n');
F.write('"nvertices" : ');
F.write('"%s",\n' % (len(vertices)));

F.write('"vertices" : [\n');
for vert in vertices[:-1]:
    coord=vert.co;
    F.write('"%f","%f","%f",\n' % (coord[0],coord[2],-coord[1]))
coord=vertices[-1].co;
F.write('"%f","%f","%f"\n' % (coord[0],coord[2],-coord[1]))
F.write('],\n');

F.write('"normals" : [\n');
for vert in vertices[:-1]:
    normal=vert.normal;
    F.write('"%f","%f","%f",\n' % (normal[0],normal[2],-normal[1]))
normal=vertices[-1].normal;
F.write('"%f","%f","%f"\n' % (normal[0],normal[2],-normal[1]))
F.write('],\n');

F.write('"colors" : [\n');
for vert in vertices[:-1]:
    
    F.write('"%f","%f","%f","%f",\n' % (1.0,0.0,0.0,1.0));
F.write('"%f","%f","%f","%f"\n' % (1.0,0.0,0.0,1.0));
F.write('],\n');


polygons=object_data.polygons;

nindices=len(polygons)*3;
F.write('"nindices" : ');
F.write('"%d",\n' % nindices);




F.write('"indices" : [\n');
for polygon in polygons[:-1]:
    verts=polygon.vertices;
    verts=[verts[0],verts[2],verts[1]];
    for vert in verts:
        F.write('"%d",\n' %(vert));
verts=polygons[-1].vertices
verts=[verts[0],verts[2],verts[1]];
for vert in verts[:-1]:
    F.write('"%d",\n' %(vert));
F.write('"%d"\n' % (verts[-1]))
F.write('],\n');

uvs=[(0.0,0.0)]*len(vertices);
F.write('"uv" : [\n');
for polygon in polygons:
    loops=polygon.loop_indices;
    
    for loopindex in loop_indices:
        loop=object_data.loops[loopindex];
        uvlopp=object_data.uv_layers.active.data[loopindex];
        uvs[loop.vertex_index]=uvloop.uv;
        
for uv in uvs[:-1]:
    F.write('"%d",\n' %(uv[0]));
    F.write('"%d",\n' %(uv[1]));
uv=uvs[-1];
F.write('"%d",\n' %(uv[0]));
F.write('"%d"\n' %(uv[0]));
F.write('"%d"\n' % (verts[-1]))
F.write(']\n');


F.write('}\n');
F.close();
print("Finalizada la escritura de los datos");