// Copyright (C) 2021-2022 Steffen Itterheim
// Usage is bound to the Unity Asset Store Terms of Service and EULA: https://unity3d.com/legal/as_terms

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		internal const int UnsetIndex = -1;
		internal const float GridSize = 0.001f; // round all positions to 1mm grid
		internal const float InvGridSize = 1f / GridSize; // inverse of grid size (eg 0.001 => 1000)

		private NativeList<Vertex> _vertices = new(Allocator.Persistent);
		private NativeList<Edge> _edges = new(Allocator.Persistent);
		private NativeList<Loop> _loops = new(Allocator.Persistent);
		private NativeList<Face> _faces = new(Allocator.Persistent);

		public int VertexCount => _vertices.Length;
		public int EdgeCount => _edges.Length;
		public int LoopCount => _loops.Length;
		public int FaceCount => _faces.Length;
		public Vertex GetVertex(int index) => _vertices[index];
		public Edge GetEdge(int index) => _edges[index];
		public Loop GetLoop(int index) => _loops[index];
		public Face GetFace(int index) => _faces[index];

		// TODO: index validations!
		public void SetVertex(in Vertex v) => _vertices[v.Index] = v;
		public void SetEdge(in Edge e) => _edges[e.Index] = e;
		public void SetLoop(in Loop l) => _loops[l.Index] = l;
		public void SetFace(in Face f) => _faces[f.Index] = f;

		public int FindEdgeIndex(int vertex0Index, int vertex1Index) => throw new NotImplementedException();

		// low-level API (Euler operators):

		/*
		 * SPLIT EDGE MAKE VERT:
		 * Takes a given edge and splits it into two, creating a new vert.
		 * The original edge, OE, is relinked to be between V1 and NV.
		 * OE is then moved from V2's disk cycle to NV's.
		 * The new edge, NE, is linked to be between NV and V2 and added to both vertices disk cycles.
		 * Finally the radial cycle of OE is traversed, splitting faceloop it encounters.
		 * Returns: index of new edge
		 */
		public int SplitEdgeAndCreateVertex(int edgeIndex, float3 pos) => throw new NotImplementedException();

		/*
		 * JOIN EDGE KILL VERT:
		 * Takes a pointer to an edge (ke) and pointer to one of its vertices (kv) and collapses
		 * the edge on that vertex. First ke is removed from the disk cycle of both kv and tv.
		 * Then the edge oe is relinked to run between ov and tv and is added to the disk cycle of ov.
		 * Finally the radial cycle of oe is traversed and all of its face loops are updated.
		 * Note that in order for this euler to work, kv must have exactly only two edges coincident
		 * upon it (valance of 2).
		 * A more generalized edge collapse function can be built using a combination of
		 * split_face_make_edge, join_face_kill_edge and join_edge_kill_vert.
		 * Returns true for success
		 */
		public bool JoinEdgesAndDeleteVertex(int edge0Index, int edge1Index) => throw new NotImplementedException();

		/*
		 * SPLIT FACE MAKE EDGE:
		 * Takes as input two vertices in a single face. An edge is created which divides
		 * the original face into two distinct regions. One of the regions is assigned to
		 * the original face and it is closed off. The second region has a new face assigned to it.
		 * Note that if the input vertices share an edge this will create a face with only two edges.
		 * Returns - new Face and new Edge indices
		 */
		public (int, int) SplitFaceAndCreateEdge(int faceIndex) => throw new NotImplementedException();

		/*
		 * JOIN FACE KILL EDGE:
		 * Takes two faces joined by a single 2-manifold edge and fuses them together.
		 * The edge shared by the faces must not be connected to any other edges which have
		 * both faces in its radial cycle.
		 * An illustration of this appears in the figure on the right. In this diagram,
		 * situation A is the only one in which join_face_kill_edge will return with a value
		 * indicating success. If the tool author wants to join two seperate faces which have
		 * multiple edges joining them as in situation B they should run JEKV on the excess
		 * edge(s) first. In the case of situation none of the edges joining the two faces
		 * can be safely removed because it would cause a face that loops back on itself.
		 * Also note that the order of arguments decides whether or not certain per-face
		 * attributes are present in the resultant face. For instance vertex winding,
		 * material index, smooth flags, ect are inherited from f1, not f2.
		 * Returns - true for success
		 */
		public bool JoinFacesAndDeleteEdge(int face0Index, int face1Index) => throw new NotImplementedException();

		/*
		 * Flip face by reversing its loops.
		 */
		public void FlipFace(int faceIndex) => throw new NotImplementedException();

		/// <summary>
		/// Creates a new face, its vertices, edges and loops by using the supplied vertex positions.
		/// Adds a face and vertexPosition.Count vertices, edges and loops to the mesh.
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

			var faceIndex = _faces.Length;
			var face = Face.Create(faceIndex, vertexCount);
			_faces.Add(face);

			CreateLoops(faceIndex, edgeIndices);

			return faceIndex;
		}

		/// <summary>
		/// Creates a new vertex at the given position with optional normal.
		/// Note: It is up to the caller to set FirstEdgeIndex.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="normal"></param>
		/// <returns>index of new vertex</returns>
		public int CreateVertex(float3 position, float3 normal = default)
		{
			var vertIndex = _vertices.Length;
			var vertex = Vertex.Create(vertIndex, UnsetIndex, position, normal);
			_vertices.Add(vertex);
			return vertIndex;
		}

		/// <summary>
		/// Creates several new vertices at once.
		/// Note: It is up to the caller to set FirstEdgeIndex of the new vertices.
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

		/// <summary>
		/// Creates a new edge using two vertex indices (must exist).
		/// Note: does not prevent creation of duplicate edges (two or more edges sharing the same vertices).
		/// </summary>
		/// <param name="v0Index"></param>
		/// <param name="v1Index"></param>
		/// <returns>index of the new edge</returns>
		public int CreateEdge(int v0Index, int v1Index)
		{
			var edgeIndex = _edges.Length;
			var edge = Edge.Create(edgeIndex, v0Index, v1Index);
			_edges.Add(edge);

			// only set this on first vertex, since the second will be called next
			// if not, an open edge can be determined if the v1 vertex has an unset FirstEdgeIndex
			SetVertexFirstEdgeIndexIfUnset(v0Index, edgeIndex);
			return edgeIndex;
		}

		/// <summary>
		/// Creates multiple new edges at once forming a closed loop (ie 0=>1, 1=>2, 2=>0). Vertices must already exist.
		/// Note: does not prevent creation of duplicate edges (two or more edges sharing the same vertices).
		/// </summary>
		/// <param name="vertexIndices"></param>
		/// <returns>indices of new edges</returns>
		public int[] CreateEdges(int[] vertexIndices)
		{
			var vertexCount = vertexIndices.Length;
			var edgeIndices = new int[vertexCount];
			var prevVertIndex = vertexCount - 1;
			for (var i = 0; i < vertexCount; i++)
			{
				edgeIndices[prevVertIndex] = CreateEdge(vertexIndices[prevVertIndex], vertexIndices[i]);
				prevVertIndex = i;
			}

			return edgeIndices;
		}

		// Internal use only.
		private int CreateLoop(int faceIndex, int edgeIndex)
		{
			var loopIndex = _loops.Length;
			var vertIndex = UnsetIndex;

			// update Edge
			int prevRadialLoopIndex = loopIndex, nextRadialLoopIndex = loopIndex;
			{
				var edge = GetEdge(edgeIndex);
				vertIndex = edge.Vertex0Index;

				if (edge.LoopIndex == UnsetIndex)
					edge.LoopIndex = loopIndex;
				else
				{
					var edgeLoop = GetLoop(edge.LoopIndex);
					prevRadialLoopIndex = edgeLoop.Index;
					nextRadialLoopIndex = edgeLoop.NextRadialLoopIndex;

					var nextRadialLoop = GetLoop(edgeLoop.NextRadialLoopIndex);
					nextRadialLoop.PrevRadialLoopIndex = loopIndex;
					SetLoop(nextRadialLoop);

					edgeLoop.NextRadialLoopIndex = loopIndex;
					SetLoop(edgeLoop);
				}

				SetEdge(edge);
			}

			// update Face
			int prevLoopIndex = loopIndex, nextLoopIndex = loopIndex;
			{
				var face = GetFace(faceIndex);

				if (face.FirstLoopIndex == UnsetIndex)
				{
					face.FirstLoopIndex = loopIndex;
					SetFace(face);
				}
				else
				{
					var firstLoop = GetLoop(face.FirstLoopIndex);
					prevLoopIndex = firstLoop.Index;
					nextLoopIndex = firstLoop.NextLoopIndex;

					var nextLoop = GetLoop(nextLoopIndex);
					nextLoop.PrevLoopIndex = loopIndex;

					// update or assign, depends on whether these two loops are the same
					if (prevLoopIndex != nextLoopIndex)
						SetLoop(nextLoop);
					else
						firstLoop = nextLoop;

					firstLoop.NextLoopIndex = loopIndex;
					SetLoop(firstLoop);
				}
			}

			var loop = Loop.Create(loopIndex, faceIndex, edgeIndex, vertIndex,
				prevRadialLoopIndex, nextRadialLoopIndex, prevLoopIndex, nextLoopIndex);
			_loops.Add(loop);
			return loopIndex;
		}

		// Internal use only.
		private void CreateLoops(int faceIndex, int[] edgeIndices)
		{
			var edgeCount = edgeIndices.Length;
			for (var i = 0; i < edgeCount; i++)
				CreateLoop(faceIndex, edgeIndices[i]);
		}

		private void DeleteVertex() => throw new NotImplementedException();
		private void DeleteEdge() => throw new NotImplementedException();
		private void DeleteLoop() => throw new NotImplementedException();
		private void DeleteFace() => throw new NotImplementedException();

		/// <summary>
		/// Sets FirstEdgeIndex of vertex indiscriminately.
		/// Prefer to use SetVertexFirstEdgeIndexIfUnset() to prevent invalidating FirstEdgeIndex unnecessarily.
		/// </summary>
		/// <param name="vertexIndex"></param>
		/// <param name="firstEdgeIndex"></param>
		public void SetVertexFirstEdgeIndex(int vertexIndex, int firstEdgeIndex)
		{
			var vertex = GetVertex(vertexIndex);
			vertex.FirstEdgeIndex = firstEdgeIndex;
			SetVertex(vertex);
		}

		/// <summary>
		/// Sets FirstEdgeIndex of vertex but only if it hasn't been set before.
		/// </summary>
		/// <param name="vertexIndex"></param>
		/// <param name="firstEdgeIndex"></param>
		public void SetVertexFirstEdgeIndexIfUnset(int vertexIndex, int firstEdgeIndex)
		{
			var vertex = GetVertex(vertexIndex);
			if (vertex.FirstEdgeIndex == UnsetIndex)
				vertex.FirstEdgeIndex = firstEdgeIndex;
			SetVertex(vertex);
		}

		/// <summary>
		/// Dump all elements for debugging purposes.
		/// </summary>
		public void DebugLogAllElements()
		{
			for (var i = 0; i < FaceCount; i++)
				Debug.Log(GetFace(i));
			for (var i = 0; i < LoopCount; i++)
				Debug.Log(GetLoop(i));
			for (var i = 0; i < EdgeCount; i++)
				Debug.Log(GetEdge(i));
			for (var i = 0; i < VertexCount; i++)
				Debug.Log(GetVertex(i));
		}
	}
}