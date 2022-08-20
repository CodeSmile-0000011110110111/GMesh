﻿// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// A vertex is primarily a coordinate in space.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Vertex
		{
			/// <summary>
			/// Index of the vertex in the Vertices list.
			/// </summary>
			public int Index;
			/// <summary>
			/// Index of the "base" edge in the Edges list. This can be any edge connected to that vertex.
			/// </summary>
			public int BaseEdgeIndex;
			/// <summary>
			/// Position of the vertex in mesh (local) space.
			/// </summary>
			public float3 Position;

			/// <summary>
			/// True if the element hasn't been flagged for deletion.
			/// </summary>
			public bool IsValid => Index != UnsetIndex;

			/// <summary>
			/// Virtual "grid" position of vertex. Position is multiplied by inverse of GridSize,
			/// then rounded to nearest int to prevent rounding errors (ie -0.5f would become -499 without rounding).
			/// This is used to determine whether two vertices occupy the same (close-enough) position.
			/// 
			/// Example result for 1mm grid: Position (-0.5f, 1.2345678f, 0f) => GridPosition (-500, 1234, 0)  
			/// </summary>
			/// <returns></returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int3 GridPosition() => new(math.round(new double3(Position) * GridUpscale)); // correct rounding requires double!

			/// <summary>
			/// Snaps the vertex position to be on a coordinate within the range given by precision.
			/// Ie if precision = 0.01f the vertex coordinate will be snapped to the nearest 1cm position.
			/// For example: Position (1.495f, -0.009f, 0.105000001f) => (1.5f, -0.01f, 0.11f)  
			/// </summary>
			/// <param name="gridSize"></param>
			public void SnapPosition(float gridSize = GridSize) =>
				Position = new float3(math.round(new double3(Position) * (1.0 / gridSize)) * gridSize);

			public override string ToString() => $"Vertex [{Index}] at {Position}, base Edge [{BaseEdgeIndex}]";

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal void Invalidate() => Index = UnsetIndex;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static Vertex Create(float3 position, int baseEdgeIndex = UnsetIndex) => new()
				{ Index = UnsetIndex, BaseEdgeIndex = baseEdgeIndex, Position = position };
		}

		/// <summary>
		/// An edge is the line between two vertices.
		/// An edge has no orientation, the two vertices can be at either end.
		/// Use GetOtherVertexIndex(int vertexIndex) to get from the given vertex of the edge to the other vertex. 
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Edge
		{
			/// <summary>
			/// Index of the edge in the Edges list.
			/// </summary>
			public int Index;
			/// <summary>
			/// Index of the "base" loop in the Loops list. This can be any loop connected to the edge.
			/// </summary>
			public int BaseLoopIndex;
			/// <summary>
			/// Index of a (A) vertex on one end of the edge. Edges are non-directed, do not misrepresent A as the "first/start" vertex!
			/// </summary>
			public int AVertexIndex;
			/// <summary>
			/// Index of other (O) vertex on the other end of the edge. Edges are non-directed, do not misrepresent O as the "second/end" vertex!
			/// </summary>
			public int OVertexIndex;
			/// <summary>
			/// Previous edge's index in the edge cycle around the V0 vertex.
			/// </summary>
			public int APrevEdgeIndex;
			/// <summary>
			/// Next edge's index in the edge cycle around the V0 vertex.
			/// </summary>
			public int ANextEdgeIndex;
			/// <summary>
			/// Previous edge's index in the edge cycle around the V1 vertex.
			/// </summary>
			public int OPrevEdgeIndex;
			/// <summary>
			/// Next edge's index in the edge cycle around the V1 vertex.
			/// </summary>
			public int ONextEdgeIndex;

			/// <summary>
			/// True if the element hasn't been flagged for deletion.
			/// </summary>
			public bool IsValid => Index != UnsetIndex;

			/// <summary>
			/// Access vertex A and O by their indices.
			/// </summary>
			/// <param name="index">Returns A's index if index is 0, for all other values O's index is returned.</param>
			public int this[int index] => index == 0 ? AVertexIndex : OVertexIndex;

			/// <summary>
			/// Given a vertex index, returns the opposite vertex index.
			/// Ie if the given index is A's index, this method will return O's index. In all other cases it returns A's index.
			/// CAUTION: There is no check if the given vertexIndex is part of the edge!
			/// </summary>
			/// <param name="vertexIndex">one of the two vertex indexes of the edge</param>
			/// <returns></returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int GetOppositeVertexIndex(int vertexIndex) => vertexIndex == AVertexIndex ? OVertexIndex : AVertexIndex;

			/// <summary>
			/// Checks if the edge is connected to the vertex with the given index.
			/// </summary>
			/// <param name="vertexIndex"></param>
			/// <returns></returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool IsConnectedToVertex(int vertexIndex) => vertexIndex == AVertexIndex || vertexIndex == OVertexIndex;

			/// <summary>
			/// Checks if the edge is connecting the two vertices given bei their index.
			/// Returns true regardless of orientation of the edge. That is: if the input indices are either A/O or O/A of the edge.
			/// </summary>
			/// <param name="v0Index"></param>
			/// <param name="v1Index"></param>
			/// <returns>True if the two indices are either A's and O's indices or O's and A's indices of the edge.</returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool IsConnectingVertices(int v0Index, int v1Index) => v0Index == AVertexIndex && v1Index == OVertexIndex ||
			                                                              v0Index == OVertexIndex && v1Index == AVertexIndex;

			/// <summary>
			/// Checks if the edge is connecting the same vertices as the otherEdge, regardless of their vertex orientation.
			/// </summary>
			/// <param name="otherEdge"></param>
			/// <returns></returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool IsConnectingVertices(in Edge otherEdge) => IsConnectingVertices(otherEdge.AVertexIndex, otherEdge.OVertexIndex);

			/// <summary>
			/// Given a vertex index, returns the previous edge connected to that vertex.
			/// </summary>
			/// <param name="vertexIndex"></param>
			/// <returns></returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int GetPrevEdgeIndex(int vertexIndex) => vertexIndex == AVertexIndex ? APrevEdgeIndex : OPrevEdgeIndex;

			/// <summary>
			/// Given a vertex index, returns the next edge connected to that vertex.
			/// </summary>
			/// <param name="vertexIndex"></param>
			/// <returns></returns>
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public int GetNextEdgeIndex(int vertexIndex) => vertexIndex == AVertexIndex ? ANextEdgeIndex : ONextEdgeIndex;

			public override string ToString() => $"Edge [{Index}] with Verts [{AVertexIndex}, {OVertexIndex}], Loop [{BaseLoopIndex}], " +
			                                     $"V0 Prev/Next [{APrevEdgeIndex}]<>[{ANextEdgeIndex}], " +
			                                     $"V1 Prev/Next [{OPrevEdgeIndex}]<>[{ONextEdgeIndex}]";

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal void SetPrevEdgeIndex(int vertexIndex, int otherEdgeIndex)
			{
				if (vertexIndex == AVertexIndex) APrevEdgeIndex = otherEdgeIndex;
				else OPrevEdgeIndex = otherEdgeIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal void SetNextEdgeIndex(int vertexIndex, int otherEdgeIndex)
			{
				if (vertexIndex == AVertexIndex) ANextEdgeIndex = otherEdgeIndex;
				else ONextEdgeIndex = otherEdgeIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal void Invalidate() => Index = UnsetIndex;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static Edge Create(int vert0Index, int vert1Index, int loopIndex = UnsetIndex,
				int vert0PrevEdgeIndex = UnsetIndex, int vert0NextEdgeIndex = UnsetIndex,
				int vert1PrevEdgeIndex = UnsetIndex, int vert1NextEdgeIndex = UnsetIndex) => new()
			{
				Index = UnsetIndex, BaseLoopIndex = loopIndex, AVertexIndex = vert0Index, OVertexIndex = vert1Index,
				APrevEdgeIndex = vert0PrevEdgeIndex, ANextEdgeIndex = vert0NextEdgeIndex,
				OPrevEdgeIndex = vert1PrevEdgeIndex, ONextEdgeIndex = vert1NextEdgeIndex,
			};
		}

		/// <summary>
		/// A loop represents a directed, clockwise winding order of vertices/edges for a face.
		/// Loops are closely tied to a face, a loop cannot exist without a face that owns the loop.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Loop
		{
			/// <summary>
			/// Index of the loop in the Loops list.
			/// </summary>
			public int Index;
			/// <summary>
			/// Index of the face in the Faces list that owns this loop.
			/// </summary>
			public int FaceIndex;
			/// <summary>
			/// Index of the edge in the Edges list that the loop runs along.
			/// </summary>
			public int EdgeIndex;
			/// <summary>
			/// Index of the start vertex of the loop. The edge's other vertex is then the next loop's start vertex.
			/// </summary>
			public int StartVertexIndex;
			/// <summary>
			/// Index of the previous loop along the face.
			/// </summary>
			public int PrevLoopIndex;
			/// <summary>
			/// Index of the next loop along the face.
			/// </summary>
			public int NextLoopIndex;
			/// <summary>
			/// Index of the next radial loop (if any) around the edge.
			/// Can point to itself, for example a single quad will have no other radial loops on each edge.
			/// In most cases will simply point to a loop of the neighbouring face, indicated by Prev/NextRadialLoopIndex being identical.
			/// </summary>
			public int PrevRadialLoopIndex; // loops around edge
			/// <summary>
			/// Index of the previous radial loop (if any) around the edge.
			/// Can point to itself, for example a single quad will have no other radial loops on each edge.
			/// In most cases will simply point to a loop of the neighbouring face, indicated by Prev/NextRadialLoopIndex being identical.
			/// </summary>
			public int NextRadialLoopIndex; // loops around edge

			/// <summary>
			/// True if the element hasn't been flagged for deletion.
			/// </summary>
			public bool IsValid => Index != UnsetIndex;

			public override string ToString() => $"Loop [{Index}] of Face [{FaceIndex}], Edge [{EdgeIndex}], Vertex [{StartVertexIndex}], " +
			                                     $"Prev/Next [{PrevLoopIndex}]<>[{NextLoopIndex}], " +
			                                     $"Radial Prev/Next [{PrevRadialLoopIndex}]<>[{NextRadialLoopIndex}]";

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal void Invalidate() => Index = UnsetIndex;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static Loop Create(int faceIndex, int edgeIndex, int vertIndex,
				int prevRadialLoopIndex, int nextRadialLoopIndex, int prevLoopIndex, int nextLoopIndex) => new()
			{
				Index = UnsetIndex, FaceIndex = faceIndex, EdgeIndex = edgeIndex, StartVertexIndex = vertIndex,
				PrevRadialLoopIndex = prevRadialLoopIndex, NextRadialLoopIndex = nextRadialLoopIndex,
				PrevLoopIndex = prevLoopIndex, NextLoopIndex = nextLoopIndex,
			};
		}

		/// <summary>
		/// A face represents a closed polygon. Its loop cycle determines the winding order of its vertices/edges.
		/// 
		/// The graph allows for faces with any number of vertices, convex and concave faces, and even faces whose
		/// vertices do not all lie on the same plane. These are things that may not be supported during conversion
		/// of the graph to a Unity Mesh, thus the GMesh consumer must ensure that faces adhere to necessary specifications.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
		public struct Face
		{
			/// <summary>
			/// Index of the face in the Faces list.
			/// </summary>
			public int Index;
			/// <summary>
			/// Index of the first loop in the Loops list. Which loop becomes the first depends on order of vertices used to create face. 
			/// </summary>
			public int FirstLoopIndex;
			/// <summary>
			/// Number of elements this face encompasses, matching the number of vertices, loops and edges. Hence the more generic name.
			/// </summary>
			public int ElementCount;

			/// <summary>
			/// True if the face hasn't been marked as deleted.
			/// </summary>
			public bool IsValid => Index != UnsetIndex;

			public override string ToString() => $"Face [{Index}] has {ElementCount} verts, first Loop [{FirstLoopIndex}]";

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal void Invalidate() => Index = UnsetIndex;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static Face Create(int itemCount, int firstLoopIndex = UnsetIndex, int materialIndex = 0) => new()
				{ Index = UnsetIndex, FirstLoopIndex = firstLoopIndex, ElementCount = itemCount };
		}
	}
}