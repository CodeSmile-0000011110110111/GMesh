// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private Calculate _calculate;

		/// <summary>
		/// Calculates vertices for an n-gon with the given number of vertices and scale where vertices go in clockwise order around
		/// the center (0,0,0) with first vertex position (0,0,scale).
		/// </summary>
		/// <param name="vertexCount"></param>
		/// <param name="scale"></param>
		/// <param name="vertices">Returned NativeArray of vertices, caller is responsible for disposing this array.</param>
		[BurstCompile]
		public static void CalculateRadialPolygonVertices(int vertexCount, float scale, out NativeArray<float3> vertices)
		{
			if (vertexCount < 2)
				throw new ArgumentException("polygon requires at least 3 vertices");

			vertices = new NativeArray<float3>(vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var twoPie = 2f * math.PI; // not: 2Pac :)
			for (var i = 0; i < vertexCount; i++)
			{
				var angle = i * twoPie / vertexCount;
				vertices[i] = math.float3(math.sin(angle) * scale, 0f, math.cos(angle) * scale);
			}
		}

		/// <summary>
		/// Calculates the mesh centroid (average position of all vertices).
		/// Equals sum of all vertex positions divided by number of vertices. 
		/// </summary>
		/// <returns></returns>
		
		public float3 CalculateCentroid() => _calculate.Centroid(_data);

		
		public float3 CalculateFaceCentroid(int faceIndex) => CalculateCentroid(GetFace(faceIndex));

		
		public float3 CalculateCentroid(in Face face)
		{
			if (face.IsValid == false)
				throw new ArgumentException("face has been invalidated");

			var sumOfVertexPositions = float3.zero;
			var loopCount = 0;
			ForEachLoop(face, loop =>
			{
				loopCount++;
				sumOfVertexPositions += GetVertex(loop.StartVertexIndex).Position;
			});

			return sumOfVertexPositions / loopCount;
		}

		
		public float3 CalculateEdgeCenter(int edgeIndex) => CalculateCenter(GetEdge(edgeIndex));

		
		public float3 CalculateCenter(in Edge edge)
		{
			if (edge.IsValid == false)
				throw new ArgumentException("edge has been invalidated");

			return CalculateCenter(edge.AVertexIndex, edge.OVertexIndex);
		}

		
		public float3 CalculateCenter(int vertex0Index, int vertex1Index) => CalculateCenter(GetVertex(vertex0Index), GetVertex(vertex1Index));

		
		public float3 CalculateCenter(in Vertex v0, in Vertex v1) => CalculateCenter(v0.Position, v1.Position);

		
		public float3 CalculateCenter(float3 pos0, float3 pos1) => pos0 + (pos1 - pos0) * 0.5f;

		public int CalculateEdgeCount(int vertexIndex) => CalculateEdgeCount(GetVertex(vertexIndex));

		public int CalculateEdgeCount(in Vertex vertex)
		{
			var edgeCount = 0;
			ForEachEdge(vertex, e => edgeCount++);
			return edgeCount;
		}

		[BurstCompile] [StructLayout(LayoutKind.Sequential)]
		private struct Calculate
		{
			public float3 Centroid(in GraphData data)
			{
				// TODO: implement using jobs
				var sum = float3.zero;
				var vCount = data.Vertices.Length; // enumerate the entire list
				for (var i = 0; i < vCount; i++)
				{
					var v = data.GetVertex(i);
					sum += math.select(float3.zero, v.Position, v.IsValid);
				}
				return sum / data.VertexCount;
			}
		}
	}
}