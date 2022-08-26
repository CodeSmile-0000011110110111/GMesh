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

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private static GMesh CreatePlaneJobified(GMeshPlane parameters)
		{
			if (parameters.VertexCountX < 2 || parameters.VertexCountY < 2)
				throw new ArgumentException("minimum of 2 vertices per axis required");

			var gMesh = new GMesh();
			var jobHandle = gMesh.ScheduleCreatePlaneJob(parameters);
			//jobHandle.Complete();

			return gMesh;
		}

		private JobHandle ScheduleCreatePlaneJob(GMeshPlane parameters)
		{
			var job = new CreatePlaneJob
			{
				Data = _data, PlaneVertexCount = new int2(parameters.VertexCountX, parameters.VertexCountY),
				Translation = parameters.Translation, Rotation = parameters.Rotation, Scale = float3(parameters.Scale, DefaultScale),
			};
			job.Schedule().Complete();

			return default;
		}

		[BurstCompile] [StructLayout(LayoutKind.Sequential)]
		private struct CreatePlaneJob : IJob
		{
			public GraphData Data;

			public int2 PlaneVertexCount;
			public float3 Translation;
			public float3 Rotation;
			public float3 Scale;

			public void Execute()
			{
				// init lists
				var subdivisions = PlaneVertexCount - 1;
				Data.InitializeVerticesWithSize(PlaneVertexCount.x * PlaneVertexCount.y);
				// TODO: initialize edges with count (shared inner edges vs border edges - there's an algorithm to be found)
				Data.InitializeFacesWithSize(subdivisions.x * subdivisions.y);
				Data.InitializeLoopsWithSize(subdivisions.x * subdivisions.y * 4);

				// create vertices
				{
					var rt = new RigidTransform(quaternion.Euler(radians(Rotation)), Translation);
					var centerOffset = float3(.5f, .5f, 0f) * Scale;
					var step = 1f / float3(subdivisions, 1f) * Scale;
					for (var y = 0; y < PlaneVertexCount.y; y++)
						for (var x = 0; x < PlaneVertexCount.x; x++)
						{
							var pos = transform(rt, float3(x, y, 0f) * step - centerOffset);
							var vertex = new Vertex { BaseEdgeIndex = UnsetIndex, Position = pos };
							Data.AddVertex(ref vertex);
						}
				}

				// create quad faces
				var fvIndices = new NativeArray<int>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				var edgeIndices = new NativeArray<int>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				for (var y = 0; y < subdivisions.y; y++)
				{
					for (var x = 0; x < subdivisions.x; x++)
					{
						// each quad has (0,0) in the lower left corner with verts: v0=down-left, v1=up-left, v2=up-right, v3=down-right
						// +z axis points towards the plane and plane normal is Vector3.back = (0,0,-1)

						// calculate quad's vertex indices
						fvIndices[0] = y * PlaneVertexCount.x + x;
						fvIndices[1] = (y + 1) * PlaneVertexCount.x + x;
						fvIndices[2] = (y + 1) * PlaneVertexCount.x + x + 1;
						fvIndices[3] = y * PlaneVertexCount.x + x + 1;

						// Create Edges
						{
							var eIndex0 = FindExistingEdgeIndex(fvIndices[0], fvIndices[1]);
							var eIndex1 = FindExistingEdgeIndex(fvIndices[1], fvIndices[2]);
							var eIndex2 = FindExistingEdgeIndex(fvIndices[2], fvIndices[3]);
							var eIndex3 = FindExistingEdgeIndex(fvIndices[3], fvIndices[0]);
							edgeIndices[0] = eIndex0 != UnsetIndex ? eIndex0 : CreateEdge(fvIndices[0], fvIndices[1]);
							edgeIndices[1] = eIndex1 != UnsetIndex ? eIndex1 : CreateEdge(fvIndices[1], fvIndices[2]);
							edgeIndices[2] = eIndex2 != UnsetIndex ? eIndex2 : CreateEdge(fvIndices[2], fvIndices[3]);
							edgeIndices[3] = eIndex3 != UnsetIndex ? eIndex3 : CreateEdge(fvIndices[3], fvIndices[0]);
						}

						// Create Face
						var face = new Face { FirstLoopIndex = UnsetIndex, ElementCount = 4 };
						var faceIndex = Data.AddFace(ref face);

						// Create Loops
						for (var i = 0; i < 4; i++)
						{
							var edgeIndex = edgeIndices[i];
							var vertexIndex = fvIndices[i];
							var loopIndex = Data.GetNextLoopIndex();
							var (prevRadialIdx, nextRadialIdx) = CreateLoopInternal_UpdateRadialLoopCycle(loopIndex, edgeIndex);
							var (prevLoopIdx, nextLoopIdx) = CreateLoopInternal_UpdateLoopCycle(loopIndex, faceIndex);
							var loop = new Loop
							{
								FaceIndex = faceIndex, EdgeIndex = edgeIndex, StartVertexIndex = vertexIndex,
								PrevLoopIndex = prevLoopIdx, NextLoopIndex = nextLoopIdx,
								PrevRadialLoopIndex = prevRadialIdx, NextRadialLoopIndex = nextRadialIdx,
							};
							Data.AddLoop(ref loop);
						}
					}
				}

				edgeIndices.Dispose();
				fvIndices.Dispose();
			}

			private int FindExistingEdgeIndex(int v0Index, int v1Index)
			{
				// check all edges in cycle, return this edge's index if it points to v1
				var edgeIndex = Data.GetVertex(v0Index).BaseEdgeIndex;
				if (edgeIndex == UnsetIndex)
					return UnsetIndex;

				var edge = Data.GetEdge(edgeIndex);
				do
				{
					if (edge.ContainsVertex(v1Index))
						return edge.Index;

					edge = Data.GetEdge(edge.GetNextEdgeIndex(v0Index));
				} while (edge.Index != edgeIndex);

				return UnsetIndex;
			}

			private int CreateEdge(int v0Index, int v1Index)
			{
				var edgeIndex = Data.GetNextEdgeIndex();
				var edge = new Edge
				{
					Index = edgeIndex, BaseLoopIndex = UnsetIndex, AVertexIndex = v0Index, OVertexIndex = v1Index,
					APrevEdgeIndex = UnsetIndex, ANextEdgeIndex = UnsetIndex, OPrevEdgeIndex = UnsetIndex, ONextEdgeIndex = UnsetIndex,
				};
				Data.AddEdge(ref edge);

				// Disk Cycle Vertex 0
				{
					var v0 = Data.GetVertex(v0Index);
					if (Hint.Likely(v0.BaseEdgeIndex == UnsetIndex))
					{
						v0.BaseEdgeIndex = edge.APrevEdgeIndex = edge.ANextEdgeIndex = edgeIndex;
						Data.SetVertex(v0);
					}
					else
					{
						var v0BaseEdge = Data.GetEdge(v0.BaseEdgeIndex);
						edge.APrevEdgeIndex = v0.BaseEdgeIndex;
						edge.ANextEdgeIndex = v0BaseEdge.GetNextEdgeIndex(v0Index);

						var v0PrevEdge = Data.GetEdge(edge.APrevEdgeIndex);
						v0PrevEdge.SetNextEdgeIndex(v0Index, edgeIndex);
						Data.SetEdge(v0PrevEdge);

						var v0NextEdge = Data.GetEdge(edge.ANextEdgeIndex);
						v0NextEdge.SetPrevEdgeIndex(v0Index, edgeIndex);
						Data.SetEdge(v0NextEdge);

						// FIX: update prev edge vertex1's edge index of v0 and v1 base edges both point to prev edge.
						// This occurs when v0 and v1 were the first vertices to be connected with an edge.
						var prevEdgeVertex0 = Data.GetVertex(v0BaseEdge.AVertexIndex);
						if (Hint.Unlikely(prevEdgeVertex0.BaseEdgeIndex == v0.BaseEdgeIndex))
						{
							v0.BaseEdgeIndex = edgeIndex;
							Data.SetVertex(v0);
						}
					}
				}

				// Disk Cycle Vertex 1
				{
					var v1 = Data.GetVertex(v1Index);
					if (Hint.Unlikely(v1.BaseEdgeIndex == UnsetIndex))
					{
						// Note: the very first edge between two vertices will set itself as BaseEdgeIndex on both vertices.
						// This is expected behaviour and is "fixed" when the next edge connects to V1 and detects that.
						v1.BaseEdgeIndex = edge.OPrevEdgeIndex = edge.ONextEdgeIndex = edgeIndex;
						Data.SetVertex(v1);
					}
					else
					{
						var v1BaseEdge = Data.GetEdge(v1.BaseEdgeIndex);
						edge.OPrevEdgeIndex = v1.BaseEdgeIndex;
						edge.ONextEdgeIndex = v1BaseEdge.GetNextEdgeIndex(v1Index);

						var v1PrevEdge = Data.GetEdge(edge.OPrevEdgeIndex);
						v1PrevEdge.SetNextEdgeIndex(v1Index, edgeIndex);
						Data.SetEdge(v1PrevEdge);

						var v1NextEdge = Data.GetEdge(edge.ONextEdgeIndex);
						v1NextEdge.SetPrevEdgeIndex(v1Index, edgeIndex);
						Data.SetEdge(v1NextEdge);
					}
				}

				Data.SetEdge(edge);
				return edgeIndex;
			}

			private (int, int) CreateLoopInternal_UpdateRadialLoopCycle(int newLoopIndex, int edgeIndex)
			{
				var prevRadialLoopIndex = newLoopIndex;
				var nextRadialLoopIndex = newLoopIndex;

				var edge = Data.GetEdge(edgeIndex);
				if (Hint.Unlikely(edge.BaseLoopIndex == UnsetIndex))
					edge.BaseLoopIndex = newLoopIndex;
				else
				{
					var edgeBaseLoop = Data.GetLoop(edge.BaseLoopIndex);
					prevRadialLoopIndex = edgeBaseLoop.Index;
					nextRadialLoopIndex = edgeBaseLoop.NextRadialLoopIndex;

					if (Hint.Likely(edgeBaseLoop.NextRadialLoopIndex == edgeBaseLoop.Index))
						edgeBaseLoop.PrevRadialLoopIndex = newLoopIndex;
					else
					{
						var nextRadialLoop = Data.GetLoop(edgeBaseLoop.NextRadialLoopIndex);
						nextRadialLoop.PrevRadialLoopIndex = newLoopIndex;
						Data.SetLoop(nextRadialLoop);
					}

					edgeBaseLoop.NextRadialLoopIndex = newLoopIndex;
					Data.SetLoop(edgeBaseLoop);
				}

				Data.SetEdge(edge);

				return (prevRadialLoopIndex, nextRadialLoopIndex);
			}

			private (int, int) CreateLoopInternal_UpdateLoopCycle(int newLoopIndex, int faceIndex)
			{
				var prevLoopIndex = newLoopIndex;
				var nextLoopIndex = newLoopIndex;

				var face = Data.GetFace(faceIndex);
				if (Hint.Unlikely(face.FirstLoopIndex == UnsetIndex))
				{
					face.FirstLoopIndex = newLoopIndex;
					Data.SetFace(face);
				}
				else
				{
					var firstLoop = Data.GetLoop(face.FirstLoopIndex);
					nextLoopIndex = firstLoop.Index;
					prevLoopIndex = firstLoop.PrevLoopIndex;

					var prevLoop = Data.GetLoop(prevLoopIndex);
					prevLoop.NextLoopIndex = newLoopIndex;

					// update nextLoop or re-assign it as firstLoop, depends on whether they are the same
					if (Hint.Likely(prevLoopIndex != nextLoopIndex))
						Data.SetLoop(prevLoop);
					else
						firstLoop = prevLoop;

					firstLoop.PrevLoopIndex = newLoopIndex;
					Data.SetLoop(firstLoop);
				}

				return (prevLoopIndex, nextLoopIndex);
			}
		}
	}
}