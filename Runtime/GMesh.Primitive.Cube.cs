// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// A configurable 1m sized cube made out of 6 planes and shared vertices with Pivot (0,0,0) in the center.
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static GMesh Cube(GMeshCube parameters)
		{
			var vertexCount = math.int3(parameters.VertexCountX, parameters.VertexCountY, parameters.VertexCountZ);
			var b = Plane(new GMeshPlane(vertexCount.xy, math.float3(0f, 0f, -0.5f), math.float3(0f, 0f, 0f)));
			var f = Plane(new GMeshPlane(vertexCount.xy, math.float3(0f, 0f, 0.5f), math.float3(0f, 180f, 0f)));
			var l = Plane(new GMeshPlane(vertexCount.zy, math.float3(-0.5f, 0f, 0f), math.float3(0f, 90f, 0f)));
			var r = Plane(new GMeshPlane(vertexCount.zy, math.float3(0.5f, 0f, 0f), math.float3(0f, 270f, 0f)));
			var u = Plane(new GMeshPlane(vertexCount.xz, math.float3(0f, 0.5f, 0f), math.float3(90f, 270f, 270f)));
			var d = Plane(new GMeshPlane(vertexCount.xz, math.float3(0f, -0.5f, 0f), math.float3(270f, 90f, 90f)));
			return Combine(new[] { b, f, l, r, u, d }, true);
		}
	}
}