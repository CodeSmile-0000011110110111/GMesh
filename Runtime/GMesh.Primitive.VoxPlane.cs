// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;
using Random = Unity.Mathematics.Random;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Creates a "voxelized" heightmap plane where cuboids stick out based on normalized heightmap values for each
		/// quad in the plane.
		/// Heightmap must have subdivisions X*Y entried, that means VertexCount (X-1)*(Y-1) must be same as heightmap.Length.
		/// .
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="normalizedHeightmap">Heightmap values in 0.0 to 1.0 range (normalized) with VertexCount (X-1)*(Y-1) entries.</param>
		/// <param name="flattenThreshold">If neighbouring quads' height differs less than this, they are considered flat (equal height).</param>
		/// <returns></returns>
		public static GMesh VoxPlane(GMeshVoxPlane parameters /*, in NativeArray<float> normalizedHeightmap*/)
		{
			if (parameters.VertexCountX < 2 || parameters.VertexCountY < 2)
				throw new ArgumentException("minimum of 2 vertices per axis required");

			var subdivisions = new int2(parameters.VertexCountX - 1, parameters.VertexCountY - 1);

			// FIXME: random heightmap for testing
			var quadCount = subdivisions.x * subdivisions.y;
			var normalizedHeightmap = new NativeArray<float>(quadCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var random = new Random();
			random.InitState(7);
			random.NextFloat();
			for (var i = 0; i < quadCount; i++)
				normalizedHeightmap[i] = random.NextFloat();

			if (normalizedHeightmap.Length != quadCount)
				throw new ArgumentException("heightmap length does not match subdivisions area (x*y)");

			var gMesh = new GMesh();
			var handle = gMesh.ScheduleCreateVoxPlane(parameters, normalizedHeightmap);
			normalizedHeightmap.Dispose(handle);
			handle.Complete();
			
			Debug.Log(gMesh);

			return gMesh;
		}

		public JobHandle ScheduleCreateVoxPlane(GMeshVoxPlane parameters, in NativeArray<float> normalizedHeightmap)
		{
			var vertexCount = new int2(parameters.VertexCountX, parameters.VertexCountY);
			Debug.Log($"VoxPlane Quads: {(vertexCount.x -1) * (vertexCount.y - 1)}, Heightmap: {normalizedHeightmap.Length}");

			var createJob = new VoxPlaneJobs.CreateVoxPlaneQuadsJob
			{
				Data = _data, Heightmap = normalizedHeightmap, PlaneVertexCount = vertexCount,
				Translation = parameters.Translation, Rotation = parameters.Rotation, Scale = parameters.Scale,
			};
			createJob.Init();
			return createJob.Schedule(normalizedHeightmap.Length, default);
		}

		[BurstCompile]
		private static class VoxPlaneJobs
		{
			public enum Neighbours
			{
				Left,
				Up,
				Right,
				Down,
			}

			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct CreateVoxPlaneQuadsJob : IJobFor
			{
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<float> Heightmap;
				public GraphData Data;
				public int2 PlaneVertexCount;
				public float3 Translation;
				public float3 Rotation;
				public float3 Scale;

				private int2 subdivisions;
				private float3 centerOffset;
				private float3 step;
				private RigidTransform transform;

				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<int> vertIndices;
				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<int> edgeIndices;

				public void Init()
				{
					subdivisions = PlaneVertexCount - 1;

					// set expected number of vertices directly
					//Data.ValidVertexCount = subdivisions.x * subdivisions.y * QuadElementCount;
					//Data.InitializeVerticesWithSize(Data.ValidVertexCount);

					transform = new RigidTransform(quaternion.Euler(radians(Rotation)), Translation);
					centerOffset = float3(.5f, .5f, 0f) * Scale;
					step = 1f / float3(subdivisions, 1f) * Scale;

					vertIndices = new NativeArray<int>(QuadElementCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
					edgeIndices = new NativeArray<int>(QuadElementCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
				}

				public void Execute(int heightmapIndex)
				{
					//var quadVertices = new QuadVertices();
					{
						var x = heightmapIndex % subdivisions.x;
						var y = heightmapIndex / subdivisions.x;
						var z = -Heightmap[heightmapIndex];

						var pos = transform(transform, float3(x, y, z) * step - centerOffset);
						vertIndices[0] = Create.Vertex(Data, pos);
						pos = transform(transform, float3(x, y + 1f, z) * step - centerOffset);
						vertIndices[1] = Create.Vertex(Data, pos);
						pos = transform(transform, float3(x + 1f, y + 1f, z) * step - centerOffset);
						vertIndices[2] = Create.Vertex(Data, pos);
						pos = transform(transform, float3(x + 1f, y, z) * step - centerOffset);
						vertIndices[3] = Create.Vertex(Data, pos);

						edgeIndices[0] = Create.Edge(Data, vertIndices[0], vertIndices[1], true);
						edgeIndices[1] = Create.Edge(Data, vertIndices[1], vertIndices[2], true);
						edgeIndices[2] = Create.Edge(Data, vertIndices[2], vertIndices[3], true);
						edgeIndices[3] = Create.Edge(Data, vertIndices[3], vertIndices[0], true);

						// Create Face and Loops
						Create.Loops(Data, Create.Face(Data, QuadElementCount), vertIndices, edgeIndices);
						Debug.Log($"{heightmapIndex}: {Data}");
					}

					/*
					{
						// init lists
						// TODO: initialize edges with count (shared inner edges vs border edges - there's an algorithm to be found)
						Data.InitializeFacesWithSize(subdivisions.x * subdivisions.y);
						Data.InitializeLoopsWithSize(subdivisions.x * subdivisions.y * 4);

						// create quads
						const int QuadElementCount = 4;
						var vertIndices = new NativeArray<int>(QuadElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
						var edgeIndices = new NativeArray<int>(QuadElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

						for (var y = 0; y < subdivisions.y; y++)
						{
							for (var x = 0; x < subdivisions.x; x++)
							{
								// each quad has (0,0) in the lower left corner with verts: v0=down-left, v1=up-left, v2=up-right, v3=down-right

								// calculate quad's vertex indices
								vertIndices[0] = y * PlaneVertexCount.x + x;
								vertIndices[1] = (y + 1) * PlaneVertexCount.x + x;
								vertIndices[2] = (y + 1) * PlaneVertexCount.x + x + 1;
								vertIndices[3] = y * PlaneVertexCount.x + x + 1;

								// Create or re-use Edges
								var eIndex0 = Find.ExistingEdgeIndex(Data, vertIndices[0], vertIndices[1]);
								var eIndex1 = Find.ExistingEdgeIndex(Data, vertIndices[1], vertIndices[2]);
								var eIndex2 = Find.ExistingEdgeIndex(Data, vertIndices[2], vertIndices[3]);
								var eIndex3 = Find.ExistingEdgeIndex(Data, vertIndices[3], vertIndices[0]);
								edgeIndices[0] = eIndex0 != UnsetIndex ? eIndex0 : Create.Edge(Data, vertIndices[0], vertIndices[1]);
								edgeIndices[1] = eIndex1 != UnsetIndex ? eIndex1 : Create.Edge(Data, vertIndices[1], vertIndices[2]);
								edgeIndices[2] = eIndex2 != UnsetIndex ? eIndex2 : Create.Edge(Data, vertIndices[2], vertIndices[3]);
								edgeIndices[3] = eIndex3 != UnsetIndex ? eIndex3 : Create.Edge(Data, vertIndices[3], vertIndices[0]);

								// Create Face and Loops
								Create.Loops(Data, Create.Face(Data, QuadElementCount), vertIndices, edgeIndices);
							}
						}

						vertIndices.Dispose();
						edgeIndices.Dispose();
					}
					*/
				}
			}

			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct QuadVertices
			{
				// current face's loop startvertices in clockwise order
				public readonly float3 WestV0;
				public readonly float3 NorthV0;
				public readonly float3 EastV0;
				public readonly float3 SouthV0;

				// preprocess neighbour heights?

				/*
				// y coordinate (height) of current face
				public readonly float FaceY;


				// vertices of surrounding faces to determine side faces (if any)
				// V0/V1 go along the loops in clockwise order, with V0 being the loop's start vertex
				public readonly float3 SideWestV0;
				public readonly float3 SideWestV1;
				public readonly float3 SideNorthV0;
				public readonly float3 SideNorthV1;
				public readonly float3 SideEastV0;
				public readonly float3 SideEastV1;
				public readonly float3 SideSouthV0;
				public readonly float3 SideSouthV1;
				*/
			}

			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct CreatePlaneVerticesJob : IJobParallelFor
			{
				public readonly int2 PlaneVertexCount;
				public readonly float3 Translation;
				public readonly float3 Rotation;
				public readonly float3 Scale;
				[ReadOnly] public NativeArray<float> Heightmap;

				[WriteOnly] private NativeArray<Vertex> Vertices;
				private float3 centerOffset;
				private float3 step;
				private RigidTransform transform;

				public void Init(ref GraphData data, int expectedVertexCount)
				{
					// set expected number of vertices directly
					data.ValidVertexCount = expectedVertexCount;
					data.InitializeVerticesWithSize(expectedVertexCount);
					Vertices = data.VerticesAsWritableArray;

					transform = new RigidTransform(quaternion.Euler(radians(Rotation)), Translation);
					var subdivisions = PlaneVertexCount - 1;
					centerOffset = float3(.5f, .5f, 0f) * Scale;
					step = 1f / float3(subdivisions, 1f) * Scale;
				}

				public void Execute(int index)
				{
					var x = index % PlaneVertexCount.x;
					var y = index / PlaneVertexCount.x;
					var z = Heightmap[index];
					var pos = transform(transform, float3(x, y, z) * step - centerOffset);
					Vertices[index] = new Vertex { Index = index, BaseEdgeIndex = UnsetIndex, Position = pos };
				}
			}

			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct CreatePlaneQuadsJob : IJob
			{
				public GraphData Data;
				public int2 PlaneVertexCount;

				public void Execute()
				{
					// init lists
					var subdivisions = PlaneVertexCount - 1;
					// TODO: initialize edges with count (shared inner edges vs border edges - there's an algorithm to be found)
					Data.InitializeFacesWithSize(subdivisions.x * subdivisions.y);
					Data.InitializeLoopsWithSize(subdivisions.x * subdivisions.y * 4);

					// create quads
					const int QuadElementCount = 4;
					var vertIndices = new NativeArray<int>(QuadElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					var edgeIndices = new NativeArray<int>(QuadElementCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

					for (var y = 0; y < subdivisions.y; y++)
					{
						for (var x = 0; x < subdivisions.x; x++)
						{
							// each quad has (0,0) in the lower left corner with verts: v0=down-left, v1=up-left, v2=up-right, v3=down-right

							// calculate quad's vertex indices
							vertIndices[0] = y * PlaneVertexCount.x + x;
							vertIndices[1] = (y + 1) * PlaneVertexCount.x + x;
							vertIndices[2] = (y + 1) * PlaneVertexCount.x + x + 1;
							vertIndices[3] = y * PlaneVertexCount.x + x + 1;

							// Create or re-use Edges
							var eIndex0 = Find.ExistingEdgeIndex(Data, vertIndices[0], vertIndices[1]);
							var eIndex1 = Find.ExistingEdgeIndex(Data, vertIndices[1], vertIndices[2]);
							var eIndex2 = Find.ExistingEdgeIndex(Data, vertIndices[2], vertIndices[3]);
							var eIndex3 = Find.ExistingEdgeIndex(Data, vertIndices[3], vertIndices[0]);
							edgeIndices[0] = eIndex0 != UnsetIndex ? eIndex0 : Create.Edge(Data, vertIndices[0], vertIndices[1]);
							edgeIndices[1] = eIndex1 != UnsetIndex ? eIndex1 : Create.Edge(Data, vertIndices[1], vertIndices[2]);
							edgeIndices[2] = eIndex2 != UnsetIndex ? eIndex2 : Create.Edge(Data, vertIndices[2], vertIndices[3]);
							edgeIndices[3] = eIndex3 != UnsetIndex ? eIndex3 : Create.Edge(Data, vertIndices[3], vertIndices[0]);

							// Create Face and Loops
							Create.Loops(Data, Create.Face(Data, QuadElementCount), vertIndices, edgeIndices);
						}
					}

					vertIndices.Dispose();
					edgeIndices.Dispose();
				}
			}
		}
	}
}