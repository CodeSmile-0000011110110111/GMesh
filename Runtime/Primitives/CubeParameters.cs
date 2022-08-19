// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
{
	[Serializable]
	public class CubeParameters
	{
		public const int MinVertexCount = 2;
		public const int MaxVertexCount = 11;

		public bool ResetToDefaults;
		[Range(MinVertexCount, MaxVertexCount)] public int VertexCountX;
		[Range(MinVertexCount, MaxVertexCount)] public int VertexCountY;
		[Range(MinVertexCount, MaxVertexCount)] public int VertexCountZ;
		public float3 Scale = GMesh.DefaultScale;

		public CubeParameters()
			: this(MinVertexCount, GMesh.DefaultScale) {}

		public CubeParameters(int3 vertexCount)
			: this(vertexCount, GMesh.DefaultScale) {}

		public CubeParameters(int3 vertexCount, float3 scale)
		{
			SetVertexCount(vertexCount);
			Scale = scale;
		}

		private void SetVertexCount(int3 vertexCount)
		{
			if (vertexCount.x < MinVertexCount || vertexCount.y < MinVertexCount || vertexCount.z < MinVertexCount)
				throw new ArgumentException("minimum of 2 vertices per axis required", nameof(vertexCount));

			VertexCountX = vertexCount.x;
			VertexCountY = vertexCount.y;
			VertexCountZ = vertexCount.z;
		}

		public void Reset()
		{
			SetVertexCount(MinVertexCount);
			Scale = GMesh.DefaultScale;
		}
	}
}