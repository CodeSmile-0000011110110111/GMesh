// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Burst;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
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
	}
}