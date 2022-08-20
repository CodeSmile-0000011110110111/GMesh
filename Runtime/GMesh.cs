// Copyright (C) 2021-2022 Steffen Itterheim
// Usage is bound to the Unity Asset Store Terms of Service and EULA: https://unity3d.com/legal/as_terms

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
{
	/// <summary>
	/// GMesh - editable mesh geometry, Burst & Job System enabled.
	/// GMesh is a graph built of Vertices, Edges, Faces and Loops. This allows for editing the mesh using simple Euler operators.
	/// GMesh has ToMesh and FromMesh methods to convert to and from Unity Mesh instances.
	/// 
	/// There's one thing you need to know about the Jobs compatibility: element references (ie edge => vertex 0 and 1 or face => loops)
	/// do not exist! Instead, they are merely indices to the elements in their respective lists.
	///
	/// All elements (vertex, edge, loop, face) are structs and thus stored and passed by value (unless ref or in keywords are used).
	/// Therefore, if you need to loop up the face of a loop, you call GetFace(loop.FaceIndex) in order to get a COPY of the Face struct
	/// stored at that index. After making modifications to the face, you'll have to call SetFace(face) which uses face.Index internally
	/// to write the modified face back to the mesh graph (specifically: assigning the face back to its position in the faces list).
	///
	/// You are strongly advised to NOT keep local copies of indexes while you or anything else is possibly modifying the mesh graph
	/// (ie inserting, deleting, moving, swapping, replacing elements) as this can invalidate the indices.
	///
	/// Vertices are shared between faces, loops and edges. Whether final Mesh faces should share vertices is a setting in ToMesh().
	/// 
	/// You should also rely exclusively on the Euler operators (and combinations of them) in order to modify the mesh graph.
	/// See: https://en.wikipedia.org/wiki/Euler_operator_(digital_geometry)
	/// The same cautiuous warning exists in the Blender developer documentation, for good reason.
	/// For similar reason the element lists are not publicly exposed, use the Set/Get and Create/Delete element methods instead.
	/// 
	/// Note: Implementation closely follows Blender's BMesh and its C# port UnityBMesh (which is not Job System compatible).
	/// </summary>
	[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
	public sealed partial class GMesh : IDisposable
	{
		/// <summary>
		/// This is used to indicate that the index referencing another element hasn't been set yet.
		/// Used internally to detect graph relation errors.
		/// </summary>
		public const int UnsetIndex = -1;

		/// <summary>
		/// GMesh may work with vertex positions on a reasonably sized grid (default: 1mm) in order to easily detect vertices
		/// which are close enough to be considered identical.
		/// For instance, rounding a position would be done as follows:
		/// var positionOnGrid = math.round(position * InvGridSize) * GridSize;
		/// </summary>
		public const float GridSize = 0.001f; // round all positions to 1mm grid

		/// <summary>
		/// The inverse of the grid, ie upscale factor. See GridSize.
		/// </summary>
		private static readonly double GridUpscale = 1.0 / GridSize; // inverse of grid size (eg 0.001 => 1000)

		/// <summary>
		/// Creates an empty GMesh.
		/// </summary>
		public GMesh() {}

		/// <summary>
		/// Moves (snaps) all vertex positions to an imaginary grid given by gridSize.
		/// For example, if gridSize is 0.01f all vertices are snapped to the nearest 1cm coordinate.
		/// </summary>
		/// <param name="gridSize"></param>
		public void SnapVerticesToGrid(float gridSize)
		{
			for (int i = 0; i < VertexCount; i++)
			{
				var vertex = GetVertex(i);
				if (vertex.IsValid)
				{
					vertex.SnapPosition(gridSize);
					SetVertex(vertex);
				}
			}
		}
		
		/// <summary>
		/// Creates a new GMesh instance with the contents of the input meshes.
		/// Vertices that are close-by (default: 1mm) will be merged. 
		/// </summary>
		/// <param name="inputMeshes">one or more GMesh instances to combine</param>
		/// <param name="disposeInputMeshes">If true, Dispose() is called on each input mesh before returning to caller.</param>
		public static GMesh Combine(IEnumerable<GMesh> inputMeshes, bool disposeInputMeshes = false)
		{
			var totalFaceCount = 0;
			foreach (var mesh in inputMeshes)
				totalFaceCount += mesh.FaceCount;

			var allFaceGridPositions = new int3[totalFaceCount][];
			var allFaceGridPosIndex = 0;

			// for the duration of the combine operation, all vertices are assumed to be on "grid" positions
			// initial capacity is a "best guess" of the minimum (ie in a triangle strip each face would add just 1 vertex)
			var knownGridPositions = new NativeParallelHashMap<int3, int>(totalFaceCount, Allocator.Temp);
			var combinedMesh = new GMesh();

			// get all vertices from all faces, use vertex GridPosition to merge close ones
			foreach (var inputMesh in inputMeshes)
			{
				foreach (var inputFace in inputMesh.Faces)
				{
					if (inputFace.IsValid == false)
						continue;

					var faceGridPos = new int3[inputFace.ElementCount];
					var faceGridPosIndex = 0;

					inputMesh.ForEachLoop(inputFace, loop =>
					{
						// loop vertex is guaranteed to be the origin vertex for this loop
						var v = inputMesh.GetVertex(loop.StartVertexIndex);
						// turn it into a "grid" position => close-by (ie rounding error) vertex positions are considered identical
						var gridPos = v.GridPosition();
						// check if that grid position already has a vertex
						if (knownGridPositions.ContainsKey(gridPos) == false)
						{
							// add the gridPos with its new index in the combined GMesh
							var index = combinedMesh.CreateVertex(v.Position);
							knownGridPositions.Add(gridPos, index);
						}

						faceGridPos[faceGridPosIndex++] = gridPos;
					});

					allFaceGridPositions[allFaceGridPosIndex++] = faceGridPos;
				}
			}

			/*
			Debug.Log("known pos: " + knownGridPositions.Count());
			foreach (var gridPos in knownGridPositions)
				Debug.Log($"[{gridPos.Value}] = {gridPos.Key} => Vertex: {combinedMesh.GetVertex(gridPos.Value)}");
			*/

			foreach (var faceVertices in allFaceGridPositions)
			{
				// get the face's vertex indices based on their grid positions
				var vertexIndices = new int[faceVertices.Length];
				for (var i = 0; i < faceVertices.Length; i++)
					vertexIndices[i] = knownGridPositions[faceVertices[i]];

				// now we can create the new face
				combinedMesh.CreateFace(vertexIndices);
			}

			knownGridPositions.Dispose();

			// dispose, if requested
			if (disposeInputMeshes)
				DisposeAll(inputMeshes);

			return combinedMesh;
		}
	}
}