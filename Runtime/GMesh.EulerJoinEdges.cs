// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
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
		public bool JoinEdgeAndDeleteVertex(int joinEdgeIndex, int deleteVertexIndex)
		{
			var joinEdge = GetEdge(joinEdgeIndex);
			var deleteVertex = GetVertex(deleteVertexIndex);
			return JoinEdgeAndDeleteVertex(ref joinEdge, ref deleteVertex);
		}

		public bool JoinEdgeAndDeleteVertex(ref Edge removeEdge, ref Vertex deleteVertex)
		{
			if (removeEdge.IsValid == false || removeEdge.ContainsVertex(deleteVertex.Index) == false ||
			    deleteVertex.IsValid == false || CalculateEdgeCount(deleteVertex) != 2)
				return false;

			// remove edge from disk cycles of A and O
			//var vertexA = GetVertex(removeEdge.AVertexIndex);
			//var vertexO = GetVertex(removeEdge.OVertexIndex);
			RemoveEdgeFromDiskCycle(removeEdge.AVertexIndex, removeEdge);
			RemoveEdgeFromDiskCycle(removeEdge.OVertexIndex, removeEdge);

			// relink edge
			// join loops

			return true;
		}
	}
}