// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private JobHandle ScheduleCreatePlane(GMeshPlane parameters)
		{
			var vertexCount = new int2(parameters.VertexCountX, parameters.VertexCountY);
			var vertsJob = new JPlane.CreatePlaneVerticesJob
			{
				PlaneVertexCount = vertexCount,
				Translation = parameters.Translation, Rotation = parameters.Rotation, Scale = float3(parameters.Scale, DefaultScale),
			};
			vertsJob.Init(ref _data, vertexCount.x * vertexCount.y);
			var vertJobHandle = vertsJob.Schedule(vertexCount.x * vertexCount.y, 4);

			var planeJob = new JPlane.CreatePlaneQuadsJob { Data = _data, PlaneVertexCount = vertexCount };
			return planeJob.Schedule(vertJobHandle);
		}

		[BurstCompile]
		private static class JPlane
		{
			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct CreatePlaneVerticesJob : IJobParallelFor
			{
				public int2 PlaneVertexCount;
				public float3 Translation;
				public float3 Rotation;
				public float3 Scale;

				[WriteOnly] private NativeArray<Vertex> Vertices;
				private float3 centerOffset;
				private float3 step;
				private RigidTransform transform;

				public void Init(ref GraphData data, int expectedVertexCount)
				{
					// set expected number of vertices directly
					data.VertexCount = expectedVertexCount;
					data.InitializeVerticesWithSize(expectedVertexCount);
					Vertices = data.VerticesArray;

					transform = new RigidTransform(quaternion.Euler(radians(Rotation)), Translation);
					var subdivisions = PlaneVertexCount - 1;
					centerOffset = float3(.5f, .5f, 0f) * Scale;
					step = 1f / float3(subdivisions, 1f) * Scale;
				}

				public void Execute(int index)
				{
					var x = index % PlaneVertexCount.x;
					var y = index / PlaneVertexCount.x;
					var pos = transform(transform, float3(x, y, 0f) * step - centerOffset);
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