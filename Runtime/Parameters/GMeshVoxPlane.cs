// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile.GraphMesh;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile
{
	[Serializable]
	public class GMeshVoxPlane
	{
		public const int MinVertexCount = 2;
		public const int MaxVertexCount = 101;
		public static readonly float3 DefaultRotation = new(90f, 0f, 0f);

		public bool ResetToDefaults;
		[Range(MinVertexCount, MaxVertexCount)] public int VertexCountX = MinVertexCount;
		[Range(MinVertexCount, MaxVertexCount)] public int VertexCountY = MinVertexCount;

		public float FlattenThreshold = 0.1f;

		//[Tooltip("Center of rotation and scale")] public float3 Pivot = float3.zero;
		[Tooltip("Vertex offset from pivot")] public float3 Translation = float3.zero;
		[Tooltip("Rotation is in degrees (Euler)")] public float3 Rotation = DefaultRotation;
		[Tooltip("Plane size scales along X/Y, with Z being the depth/height scale")] public float3 Scale = GMesh.DefaultScale;

		public GMeshVoxPlane()
			: this(MinVertexCount, float3.zero, DefaultRotation, GMesh.DefaultScale) {}

		public GMeshVoxPlane(int2 vertexCount)
			: this(vertexCount, float3.zero, DefaultRotation, GMesh.DefaultScale) {}

		public GMeshVoxPlane(int2 vertexCount, float3 scale)
			: this(vertexCount, float3.zero, DefaultRotation, scale) {}

		public GMeshVoxPlane(int2 vertexCount, float3 translation, float3 rotation)
			: this(vertexCount, translation, rotation, GMesh.DefaultScale) {}

		public GMeshVoxPlane(int2 vertexCount, float3 translation, float3 rotation, float3 scale)
		{
			if (vertexCount.x < 2 || vertexCount.y < 2)
				throw new ArgumentException("minimum of 2 vertices per axis required", nameof(vertexCount));

			VertexCountX = vertexCount.x;
			VertexCountY = vertexCount.y;
			Translation = translation;
			Rotation = rotation;
			Scale = scale;
		}

		public void Reset()
		{
			ResetToDefaults = false;
			VertexCountX = VertexCountY = MinVertexCount;
			Translation = float3.zero;
			Rotation = DefaultRotation;
			Scale = GMesh.DefaultScale;
		}
	}
}