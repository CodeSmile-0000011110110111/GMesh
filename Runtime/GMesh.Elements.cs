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
			public int V0PrevRadialEdgeIndex;
			public int V0NextRadialEdgeIndex;
			public int V1PrevRadialEdgeIndex;
			public int V1NextRadialEdgeIndex;

			public void Invalidate() => Index = UnsetIndex;
			public bool IsValid => Index != UnsetIndex;

			public override string ToString() => $"Edge [{Index}] with Verts [{Vertex0Index}, {Vertex1Index}], Loop [{LoopIndex}], " +
			                                     $"V0 Edges [{V0PrevRadialEdgeIndex}, {V0NextRadialEdgeIndex}], " +
			                                     $"V1 Edges [{V1PrevRadialEdgeIndex}, {V1NextRadialEdgeIndex}]";

			public bool IsAttachedToVertex(int vertexIndex) => vertexIndex == Vertex0Index || vertexIndex == Vertex1Index;
			public int GetPrevRadialEdgeIndex(int vertexIndex) => vertexIndex == Vertex0Index ? V0PrevRadialEdgeIndex : V1PrevRadialEdgeIndex;
			public int GetNextRadialEdgeIndex(int vertexIndex) => vertexIndex == Vertex0Index ? V0NextRadialEdgeIndex : V1NextRadialEdgeIndex;

			public void SetPrevRadialEdgeIndex(int vertexIndex, int otherEdgeIndex)
			{
				if (vertexIndex == Vertex0Index) V0PrevRadialEdgeIndex = otherEdgeIndex;
				else V1PrevRadialEdgeIndex = otherEdgeIndex;
			}

			public void SetNextRadialEdgeIndex(int vertexIndex, int otherEdgeIndex)
			{
				if (vertexIndex == Vertex0Index) V0NextRadialEdgeIndex = otherEdgeIndex;
				else V1NextRadialEdgeIndex = otherEdgeIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static Edge Create(int vert0Index, int vert1Index, int loopIndex = UnsetIndex,
				int vert0PrevEdgeIndex = UnsetIndex, int vert0NextEdgeIndex = UnsetIndex,
				int vert1PrevEdgeIndex = UnsetIndex, int vert1NextEdgeIndex = UnsetIndex) => new()
			{
				Index = UnsetIndex, LoopIndex = loopIndex, Vertex0Index = vert0Index, Vertex1Index = vert1Index,
				V0PrevRadialEdgeIndex = vert0PrevEdgeIndex, V0NextRadialEdgeIndex = vert0NextEdgeIndex,
				V1PrevRadialEdgeIndex = vert1PrevEdgeIndex, V1NextRadialEdgeIndex = vert1NextEdgeIndex,
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
			                                     $"Prev/Next Loop [{PrevLoopIndex}, {NextLoopIndex}], " +
			                                     $"Prev/Next Radial [{PrevRadialLoopIndex}, {NextRadialLoopIndex}]";

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

			public override string ToString() => $"Face [{Index}] has {ElementCount} verts, 1st Loop [{FirstLoopIndex}]";

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static Face Create(int itemCount, int firstLoopIndex = UnsetIndex, int materialIndex = 0) => new()
				{ Index = UnsetIndex, FirstLoopIndex = firstLoopIndex, ElementCount = itemCount, MaterialIndex = materialIndex };
		}
	}
}