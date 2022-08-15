// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Returns index of edge that connects the two vertices.
		/// </summary>
		/// <param name="vertex0Index"></param>
		/// <param name="vertex1Index"></param>
		/// <returns></returns>
		public int FindEdgeIndex(int vertex0Index, int vertex1Index)
		{
			if (vertex0Index == vertex1Index)
				throw new ArgumentException($"vertex indices are the same: {vertex0Index}, {vertex1Index}");
			if (vertex0Index < 0 || vertex1Index < 0)
				throw new ArgumentException($"vertex index cannot be negative: {vertex0Index}, {vertex1Index}");

			var v0 = GetVertex(vertex0Index);
			var v1 = GetVertex(vertex1Index);
			if (v0.BaseEdgeIndex == UnsetIndex || v1.BaseEdgeIndex == UnsetIndex)
				return UnsetIndex;

			var edge0 = GetEdge(v0.BaseEdgeIndex);
			var edge1 = GetEdge(v1.BaseEdgeIndex);

			do
			{
				if (edge0.IsAttachedToVertex(vertex1Index))
					return edge0.Index;
				if (edge1.IsAttachedToVertex(vertex0Index))
					return edge1.Index;

				edge0 = GetEdge(edge0.GetNextRadialEdgeIndex(vertex0Index));
				edge1 = GetEdge(edge1.GetNextRadialEdgeIndex(vertex1Index));
			} while (edge0.Index != v0.BaseEdgeIndex && edge1.Index != v1.BaseEdgeIndex);

			return UnsetIndex;
		}

		/**
     * Return an edge that links vert1 to vert2 in the mesh (an arbitrary one
     * if there are several such edges, which is possible with this structure).
     * Return null if there is no edge between vert1 and vert2 in the mesh.
     */
		/*
		public Edge FindEdge(Vertex vert1, Vertex vert2)
		{
			Debug.Assert(vert1 != vert2);
			if (vert1.edge == null || vert2.edge == null) return null;

			var e1 = vert1.edge;
			var e2 = vert2.edge;
			do
			{
				if (e1.ContainsVertex(vert2)) return e1;
				if (e2.ContainsVertex(vert1)) return e2;

				e1 = e1.Next(vert1);
				e2 = e2.Next(vert2);
			} while (e1 != vert1.edge && e2 != vert2.edge);
			return null;
		}
		*/
	}
}