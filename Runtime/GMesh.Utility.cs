// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		private void InsertLoopAfter(ref Loop existingLoop, int newLoopIndex)
		{
			var nextLoopIndex = existingLoop.NextLoopIndex;
			existingLoop.NextLoopIndex = newLoopIndex;

			var nextLoop = GetLoop(nextLoopIndex);
			nextLoop.PrevLoopIndex = newLoopIndex;
			SetLoop(nextLoop);
		}
		
		private void IncrementFaceElementCount(int faceIndex)
		{
			var face = GetFace(faceIndex);
			face.ElementCount++;
			SetFace(face);
		}

		private void GetVerticesPreferBaseEdgeVertex(in Edge edge, out Vertex baseVertex, out Vertex otherVertex)
		{
			var vertexA = GetVertex(edge.AVertexIndex);
			var vertexO = GetVertex(edge.OVertexIndex);
			baseVertex = edge.Index == vertexA.BaseEdgeIndex ? vertexA : vertexO;
			otherVertex = baseVertex.Index == vertexO.Index ? vertexA : vertexO;
		}

		private (int, int) GetBaseEdgeDiskCycleIndices(int vertexIndex)
		{
			// get prev/next edge from vertex base edge
			var vertex = GetVertex(vertexIndex);
			var baseEdge = GetEdge(vertex.BaseEdgeIndex);
			return baseEdge.GetDiskCycleIndices(vertexIndex);
		}
	}
}