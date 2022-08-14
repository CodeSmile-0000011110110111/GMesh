// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Vertex
		{
			public int Index;
			public int FirstEdgeIndex;
			public float3 Position;
			public float3 Normal;

			public float3 GridPosition() => math.round(Position * InvGridSize) * GridSize;

			public override string ToString() => $"Vertex [{Index}] at {Position}, 1st Edge [{FirstEdgeIndex}]";

			public static Vertex Create(int index, int firstEdgeIndex, float3 position, float3 normal = default) => new()
				{ Index = index, FirstEdgeIndex = firstEdgeIndex, Position = position, Normal = normal };
		}

		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Edge
		{
			public int Index;
			public int LoopIndex;
			public int Vertex0Index;
			public int Vertex1Index;
			public int Vertex0PrevEdgeIndex;
			public int Vertex0NextEdgeIndex;
			public int Vertex1PrevEdgeIndex;
			public int Vertex1NextEdgeIndex;

			public override string ToString() => $"Edge [{Index}] with Verts [{Vertex0Index}, {Vertex1Index}], Loop [{LoopIndex}], " +
			                                     $"V0 Edges [{Vertex0PrevEdgeIndex}, {Vertex0NextEdgeIndex}], " +
			                                     $"V1 Edges [{Vertex1PrevEdgeIndex}, {Vertex1NextEdgeIndex}]";

			public static Edge Create(int index, int vert0Index, int vert1Index, int loopIndex = UnsetIndex,
				int vert0PrevEdgeIndex = UnsetIndex, int vert0NextEdgeIndex = UnsetIndex,
				int vert1PrevEdgeIndex = UnsetIndex, int vert1NextEdgeIndex = UnsetIndex) => new()
			{
				Index = index, LoopIndex = loopIndex, Vertex0Index = vert0Index, Vertex1Index = vert1Index,
				Vertex0PrevEdgeIndex = vert0PrevEdgeIndex, Vertex0NextEdgeIndex = vert0NextEdgeIndex,
				Vertex1PrevEdgeIndex = vert1PrevEdgeIndex, Vertex1NextEdgeIndex = vert1NextEdgeIndex,
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
			public int PrevRadialLoopIndex; // loops around edge
			public int NextRadialLoopIndex; // loops around edge
			public int PrevLoopIndex; // loops around face
			public int NextLoopIndex; // loops around face
			public float2 vertex0UV;
			public float2 vertex1UV;

			public override string ToString() => $"Loop [{Index}] of Face [{FaceIndex}], Edge [{EdgeIndex}], Vertex [{VertexIndex}], " +
			                                     $"Prev/Next Loop [{PrevLoopIndex}, {NextLoopIndex}], " +
			                                     $"Prev/Next Radial [{PrevRadialLoopIndex}, {NextRadialLoopIndex}]";

			public static Loop Create(int index, int faceIndex, int edgeIndex, int vertIndex,
				int prevRadialLoopIndex, int nextRadialLoopIndex, int prevLoopIndex, int nextLoopIndex) => new()
			{
				Index = index, FaceIndex = faceIndex, EdgeIndex = edgeIndex, VertexIndex = vertIndex,
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

			public override string ToString() => $"Face [{Index}] has {ElementCount} verts, 1st Loop [{FirstLoopIndex}]";

			public static Face Create(int index, int itemCount, int firstLoopIndex = UnsetIndex, int materialIndex = 0) => new()
				{ Index = index, FirstLoopIndex = firstLoopIndex, ElementCount = itemCount, MaterialIndex = materialIndex };
		}
	}
}