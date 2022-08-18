// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateFaceCentroid(int faceIndex) => CalculateFaceCentroid(GetFace(faceIndex));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateFaceCentroid(in Face face)
		{
			var sumOfVertexPositions = float3.zero;
			var loopCount = 0;

			ForEachLoop(face, loop =>
			{
				loopCount++;
				sumOfVertexPositions += GetVertex(loop.VertexIndex).Position;
			});

			return sumOfVertexPositions / loopCount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateEdgeCenter(int edgeIndex) => CalculateEdgeCenter(GetEdge(edgeIndex));
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateEdgeCenter(in Edge edge) => CalculateCenter(edge.Vertex0Index, edge.Vertex1Index);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateCenter(int vertex0Index, int vertex1Index) => CalculateCenter(GetVertex(vertex0Index), GetVertex(vertex1Index));
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateCenter(in Vertex v0, in Vertex v1) => CalculateCenter(v0.Position, v1.Position);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateCenter(float3 pos0, float3 pos1) => pos0 + (pos1 - pos0) * 0.5f;
		
		
		/*
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateEdgeDir(int edgeIndex) => CalculateEdgeDir(GetEdge(edgeIndex));
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateEdgeDir(in Edge edge) => CalculateDirection(edge.Vertex0Index, edge.Vertex1Index);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateDirection(int vertex0Index, int vertex1Index) => CalculateDirection(GetVertex(vertex0Index), GetVertex(vertex1Index));
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateDirection(in Vertex v0, in Vertex v1) => CalculateDirection(v0.Position, v1.Position);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 CalculateDirection(float3 pos0, float3 pos1) => pos1 - pos0;
*/
	}
}