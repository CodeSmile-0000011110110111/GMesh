// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Creates a new face using existing vertices. Adds edges and loops by using the supplied vertices.
		/// A face must have at least 3 vertices (triangle) but it can be any number of vertices.
		/// 
		/// Note: no check is performed to ensure the vertex positions all lie on the same plane. Upon triangulation
		/// (convert to Unity Mesh) such a face would not be represented as a single plane.
		/// </summary>
		/// <param name="vertexIndices">minimum of 3 vertex indices in CLOCKWISE winding order</param>
		/// <returns>the index of the new face</returns>
		public int CreateFace(IEnumerable<int> vertexIndices)
		{
			if (vertexIndices == null)
				throw new ArgumentNullException(nameof(vertexIndices));

			var vertexCount = vertexIndices.Count();
			if (vertexCount < 3)
				throw new ArgumentException($"Face must have 3 or more vertices, got only {vertexCount}", nameof(vertexIndices));

			var edgeIndices = CreateEdges(vertexIndices);
			var face = Face.Create(vertexCount);
			var faceIndex = AddFace(ref face);
			CreateLoopsInternal(faceIndex, edgeIndices);

			return faceIndex;
		}

		/// <summary>
		/// Creates a new face, along with its vertices, edges and loops by using the supplied vertex positions.
		/// A face must have at least 3 vertices (triangle) but it can be any number of vertices.
		/// 
		/// Note: no check is performed to ensure the vertex positions all lie on the same plane. Upon triangulation
		/// (convert to Unity Mesh) such a face would not be represented as a single plane.
		/// </summary>
		/// <param name="vertexPositions">minimum of 3 (triangle) vertex positions in CLOCKWISE winding order</param>
		/// <returns>the index of the new face</returns>
		public int CreateFace(IEnumerable<float3> vertexPositions) => CreateFace(CreateVertices(vertexPositions));

		/*
		/// <summary>
		/// Creates multiple faces at once, under the assumption that all faces use the same number of vertices.
		/// Faces are not connected however, it requires an extra call to MergeNearbyVertices().
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
		/// <param name="v0Index"></param>
		/// <param name="v1Index"></param>
		/// <returns>index of the new edge</returns>
		public int CreateEdge(int v0Index, int v1Index)
		{
			// avoid edge duplication: if there is already an edge between edge[0] and edge[1] vertices, return existing edge instead
			var existingEdgeIndex = FindExistingEdgeIndex(v0Index, v1Index);
			if (existingEdgeIndex != UnsetIndex)
				return existingEdgeIndex;

			var edge = Edge.Create(v0Index, v1Index);
			var edgeIndex = AddEdge(ref edge);
			CreateEdgeInternal_UpdateEdgeCycle(ref edge, v0Index, v1Index);
			return edgeIndex;
		}
		
		private void CreateEdgeInternal_UpdateEdgeCycle(ref Edge edge, int v0Index, int v1Index)
		{
			var edgeIndex = edge.Index;

			// Vertex 0
			{
				var v0 = GetVertex(v0Index);
				if (v0.BaseEdgeIndex == UnsetIndex)
				{
					v0.BaseEdgeIndex = edge.V0PrevEdgeIndex = edge.V0NextEdgeIndex = edgeIndex;
					SetVertex(v0);
				}
				else
				{
					edge.V0PrevEdgeIndex = v0.BaseEdgeIndex;
					edge.V0NextEdgeIndex = GetEdge(v0.BaseEdgeIndex).GetNextEdgeIndex(v0Index);

					var v0PrevEdge = GetEdge(edge.V0PrevEdgeIndex);
					v0PrevEdge.SetNextEdgeIndex(v0Index, edgeIndex);
					SetEdge(v0PrevEdge);

					var v0NextEdge = GetEdge(edge.V0NextEdgeIndex);
					v0NextEdge.SetPrevEdgeIndex(v0Index, edgeIndex);
					SetEdge(v0NextEdge);
				}
			}

			// Vertex 1
			{
				var v1 = GetVertex(v1Index);
				if (v1.BaseEdgeIndex == UnsetIndex)
				{
					// Note: the very first edge between two vertices will set itself as BaseEdgeIndex on both vertices
					v1.BaseEdgeIndex = edge.V1PrevEdgeIndex = edge.V1NextEdgeIndex = edgeIndex;
					SetVertex(v1);
				}
				else
				{
					edge.V1PrevEdgeIndex = v1.BaseEdgeIndex;
					edge.V1NextEdgeIndex = GetEdge(v1.BaseEdgeIndex).GetNextEdgeIndex(v1Index);

					var v1PrevEdge = GetEdge(edge.V1PrevEdgeIndex);
					v1PrevEdge.SetNextEdgeIndex(v1Index, edgeIndex);
					SetEdge(v1PrevEdge);

					var v1NextEdge = GetEdge(edge.V1NextEdgeIndex);
					v1NextEdge.SetPrevEdgeIndex(v1Index, edgeIndex);
					SetEdge(v1NextEdge);
				}
			}

			SetEdge(edge);
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
		/// <param name="normal"></param>
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

		private void CreateLoopInternal(int faceIndex, int edgeIndex)
		{
			var newLoopIndex = LoopCount;
			var (prevRadialIdx, nextRadialIdx, vertIdx) = CreateLoopInternal_UpdateRadialLoopCycle(newLoopIndex, edgeIndex);
			var (prevLoopIdx, nextLoopIdx) = CreateLoopInternal_UpdateLoopCycle(newLoopIndex, faceIndex);
			var loop = Loop.Create(faceIndex, edgeIndex, vertIdx, prevRadialIdx, nextRadialIdx, prevLoopIdx, nextLoopIdx);
			AddLoop(ref loop);
		}

		private void CreateLoopsInternal(int faceIndex, int[] edgeIndices)
		{
			var edgeCount = edgeIndices.Length;
			for (var i = 0; i < edgeCount; i++)
				CreateLoopInternal(faceIndex, edgeIndices[i]);
		}

		private (int, int, int) CreateLoopInternal_UpdateRadialLoopCycle(int newLoopIndex, int edgeIndex)
		{
			var prevRadialLoopIndex = newLoopIndex;
			var nextRadialLoopIndex = newLoopIndex;

			var edge = GetEdge(edgeIndex);
			if (edge.LoopIndex == UnsetIndex)
				edge.LoopIndex = newLoopIndex;
			else
			{
				var edgeLoop = GetLoop(edge.LoopIndex);
				prevRadialLoopIndex = edgeLoop.Index;
				nextRadialLoopIndex = edgeLoop.NextRadialLoopIndex;

				var nextRadialLoop = GetLoop(edgeLoop.NextRadialLoopIndex);
				nextRadialLoop.PrevRadialLoopIndex = newLoopIndex;
				SetLoop(nextRadialLoop);

				edgeLoop.NextRadialLoopIndex = newLoopIndex;
				SetLoop(edgeLoop);
			}

			SetEdge(edge);

			return (prevRadialLoopIndex, nextRadialLoopIndex, edge.Vertex0Index);
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