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
			jobHandle.Complete();

			gMesh.SetElementsCountAfterBatchOperation();

			return gMesh;
		}

		private void SetElementsCountAfterBatchOperation()
		{
			_vertexCount = _vertices.Length;
			_edgeCount = _edges.Length;
			_loopCount = _loops.Length;
			_faceCount = _faces.Length;
		}

		private JobHandle ScheduleCreatePlaneJob(GMeshPlane parameters)
		{
			var transform = new Transform(parameters.Translation, parameters.Rotation, DefaultScale);
			var job = new CreatePlaneJob
			{
				Vertices = _vertices, Edges = _edges, Loops = _loops, Faces = _faces,
				PlaneVertexCount = new int2(parameters.VertexCountX, parameters.VertexCountY),
				Translation = parameters.Translation, Rotation = parameters.Rotation, Scale = float3(parameters.Scale, DefaultScale),
			};
			return job.Schedule();
		}

		[BurstCompile] [StructLayout(LayoutKind.Sequential)]
		private struct CreatePlaneJob : IJob
		{
			public NativeList<Vertex> Vertices;
			public NativeList<Edge> Edges;
			public NativeList<Loop> Loops;
			public NativeList<Face> Faces;

			public int2 PlaneVertexCount;
			public float3 Translation;
			public float3 Rotation;
			public float3 Scale;

			private Vertex GetVertex(int index) => Vertices[index];
			private void SetVertex(in Vertex vertex) => Vertices[vertex.Index] = vertex;
			private Edge GetEdge(int index) => Edges[index];
			private void SetEdge(in Edge edge) => Edges[edge.Index] = edge;
			private Loop GetLoop(int index) => Loops[index];
			private void SetLoop(in Loop loop) => Loops[loop.Index] = loop;
			private Face GetFace(int index) => Faces[index];
			private void SetFace(in Face face) => Faces[face.Index] = face;

			public void Execute()
			{
				// init lists
				var subdivisions = PlaneVertexCount - 1;
				var totalVertexCount = PlaneVertexCount.x * PlaneVertexCount.y;
				Vertices.ResizeUninitialized(totalVertexCount);
				Faces.ResizeUninitialized(subdivisions.x * subdivisions.y);
				Loops.ResizeUninitialized(subdivisions.x * subdivisions.y * 4);
				var vertexCount = 0;

				// create vertices
				var rt = new RigidTransform(quaternion.Euler(radians(Rotation)), Translation);
				var centerOffset = float3(.5f, .5f, 0f) * Scale;
				var step = 1f / float3(subdivisions, 1f) * Scale;
				for (var y = 0; y < PlaneVertexCount.y; y++)
					for (var x = 0; x < PlaneVertexCount.x; x++)
					{
						Vertices[vertexCount] = new Vertex
						{
							Index = vertexCount, BaseEdgeIndex = UnsetIndex,
							Position = transform(rt, float3(x, y, 0f) * step - centerOffset),
							//Position = float3(x, y, 0f) * step - centerOffset,
						};
						vertexCount++;
					}

				// create quad faces
				var faceIndex = 0;
				var loopIndex = 0;
				var fvIndices = new NativeArray<int>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				var edgeIndices = new NativeArray<int>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				for (var y = 0; y < subdivisions.y; y++)
				{
					for (var x = 0; x < subdivisions.x; x++)
					{
						// each quad has (0,0) in the lower left corner with verts: v0=down-left, v1=up-left, v2=up-right, v3=down-right
						// +z axis points towards the plane and plane normal is Vector3.back = (0,0,-1)

						// calculate quad's vertex indices
						var vi0 = y * PlaneVertexCount.x + x;
						var vi1 = (y + 1) * PlaneVertexCount.x + x;
						var vi2 = (y + 1) * PlaneVertexCount.x + x + 1;
						var vi3 = y * PlaneVertexCount.x + x + 1;
						fvIndices[0] = vi0;
						fvIndices[1] = vi1;
						fvIndices[2] = vi2;
						fvIndices[3] = vi3;

						// Create Edges
						{
							var eIndex0 = FindExistingEdgeIndex(fvIndices[0], fvIndices[1]);
							var eIndex1 = FindExistingEdgeIndex(fvIndices[1], fvIndices[2]);
							var eIndex2 = FindExistingEdgeIndex(fvIndices[2], fvIndices[3]);
							var eIndex3 = FindExistingEdgeIndex(fvIndices[3], fvIndices[0]);
							edgeIndices[0] = select(CreateEdge(fvIndices[0], fvIndices[1]), eIndex0, eIndex0 != UnsetIndex);
							edgeIndices[1] = select(CreateEdge(fvIndices[1], fvIndices[2]), eIndex1, eIndex1 != UnsetIndex);
							edgeIndices[2] = select(CreateEdge(fvIndices[2], fvIndices[3]), eIndex2, eIndex2 != UnsetIndex);
							edgeIndices[3] = select(CreateEdge(fvIndices[3], fvIndices[0]), eIndex3, eIndex3 != UnsetIndex);
						}

						// Create Face
						Faces[faceIndex] = new Face { Index = faceIndex, FirstLoopIndex = UnsetIndex, ElementCount = 4 };

						// Create Loops
						for (var i = 0; i < 4; i++)
						{
							var edgeIndex = edgeIndices[i];
							var vertexIndex = fvIndices[i];
							var (prevRadialIdx, nextRadialIdx) = CreateLoopInternal_UpdateRadialLoopCycle(loopIndex, edgeIndex);
							var (prevLoopIdx, nextLoopIdx) = CreateLoopInternal_UpdateLoopCycle(loopIndex, faceIndex);
							var loop = new Loop
							{
								Index = loopIndex, FaceIndex = faceIndex, EdgeIndex = edgeIndex, StartVertexIndex = vertexIndex,
								PrevLoopIndex = prevLoopIdx, NextLoopIndex = nextLoopIdx,
								PrevRadialLoopIndex = prevRadialIdx, NextRadialLoopIndex = nextRadialIdx,
							};
							Loops[loopIndex] = loop;
							loopIndex++;
						}

						faceIndex++;
					}
				}

				edgeIndices.Dispose();
				fvIndices.Dispose();
			}

			private int FindExistingEdgeIndex(int v0Index, int v1Index)
			{
				// check all edges in cycle, return this edge's index if it points to v1
				var edgeIndex = GetVertex(v0Index).BaseEdgeIndex;
				if (edgeIndex == UnsetIndex)
					return UnsetIndex;

				var edge = GetEdge(edgeIndex);
				do
				{
					if (edge.ContainsVertex(v1Index))
						return edge.Index;

					edge = GetEdge(edge.GetNextEdgeIndex(v0Index));
				} while (edge.Index != edgeIndex);

				return UnsetIndex;
			}

			private int CreateEdge(int v0Index, int v1Index)
			{
				var edgeIndex = Edges.Length;
				var edge = new Edge
				{
					Index = edgeIndex, BaseLoopIndex = UnsetIndex, AVertexIndex = v0Index, OVertexIndex = v1Index,
					APrevEdgeIndex = UnsetIndex, ANextEdgeIndex = UnsetIndex, OPrevEdgeIndex = UnsetIndex, ONextEdgeIndex = UnsetIndex,
				};
				Edges.Add(edge);

				// Disk Cycle Vertex 0
				{
					var v0 = GetVertex(v0Index);
					if (Hint.Likely(v0.BaseEdgeIndex == UnsetIndex))
					{
						v0.BaseEdgeIndex = edge.APrevEdgeIndex = edge.ANextEdgeIndex = edgeIndex;
						SetVertex(v0);
					}
					else
					{
						var v0BaseEdge = GetEdge(v0.BaseEdgeIndex);
						edge.APrevEdgeIndex = v0.BaseEdgeIndex;
						edge.ANextEdgeIndex = v0BaseEdge.GetNextEdgeIndex(v0Index);

						var v0PrevEdge = GetEdge(edge.APrevEdgeIndex);
						v0PrevEdge.SetNextEdgeIndex(v0Index, edgeIndex);
						SetEdge(v0PrevEdge);

						var v0NextEdge = GetEdge(edge.ANextEdgeIndex);
						v0NextEdge.SetPrevEdgeIndex(v0Index, edgeIndex);
						SetEdge(v0NextEdge);

						// FIX: update prev edge vertex1's edge index of v0 and v1 base edges both point to prev edge.
						// This occurs when v0 and v1 were the first vertices to be connected with an edge.
						var prevEdgeVertex0 = GetVertex(v0BaseEdge.AVertexIndex);
						if (Hint.Unlikely(prevEdgeVertex0.BaseEdgeIndex == v0.BaseEdgeIndex))
						{
							v0.BaseEdgeIndex = edgeIndex;
							SetVertex(v0);
						}
					}
				}

				// Disk Cycle Vertex 1
				{
					var v1 = GetVertex(v1Index);
					if (Hint.Unlikely(v1.BaseEdgeIndex == UnsetIndex))
					{
						// Note: the very first edge between two vertices will set itself as BaseEdgeIndex on both vertices.
						// This is expected behaviour and is "fixed" when the next edge connects to V1 and detects that.
						v1.BaseEdgeIndex = edge.OPrevEdgeIndex = edge.ONextEdgeIndex = edgeIndex;
						SetVertex(v1);
					}
					else
					{
						var v1BaseEdge = GetEdge(v1.BaseEdgeIndex);
						edge.OPrevEdgeIndex = v1.BaseEdgeIndex;
						edge.ONextEdgeIndex = v1BaseEdge.GetNextEdgeIndex(v1Index);

						var v1PrevEdge = GetEdge(edge.OPrevEdgeIndex);
						v1PrevEdge.SetNextEdgeIndex(v1Index, edgeIndex);
						SetEdge(v1PrevEdge);

						var v1NextEdge = GetEdge(edge.ONextEdgeIndex);
						v1NextEdge.SetPrevEdgeIndex(v1Index, edgeIndex);
						SetEdge(v1NextEdge);
					}
				}

				Edges[edgeIndex] = edge;
				return edgeIndex;
			}

			private (int, int) CreateLoopInternal_UpdateRadialLoopCycle(int newLoopIndex, int edgeIndex)
			{
				var prevRadialLoopIndex = newLoopIndex;
				var nextRadialLoopIndex = newLoopIndex;

				var edge = GetEdge(edgeIndex);
				if (Hint.Unlikely(edge.BaseLoopIndex == UnsetIndex))
					edge.BaseLoopIndex = newLoopIndex;
				else
				{
					var edgeBaseLoop = GetLoop(edge.BaseLoopIndex);
					prevRadialLoopIndex = edgeBaseLoop.Index;
					nextRadialLoopIndex = edgeBaseLoop.NextRadialLoopIndex;

					if (Hint.Likely(edgeBaseLoop.NextRadialLoopIndex == edgeBaseLoop.Index))
						edgeBaseLoop.PrevRadialLoopIndex = newLoopIndex;
					else
					{
						var nextRadialLoop = GetLoop(edgeBaseLoop.NextRadialLoopIndex);
						nextRadialLoop.PrevRadialLoopIndex = newLoopIndex;
						SetLoop(nextRadialLoop);
					}

					edgeBaseLoop.NextRadialLoopIndex = newLoopIndex;
					SetLoop(edgeBaseLoop);
				}

				SetEdge(edge);

				return (prevRadialLoopIndex, nextRadialLoopIndex);
			}

			private (int, int) CreateLoopInternal_UpdateLoopCycle(int newLoopIndex, int faceIndex)
			{
				var prevLoopIndex = newLoopIndex;
				var nextLoopIndex = newLoopIndex;

				var face = GetFace(faceIndex);
				if (Hint.Unlikely(face.FirstLoopIndex == UnsetIndex))
				{
					face.FirstLoopIndex = newLoopIndex;
					SetFace(face);
				}
				else
				{
					var firstLoop = GetLoop(face.FirstLoopIndex);
					nextLoopIndex = firstLoop.Index;
					prevLoopIndex = firstLoop.PrevLoopIndex;

					var prevLoop = GetLoop(prevLoopIndex);
					prevLoop.NextLoopIndex = newLoopIndex;

					// update nextLoop or re-assign it as firstLoop, depends on whether they are the same
					if (Hint.Likely(prevLoopIndex != nextLoopIndex))
						SetLoop(prevLoop);
					else
						firstLoop = prevLoop;

					firstLoop.PrevLoopIndex = newLoopIndex;
					SetLoop(firstLoop);
				}

				return (prevLoopIndex, nextLoopIndex);
			}
		}
	}
}