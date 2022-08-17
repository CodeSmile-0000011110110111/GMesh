# GMesh
GMesh enables creating reliable geometry editing tools. It is a graph-based Mesh data structure (kind of like a relational database) for geometry editing via Euler operators. It is built from the ground up to enable Unity Job System support. 

GMesh is developed to support "Mesh Graph", a node-based procedural geometry generator for Unity.

Note: switch to "develop" branch to see latest work in progress.

Simple code example:
```
var gmesh = new GMesh();
gmesh.CreateFace(vertices);
_meshFilter.sharedMesh = gmesh.ToMesh();
gmesh.Dispose();
```

Where _vertices_ is any IEnumerable collection of 3 or more points (float3 or Vector3).

## History

GMesh is similar to [Blender's BMesh](https://wiki.blender.org/wiki/Source/Modeling/BMesh/Design) and [BMeshUnity](https://github.com/eliemichel/BMeshUnity) (incompatible with Job System). 

## Why not just edit Mesh vertices?

You cannot easily edit a Mesh knowing just about the vertices and triangles since you are lacking information about other topology features: faces, loops and edges, and how they relate to each other.

The key word is "easily". While you can do a lot of modifications to a Unity Mesh, the operations (and the code needed to implement them) are a lot simpler and more reliable if the data structure allows you to work on faces and edges too.

## Note on the Name "GMesh"

"G" stands for "Graph" but could also refer to "Geometry" or "General" because the primary purpose is to provide a reliably editable mesh data structure. In turn it cannot be rendered directly by Unity, a (fast) conversion to Mesh as a final step is required and will be included.

Since the "B" in BMesh stands for Blender I thought it better to use a different prefix, and since I am working on a Node Graph based visual mesh geometry editing tool called "Mesh Graph" and the data structure itself is sort of a static graph (or relational database) it came to be called "GMesh".
