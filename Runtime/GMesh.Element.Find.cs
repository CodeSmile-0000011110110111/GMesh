// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Burst;
using Unity.Burst.CompilerServices;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Tries to find an existing edge connecting the given vertices of the input edge.
		/// </summary>
		/// <param name="edge">Edge to check for, will be assigned to existing one.</param>
		/// <returns>True if edge exists between v0 and v1. False if there is no edge connecting the two vertices.</returns>
		public int FindExistingEdgeIndex(in Edge edge) => Find.ExistingEdgeIndex(_data, edge.AVertexIndex, edge.OVertexIndex);

		/// <summary>
		/// Tries to find an existing edge connecting the two vertices (via their indices).
		/// </summary>
		/// <param name="v0Index">Index of one vertex</param>
		/// <param name="v1Index">Index of another vertex</param>
		/// <returns>True if edge exists between v0 and v1. False if there is no edge connecting the two vertices.</returns>
		public int FindExistingEdgeIndex(int v0Index, int v1Index) => Find.ExistingEdgeIndex(_data, v0Index, v1Index);

		[BurstCompile]
		internal readonly struct Find
		{
			public static int ExistingEdgeIndex(in GraphData data, int v0Index, int v1Index)
			{
#if GMESH_VALIDATION
				// TODO: validate disk cycle to prevent infinite loop
#endif

				// check all edges in cycle, return this edge's index if it points to v1
				var edgeIndex = data.GetVertex(v0Index).BaseEdgeIndex;
				if (edgeIndex == UnsetIndex)
					return UnsetIndex;

				var edge = data.GetEdge(edgeIndex);
				do
				{
					if (edge.ContainsVertex(v1Index))
						return edge.Index;

					edge = data.GetEdge(edge.GetNextEdgeIndex(v0Index));
				} while (Hint.Likely(edge.Index != edgeIndex));

				return UnsetIndex;
			}
		}
	}
}