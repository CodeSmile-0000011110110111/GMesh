// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Collections;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// A "diamond" is a polygon with 4 vertices (quad) which is rotated by 45°, ie its pointy edges align with XZ axis.
		/// Pivot (0,0,0) at the center, lying flat, facing up (+y), pointy tip up (+z).
		/// </summary>
		/// <returns></returns>
		public static GMesh Diamond(float scale = DefaultScale) => Polygon(4, scale);

		/// <summary>
		/// A pentagon with five vertices, Pivot (0,0,0) at the center, lying flat, facing up (+y), pointy tip up (+z).
		/// </summary>
		/// <returns></returns>
		public static GMesh Pentagon(float scale = DefaultScale) => Polygon(5, scale);

		/// <summary>
		/// A hexagon with six vertices, Pivot (0,0,0) at the center, lying flat, facing up (+y), pointy tip up (+z).
		/// </summary>
		/// <returns></returns>
		public static GMesh Hexagon(float scale = DefaultScale) => Polygon(6, scale);

		/// <summary>
		/// A polygon with the given number of vertices and scale, where first point is at (0,0,scale) and other vertices
		/// go around in clockwise order.
		/// </summary>
		/// <param name="vertexCount"></param>
		/// <param name="scale"></param>
		/// <returns></returns>
		public static GMesh Polygon(int vertexCount = 3, float scale = DefaultScale)
		{
			Calculate.RadialPolygonVertices(vertexCount, scale, out var vertices, Allocator.Temp);
			var gMesh = new GMesh(vertices);
			vertices.Dispose();
			return gMesh;
		}
	}
}