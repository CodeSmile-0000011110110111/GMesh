// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Burst;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Removes the edge from the given vertex disk cycle.
		/// </summary>
		/// <param name="vertexIndex"></param>
		/// <param name="removeEdge"></param>
		internal void RemoveEdgeFromDiskCycle(int vertexIndex, in Edge removeEdge) => Remove.EdgeFromDiskCycle(_data, vertexIndex, removeEdge);

		[BurstCompile]
		private readonly struct Remove
		{
			public static void EdgeFromDiskCycle(in GraphData data, int vertexIndex, in Edge removeEdge)
			{
				var (prevEdgeIndex, nextEdgeIndex) = removeEdge.GetDiskCycleIndices(vertexIndex);
				if (prevEdgeIndex == nextEdgeIndex)
				{
					// make sure we aren't updating the removeEdge
					if (nextEdgeIndex != removeEdge.Index)
					{
						// this is the last edge remaining on this vertex, point to the edge itself
						var otherEdge = data.GetEdge(prevEdgeIndex);
						otherEdge.SetDiskCycleIndices(vertexIndex, otherEdge.Index);
						data.SetEdge(otherEdge);
					}
				}
				else
				{
					// remove edge from the link
					var prevEdge = data.GetEdge(prevEdgeIndex);
					var nextEdge = data.GetEdge(nextEdgeIndex);
					prevEdge.SetNextEdgeIndex(vertexIndex, nextEdgeIndex);
					nextEdge.SetPrevEdgeIndex(vertexIndex, prevEdgeIndex);
					data.SetEdge(prevEdge);
					data.SetEdge(nextEdge);
				}

				UnlinkAsVertexBaseEdge(data, vertexIndex, removeEdge);
			}

			/// <summary>
			/// If the given edge is the BaseEdge of the vertex, change the BaseEdge to the next edge in the disk cycle.
			/// </summary>
			/// <param name="vertexIndex"></param>
			/// <param name="removeEdge"></param>
			private static void UnlinkAsVertexBaseEdge(in GraphData data, int vertexIndex, in Edge removeEdge)
			{
				var vertex = data.GetVertex(vertexIndex);
				if (vertex.BaseEdgeIndex == removeEdge.Index)
				{
					var (prevEdgeIndex, nextEdgeIndex) = removeEdge.GetDiskCycleIndices(vertexIndex);

					// is the edge only pointing to itself? if yes, the vertex will no longer have a base edge ...
					if (prevEdgeIndex == nextEdgeIndex && nextEdgeIndex == removeEdge.Index)
						vertex.BaseEdgeIndex = UnsetIndex;
					else
						vertex.BaseEdgeIndex = nextEdgeIndex;

					data.SetVertex(vertex);
				}
			}
		}
	}
}