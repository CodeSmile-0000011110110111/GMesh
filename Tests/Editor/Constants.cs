// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Mathematics;

namespace Tests.Editor
{
	public static class Constants
	{
		public static readonly float3[] TriangleVertices = { new(0f, 0f, 0f), new(1f, .1f, 1f), new(2f, 2f, 2f) };
		public static readonly float3[] TriangleVertices2 = { new(0f, 0f, 0f), new(-1f, -.1f, -1f), new(-2f, -2f, -2f) };
		public static readonly float3[] QuadVertices = { new(0f, 0f, 0f), new(1f, .1f, 1f), new(2f, .2f, 2f), new(3f, 3f, 3f) };
		public static readonly float3[] PentagonVertices =
			{ new(0f, 0f, 0f), new(1f, .1f, 1f), new(2f, .2f, 2f), new(3f, .3f, 3f), new(4f, 4f, 4f) };
		public static readonly float3[] HexagonVertices =
			{ new(0f, 0f, 0f), new(1f, .1f, 1f), new(2f, .2f, 2f), new(3f, .3f, 3f), new(4f, .4f, 4f), new(5f, 5f, 5f) };
	}
}