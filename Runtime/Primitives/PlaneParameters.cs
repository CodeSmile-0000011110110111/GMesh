// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
{
	[Serializable]
	public class PlaneParameters : IEqualityComparer<PlaneParameters>
	{
		public const int MinVertexCount = 2; 
		public const int MaxVertexCount = 11; 
		public static readonly float2 DefaultScale = new(1f);
		public static readonly float3 DefaultRotation = new(90f, 0f, 0f);

		public bool ResetToDefaults;
		[Range(MinVertexCount, MaxVertexCount)] public int VertexCountX = MinVertexCount;
		[Range(MinVertexCount, MaxVertexCount)] public int VertexCountY = MinVertexCount;
		public float3 Translation = float3.zero;
		[Tooltip("Rotation in Euler coordinates")]
		public float3 Rotation = DefaultRotation;
		public float2 Scale = DefaultScale;

		public void Reset()
		{
			ResetToDefaults = false;
			VertexCountX = VertexCountY = MinVertexCount;
			Translation = float3.zero;
			Rotation = DefaultRotation;
			Scale = DefaultScale;
		}
		
		public PlaneParameters(int2 vertexCount)
			: this(vertexCount, float3.zero, DefaultRotation, DefaultScale) {}

		public PlaneParameters(int2 vertexCount, float2 scale)
			: this(vertexCount, float3.zero, DefaultRotation, scale) {}

		public PlaneParameters(int2 vertexCount, float3 translation)
			: this(vertexCount, translation, DefaultRotation, DefaultScale) {}

		public PlaneParameters(int2 vertexCount, float3 translation, float3 rotation)
			: this(vertexCount, translation, rotation, DefaultScale) {}

		public PlaneParameters(int2 vertexCount, float3 translation, float3 rotation, float2 scale)
		{
			if (vertexCount.x < 2 || vertexCount.y < 2)
				throw new ArgumentException("minimum of 2 vertices per axis required", nameof(vertexCount));

			VertexCountX = vertexCount.x;
			VertexCountY = vertexCount.y;
			Translation = translation;
			Rotation = rotation;
			Scale = scale;
		}

		public bool Equals(PlaneParameters x, PlaneParameters y) => x.GetHashCode() == y.GetHashCode();

		public int GetHashCode(PlaneParameters obj) =>
			VertexCountX.GetHashCode() ^ VertexCountY.GetHashCode() ^ Translation.GetHashCode() ^ Rotation.GetHashCode() ^ Scale.GetHashCode();
	}
}