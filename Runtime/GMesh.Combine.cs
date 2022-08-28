// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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

		private static GMesh CombineInternalJobified(IList<GMesh> inputMeshes, int totalFaceCount, int totalLoopCount)
		{
			// ===============================================================================================================================
			// GATHER FACE + VERTEX DATA FROM INPUT MESHES
			// ===============================================================================================================================
			var meshCount = inputMeshes.Count;
			var gatherData = new NativeArray<JCombine.MergeData>[meshCount];
			var gatherHandles = new NativeArray<JobHandle>(meshCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

			for (var meshIndex = 0; meshIndex < meshCount; meshIndex++)
			{
				var inputMesh = inputMeshes[meshIndex];
				var data = new NativeArray<JCombine.MergeData>(inputMesh.LoopCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
				var job = new JCombine.GatherDataJob
				{
					CombineData = data, MeshIndex = meshIndex, Faces = inputMesh.Faces, Loops = inputMesh.Loops, Vertices = inputMesh.Vertices,
				};
				gatherData[meshIndex] = data;
				gatherHandles[meshIndex] = job.Schedule(inputMesh.LoopCount, 1);
			}

			var combinedGatherHandle = JobHandle.CombineDependencies(gatherHandles);
			gatherHandles.Dispose();

			// ===============================================================================================================================
			// COPY GATHERED DATA INTO SINGLE ARRAY
			// ===============================================================================================================================
			// TODO: copy in a job? it's only 1/80th of total time though in my test case
			// combine all mesh data into one array
			var combinedData = new NativeArray<JCombine.MergeData>(totalLoopCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var dstIndex = 0;

			combinedGatherHandle.Complete();

			for (var meshIndex = 0; meshIndex < meshCount; meshIndex++)
			{
				var meshGatherData = gatherData[meshIndex];
				var length = meshGatherData.Length;
				NativeArray<JCombine.MergeData>.Copy(meshGatherData, 0, combinedData, dstIndex, length);
				dstIndex += length;
				meshGatherData.Dispose();
			}
			gatherData = null;

			// ===============================================================================================================================
			// COLLATE VERTICES TO GRID, CREATE UNIQUE VERTICES
			// ===============================================================================================================================
			// for the duration of the combine operation, all vertices are assumed to be on "grid" positions
			// initial capacity is a "best guess" of the minimum expected elements
			var knownGridPositions = new NativeParallelHashMap<uint, int>(totalFaceCount / 4, Allocator.TempJob);
			var combinedVertexIndices = new NativeArray<int>(totalLoopCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var combinedMesh = new GMesh();

			var createVertsJob = new JCombine.CreateVerticesJob
			{
				Data = combinedMesh._data, CombinedData = combinedData, CombinedVertexIndices = combinedVertexIndices,
				KnownGridPositions = knownGridPositions, TotalFaceCount = totalFaceCount, TotalLoopCount = totalLoopCount,
			};

			var createVertsHandle = createVertsJob.Schedule();
			knownGridPositions.Dispose(createVertsHandle);
			createVertsHandle.Complete();

			// ===============================================================================================================================
			// CREATE COMBINED MESH FACES
			// ===============================================================================================================================

			var createFacesJob = new JCombine.CreateFacesJob
				{ Data = combinedMesh._data, CombinedData = combinedData, CombinedVertexIndices = combinedVertexIndices };
			var createFacesHandle = createFacesJob.Schedule();
			combinedData.Dispose(createFacesHandle);
			combinedVertexIndices.Dispose(createFacesHandle);
			createFacesHandle.Complete();

			return combinedMesh;
		}

		[BurstCompile]
		private static class JCombine
		{
			[BurstCompile]
			public struct CreateFacesJob : IJob
			{
				public NativeArray<MergeData> CombinedData;
				public NativeArray<int> CombinedVertexIndices;

				public GraphData Data;

				public void Execute()
				{
					// TODO: maybe IJob(Parallel)For ? Takes 80% of the time ...

					var combinedDataLength = CombinedData.Length;
					var currentElementCount = CombinedData[0].FaceElementCount;
					var vertexIndices = new NativeArray<int>(currentElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					var edgeIndices = new NativeArray<int>(currentElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					var vertexIndex = 0;
					var currentMeshIndex = 0;
					var currentFaceIndex = 0;

					for (var i = 0; i < combinedDataLength; i++)
					{
						var faceData = CombinedData[i];
						if (Hint.Unlikely(faceData.FaceIndex != currentFaceIndex || faceData.MeshIndex != currentMeshIndex))
						{
							// create the new face with collected vertices
							Create.Edges(Data, vertexIndices, ref edgeIndices);
							Create.Loops(Data, Create.Face(Data, currentElementCount), vertexIndices, edgeIndices);

							// reset face elements
							currentMeshIndex = faceData.MeshIndex;
							currentFaceIndex = faceData.FaceIndex;
							vertexIndex = 0;

							if (Hint.Unlikely(currentElementCount != faceData.FaceElementCount))
							{
								// TODO: is it faster to use NativeList and Resize??
								vertexIndices.Dispose();
								edgeIndices.Dispose();

								currentElementCount = faceData.FaceElementCount;
								vertexIndices = new NativeArray<int>(currentElementCount, Allocator.Temp,
									NativeArrayOptions.UninitializedMemory);
								edgeIndices = new NativeArray<int>(currentElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
							}
						}

						// get the face's vertex indices based on their grid positions
						vertexIndices[vertexIndex] = CombinedVertexIndices[i];
						vertexIndex++;
					}

					// create the last face
					Create.Edges(Data, vertexIndices, ref edgeIndices);
					Create.Loops(Data, Create.Face(Data, currentElementCount), vertexIndices, edgeIndices);

					vertexIndices.Dispose();
					edgeIndices.Dispose();
				}
			}

			[BurstCompile]
			public struct CreateVerticesJob : IJob
			{
				public NativeArray<MergeData> CombinedData;
				public NativeParallelHashMap<uint, int> KnownGridPositions;
				public NativeArray<int> CombinedVertexIndices;

				public int TotalFaceCount;
				public int TotalLoopCount;

				public GraphData Data;

				public void Execute()
				{
					Data.InitializeVerticesWithSize(KnownGridPositions.Capacity);
					Data.InitializeEdgesWithSize(TotalFaceCount);
					Data.InitializeLoopsWithSize(TotalLoopCount);
					Data.InitializeFacesWithSize(TotalFaceCount);

					var combinedDataLength = CombinedData.Length;
					for (var i = 0; i < combinedDataLength; i++)
					{
						var data = CombinedData[i];

						// check if there is already a vertex at the grid position
						if (Hint.Unlikely(KnownGridPositions.ContainsKey(data.VertexGridPosHash)))
							CombinedVertexIndices[i] = KnownGridPositions[data.VertexGridPosHash];
						else
						{
							// add the gridPos with its new index in the combined GMesh
							var vIndex = Create.Vertex(Data, data.VertexPosition);
							KnownGridPositions.Add(data.VertexGridPosHash, vIndex);
							CombinedVertexIndices[i] = vIndex;
						}
					}
				}
			}

			[BurstCompile]
			public struct GatherDataJob : IJobParallelFor
			{
				[WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<MergeData> CombineData;

				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Face>.ReadOnly Faces;
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Loop>.ReadOnly Loops;
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Vertex>.ReadOnly Vertices;

				public int MeshIndex;

				public void Execute(int loopIndex)
				{
					var loop = Loops[loopIndex];
					var faceIndex = loop.FaceIndex;
					var face = Faces[faceIndex];

					if (Hint.Unlikely(face.FirstLoopIndex == loopIndex) && Hint.Likely(face.IsValid))
					{
						var iterCount = 0;
						var elementCount = face.ElementCount;

						do
						{
							// loop vertex is guaranteed to be the origin vertex for this loop
							var v = Vertices[loop.StartVertexIndex];
							CombineData[loopIndex + iterCount] = new MergeData
							{
								MeshIndex = MeshIndex, FaceIndex = faceIndex, FaceElementCount = elementCount, VertexPosition = v.Position,
								VertexGridPosHash = math.hash(v.GridPosition()),
							};

							loop = Loops[loop.NextLoopIndex];
							iterCount++;
						} while (Hint.Likely(iterCount < elementCount));
					}
				}
			}

			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct MergeData
			{
				public int MeshIndex;
				public int FaceIndex;
				public int FaceElementCount;
				public uint VertexGridPosHash;
				public float3 VertexPosition;

				public override string ToString() =>
					$"Face {FaceIndex}, Count {FaceElementCount}, GridPosHash {VertexGridPosHash}, Pos {VertexPosition}";
			}
		}
	}
}