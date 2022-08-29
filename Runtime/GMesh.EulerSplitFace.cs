// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
	
		/*
		 * SPLIT FACE MAKE EDGE:
		 * Takes as input two vertices in a single face. An edge is created which divides
		 * the original face into two distinct regions. One of the regions is assigned to
		 * the original face and it is closed off. The second region has a new face assigned to it.
		 * Note that if the input vertices share an edge this will create a face with only two edges.
		 * Returns - new Face and new Edge indices
		 */
		public (int, int) SplitFaceAndCreateEdge(int faceIndex) => throw new NotImplementedException();
	}
}