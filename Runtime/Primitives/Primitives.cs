// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public static class Primitives
	{
		public static GMesh Quad() => Plane(new PlaneParameters(new int2(2)));

		public static GMesh Plane(PlaneParameters parameters)
		{
			if (parameters.VertexCount.x < 2 || parameters.VertexCount.y < 2)
				throw new ArgumentException("minimum of 2 vertices per axis required");

			var subdivisions = parameters.VertexCount - 1;
			var vertexCount = parameters.VertexCount.x * parameters.VertexCount.y;
			var vertices = new float3[vertexCount];

			// create vertices
			var step = 1f / new float3(subdivisions, 1f);
			var vertexIndex = 0;
			for (var y = 0; y < parameters.VertexCount.y; y++)
			{
				for (var x = 0; x < parameters.VertexCount.x; x++)
				{
					vertices[vertexIndex++] = new float3(x, y, 0f) * step;
				}
			}

			var gmesh = new GMesh();
			gmesh.CreateVertices(vertices);

			// create quad faces
			var centerOffset = new float3(.5f, .5f, 0f);
			for (var y = 0; y < subdivisions.y; y++)
			{
				for (var x = 0; x < subdivisions.x; x++)
				{
					// each quad has (0,0) in the lower left corner with verts: v0=down-left, v1=up-left, v2=up-right, v3=down-right
					// +z axis points towards the plane and plane normal is Vector3.back = (0,0,-1)

					// calculate quad's vertex indices
					var vi0 = y * parameters.VertexCount.x + x;
					var vi1 = (y + 1) * parameters.VertexCount.x + x;
					var vi2 = (y + 1) * parameters.VertexCount.x + x + 1;
					var vi3 = y * parameters.VertexCount.x + x + 1;

					// get the vertices
					var v0 = vertices[vi0];
					var v1 = vertices[vi1];
					var v2 = vertices[vi2];
					var v3 = vertices[vi3];
					
					// TODO: add UV to face
					// derive UV from vertices
					var uv01 = new float4(v0.x, v0.y, v1.x, v1.y);
					var uv23 = new float4(v2.x, v2.y, v3.x, v3.y);

					// offset vertices 
					// TODO: is this needed?
					vertices[vi0] = v0 - centerOffset;
					vertices[vi1] = v1 - centerOffset;
					vertices[vi2] = v2 - centerOffset;
					vertices[vi3] = v3 - centerOffset;

					gmesh.CreateFace(new[] { vi0, vi1, vi2, vi3 });
				}
			}
			
			// TODO: apply transform

			return gmesh;
		}

		private struct QuadInfo
		{
			public float4 UV01;
			public float4 UV23;
		}
	}
}