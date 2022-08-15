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
		/// Creates a new face, along with its vertices, edges and loops by using the supplied vertex positions.
		/// A face must have at least 3 vertices (triangle) but it can be any number of vertices.
		/// 
		/// Note: no check is performed to ensure the vertex positions all lie on the same plane. Upon triangulation
		/// (convert to Unity Mesh) such a face would not be represented as a single plane.
		/// </summary>
		/// <param name="vertexPositions">minimum of 3 (triangle) vertex positions in CLOCKWISE winding order</param>
		/// <returns>the index of the new face</returns>
		public int CreateFace(IEnumerable<float3> vertexPositions)
		{
			if (vertexPositions == null)
				throw new ArgumentNullException(nameof(vertexPositions));

			var vertexCount = vertexPositions.Count();
			if (vertexCount < 3)
				throw new ArgumentException($"Face must have 3 or more vertices, got only {vertexCount}", nameof(vertexPositions));

			var vertIndices = CreateVertices(vertexPositions);
			var edgeIndices = CreateEdges(vertIndices);
			var faceIndex = AddFace(Face.Create(vertexCount));
			CreateLoopsInternal(faceIndex, edgeIndices);

			return faceIndex;
		}

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
			var edgeIndex = AddEdge(Edge.Create(v0Index, v1Index));
			CreateEdgeInternal_UpdateVertexCycle(edgeIndex, v0Index, v1Index);
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
		public int[] CreateEdges(int[] vertexIndices)
		{
			var vCount = VertexCount;
			var edgeIndices = new int[vCount];
			var prevVertIndex = vCount - 1;
			for (var i = 0; i < vCount; i++)
			{
				edgeIndices[i] = CreateEdge(vertexIndices[prevVertIndex], vertexIndices[i]);
				prevVertIndex = i;
			}

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
		public int CreateVertex(float3 position) => AddVertex(Vertex.Create(position));

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
			var i = 0;
			var vertIndices = new int[positions.Count()];
			foreach (var position in positions)
				vertIndices[i++] = CreateVertex(position);

			return vertIndices;
		}

		private void CreateLoopInternal(int faceIndex, int edgeIndex)
		{
			var newLoopIndex = LoopCount;
			var (prevRadialIdx, nextRadialIdx, vertIdx) = CreateLoopInternal_UpdateEdge(newLoopIndex, edgeIndex);
			var (prevLoopIdx, nextLoopIdx) = CreateLoopInternal_UpdateFace(newLoopIndex, faceIndex);
			AddLoop(Loop.Create(faceIndex, edgeIndex, vertIdx, prevRadialIdx, nextRadialIdx, prevLoopIdx, nextLoopIdx));
		}

		private void CreateLoopsInternal(int faceIndex, int[] edgeIndices)
		{
			var edgeCount = EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				CreateLoopInternal(faceIndex, edgeIndices[i]);
		}

		private (int, int, int) CreateLoopInternal_UpdateEdge(int newLoopIndex, int edgeIndex)
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

		private (int, int) CreateLoopInternal_UpdateFace(int newLoopIndex, int faceIndex)
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
				prevLoopIndex = firstLoop.Index;
				nextLoopIndex = firstLoop.NextLoopIndex;

				var nextLoop = GetLoop(nextLoopIndex);
				nextLoop.PrevLoopIndex = newLoopIndex;

				// update nextLoop or re-assign it as firstLoop, depends on whether they are the same
				if (prevLoopIndex != nextLoopIndex)
					SetLoop(nextLoop);
				else
					firstLoop = nextLoop;

				firstLoop.NextLoopIndex = newLoopIndex;
				SetLoop(firstLoop);
			}

			return (prevLoopIndex, nextLoopIndex);
		}

		private void CreateEdgeInternal_UpdateVertexCycle(int edgeIndex, int v0Index, int v1Index)
		{
			var edge = GetEdge(edgeIndex);

			{
				var v0 = GetVertex(v0Index);
				if (v0.BaseEdgeIndex == UnsetIndex)
				{
					v0.BaseEdgeIndex = edge.V0PrevRadialEdgeIndex = edge.V0NextRadialEdgeIndex = edgeIndex;
					SetVertex(v0);
				}
				else
				{
					edge.V0PrevRadialEdgeIndex = v0.BaseEdgeIndex;
					edge.V0NextRadialEdgeIndex = GetEdge(v0.BaseEdgeIndex).GetNextRadialEdgeIndex(v0Index);

					var v0PrevEdge = GetEdge(edge.V0PrevRadialEdgeIndex);
					v0PrevEdge.SetNextRadialEdgeIndex(v0Index, edgeIndex);
					SetEdge(v0PrevEdge);

					var v0NextEdge = GetEdge(edge.V0NextRadialEdgeIndex);
					v0NextEdge.SetPrevRadialEdgeIndex(v0Index, edgeIndex);
					SetEdge(v0NextEdge);
				}
			}

			{
				var v1 = GetVertex(v1Index);
				if (v1.BaseEdgeIndex == UnsetIndex)
				{
					edge.V1PrevRadialEdgeIndex = edge.V1NextRadialEdgeIndex = edgeIndex;

					// Test: this should be updated with the next edge
					/*
					v1.BaseEdgeIndex = edgeIndex;
					SetVertex(v1);
					*/
				}
				else
				{
					edge.V1PrevRadialEdgeIndex = v1.BaseEdgeIndex;
					edge.V1NextRadialEdgeIndex = GetEdge(v1.BaseEdgeIndex).GetNextRadialEdgeIndex(v1Index);

					var v1PrevEdge = GetEdge(edge.V1PrevRadialEdgeIndex);
					v1PrevEdge.SetNextRadialEdgeIndex(v1Index, edgeIndex);
					SetEdge(v1PrevEdge);

					var v1NextEdge = GetEdge(edge.V1NextRadialEdgeIndex);
					v1NextEdge.SetPrevRadialEdgeIndex(v1Index, edgeIndex);
					SetEdge(v1NextEdge);
				}
			}

			SetEdge(edge);
		}
	}
}