// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Calculates vertices for an n-gon with the given number of vertices and scale where vertices go in clockwise order around
		/// the center (0,0,0) with first vertex position (0,0,scale).
		/// </summary>
		/// <param name="vertexCount"></param>
		/// <param name="scale"></param>
		/// <param name="vertices">Returned NativeArray of vertices, caller is responsible for disposing this array.</param>
		/// <param name="allocator">The allocator to use for the returned vertices array. Defaults to TempJob.</param>
		public static void CalculateRadialPolygonVertices(int vertexCount, float scale, out NativeArray<float3> vertices,
			Allocator allocator = Allocator.TempJob) => Calculate.RadialPolygonVertices(vertexCount, scale, out vertices, allocator);

		/// <summary>
		/// Calculates center position between the two positions.
		/// </summary>
		/// <param name="pos0"></param>
		/// <param name="pos1"></param>
		/// <returns></returns>
		public static float3 CalculateCenter(float3 pos0, float3 pos1) => Calculate.Center(pos0, pos1);

		/// <summary>
		/// Calculates the mesh centroid => average position of all vertices divided by number of vertices.
		/// </summary>
		/// <returns></returns>
		public float3 CalculateCentroid() => Calculate.Centroid(_data);

		/// <summary>
		/// Calculates the face (polygon) centroid => average position of all face vertices divided by number of vertices.
		/// </summary>
		/// <param name="faceIndex"></param>
		/// <returns></returns>
		public float3 CalculateFaceCentroid(int faceIndex) => Calculate.Centroid(_data, GetFace(faceIndex));

		/// <summary>
		/// Calculates the face (polygon) centroid => average position of all face vertices divided by number of vertices.
		/// </summary>
		/// <param name="face"></param>
		/// <returns></returns>
		public float3 CalculateFaceCentroid(in Face face) => Calculate.Centroid(_data, face);

		/// <summary>
		/// Calculates the center position of the edge.
		/// </summary>
		/// <param name="edgeIndex"></param>
		/// <returns></returns>
		public float3 CalculateEdgeCenter(int edgeIndex) => CalculateEdgeCenter(GetEdge(edgeIndex));

		/// <summary>
		/// Calculates the center position of the edge.
		/// </summary>
		/// <param name="edge"></param>
		/// <returns></returns>
		public float3 CalculateEdgeCenter(in Edge edge) =>
			CalculateCenter(GetVertex(edge.AVertexIndex).Position, GetVertex(edge.OVertexIndex).Position);

		/// <summary>
		/// Calculates the center position between the two vertices.
		/// </summary>
		/// <param name="vertex0Index"></param>
		/// <param name="vertex1Index"></param>
		/// <returns></returns>
		public float3 CalculateVertexCenter(int vertex0Index, int vertex1Index) =>
			CalculateCenter(GetVertex(vertex0Index).Position, GetVertex(vertex1Index).Position);

		/// <summary>
		/// Calculates the center position between the two vertices.
		/// </summary>
		/// <param name="vertex0"></param>
		/// <param name="vertex1"></param>
		/// <returns></returns>
		public float3 CalculateVertexCenter(in Vertex vertex0, in Vertex vertex1) => CalculateCenter(vertex0.Position, vertex1.Position);

		/// <summary>
		/// Calculates the number of edges in the disk cycle of the vertex.
		/// </summary>
		/// <param name="vertexIndex"></param>
		/// <returns></returns>
		public int CalculateEdgeCount(int vertexIndex) => Calculate.DiskCycleEdgeCount(_data, GetVertex(vertexIndex));

		/// <summary>
		/// Calculates the number of edges in the disk cycle of the vertex.
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public int CalculateEdgeCount(in Vertex vertex) => Calculate.DiskCycleEdgeCount(_data, vertex);

		[BurstCompile]
		internal readonly struct Calculate
		{
			public static float3 Centroid(in GraphData data)
			{
				// TODO: this could be a parallel job
				var sum = float3.zero;
				var vCount = data.Vertices.Length; // enumerate the entire list, including possibly invalid vertices
				for (var i = 0; i < vCount; i++)
				{
					var v = data.GetVertex(i);
					sum += math.select(float3.zero, v.Position, v.IsValid);
				}
				return sum / data.VertexCount;
			}

			public static float3 Centroid(in GraphData data, in Face face)
			{
#if GMESH_VALIDATION
				if (face.IsValid == false) throw new ArgumentException("face is not valid");
				if (face.FirstLoopIndex == UnsetIndex) throw new ArgumentException("face's loop index is unset");
				// TODO: validate loop cycle to catch possible infinite loops
#endif

				var firstLoopIndex = face.FirstLoopIndex;
				var sumOfVertexPositions = float3.zero;
				var loop = data.GetLoop(firstLoopIndex);
				do
				{
					sumOfVertexPositions += data.GetVertex(loop.StartVertexIndex).Position;
					loop = data.GetLoop(loop.NextLoopIndex);
				} while (Hint.Likely(loop.Index != firstLoopIndex));

				return sumOfVertexPositions / face.ElementCount;
			}

			public static float3 Center(float3 pos0, float3 pos1) => pos0 + (pos1 - pos0) * 0.5f;

			public static float3 RadialVertexPosition(int vertexNumber, int vertexCount, float scale)
			{
				var angle = vertexNumber * 2f * math.PI / vertexCount;
				return new float3(math.sin(angle) * scale, 0f, math.cos(angle) * scale);
			}

			public static void RadialPolygonVertices(int vertexCount, float scale, out NativeArray<float3> vertices,
				Allocator allocator = Allocator.TempJob)
			{
				if (vertexCount <= 2)
					throw new ArgumentException("polygon requires at least 3 vertices");

				vertices = new NativeArray<float3>(vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
				for (var i = 0; i < vertexCount; i++)
					vertices[i] = RadialVertexPosition(i, vertexCount, scale);
			}

			public static int DiskCycleEdgeCount(in GraphData data, in Vertex vertex)
			{
#if GMESH_VALIDATION
				if (vertex.IsValid == false) throw new ArgumentException("vertex is not valid");
				// TODO: validate loop cycle to catch possible infinite loops
#endif

				if (vertex.BaseEdgeIndex == UnsetIndex)
					return 0;
				
				var edgeCount = 0;
				var edge = data.GetEdge(vertex.BaseEdgeIndex);
				do
				{
					edgeCount++;
					edge = data.GetEdge(edge.GetNextEdgeIndex(vertex.Index));
				} while (Hint.Likely(edge.Index != vertex.BaseEdgeIndex));

				return edgeCount;
			}
		}
	}
}