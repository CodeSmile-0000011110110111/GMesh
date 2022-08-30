// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Creates a new face using existing vertices. Adds edges and loops by using the supplied vertices.
		/// A face should have at least 3 vertices (triangle) but it can be any number of vertices.
		/// 
		/// Note: no check is performed to ensure the vertex positions all lie on the same plane. Upon triangulation
		/// (convert to Unity Mesh) such a face would not be represented as a single plane.
		/// </summary>
		/// <param name="vertexIndices">minimum of 3 vertex indices in CLOCKWISE winding order</param>
		/// <returns>the index of the new face</returns>
		public int CreateFace(in NativeArray<int> vertexIndices)
		{
#if GMESH_VALIDATION
			Validate.VertexCollection(vertexIndices);
#endif

			var vCount = vertexIndices.Length;
			var edgeIndices = new NativeArray<int>(vCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			Create.Edges(_data, vertexIndices, ref edgeIndices);

			var faceIndex = Create.Face(_data, vertexIndices.Length);
			Create.Loops(_data, faceIndex, vertexIndices, edgeIndices);

			edgeIndices.Dispose();
			return faceIndex;
		}

		/// <summary>
		/// Creates a new face, along with its vertices, edges and loops by using the supplied vertex positions.
		/// A face must have at least 3 vertices (triangle) but it can be any number of vertices.
		/// 
		/// Note: no check is performed to ensure the vertex positions all lie on the same plane. 
		/// </summary>
		/// <param name="vertexPositions">3 or more vertex positions in CLOCKWISE winding order</param>
		/// <returns>the index of the new face</returns>
		public int CreateFace(in NativeArray<float3> vertexPositions)
		{
#if GMESH_VALIDATION
			Validate.VertexCollection(vertexPositions);
#endif

			Create.Vertices(_data, vertexPositions, out var vertexIndices, Allocator.Temp);
			var faceIndex = CreateFace(vertexIndices);
			vertexIndices.Dispose();
			return faceIndex;
		}

		/// <summary>
		/// Creates a new edge using two vertex indices (must exist).
		/// 
		/// Note: This is a low-level operation. Prefer to use Euler operators or CreateFace/DeleteFace methods.
		/// Note: does not prevent creation of duplicate edges (two or more edges sharing the same vertices).
		/// </summary>
		/// <param name="vertexIndexA"></param>
		/// <param name="vertexIndexO"></param>
		/// <returns>index of the new edge</returns>
		public int CreateEdge(int vertexIndexA, int vertexIndexO) => Create.Edge(_data, vertexIndexA, vertexIndexO);

		/// <summary>
		/// Creates multiple new edges at once forming a closed loop (ie 0=>1, 1=>2, 2=>0). Vertices must already exist.
		/// 
		/// Note: This is a low-level operation. Prefer to use Euler operators or CreateFace/DeleteFace methods.
		/// Note: does not prevent creation of duplicate edges (two or more edges sharing the same vertices).
		/// </summary>
		/// <param name="vertexIndices"></param>
		/// <param name="edgeIndices"></param>
		/// <param name="allocator"></param>
		/// <returns>indices of new edges</returns>
		public void CreateEdges(in NativeArray<int> vertexIndices, out NativeArray<int> edgeIndices, Allocator allocator = Allocator.Temp)
		{
			var vCount = vertexIndices.Length;
			edgeIndices = new NativeArray<int>(vCount, allocator, NativeArrayOptions.UninitializedMemory);
			Create.Edges(_data, vertexIndices, ref edgeIndices);
		}

		/// <summary>
		/// Creates a new vertex at the given position with optional normal.
		///
		/// Note: This is a low-level operation. Prefer to use Euler operators or CreateFace/DeleteFace methods.
		/// Note: It is up to the caller to set BaseEdgeIndex.
		/// </summary>
		/// <param name="position"></param>
		/// <returns>index of new vertex</returns>
		public int CreateVertex(float3 position) => Create.Vertex(_data, position);

		/// <summary>
		/// Creates several new vertices at once but does not return the indices.
		/// Note: It is up to the caller to set BaseEdgeIndex of the new vertices.
		/// </summary>
		/// <param name="positions"></param>
		public void CreateVertices(in NativeArray<float3> positions) => Create.Vertices(_data, positions);

		/// <summary>
		/// Creates several new vertices at once and returns the indices.
		/// Note: It is up to the caller to set BaseEdgeIndex of the new vertices.
		/// </summary>
		/// <param name="positions"></param>
		/// <param name="vertexIndices">list of vertex indices - caller is responsible for Dispose()</param>
		/// <param name="allocator">the allocator for vertexIndices, defaults to TempJob</param>
		public void CreateVertices(in NativeArray<float3> positions, out NativeArray<int> vertexIndices,
			Allocator allocator = Allocator.TempJob) => Create.Vertices(_data, positions, out vertexIndices, allocator);

		[BurstCompile]
		internal readonly struct Create
		{
			public static int Face(in GraphData data, int vertexCount)
			{
				var face = GMesh.Face.Create(vertexCount);
				return data.AddFace(ref face);
			}

			public static void Loop(in GraphData data, int faceIndex, int vertexIndex, int edgeIndex)
			{
				var newLoopIndex = data.NextLoopIndex;
				var (prevRadialIdx, nextRadialIdx) = CreateLoop_UpdateRadialLoopCycle(data, newLoopIndex, edgeIndex);
				var (prevLoopIdx, nextLoopIdx) = CreateLoop_UpdateLoopCycle(data, newLoopIndex, faceIndex);
				var loop = GMesh.Loop.Create(faceIndex, edgeIndex, vertexIndex, prevRadialIdx, nextRadialIdx, prevLoopIdx, nextLoopIdx);
				data.AddLoop(ref loop);
			}

			public static void Loops(in GraphData data, int faceIndex, in NativeArray<int> vertexIndices, in NativeArray<int> edgeIndices)
			{
				var vCount = vertexIndices.Length;
				for (var i = 0; i < vCount; i++)
					Loop(data, faceIndex, vertexIndices[i], edgeIndices[i]);
			}

			private static (int, int) CreateLoop_UpdateRadialLoopCycle(in GraphData data, int newLoopIndex, int edgeIndex)
			{
				var prevRadialLoopIndex = newLoopIndex;
				var nextRadialLoopIndex = newLoopIndex;

				var edge = data.GetEdge(edgeIndex);
				if (Hint.Unlikely(edge.BaseLoopIndex == UnsetIndex))
					edge.BaseLoopIndex = newLoopIndex;
				else
				{
					var edgeBaseLoop = data.GetLoop(edge.BaseLoopIndex);
					prevRadialLoopIndex = edgeBaseLoop.Index;
					nextRadialLoopIndex = edgeBaseLoop.NextRadialLoopIndex;

					if (Hint.Likely(edgeBaseLoop.NextRadialLoopIndex == edgeBaseLoop.Index))
						edgeBaseLoop.PrevRadialLoopIndex = newLoopIndex;
					else
					{
						var nextRadialLoop = data.GetLoop(edgeBaseLoop.NextRadialLoopIndex);
						nextRadialLoop.PrevRadialLoopIndex = newLoopIndex;
						data.SetLoop(nextRadialLoop);
					}

					edgeBaseLoop.NextRadialLoopIndex = newLoopIndex;
					data.SetLoop(edgeBaseLoop);
				}

				data.SetEdge(edge);

				return (prevRadialLoopIndex, nextRadialLoopIndex);
			}

			public static (int, int) CreateLoop_UpdateLoopCycle(in GraphData data, int newLoopIndex, int faceIndex)
			{
				var prevLoopIndex = newLoopIndex;
				var nextLoopIndex = newLoopIndex;

				var face = data.GetFace(faceIndex);
				if (Hint.Unlikely(face.FirstLoopIndex == UnsetIndex))
				{
					face.FirstLoopIndex = newLoopIndex;
					data.SetFace(face);
				}
				else
				{
					var firstLoop = data.GetLoop(face.FirstLoopIndex);
					nextLoopIndex = firstLoop.Index;
					prevLoopIndex = firstLoop.PrevLoopIndex;

					var prevLoop = data.GetLoop(prevLoopIndex);
					prevLoop.NextLoopIndex = newLoopIndex;

					// update nextLoop or re-assign it as firstLoop, depends on whether they are the same
					if (Hint.Likely(prevLoopIndex != nextLoopIndex))
						data.SetLoop(prevLoop);
					else
						firstLoop = prevLoop;

					firstLoop.PrevLoopIndex = newLoopIndex;
					data.SetLoop(firstLoop);
				}

				return (prevLoopIndex, nextLoopIndex);
			}

			public static int Edge(in GraphData data, int vertexIndexA, int vertexIndexO, bool allowDuplicates = false)
			{
				if (allowDuplicates == false)
				{
					// avoid edge duplication: if there is already an edge between edge[0] and edge[1] vertices, return existing edge instead
					var existingEdgeIndex = Find.ExistingEdgeIndex(data, vertexIndexA, vertexIndexO);
					if (existingEdgeIndex != UnsetIndex)
						return existingEdgeIndex;
				}

				var edge = GMesh.Edge.Create(vertexIndexA, vertexIndexO);
				var edgeIndex = data.AddEdge(ref edge);

				// TODO: this should be handled via common method ie InsertEdge
				// set or update disk cycle and base edge 
				{
					// Vertex 0
					{
						var v0 = data.GetVertex(vertexIndexA);
						if (Hint.Unlikely(v0.BaseEdgeIndex == UnsetIndex))
						{
							v0.BaseEdgeIndex = edge.APrevEdgeIndex = edge.ANextEdgeIndex = edgeIndex;
							data.SetVertex(v0);
						}
						else
						{
							var v0BaseEdge = data.GetEdge(v0.BaseEdgeIndex);
							edge.APrevEdgeIndex = v0.BaseEdgeIndex;
							edge.ANextEdgeIndex = v0BaseEdge.GetNextEdgeIndex(vertexIndexA);

							var v0PrevEdge = data.GetEdge(edge.APrevEdgeIndex);
							v0PrevEdge.SetNextEdgeIndex(vertexIndexA, edgeIndex);
							data.SetEdge(v0PrevEdge);

							var v0NextEdge = data.GetEdge(edge.ANextEdgeIndex);
							v0NextEdge.SetPrevEdgeIndex(vertexIndexA, edgeIndex);
							data.SetEdge(v0NextEdge);

							// FIX: update prev edge vertex1's edge index of v0 and v1 base edges both point to prev edge.
							// This occurs when v0 and v1 were the first vertices to be connected with an edge.
							var prevEdgeVertex0 = data.GetVertex(v0BaseEdge.AVertexIndex);
							if (Hint.Unlikely(prevEdgeVertex0.BaseEdgeIndex == v0.BaseEdgeIndex))
							{
								v0.BaseEdgeIndex = edgeIndex;
								data.SetVertex(v0);
							}
						}
					}

					// Vertex 1
					{
						var v1 = data.GetVertex(vertexIndexO);
						if (Hint.Unlikely(v1.BaseEdgeIndex == UnsetIndex))
						{
							// Note: the very first edge between two vertices will set itself as BaseEdgeIndex on both vertices.
							// This is expected behaviour and is "fixed" when the next edge connects to V1 and detects that.
							v1.BaseEdgeIndex = edge.OPrevEdgeIndex = edge.ONextEdgeIndex = edgeIndex;
							data.SetVertex(v1);
						}
						else
						{
							var v1BaseEdge = data.GetEdge(v1.BaseEdgeIndex);
							edge.OPrevEdgeIndex = v1.BaseEdgeIndex;
							edge.ONextEdgeIndex = v1BaseEdge.GetNextEdgeIndex(vertexIndexO);

							var v1PrevEdge = data.GetEdge(edge.OPrevEdgeIndex);
							v1PrevEdge.SetNextEdgeIndex(vertexIndexO, edgeIndex);
							data.SetEdge(v1PrevEdge);

							var v1NextEdge = data.GetEdge(edge.ONextEdgeIndex);
							v1NextEdge.SetPrevEdgeIndex(vertexIndexO, edgeIndex);
							data.SetEdge(v1NextEdge);
						}
					}

					data.SetEdge(edge);
				}

				return edgeIndex;
			}

			public static void Edges(in GraphData data, in NativeArray<int> vertexIndices, ref NativeArray<int> edgeIndices)
			{
				var iterCount = vertexIndices.Length - 1;
				for (var i = 0; i < iterCount; i++)
					edgeIndices[i] = Edge(data, vertexIndices[i], vertexIndices[i + 1]);

				// last one closes the loop
				edgeIndices[iterCount] = Edge(data, vertexIndices[iterCount], vertexIndices[0]);
			}

			public static int Vertex(in GraphData data, float3 position)
			{
				var vertex = GMesh.Vertex.Create(position);
				return data.AddVertex(ref vertex);
			}

			public static void Vertices(in GraphData data, in NativeArray<float3> positions)
			{
				var vCount = positions.Length;
				for (var i = 0; i < vCount; i++)
					Vertex(data, positions[i]);
			}

			public static void Vertices(in GraphData data, in NativeArray<float3> positions, out NativeArray<int> vertexIndices,
				Allocator allocator = Allocator.TempJob)
			{
				var vCount = positions.Length;
				vertexIndices = new NativeArray<int>(vCount, allocator, NativeArrayOptions.UninitializedMemory);
				for (var i = 0; i < vCount; i++)
					vertexIndices[i] = Vertex(data, positions[i]);
			}
		}
	}
}