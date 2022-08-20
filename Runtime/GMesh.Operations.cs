// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
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

		/// <summary>
		/// Moves (snaps) all vertex positions to an imaginary grid given by gridSize.
		/// For example, if gridSize is 0.01f all vertices are snapped to the nearest 1cm coordinate.
		/// </summary>
		/// <param name="gridSize"></param>
		public void SnapVerticesToGrid(float gridSize)
		{
			for (var i = 0; i < VertexCount; i++)
			{
				var vertex = GetVertex(i);
				if (vertex.IsValid)
				{
					vertex.SnapPosition(gridSize);
					SetVertex(vertex);
				}
			}
		}
	}
}