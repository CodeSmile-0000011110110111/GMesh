// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Collections;
using UnityEngine;
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
		public static GMesh Triangle(float3 translation, float scale = DefaultScale)
		{
			var vertices = new NativeArray<float3>(3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			vertices[0] = translation * scale;
			vertices[1] = (forward() + translation) * scale;
			vertices[2] = (right() + translation) * scale;
			return new GMesh(vertices, true);
		}

		/// <summary>
		/// A quad with four vertices with pivot (0,0,0) in the center of the four vertices, lying flat, facing up (+y), axis-aligned (XZ).
		/// </summary>
		/// <returns></returns>
		public static GMesh Quad(float scale = DefaultScale)
		{
			var vertices = new NativeArray<float3>(4, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			vertices[0] = float3(-.5f, 0f, -.5f) * scale;
			vertices[1] = float3(-.5f, 0f, .5f) * scale;
			vertices[2] = float3(.5f, 0f, .5f) * scale;
			vertices[3] = float3(.5f, 0f, -.5f) * scale ;
			return new GMesh(vertices, true);
		}

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
			CalculateRadialPolygonVertices(vertexCount, scale, out var vertices);
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

			var gMesh = new GMesh();
			gMesh.ScheduleCreatePlane(parameters).Complete();
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