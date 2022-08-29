// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
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
	}
}