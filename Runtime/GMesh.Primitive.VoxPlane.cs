// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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

			return gMesh;
		}

		public JobHandle ScheduleCreateVoxPlane(GMeshVoxPlane parameters, in NativeArray<float> normalizedHeightmap)
		{
			var vertexCount = new int2(parameters.VertexCountX, parameters.VertexCountY);
			var createJob = new VoxPlaneJobs.CreateVoxPlaneQuadsJob
			{
				Data = _data, Heightmap = normalizedHeightmap, PlaneVertexCount = vertexCount,
				Translation = parameters.Translation, Rotation = parameters.Rotation, Scale = parameters.Scale,
			};
			createJob.Init();
			var createHandle = createJob.Schedule(normalizedHeightmap.Length, default);

			var borderJob = new VoxPlaneJobs.CreateVoxPlaneBorderJob
			{
				Data = _data, PlaneVertexCount = vertexCount,
				Translation = parameters.Translation, Rotation = parameters.Rotation, Scale = parameters.Scale,
			};
			borderJob.Init();
			return borderJob.Schedule(createHandle);
		}

		[BurstCompile]
		private static class VoxPlaneJobs
		{
			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct CreateVoxPlaneBorderJob : IJob
			{
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

				/*
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<float> Heightmap;
				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<float3> vertPositions;
				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<int> vertIndices;
				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<int> sideVertIndices;
				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<int> edgeIndices;
*/
				public void Init()
				{
					subdivisions = PlaneVertexCount - 1;

					transform = new RigidTransform(quaternion.Euler(radians(Rotation)), Translation);
					centerOffset = float3(.5f, .5f, 0f) * Scale;
					step = 1f / float3(subdivisions, 1f) * Scale;

					vertIndices = new NativeArray<int>(QuadElementCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
					edgeIndices = new NativeArray<int>(QuadElementCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
				}

				public void Execute()
				{
					int x = 0, y = 0, z = 0;

					// WEST SIDE BORDER
					for (y = 0; Hint.Likely(y < subdivisions.y); y++)
					{
						var baseY = y * subdivisions.x * QuadElementCount;
						vertIndices[0] = Create.Vertex(Data, transform(transform, float3(x, y, 0f) * step - centerOffset));
						vertIndices[1] = Create.Vertex(Data, transform(transform, float3(x, y + 1f, 0f) * step - centerOffset));
						vertIndices[2] = baseY + 1;
						vertIndices[3] = baseY;
						edgeIndices[0] = Create.Edge(Data, vertIndices[0], vertIndices[1]);
						edgeIndices[1] = Create.Edge(Data, vertIndices[1], vertIndices[2]);
						edgeIndices[2] = Create.Edge(Data, vertIndices[2], vertIndices[3]);
						edgeIndices[3] = Create.Edge(Data, vertIndices[3], vertIndices[0]);
						Create.Loops(Data, Create.Face(Data, QuadElementCount), vertIndices, edgeIndices);
					}

					// EAST SIDE BORDER
					x = subdivisions.x;
					for (y = 0; Hint.Likely(y < subdivisions.y); y++)
					{
						var baseY = subdivisions.x * QuadElementCount + y * subdivisions.x * QuadElementCount - 1;
						vertIndices[0] = baseY;
						vertIndices[1] = baseY - 1;
						vertIndices[2] = Create.Vertex(Data, transform(transform, float3(x, y + 1f, 0f) * step - centerOffset));
						vertIndices[3] = Create.Vertex(Data, transform(transform, float3(x, y, 0f) * step - centerOffset));
						edgeIndices[0] = Create.Edge(Data, vertIndices[0], vertIndices[1]);
						edgeIndices[1] = Create.Edge(Data, vertIndices[1], vertIndices[2]);
						edgeIndices[2] = Create.Edge(Data, vertIndices[2], vertIndices[3]);
						edgeIndices[3] = Create.Edge(Data, vertIndices[3], vertIndices[0]);
						Create.Loops(Data, Create.Face(Data, QuadElementCount), vertIndices, edgeIndices);
					}

					// SOUTH SIDE BORDER
					y = 0;
					for (x = 0; Hint.Likely(x < subdivisions.x); x++)
					{
						var baseX = x * QuadElementCount;
						vertIndices[0] = Create.Vertex(Data, transform(transform, float3(x, y, 0f) * step - centerOffset));
						vertIndices[1] = baseX;
						vertIndices[2] = baseX + 3;
						vertIndices[3] = Create.Vertex(Data, transform(transform, float3(x + 1f, y, 0f) * step - centerOffset));
						edgeIndices[0] = Create.Edge(Data, vertIndices[0], vertIndices[1]);
						edgeIndices[1] = Create.Edge(Data, vertIndices[1], vertIndices[2]);
						edgeIndices[2] = Create.Edge(Data, vertIndices[2], vertIndices[3]);
						edgeIndices[3] = Create.Edge(Data, vertIndices[3], vertIndices[0]);
						Create.Loops(Data, Create.Face(Data, QuadElementCount), vertIndices, edgeIndices);
					}
					
					// NORTH SIDE BORDER
					y = subdivisions.y - 1;
					for (x = 0; Hint.Likely(x < subdivisions.x); x++)
					{
						var baseX = x * QuadElementCount + y * subdivisions.x * QuadElementCount;
						vertIndices[0] = baseX + 1;
						vertIndices[1] = Create.Vertex(Data, transform(transform, float3(x, y + 1f, 0f) * step - centerOffset));
						vertIndices[2] = Create.Vertex(Data, transform(transform, float3(x + 1f, y + 1f, 0f) * step - centerOffset));
						vertIndices[3] = baseX + 2;
						edgeIndices[0] = Create.Edge(Data, vertIndices[0], vertIndices[1]);
						edgeIndices[1] = Create.Edge(Data, vertIndices[1], vertIndices[2]);
						edgeIndices[2] = Create.Edge(Data, vertIndices[2], vertIndices[3]);
						edgeIndices[3] = Create.Edge(Data, vertIndices[3], vertIndices[0]);
						Create.Loops(Data, Create.Face(Data, QuadElementCount), vertIndices, edgeIndices);
					}
				}
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

				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<float3> vertPositions;
				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<int> vertIndices;
				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<int> sideVertIndices;
				[DeallocateOnJobCompletion] [NativeDisableParallelForRestriction] private NativeArray<int> edgeIndices;

				public void Init()
				{
					subdivisions = PlaneVertexCount - 1;

					// set expected number of vertices directly
					var expectedVertexCount = subdivisions.x * subdivisions.y * QuadElementCount;
					Data.InitializeVerticesWithSize(expectedVertexCount);

					transform = new RigidTransform(quaternion.Euler(radians(Rotation)), Translation);
					centerOffset = float3(.5f, .5f, 0f) * Scale;
					step = 1f / float3(subdivisions, 1f) * Scale;

					vertPositions = new NativeArray<float3>(QuadElementCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
					vertIndices = new NativeArray<int>(QuadElementCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
					sideVertIndices = new NativeArray<int>(QuadElementCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
					edgeIndices = new NativeArray<int>(QuadElementCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
				}

				public void Execute(int heightmapIndex)
				{
					// TODO: vertices could be generated up-front in parallel

					var x = heightmapIndex % subdivisions.x;
					var y = heightmapIndex / subdivisions.x;
					var z = -Heightmap[heightmapIndex];

					{ // TOPMOST QUAD
						vertPositions[0] = transform(transform, float3(x, y, z) * step - centerOffset);
						vertPositions[1] = transform(transform, float3(x, y + 1f, z) * step - centerOffset);
						vertPositions[2] = transform(transform, float3(x + 1f, y + 1f, z) * step - centerOffset);
						vertPositions[3] = transform(transform, float3(x + 1f, y, z) * step - centerOffset);
						vertIndices[0] = Create.Vertex(Data, vertPositions[0]);
						vertIndices[1] = Create.Vertex(Data, vertPositions[1]);
						vertIndices[2] = Create.Vertex(Data, vertPositions[2]);
						vertIndices[3] = Create.Vertex(Data, vertPositions[3]);
						edgeIndices[0] = Create.Edge(Data, vertIndices[0], vertIndices[1], true);
						edgeIndices[1] = Create.Edge(Data, vertIndices[1], vertIndices[2], true);
						edgeIndices[2] = Create.Edge(Data, vertIndices[2], vertIndices[3], true);
						edgeIndices[3] = Create.Edge(Data, vertIndices[3], vertIndices[0], true);
						Create.Loops(Data, Create.Face(Data, QuadElementCount), vertIndices, edgeIndices);
					}

					if (Hint.Likely(x > 0))
					{ // WEST SIDE QUAD
						sideVertIndices[0] = vertIndices[0] - 1;
						sideVertIndices[1] = vertIndices[0] - 2;
						sideVertIndices[2] = vertIndices[1];
						sideVertIndices[3] = vertIndices[0];
						edgeIndices[0] = Create.Edge(Data, sideVertIndices[0], sideVertIndices[1]);
						edgeIndices[1] = Create.Edge(Data, sideVertIndices[1], sideVertIndices[2]);
						edgeIndices[2] = Create.Edge(Data, sideVertIndices[2], sideVertIndices[3]);
						edgeIndices[3] = Create.Edge(Data, sideVertIndices[3], sideVertIndices[0]);
						Create.Loops(Data, Create.Face(Data, QuadElementCount), sideVertIndices, edgeIndices);
					}

					// SOUTH SIDE QUAD
					if (Hint.Likely(y > 0))
					{
						var yIndexOffset = subdivisions.x * QuadElementCount;
						sideVertIndices[0] = vertIndices[0] - yIndexOffset + 1;
						sideVertIndices[1] = vertIndices[0];
						sideVertIndices[2] = vertIndices[3];
						sideVertIndices[3] = vertIndices[0] - yIndexOffset + 2;
						edgeIndices[0] = Create.Edge(Data, sideVertIndices[0], sideVertIndices[1]);
						edgeIndices[1] = Create.Edge(Data, sideVertIndices[1], sideVertIndices[2]);
						edgeIndices[2] = Create.Edge(Data, sideVertIndices[2], sideVertIndices[3]);
						edgeIndices[3] = Create.Edge(Data, sideVertIndices[3], sideVertIndices[0]);
						Create.Loops(Data, Create.Face(Data, QuadElementCount), sideVertIndices, edgeIndices);
					}
				}
			}

			/*
			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct QuadVertices
			{
				// current face's loop startvertices in clockwise order
				public readonly float3 WestV0;
				public readonly float3 NorthV0;
				public readonly float3 EastV0;
				public readonly float3 SouthV0;
			}
			*/
		}
	}
}