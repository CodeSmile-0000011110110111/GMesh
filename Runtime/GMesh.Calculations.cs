// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Calculates the mesh centroid (average position of all vertices).
		/// Equals sum of all vertex positions divided by number of vertices. 
		/// </summary>
		/// <returns></returns>
		public float3 CalculateCentroid()
		{
			// TODO: implement using jobs
			var sum = float3.zero;
			var validVertexCount = 0;
			for (var i = 0; i < VertexCount; i++)
			{
				var v = GetVertex(i);
				if (v.IsValid)
				{
					sum += v.Position;
					validVertexCount++;
				}
			}
			return sum / validVertexCount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateFaceCentroid(int faceIndex) => CalculateFaceCentroid(GetFace(faceIndex));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateFaceCentroid(in Face face)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateEdgeCenter(int edgeIndex) => CalculateEdgeCenter(GetEdge(edgeIndex));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateEdgeCenter(in Edge edge)
		{
			if (edge.IsValid == false)
				throw new ArgumentException("edge has been invalidated");
				
			return CalculateCenter(edge.AVertexIndex, edge.OVertexIndex);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateCenter(int vertex0Index, int vertex1Index) => CalculateCenter(GetVertex(vertex0Index), GetVertex(vertex1Index));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateCenter(in Vertex v0, in Vertex v1) => CalculateCenter(v0.Position, v1.Position);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateCenter(float3 pos0, float3 pos1) => pos0 + (pos1 - pos0) * 0.5f;

	}
}