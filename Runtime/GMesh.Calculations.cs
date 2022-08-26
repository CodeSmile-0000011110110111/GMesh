// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private Calculate _calculate;
		private Count _count;

		/// <summary>
		/// Calculates vertices for an n-gon with the given number of vertices and scale where vertices go in clockwise order around
		/// the center (0,0,0) with first vertex position (0,0,scale).
		/// </summary>
		/// <param name="vertexCount"></param>
		/// <param name="scale"></param>
		/// <param name="vertices">Returned NativeArray of vertices, caller is responsible for disposing this array.</param>
		/// <param name="allocator">The allocator to use for the returned vertices array. Defaults to TempJob.</param>
		public static void CalculateRadialPolygonVertices(int vertexCount, float scale, out NativeArray<float3> vertices,
			Allocator allocator = Allocator.TempJob)
		{
			if (vertexCount < 2)
				throw new ArgumentException("polygon requires at least 3 vertices");

			vertices = new NativeArray<float3>(vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
			for (var i = 0; i < vertexCount; i++)
				vertices[i] = Calculate.RadialVertexPosition(i, vertexCount, scale);
		}

		/// <summary>
		/// Calculates the mesh centroid (average position of all vertices).
		/// Equals sum of all vertex positions divided by number of vertices. 
		/// </summary>
		/// <returns></returns>
		public float3 CalculateCentroid() => _calculate.Centroid(_data);

		public float3 CalculateFaceCentroid(int faceIndex) => CalculateFaceCentroid(GetFace(faceIndex));
		public float3 CalculateFaceCentroid(in Face face) => _calculate.Centroid(_data, face);

		public float3 CalculateEdgeCenter(int edgeIndex) => CalculateEdgeCenter(GetEdge(edgeIndex));
		public float3 CalculateEdgeCenter(in Edge edge) => CalculateCenter(edge.AVertexIndex, edge.OVertexIndex);
		public float3 CalculateCenter(int vertex0Index, int vertex1Index) => CalculateCenter(GetVertex(vertex0Index), GetVertex(vertex1Index));
		public float3 CalculateCenter(in Vertex v0, in Vertex v1) => CalculateCenter(v0.Position, v1.Position);
		public float3 CalculateCenter(float3 pos0, float3 pos1) => Calculate.Center(pos0, pos1);

		public int CalculateEdgeCount(int vertexIndex) => CalculateEdgeCount(GetVertex(vertexIndex));
		public int CalculateEdgeCount(in Vertex vertex) => _count.DiskCycleEdges(_data, vertex);

		[BurstCompile] [StructLayout(LayoutKind.Sequential)]
		internal struct Calculate
		{
			public float3 Centroid(in GraphData data)
			{
				// TODO: implement using jobs
				var sum = float3.zero;
				var vCount = data.Vertices.Length; // enumerate the entire list, including possibly invalid vertices
				for (var i = 0; i < vCount; i++)
				{
					var v = data.GetVertex(i);
					sum += math.select(float3.zero, v.Position, v.IsValid);
				}
				return sum / data.VertexCount;
			}

			public float3 Centroid(in GraphData data, in Face face)
			{
#if GMESH_VALIDATION
				if (face.IsValid == false)
					throw new ArgumentException("face is not valid");

				// TODO: validate loop cycle to catch infinite loops
#endif

				var sumOfVertexPositions = float3.zero;
				var firstLoopIndex = face.FirstLoopIndex;
				var loop = data.GetLoop(firstLoopIndex);
				do
				{
					sumOfVertexPositions += data.GetVertex(loop.StartVertexIndex).Position;
					loop = data.GetLoop(loop.NextLoopIndex);
				} while (loop.Index != firstLoopIndex);

				return sumOfVertexPositions / face.ElementCount;
			}

			public static float3 Center(float3 pos0, float3 pos1) => pos0 + (pos1 - pos0) * 0.5f;

			public static float3 RadialVertexPosition(int vertexNumber, int vertexCount, float scale)
			{
				var angle = vertexNumber * 2f * math.PI / vertexCount;
				return new float3(math.sin(angle) * scale, 0f, math.cos(angle) * scale);
			}
		}

		[BurstCompile] [StructLayout(LayoutKind.Sequential)]
		internal struct Count
		{
			public int DiskCycleEdges(in GraphData data, in Vertex vertex)
			{
#if GMESH_VALIDATION
				if (vertex.IsValid == false)
					throw new ArgumentException("vertex is not valid");

				// TODO: validate disk cycle to catch infinite loops
#endif

				var edgeCount = 0;
				var edge = data.GetEdge(vertex.BaseEdgeIndex);
				do
				{
					edgeCount++;
					edge = data.GetEdge(edge.GetNextEdgeIndex(vertex.Index));
				} while (edge.Index != vertex.BaseEdgeIndex);

				return edgeCount;
			}
		}
	}
}