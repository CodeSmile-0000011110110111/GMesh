// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Collections.Generic;
using System.Linq;
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
		public int CreateFace(IEnumerable<int> vertexIndices)
		{
			EnsureValidVertexCollection(vertexIndices);
			var vertexCount = vertexIndices.Count();
			var edgeIndices = CreateEdges(vertexIndices);
			var face = Face.Create(vertexCount);
			var faceIndex = AddFace(ref face);
			CreateLoopsInternal(faceIndex, edgeIndices, vertexIndices);

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
		public int CreateFace(IEnumerable<float3> vertexPositions)
		{
			EnsureValidVertexCollection(vertexPositions);
			return CreateFace(CreateVertices(vertexPositions));
		}

		private void EnsureValidVertexCollection<T>(IEnumerable<T> vertices)
		{
			if (vertices == null)
				throw new ArgumentNullException(nameof(vertices));

			var vertexCount = vertices.Count();
			if (vertexCount == 0)
				throw new ArgumentException("no point in creating a face with no points");
			if (vertexCount < 3)
				throw new ArgumentException($"face with only {vertexCount} vertices is technically possible but reasonably nonsensical");
		}

		/*
		/// <summary>
		/// Creates multiple faces at once, under the assumption that all faces use the same number of vertices.
		/// Faces are not connected, vertices on the same position are not merged.
		/// </summary>
		/// <param name="vertexPositions"></param>
		/// <param name="vertexCountPerFace"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public int[] CreateFaces(IEnumerable<float3> vertexPositions, int vertexCountPerFace)
		{
			if (vertexPositions == null)
				throw new ArgumentNullException(nameof(vertexPositions));
			if (vertexCountPerFace < 3)
				throw new ArgumentException("faces must have at least 3 vertices", nameof(vertexCountPerFace));

			
			var vertexCount = vertexPositions.Count();
			if (vertexCount % vertexCountPerFace != 0)
				throw new ArgumentException($"got {vertexCount} vertices which is not cleanly dividable by " +
				                            $"{vertexCountPerFace} vertices per face", nameof(vertexPositions));

			var faces = new int[vertexCount / vertexCountPerFace];
			var perFaceVertices = new float3[vertexCountPerFace];
			var faceIndex = 0;
			var vertexIndex = 0;
			
			foreach (var vertex in vertexPositions)
			{
				perFaceVertices[vertexIndex++] = vertex;
				if (vertexIndex % vertexCountPerFace == 0)
				{
					vertexIndex = 0;
					faces[faceIndex++] = CreateFace(perFaceVertices);
				}
			}

			return faces;
		}
		*/

		/// <summary>
		/// Creates a new edge using two vertex indices (must exist).
		/// 
		/// Note: This is a low-level operation. Prefer to use Euler operators or CreateFace/DeleteFace methods.
		/// Note: does not prevent creation of duplicate edges (two or more edges sharing the same vertices).
		/// </summary>
		/// <param name="vertexIndexA"></param>
		/// <param name="vertexIndexO"></param>
		/// <returns>index of the new edge</returns>
		public int CreateEdge(int vertexIndexA, int vertexIndexO)
		{
			// avoid edge duplication: if there is already an edge between edge[0] and edge[1] vertices, return existing edge instead
			var existingEdgeIndex = FindEdgeIndex(vertexIndexA, vertexIndexO);
			if (existingEdgeIndex != UnsetIndex)
				return existingEdgeIndex;

			var edge = Edge.Create(vertexIndexA, vertexIndexO);
			var edgeIndex = AddEdge(ref edge);
			CreateEdgeInternal_UpdateEdgeCycle(ref edge, vertexIndexA, vertexIndexO);
			return edgeIndex;
		}

		/// <summary>
		/// Creates multiple new edges at once forming a closed loop (ie 0=>1, 1=>2, 2=>0). Vertices must already exist.
		/// 
		/// Note: This is a low-level operation. Prefer to use Euler operators or CreateFace/DeleteFace methods.
		/// Note: does not prevent creation of duplicate edges (two or more edges sharing the same vertices).
		/// </summary>
		/// <param name="vertexIndices"></param>
		/// <returns>indices of new edges</returns>
		public int[] CreateEdges(IEnumerable<int> vertexIndices)
		{
			var vCount = vertexIndices.Count();
			var edgeIndices = new int[vCount];
			var i = 0;
			var firstIndex = UnsetIndex;
			var prevIndex = UnsetIndex;
			foreach (var vertexIndex in vertexIndices)
			{
				// skip the first
				if (firstIndex == UnsetIndex)
				{
					firstIndex = prevIndex = vertexIndex;
					continue;
				}

				edgeIndices[i++] = CreateEdge(prevIndex, vertexIndex);
				prevIndex = vertexIndex;
			}

			// close the loop
			edgeIndices[i++] = CreateEdge(prevIndex, firstIndex);

			return edgeIndices;
		}

		/// <summary>
		/// Creates a new vertex at the given position with optional normal.
		///
		/// Note: This is a low-level operation. Prefer to use Euler operators or CreateFace/DeleteFace methods.
		/// Note: It is up to the caller to set BaseEdgeIndex.
		/// </summary>
		/// <param name="position"></param>
		/// <returns>index of new vertex</returns>
		public int CreateVertex(float3 position)
		{
			var vertex = Vertex.Create(position);
			return AddVertex(ref vertex);
		}

		/// <summary>
		/// Creates several new vertices at once.
		/// 
		/// Note: This is a low-level operation. Prefer to use Euler operators or CreateFace/DeleteFace methods.
		/// Note: It is up to the caller to set BaseEdgeIndex of the new vertices.
		/// </summary>
		/// <param name="positions"></param>
		/// <returns>indices of new vertices</returns>
		public int[] CreateVertices(IEnumerable<float3> positions)
		{
			if (positions == null)
				throw new ArgumentNullException(nameof(positions));

			var i = 0;
			var vertIndices = new int[positions.Count()];
			foreach (var position in positions)
				vertIndices[i++] = CreateVertex(position);

			return vertIndices;
		}

		private void CreateEdgeInternal_UpdateEdgeCycle(ref Edge edge, int v0Index, int v1Index)
		{
			var edgeIndex = edge.Index;

			// Vertex 0
			{
				var v0 = GetVertex(v0Index);
				if (v0.BaseEdgeIndex == UnsetIndex)
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
					if (prevEdgeVertex0.BaseEdgeIndex == v0.BaseEdgeIndex)
					{
						v0.BaseEdgeIndex = edgeIndex;
						SetVertex(v0);
					}
				}
			}

			// Vertex 1
			{
				var v1 = GetVertex(v1Index);
				if (v1.BaseEdgeIndex == UnsetIndex)
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

			SetEdge(edge);
		}

		private int CreateLoopInternal(int faceIndex, int edgeIndex, int vertexIndex)
		{
			var newLoopIndex = LoopCount;
			var (prevRadialIdx, nextRadialIdx) = CreateLoopInternal_UpdateRadialLoopCycle(newLoopIndex, edgeIndex);
			var (prevLoopIdx, nextLoopIdx) = CreateLoopInternal_UpdateLoopCycle(newLoopIndex, faceIndex);
			var loop = Loop.Create(faceIndex, edgeIndex, vertexIndex, prevRadialIdx, nextRadialIdx, prevLoopIdx, nextLoopIdx);
			return AddLoop(ref loop);
		}

		private void CreateLoopsInternal(int faceIndex, int[] edgeIndices, IEnumerable<int> vertexIndices)
		{
			var edgeCount = edgeIndices.Length;
			var i = 0;
			foreach (var vertexIndex in vertexIndices)
			{
				CreateLoopInternal(faceIndex, edgeIndices[i], vertexIndex);
				i++;
			}
		}

		private (int, int) CreateLoopInternal_UpdateRadialLoopCycle(int newLoopIndex, int edgeIndex)
		{
			var prevRadialLoopIndex = newLoopIndex;
			var nextRadialLoopIndex = newLoopIndex;

			var edge = GetEdge(edgeIndex);
			if (edge.BaseLoopIndex == UnsetIndex)
				edge.BaseLoopIndex = newLoopIndex;
			else
			{
				var edgeLoop = GetLoop(edge.BaseLoopIndex);
				prevRadialLoopIndex = edgeLoop.Index;
				nextRadialLoopIndex = edgeLoop.NextRadialLoopIndex;

				if (edgeLoop.NextRadialLoopIndex == edgeLoop.Index)
					edgeLoop.PrevRadialLoopIndex = newLoopIndex;
				else
				{
					var nextRadialLoop = GetLoop(edgeLoop.NextRadialLoopIndex);
					nextRadialLoop.PrevRadialLoopIndex = newLoopIndex;
					SetLoop(nextRadialLoop);
				}

				edgeLoop.NextRadialLoopIndex = newLoopIndex;
				SetLoop(edgeLoop);
			}

			SetEdge(edge);

			return (prevRadialLoopIndex, nextRadialLoopIndex);
		}

		private (int, int) CreateLoopInternal_UpdateLoopCycle(int newLoopIndex, int faceIndex)
		{
			var prevLoopIndex = newLoopIndex;
			var nextLoopIndex = newLoopIndex;

			var face = GetFace(faceIndex);
			if (face.FirstLoopIndex == UnsetIndex)
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
				if (prevLoopIndex != nextLoopIndex)
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