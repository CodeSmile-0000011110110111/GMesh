// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// A right triangle with vertices (0,0,0) - (0,0,1) - (1,0,0) lying flat, facing up (positive Y), pivot at right triangle.
		/// </summary>
		/// <param name="scale"></param>
		/// <returns></returns>
		public static GMesh Triangle(float scale = DefaultScale) => Triangle(float3.zero, scale);

		/// <summary>
		/// A right triangle with lying flat, facing up (positive Y), pivot depends on translation.
		/// </summary>
		/// <param name="translation"></param>
		/// <param name="scale"></param>
		/// <returns></returns>
		public static GMesh Triangle(float3 translation, float scale = DefaultScale)
		{
			var vertices = new NativeArray<float3>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			vertices[0] = translation * scale;
			vertices[1] = (math.forward() + translation) * scale;
			vertices[2] = (math.right() + translation) * scale;
			return new GMesh(vertices, true);
		}
	}
}