// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Creates a new GMesh instance with the contents of the input meshes.
		/// Vertices that are close-by (default: 1mm) will be merged. 
		/// </summary>
		/// <param name="inputMeshes">one or more GMesh instances to combine</param>
		/// <param name="disposeInputMeshes">If true, Dispose() is called on each input mesh before returning to caller.</param>
		public static GMesh Combine(IList<GMesh> inputMeshes, bool disposeInputMeshes = false)
		{
			if (inputMeshes == null)
				throw new ArgumentNullException(nameof(inputMeshes));

			var meshCount = inputMeshes.Count;
			if (meshCount == 0)
				throw new InvalidOperationException("nothing to combine - input meshes is empty");

			if (meshCount == 1)
				return inputMeshes.First();

			var totalFaceCount = 0;
			var totalLoopCount = 0;
			for (var i = 0; i < meshCount; i++)
			{
				totalFaceCount += inputMeshes[i].FaceCount;
				totalLoopCount += inputMeshes[i].LoopCount;
			}

			if (totalFaceCount == 0)
				throw new InvalidOperationException("input meshes do not have a single face");

			var gMesh = CombineInternalJobified(inputMeshes, totalFaceCount, totalLoopCount);

			if (disposeInputMeshes)
				DisposeAll(inputMeshes);
			
			return gMesh;
		}

		/*
		private static GMesh CombineInternal(IList<GMesh> inputMeshes, int totalFaceCount, int totalLoopCount)
		{
			var allFaceData = new NativeArray<CombineFaceData>(totalLoopCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			var allFaceDataIndex = 0;

			// for the duration of the combine operation, all vertices are assumed to be on "grid" positions
			// initial capacity is a "best guess" of the minimum (ie in a triangle strip each face would add just 1 vertex)
			var knownGridPositions = new NativeParallelHashMap<int3, int>(totalFaceCount, Allocator.Temp);
			
			var combinedMesh = new GMesh();

			// get all vertices from all faces, use vertex GridPosition to merge close ones
			var meshCount = inputMeshes.Count;
			for (var meshIndex = 0; meshIndex < meshCount; meshIndex++)
			{
				var inputMesh = inputMeshes[meshIndex];
				var meshFaceCount = inputMesh.Faces.Length;
				for (var faceIndex = 0; faceIndex < meshFaceCount; faceIndex++)
				{
					var inputFace = inputMesh.Faces[faceIndex];
					if (inputFace.IsValid == false)
						continue;

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
							var vIndex = combinedMesh.CreateVertex(v.Position);
							knownGridPositions.Add(gridPos, vIndex);
						}

						allFaceData[allFaceDataIndex++] = new CombineFaceData
							{ FaceIndex = inputFace.Index, FaceElementCount = inputFace.ElementCount, VertexGridPos = gridPos };
					});
				}
			}

			var currentFaceIndex = 0;
			var faceDataCount = allFaceData.Length;
			var vertexIndices = new NativeArray<int>(allFaceData[0].FaceElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			var faceVertexIndex = 0;
			for (var dataIndex = 0; dataIndex < faceDataCount; dataIndex++)
			{
				var data = allFaceData[dataIndex];
				if (data.FaceIndex != currentFaceIndex)
				{
					// now we can create the new face
					combinedMesh.CreateFace(vertexIndices);

					// reset face elements
					currentFaceIndex = data.FaceIndex;
					faceVertexIndex = 0;

					if (vertexIndices.Length != data.FaceElementCount)
					{
						vertexIndices.Dispose();
						vertexIndices = new NativeArray<int>(data.FaceElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					}
				}

				// get the face's vertex indices based on their grid positions
				vertexIndices[faceVertexIndex] = knownGridPositions[data.VertexGridPos];
				faceVertexIndex++;
			}
			
			// create the last face
			combinedMesh.CreateFace(vertexIndices);

			vertexIndices.Dispose();
			knownGridPositions.Dispose();
			allFaceData.Dispose();

			return combinedMesh;
		}*/

		private static GMesh CombineInternalJobified(IList<GMesh> inputMeshes, int totalFaceCount, int totalLoopCount)
		{
			// TODO:
			// JOB1: for each mesh in parallel:
			//		generate CombineFaceData (face + elementCount + vertex pos & gridPos)
			//	1b	generate hashmap of known grid positions
			// JOB2: combine lists and known grid positions, adjust indices
			// JOB3: create unique vertices in parallel
			//		update vert indices in CombineFaceData
			// JOB4: create faces using gridPos and vert indices
			
			
			var allFaceData = new NativeArray<CombineFaceData>(totalLoopCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			var allFaceDataIndex = 0;

			// for the duration of the combine operation, all vertices are assumed to be on "grid" positions
			// initial capacity is a "best guess" of the minimum (ie in a triangle strip each face would add just 1 vertex)
			var knownGridPositions = new NativeParallelHashMap<int3, int>(totalFaceCount, Allocator.Temp);
			
			var combinedMesh = new GMesh();

			// get all vertices from all faces, use vertex GridPosition to merge close ones
			var meshCount = inputMeshes.Count;
			for (var meshIndex = 0; meshIndex < meshCount; meshIndex++)
			{
				var inputMesh = inputMeshes[meshIndex];
				var meshFaceCount = inputMesh.Faces.Length;
				for (var faceIndex = 0; faceIndex < meshFaceCount; faceIndex++)
				{
					var inputFace = inputMesh.Faces[faceIndex];
					if (inputFace.IsValid == false)
						continue;

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
							var vIndex = combinedMesh.CreateVertex(v.Position);
							knownGridPositions.Add(gridPos, vIndex);
						}

						allFaceData[allFaceDataIndex++] = new CombineFaceData
							{ FaceIndex = inputFace.Index, FaceElementCount = inputFace.ElementCount, VertexGridPos = gridPos };
					});
				}
			}

			var currentFaceIndex = 0;
			var faceDataCount = allFaceData.Length;
			var vertexIndices = new NativeArray<int>(allFaceData[0].FaceElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			var faceVertexIndex = 0;
			for (var dataIndex = 0; dataIndex < faceDataCount; dataIndex++)
			{
				var data = allFaceData[dataIndex];
				if (data.FaceIndex != currentFaceIndex)
				{
					// now we can create the new face
					combinedMesh.CreateFace(vertexIndices);

					// reset face elements
					currentFaceIndex = data.FaceIndex;
					faceVertexIndex = 0;

					if (vertexIndices.Length != data.FaceElementCount)
					{
						vertexIndices.Dispose();
						vertexIndices = new NativeArray<int>(data.FaceElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					}
				}

				// get the face's vertex indices based on their grid positions
				vertexIndices[faceVertexIndex] = knownGridPositions[data.VertexGridPos];
				faceVertexIndex++;
			}
			
			// create the last face
			combinedMesh.CreateFace(vertexIndices);

			vertexIndices.Dispose();
			knownGridPositions.Dispose();
			allFaceData.Dispose();

			return combinedMesh;
		}

		
		private struct CombineMeshCollectDataJob : IJob
		{
			public void Execute()
			{
				
			}
		}

		/// <summary>
		/// Calls Dispose() on all non-null meshes in the collection that have not been disposed yet.
		/// </summary>
		/// <param name="meshes"></param>
		private static void DisposeAll(IEnumerable<GMesh> meshes)
		{
			if (meshes != null)
			{
				foreach (var mesh in meshes)
				{
					if (mesh != null && mesh.IsDisposed == false)
						mesh.Dispose();
				}
			}
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

		private struct CombineFaceData
		{
			public int FaceIndex;
			public int FaceElementCount;
			public int3 VertexGridPos;
		}
	}
}