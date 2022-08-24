// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		private (int, int) GetBaseEdgeDiskCycleIndices(int vertexIndex)
		{
			// get prev/next edge from vertex base edge
			var vertex = GetVertex(vertexIndex);
			if (vertex.IsValid == false)
				throw new InvalidOperationException($"vertex {vertexIndex} not valid");

			var baseEdge = GetEdge(vertex.BaseEdgeIndex);
			if (baseEdge.IsValid == false)
				throw new InvalidOperationException($"baseEdge of vertex {vertexIndex} not valid");

			return baseEdge.GetDiskCycleIndices(vertexIndex);
		}

		internal void InsertEdgeInDiskCycle(int vertexIndex, ref Edge insertEdge)
		{
			var vertex = GetVertex(vertexIndex);
			var baseEdge = GetEdge(vertex.BaseEdgeIndex);
			var nextEdgeIndex = baseEdge.GetNextEdgeIndex(vertexIndex);

			// only one edge on this vertex?
			if (nextEdgeIndex == UnsetIndex || nextEdgeIndex == baseEdge.Index)
			{
				baseEdge.SetDiskCycleIndices(vertexIndex, insertEdge.Index);
				insertEdge.SetDiskCycleIndices(vertexIndex, baseEdge.Index);
				SetEdge(baseEdge);
			}
			else
			{
				var nextEdge = GetEdge(baseEdge.GetNextEdgeIndex(vertexIndex));
				baseEdge.SetNextEdgeIndex(vertexIndex, insertEdge.Index);
				nextEdge.SetPrevEdgeIndex(vertexIndex, insertEdge.Index);
				insertEdge.SetPrevEdgeIndex(vertexIndex, baseEdge.Index);
				insertEdge.SetNextEdgeIndex(vertexIndex, nextEdge.Index);
				SetEdge(baseEdge);
				SetEdge(nextEdge);
			}
		}

		/// <summary>
		/// Removes the edge from the given vertex disk cycle.
		/// </summary>
		/// <param name="vertexIndex"></param>
		/// <param name="removeEdge"></param>
		internal void RemoveEdgeFromDiskCycle(int vertexIndex, in Edge removeEdge)
		{
			var (prevEdgeIndex, nextEdgeIndex) = removeEdge.GetDiskCycleIndices(vertexIndex);
			if (prevEdgeIndex == nextEdgeIndex)
			{
				// make sure we aren't updating the removeEdge
				if (nextEdgeIndex != removeEdge.Index)
				{
					// this is the last edge remaining on this vertex, point to the edge itself
					var otherEdge = GetEdge(prevEdgeIndex);
					otherEdge.SetDiskCycleIndices(vertexIndex, otherEdge.Index);
					SetEdge(otherEdge);
				}
			}
			else
			{
				// remove edge from the link
				var prevEdge = GetEdge(prevEdgeIndex);
				var nextEdge = GetEdge(nextEdgeIndex);
				prevEdge.SetNextEdgeIndex(vertexIndex, nextEdgeIndex);
				nextEdge.SetPrevEdgeIndex(vertexIndex, prevEdgeIndex);
				SetEdge(prevEdge);
				SetEdge(nextEdge);
			}

			UnlinkEdgeAsBaseEdge(vertexIndex, removeEdge);
		}

		/// <summary>
		/// If the given edge is the BaseEdge of the vertex, change the BaseEdge to the next edge in the disk cycle.
		/// </summary>
		/// <param name="vertexIndex"></param>
		/// <param name="removeEdge"></param>
		internal void UnlinkEdgeAsBaseEdge(int vertexIndex, in Edge removeEdge)
		{
			var vertex = GetVertex(vertexIndex);
			if (vertex.BaseEdgeIndex == removeEdge.Index)
			{
				var (prevEdgeIndex, nextEdgeIndex) = removeEdge.GetDiskCycleIndices(vertexIndex);

				// is the edge only pointing to itself? if yes, the vertex will no longer have a base edge ...
				if (prevEdgeIndex == nextEdgeIndex && nextEdgeIndex == removeEdge.Index)
					vertex.BaseEdgeIndex = UnsetIndex;
				else
					vertex.BaseEdgeIndex = nextEdgeIndex;

				SetVertex(vertex);
			}
		}
	}
}