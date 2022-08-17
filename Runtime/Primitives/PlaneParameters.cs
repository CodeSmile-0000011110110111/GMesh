// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public struct PlaneParameters : IEqualityComparer<PlaneParameters>
	{
		public static readonly float2 DefaultScale = new(1f);
		public static readonly quaternion DefaultRotation = quaternion.Euler(new float3(math.radians(90f), 0f, 0f));

		public int2 VertexCount;
		public float3 Translation;
		public quaternion Rotation;
		public float2 Scale;

		public PlaneParameters(int2 vertexCount)
			: this(vertexCount, float3.zero, DefaultRotation, DefaultScale) {}

		public PlaneParameters(int2 vertexCount, float2 scale)
			: this(vertexCount, float3.zero, DefaultRotation, scale) {}

		public PlaneParameters(int2 vertexCount, float3 translation)
			: this(vertexCount, translation, DefaultRotation, DefaultScale) {}

		public PlaneParameters(int2 vertexCount, float3 translation, quaternion rotation)
			: this(vertexCount, translation, rotation, DefaultScale) {}

		public PlaneParameters(int2 vertexCount, float3 translation, quaternion rotation, float2 scale)
		{
			if (vertexCount.x < 2 || vertexCount.y < 2)
				throw new ArgumentException("minimum of 2 vertices per axis required", nameof(vertexCount));

			VertexCount = vertexCount;
			Translation = translation;
			Rotation = rotation;
			Scale = scale;
		}

		public bool Equals(PlaneParameters x, PlaneParameters y) => x.GetHashCode() == y.GetHashCode();

		public int GetHashCode(PlaneParameters obj) =>
			VertexCount.GetHashCode() ^ Translation.GetHashCode() ^ Rotation.GetHashCode() ^ Scale.GetHashCode();
	}
}