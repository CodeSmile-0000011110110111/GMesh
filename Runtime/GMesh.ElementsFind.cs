// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		/*
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
				if (edge0.IsConnectedToVertex(vertex1Index))
					return edge0.Index;
				if (edge1.IsConnectedToVertex(vertex0Index))
					return edge1.Index;

				edge0 = GetEdge(edge0.GetNextRadialEdgeIndex(vertex0Index));
				edge1 = GetEdge(edge1.GetNextRadialEdgeIndex(vertex1Index));
			} while (edge0.Index != v0.BaseEdgeIndex && edge1.Index != v1.BaseEdgeIndex);

			return UnsetIndex;
		}
		*/

		/// <summary>
		/// Tries to find an existing edge connecting the given vertices of the input edge.
		/// </summary>
		/// <param name="edge">Edge to check for, will be assigned to existing one.</param>
		/// <returns>True if edge exists between v0 and v1. False if there is no edge connecting the two vertices.</returns>
		public int FindExistingEdgeIndex(in Edge edge) => FindExistingEdgeIndex(edge.Vertex0Index, edge.Vertex1Index);
		
		/// <summary>
		/// Tries to find an existing edge connecting the two vertices (via their indices).
		/// </summary>
		/// <param name="v0Index">Index of one vertex</param>
		/// <param name="v1Index">Index of another vertex</param>
		/// <returns>True if edge exists between v0 and v1. False if there is no edge connecting the two vertices.</returns>
		public int FindExistingEdgeIndex(int v0Index, int v1Index)
		{
			// check all edges in cycle, return this edge's index if it points to v1
			var existingEdgeIndex = UnsetIndex;
			ForEachEdge(v0Index, e =>
			{
				if (e.IsConnectingVertices(v0Index, v1Index))
				{
					existingEdgeIndex = e.Index;
					return true;
				}

				return false;
			});

			if (existingEdgeIndex != UnsetIndex)
				return existingEdgeIndex;

			ForEachEdge(v1Index, e =>
			{
				if (e.IsConnectingVertices(v0Index, v1Index))
				{
					existingEdgeIndex = e.Index;
					return true;
				}

				return false;
			});

			return existingEdgeIndex;
		}

	}
}