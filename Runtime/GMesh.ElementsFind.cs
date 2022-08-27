// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/*
		public int[] GetFaceVertexIndices(int faceIndex)
		{
			return GetFaceVertexIndices(GetFace(faceIndex));
		}
		
		public int[] GetFaceVertexIndices(in Face face)
		{
			if (face.IsValid == false)
				throw new ArgumentException($"face {face.Index} was invalidated");
			
			var indices = new int[face.ElementCount];
			
		}
		*/

		/*
		/// <summary>
		/// Returns index of edge that connects the two vertices.
		/// </summary>
		/// <param name="vertex0Index"></param>
		/// <param name="vertex1Index"></param>
		/// <returns></returns>
		public int FindEdgeIndex(int vertex0Index, int vertex1Index)
		{
			if (vertex0Index == vertex1Index)
				throw new ArgumentException($"vertex indices are the same: {vertex0Index}, {vertex1Index}");
			if (vertex0Index < 0 || vertex1Index < 0)
				throw new ArgumentException($"vertex index cannot be negative: {vertex0Index}, {vertex1Index}");

			var v0 = GetVertex(vertex0Index);
			var v1 = GetVertex(vertex1Index);
			if (v0.BaseEdgeIndex == UnsetIndex || v1.BaseEdgeIndex == UnsetIndex)
				return UnsetIndex;

			var edge0 = GetEdge(v0.BaseEdgeIndex);
			var edge1 = GetEdge(v1.BaseEdgeIndex);

			do
			{
				if (edge0.IsConnectedToVertex(vertex1Index))
					return edge0.Index;
				if (edge1.IsConnectedToVertex(vertex0Index))
					return edge1.Index;

				edge0 = GetEdge(edge0.GetNextRadialEdgeIndex(vertex0Index));
				edge1 = GetEdge(edge1.GetNextRadialEdgeIndex(vertex1Index));
			} while (edge0.Index != v0.BaseEdgeIndex && edge1.Index != v1.BaseEdgeIndex);

			return UnsetIndex;
		}
		*/

		/// <summary>
		/// Tries to find an existing edge connecting the given vertices of the input edge.
		/// </summary>
		/// <param name="edge">Edge to check for, will be assigned to existing one.</param>
		/// <returns>True if edge exists between v0 and v1. False if there is no edge connecting the two vertices.</returns>
		public int FindExistingEdgeIndex(in Edge edge) => Find.ExistingEdgeIndex(_data, edge.AVertexIndex, edge.OVertexIndex);

		/// <summary>
		/// Tries to find an existing edge connecting the two vertices (via their indices).
		/// </summary>
		/// <param name="v0Index">Index of one vertex</param>
		/// <param name="v1Index">Index of another vertex</param>
		/// <returns>True if edge exists between v0 and v1. False if there is no edge connecting the two vertices.</returns>
		public int FindExistingEdgeIndex(int v0Index, int v1Index) => Find.ExistingEdgeIndex(_data, v0Index, v1Index);

		[BurstCompile]
		internal readonly struct Find
		{
			public static int ExistingEdgeIndex(in GraphData data, int v0Index, int v1Index)
			{
#if GMESH_VALIDATION
				// TODO: validate disk cycle to prevent infinite loop
#endif

				// check all edges in cycle, return this edge's index if it points to v1
				var edgeIndex = data.GetVertex(v0Index).BaseEdgeIndex;
				if (edgeIndex == UnsetIndex)
					return UnsetIndex;

				var edge = data.GetEdge(edgeIndex);
				do
				{
					if (edge.ContainsVertex(v1Index))
						return edge.Index;

					edge = data.GetEdge(edge.GetNextEdgeIndex(v0Index));
				} while (Hint.Likely(edge.Index != edgeIndex));

				return UnsetIndex;
			}
		}

		// private void AddEdgeVertexPair(int vertexIndexA, int vertexIndexO, int edgeIndex) => _edgeIndexForVertices.Add(new int2(vertexIndexA, vertexIndexO), edgeIndex);

		[BurstCompile] [StructLayout(LayoutKind.Sequential)]
		private struct FindExistingEdgeIndexJob : IJob
		{
			[ReadOnly] public NativeList<Vertex> vertices;
			[ReadOnly] public NativeList<Edge> edges;
			public readonly int v0Index;
			public readonly int v1Index;
			public int existingEdgeIndex;

			private Vertex GetVertex(int index) => vertices[index];
			private Edge GetEdge(int index) => edges[index];

			public void Execute()
			{
				existingEdgeIndex = UnsetIndex;
				if (v0Index == UnsetIndex || v1Index == UnsetIndex)
					return;

				var edgeIndex = GetVertex(v0Index).BaseEdgeIndex;
				if (edgeIndex == UnsetIndex)
					return;

				// check all edges in cycle, return this edge's index if it points to v1
				var edge = GetEdge(edgeIndex);
				var maxIterations = 10000;
				do
				{
					if (edge.ContainsVertex(v1Index))
					{
						existingEdgeIndex = edge.Index;
						break;
					}

					edge = GetEdge(edge.GetNextEdgeIndex(v0Index));

					maxIterations--;
					if (maxIterations == 0)
					{
						throw new Exception(
							$"{nameof(FindExistingEdgeIndexJob)}: possible infinite loop due to malformed mesh graph around {edge}");
					}
				} while (edge.Index != edgeIndex);
			}
		}
	}
}