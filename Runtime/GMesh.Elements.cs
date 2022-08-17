// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Vertex GetVertex(int index) => _vertices[index];
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Edge GetEdge(int index) => _edges[index];
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Loop GetLoop(int index) => _loops[index];
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Face GetFace(int index) => _faces[index];

		[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVertex(in Vertex v) => _vertices[v.Index] = v;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetEdge(in Edge e) => _edges[e.Index] = e;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetLoop(in Loop l) => _loops[l.Index] = l;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFace(in Face f) => _faces[f.Index] = f;

		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Vertex
		{
			public int Index;
			public int BaseEdgeIndex;
			public float3 Position;
			public float3 Normal;

			public void Invalidate() => Index = UnsetIndex;
			public bool IsValid => Index != UnsetIndex;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public float3 GridPosition() => math.round(Position * InvGridSize) * GridSize;

			public override string ToString() => $"Vertex [{Index}] at {Position}, base Edge [{BaseEdgeIndex}]";

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static Vertex Create(float3 position, int baseEdgeIndex = UnsetIndex) => new()
				{ Index = UnsetIndex, BaseEdgeIndex = baseEdgeIndex, Position = position, Normal = default };
		}

		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Edge
		{
			public int Index;
			public int LoopIndex;
			public int Vertex0Index;
			public int Vertex1Index;
			public int V0PrevEdgeIndex;
			public int V0NextEdgeIndex;
			public int V1PrevEdgeIndex;
			public int V1NextEdgeIndex;

			public int this[int index]
			{
				get
				{
					if (index == 0)
						return Vertex0Index;

					return Vertex1Index;
				}
			}

			public void Invalidate() => Index = UnsetIndex;
			public bool IsValid => Index != UnsetIndex;

			public override string ToString() => $"Edge [{Index}] with Verts [{Vertex0Index}, {Vertex1Index}], Loop [{LoopIndex}], " +
			                                     $"V0 Prev/Next [{V0PrevEdgeIndex}]<>[{V0NextEdgeIndex}], " +
			                                     $"V1 Prev/Next [{V1PrevEdgeIndex}]<>[{V1NextEdgeIndex}]";

			public bool IsConnectedToVertex(int vertexIndex) => vertexIndex == Vertex0Index || vertexIndex == Vertex1Index;
			public bool IsConnectingSameVertices(in Edge edge) => IsConnectingVertices(edge.Vertex0Index, edge.Vertex1Index);

			public bool IsConnectingVertices(int v0Index, int v1Index) => (v0Index == Vertex0Index && v1Index == Vertex1Index) ||
			                                                              (v0Index == Vertex1Index && v1Index == Vertex0Index);

			public int GetPrevEdgeIndex(int vertexIndex) => vertexIndex == Vertex0Index ? V0PrevEdgeIndex : V1PrevEdgeIndex;
			public int GetNextEdgeIndex(int vertexIndex) => vertexIndex == Vertex0Index ? V0NextEdgeIndex : V1NextEdgeIndex;

			public void SetPrevEdgeIndex(int vertexIndex, int otherEdgeIndex)
			{
				if (vertexIndex == Vertex0Index) V0PrevEdgeIndex = otherEdgeIndex;
				else V1PrevEdgeIndex = otherEdgeIndex;
			}

			public void SetNextEdgeIndex(int vertexIndex, int otherEdgeIndex)
			{
				if (vertexIndex == Vertex0Index) V0NextEdgeIndex = otherEdgeIndex;
				else V1NextEdgeIndex = otherEdgeIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static Edge Create(int vert0Index, int vert1Index, int loopIndex = UnsetIndex,
				int vert0PrevEdgeIndex = UnsetIndex, int vert0NextEdgeIndex = UnsetIndex,
				int vert1PrevEdgeIndex = UnsetIndex, int vert1NextEdgeIndex = UnsetIndex) => new()
			{
				Index = UnsetIndex, LoopIndex = loopIndex, Vertex0Index = vert0Index, Vertex1Index = vert1Index,
				V0PrevEdgeIndex = vert0PrevEdgeIndex, V0NextEdgeIndex = vert0NextEdgeIndex,
				V1PrevEdgeIndex = vert1PrevEdgeIndex, V1NextEdgeIndex = vert1NextEdgeIndex,
			};
		}

		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Loop
		{
			public int Index;
			public int FaceIndex;
			public int EdgeIndex;
			public int VertexIndex;
			public int PrevLoopIndex; // loops around face
			public int NextLoopIndex; // loops around face
			public int PrevRadialLoopIndex; // loops around edge
			public int NextRadialLoopIndex; // loops around edge
			public float2 UV;
			public float2 UV1;

			public void Invalidate() => Index = UnsetIndex;
			public bool IsValid => Index != UnsetIndex;

			public override string ToString() => $"Loop [{Index}] of Face [{FaceIndex}], Edge [{EdgeIndex}], Vertex [{VertexIndex}], " +
			                                     $"Prev/Next [{PrevLoopIndex}]<>[{NextLoopIndex}], " +
			                                     $"Radial Prev/Next [{PrevRadialLoopIndex}]<>[{NextRadialLoopIndex}]";

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static Loop Create(int faceIndex, int edgeIndex, int vertIndex,
				int prevRadialLoopIndex, int nextRadialLoopIndex, int prevLoopIndex, int nextLoopIndex) => new()
			{
				Index = UnsetIndex, FaceIndex = faceIndex, EdgeIndex = edgeIndex, VertexIndex = vertIndex,
				PrevRadialLoopIndex = prevRadialLoopIndex, NextRadialLoopIndex = nextRadialLoopIndex,
				PrevLoopIndex = prevLoopIndex, NextLoopIndex = nextLoopIndex,
			};
		}

		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Face
		{
			public int Index;
			public int FirstLoopIndex;
			public int ElementCount; // = number of: vertices, loops, edges
			public int MaterialIndex;
			public float3 Normal;
			public Color Color;

			public void Invalidate() => Index = UnsetIndex;
			public bool IsValid => Index != UnsetIndex;

			public override string ToString() => $"Face [{Index}] has {ElementCount} verts/edges/loops, first Loop [{FirstLoopIndex}]";

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static Face Create(int itemCount, int firstLoopIndex = UnsetIndex, int materialIndex = 0) => new()
				{ Index = UnsetIndex, FirstLoopIndex = firstLoopIndex, ElementCount = itemCount, MaterialIndex = materialIndex };
		}
	}
}