// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// A quad with four vertices with pivot (0,0,0) in the center of the four vertices, lying flat, facing up (+y), axis-aligned (XZ).
		/// </summary>
		/// <returns></returns>
		public static GMesh Quad(float scale = DefaultScale)
		{
			var vertices = new NativeArray<float3>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			vertices[0] = math.float3(-.5f, 0f, -.5f) * scale;
			vertices[1] = math.float3(-.5f, 0f, .5f) * scale;
			vertices[2] = math.float3(.5f, 0f, .5f) * scale;
			vertices[3] = math.float3(.5f, 0f, -.5f) * scale;
			return new GMesh(vertices, true);
		}
	}
}