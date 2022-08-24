// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

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
		public static GMesh Triangle(float3 translation, float scale = DefaultScale) => new(new[] { translation*scale, (forward() + translation) * scale, (right()+translation) * scale });

		/// <summary>
		/// A quad with four vertices with pivot (0,0,0) in the center of the four vertices, lying flat, facing up (+y), axis-aligned (XZ).
		/// </summary>
		/// <returns></returns>
		public static GMesh Quad(float scale = DefaultScale) => new(new[]
			{ float3(-.5f, 0f, -.5f) * scale, float3(-.5f, 0f, .5f) * scale, float3(.5f, 0f, .5f) * scale, float3(.5f, 0f, -.5f) * scale });

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
			CalculatePolygonVertices(vertexCount, scale, out var vertices);
			var gMesh = new GMesh(vertices);
			vertices.Dispose();
			return gMesh;
		}

		/// <summary>
		/// A configurable plane made out of quad faces with shared vertices with Pivot (0,0,0) in the center. Lying flat, facing up (+y).
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static GMesh Plane(GMeshPlane parameters)
		{
			if (parameters.VertexCountX < 2 || parameters.VertexCountY < 2)
				throw new ArgumentException("minimum of 2 vertices per axis required");

			var subdivisions = int2(parameters.VertexCountX - 1, parameters.VertexCountY - 1);
			var vertexCount = parameters.VertexCountX * parameters.VertexCountY;
			var vertices = new float3[vertexCount];

			// create vertices
			var scale = float3((float2)parameters.Scale, DefaultScale);
			var centerOffset = float3(.5f, .5f, 0f) * scale;
			var step = 1f / float3(subdivisions, 1f) * scale;
			var vIndex = 0;
			for (var y = 0; y < parameters.VertexCountY; y++)
				for (var x = 0; x < parameters.VertexCountX; x++)
					vertices[vIndex++] = float3(x, y, 0f) * step - centerOffset;

			var gMesh = new GMesh();
			gMesh.CreateVertices(vertices);

			// create quad faces
			for (var y = 0; y < subdivisions.y; y++)
			{
				for (var x = 0; x < subdivisions.x; x++)
				{
					// each quad has (0,0) in the lower left corner with verts: v0=down-left, v1=up-left, v2=up-right, v3=down-right
					// +z axis points towards the plane and plane normal is Vector3.back = (0,0,-1)

					// calculate quad's vertex indices
					var vi0 = y * parameters.VertexCountX + x;
					var vi1 = (y + 1) * parameters.VertexCountX + x;
					var vi2 = (y + 1) * parameters.VertexCountX + x + 1;
					var vi3 = y * parameters.VertexCountX + x + 1;

					// TODO: add UV to face
					// get the vertices
					//var v0 = vertices[vi0];
					//var v1 = vertices[vi1];
					//var v2 = vertices[vi2];
					//var v3 = vertices[vi3];
					// derive UV from vertices
					//var uv01 = new float4(v0.x, v0.y, v1.x, v1.y);
					//var uv23 = new float4(v2.x, v2.y, v3.x, v3.y);

					gMesh.CreateFace(new[] { vi0, vi1, vi2, vi3 });
				}
			}

			// Note: scale was applied to vertices earlier
			var transform = new Transform(parameters.Translation, parameters.Rotation, DefaultScale);
			//gMesh.Pivot = parameters.Pivot;
			gMesh.ApplyTransform(transform);

			return gMesh;
		}

		/// <summary>
		/// A configurable 1m sized cube made out of 6 planes and shared vertices with Pivot (0,0,0) in the center.
		/// </summary>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static GMesh Cube(GMeshCube parameters)
		{
			var vertexCount = int3(parameters.VertexCountX, parameters.VertexCountY, parameters.VertexCountZ);
			var b = Plane(new GMeshPlane(vertexCount.xy, float3(0f, 0f, -0.5f), float3(0f, 0f, 0f)));
			var f = Plane(new GMeshPlane(vertexCount.xy, float3(0f, 0f, 0.5f), float3(0f, 180f, 0f)));
			var l = Plane(new GMeshPlane(vertexCount.zy, float3(-0.5f, 0f, 0f), float3(0f, 90f, 0f)));
			var r = Plane(new GMeshPlane(vertexCount.zy, float3(0.5f, 0f, 0f), float3(0f, 270f, 0f)));
			var u = Plane(new GMeshPlane(vertexCount.xz, float3(0f, 0.5f, 0f), float3(90f, 270f, 270f)));
			var d = Plane(new GMeshPlane(vertexCount.xz, float3(0f, -0.5f, 0f), float3(270f, 90f, 90f)));
			return Combine(new[] { b, f, l, r, u, d }, true);
		}
	}
}