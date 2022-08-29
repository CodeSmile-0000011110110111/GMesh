// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// A vertex is primarily a coordinate in space.
		/// </summary>
		[Serializable] [BurstCompile] [StructLayout(LayoutKind.Sequential)]
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

			internal static Vertex Create(float3 position, int baseEdgeIndex = UnsetIndex) => new()
				{ Index = UnsetIndex, BaseEdgeIndex = baseEdgeIndex, Position = position };

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

			internal void Invalidate() => Index = UnsetIndex;
		}

		/// <summary>
		/// An edge is the line between two vertices.
		/// An edge has no orientation, the two vertices can be at either end.
		/// Use GetOtherVertexIndex(int vertexIndex) to get from the given vertex of the edge to the other vertex. 
		/// </summary>
		[Serializable] [BurstCompile] [StructLayout(LayoutKind.Sequential)]
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

			internal static Edge Create(int aVertIndex, int oVertIndex, int loopIndex = UnsetIndex,
				int aPrevEdgeIndex = UnsetIndex, int aNextEdgeIndex = UnsetIndex,
				int oPrevEdgeIndex = UnsetIndex, int oNextEdgeIndex = UnsetIndex) => new()
			{
				Index = UnsetIndex, BaseLoopIndex = loopIndex, AVertexIndex = aVertIndex, OVertexIndex = oVertIndex,
				APrevEdgeIndex = aPrevEdgeIndex, ANextEdgeIndex = aNextEdgeIndex,
				OPrevEdgeIndex = oPrevEdgeIndex, ONextEdgeIndex = oNextEdgeIndex,
			};

			/// <summary>
			/// True if the element hasn't been flagged for deletion.
			/// </summary>
			public bool IsValid => Index != UnsetIndex;

			/// <summary>
			/// Access vertex A and O by their indices (0 or 1).
			/// </summary>
			/// <param name="index">Returns A's index if index is 0. Returns O's index if index is 1.
			/// Throws IndexOutOfRangeException for all other indices.</param>
			public int this[int index] => index == 0 ? AVertexIndex :
				index == 1 ? OVertexIndex :
				throw new IndexOutOfRangeException("only indices 0 and 1 allowed");

			/// <summary>
			/// Checks if the edge is connected to the vertex with the given index.
			/// </summary>
			/// <param name="vertexIndex"></param>
			/// <returns></returns>
			[BurstCompile]
			public bool ContainsVertex(int vertexIndex) => vertexIndex == AVertexIndex || vertexIndex == OVertexIndex;

			/// <summary>
			/// Checks if the edge is connecting the two vertices given bei their index.
			/// Returns true regardless of orientation of the edge. That is: if the input indices are either A/O or O/A of the edge.
			/// </summary>
			/// <param name="v0Index"></param>
			/// <param name="v1Index"></param>
			/// <returns>True if the two indices are either A's and O's indices or O's and A's indices of the edge.</returns>
			[BurstCompile]
			public bool IsConnectingVertices(int v0Index, int v1Index) => v0Index == AVertexIndex && v1Index == OVertexIndex ||
			                                                              v0Index == OVertexIndex && v1Index == AVertexIndex;

			/// <summary>
			/// Checks if the edge is connecting the same vertices as the otherEdge, regardless of their vertex orientation.
			/// </summary>
			/// <param name="otherEdge"></param>
			/// <returns></returns>
			[BurstCompile]
			public bool IsConnectingVertices(in Edge otherEdge) => IsConnectingVertices(otherEdge.AVertexIndex, otherEdge.OVertexIndex);

			/// <summary>
			/// Given a vertex index, returns the vertex index at the opposite end of the edge.
			/// CAUTION: There is no check if the given vertexIndex is part of the edge!
			/// </summary>
			/// <param name="vertexIndex">one of the two vertex indexes of the edge</param>
			/// <param name="noThrow">If true, will not throw exception if vertexIndex is neither A nor O.</param>
			/// <returns></returns>
			[BurstCompile]
			public int GetOppositeVertexIndex(int vertexIndex) => vertexIndex == AVertexIndex ? OVertexIndex :
				vertexIndex == OVertexIndex ? AVertexIndex :
				throw new InvalidOperationException($"Vertex {vertexIndex} is not connected by Edge {Index}");

			/// <summary>
			/// Returns the vertex index that both edges connect to.
			/// Note: It is assumed they share a common vertex.
			/// </summary>
			/// <param name="otherEdge"></param>
			/// <returns>the vertex index both connect to, or UnsetIndex if they do not connect</returns>
			[BurstCompile]
			public int GetConnectingVertexIndex(in Edge otherEdge) =>
				AVertexIndex == otherEdge.AVertexIndex ? AVertexIndex :
				AVertexIndex == otherEdge.OVertexIndex ? AVertexIndex :
				OVertexIndex == otherEdge.AVertexIndex ? OVertexIndex :
				OVertexIndex == otherEdge.OVertexIndex ? OVertexIndex : UnsetIndex;

			/// <summary>
			/// Given a vertex index, returns the previous edge connected to that vertex.
			/// </summary>
			/// <param name="vertexIndex"></param>
			/// <returns></returns>
			[BurstCompile]
			public int GetPrevEdgeIndex(int vertexIndex) => vertexIndex == AVertexIndex ? APrevEdgeIndex :
				vertexIndex == OVertexIndex ? OPrevEdgeIndex :
				throw new InvalidOperationException($"Vertex {vertexIndex} is not connected by {this}");

			/// <summary>
			/// Given a vertex index, returns the next edge connected to that vertex.
			/// </summary>
			/// <param name="vertexIndex"></param>
			/// <returns></returns>
			[BurstCompile]
			public int GetNextEdgeIndex(int vertexIndex) => vertexIndex == AVertexIndex ? ANextEdgeIndex :
				vertexIndex == OVertexIndex ? ONextEdgeIndex :
				throw new InvalidOperationException($"Vertex {vertexIndex} is not connected by {this}");

			/// <summary>
			/// Returns both prev and next edge indices for the given vertex.
			/// </summary>
			/// <param name="vertexIndex"></param>
			/// <returns></returns>
			/// <exception cref="InvalidOperationException">Thrown if the vertexIndex is not linked to this edge</exception>
			[BurstCompile]
			public (int, int) GetDiskCycleIndices(int vertexIndex) => vertexIndex == AVertexIndex ? (APrevEdgeIndex, ANextEdgeIndex) :
				vertexIndex == OVertexIndex ? (OPrevEdgeIndex, ONextEdgeIndex) :
				throw new InvalidOperationException($"Vertex {vertexIndex} is not connected by Edge {Index}");

			public override string ToString() => $"Edge [{Index}] with Verts A[{AVertexIndex}], O[{OVertexIndex}], " +
			                                     $"Cycle A <{APrevEdgeIndex}°{ANextEdgeIndex}>, " +
			                                     $"Cycle O <{OPrevEdgeIndex}°{ONextEdgeIndex}>, " +
			                                     $"Loop [{BaseLoopIndex}]";

			[BurstCompile] internal int GetOppositeVertexIndexNoThrow(int vertexIndex) =>
				vertexIndex == AVertexIndex ? OVertexIndex : AVertexIndex;

			[BurstCompile]
			internal void SetPrevEdgeIndex(int vertexIndex, int otherEdgeIndex)
			{
				if (vertexIndex == AVertexIndex) APrevEdgeIndex = otherEdgeIndex;
				else OPrevEdgeIndex = otherEdgeIndex;
			}

			[BurstCompile]
			internal void SetNextEdgeIndex(int vertexIndex, int otherEdgeIndex)
			{
				if (vertexIndex == AVertexIndex) ANextEdgeIndex = otherEdgeIndex;
				else ONextEdgeIndex = otherEdgeIndex;
			}

			[BurstCompile]
			internal void SetDiskCycleIndices(int vertexIndex, int edgeIndex)
			{
				SetPrevEdgeIndex(vertexIndex, edgeIndex);
				SetNextEdgeIndex(vertexIndex, edgeIndex);
			}

			[BurstCompile]
			internal void CopyDiskCycleFrom(int vertexIndex, in Edge sourceEdge)
			{
				SetPrevEdgeIndex(vertexIndex, sourceEdge.GetPrevEdgeIndex(vertexIndex));
				SetNextEdgeIndex(vertexIndex, sourceEdge.GetNextEdgeIndex(vertexIndex));
			}

			[BurstCompile]
			internal void SetOppositeVertexIndex(int oppositeVertexIndex, int newVertexIndex)
			{
				if (oppositeVertexIndex == AVertexIndex) OVertexIndex = newVertexIndex;
				else AVertexIndex = newVertexIndex;
			}

			[BurstCompile]
			internal void Invalidate() => Index = UnsetIndex;
		}

		/// <summary>
		/// A loop represents a directed, clockwise winding order of vertices/edges for a face.
		/// Loops are closely tied to a face, a loop cannot exist without a face that owns the loop.
		/// </summary>
		[Serializable] [BurstCompile] [StructLayout(LayoutKind.Sequential)]
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

			internal static Loop Create(int faceIndex, int edgeIndex, int vertIndex,
				int prevRadialLoopIndex, int nextRadialLoopIndex, int prevLoopIndex, int nextLoopIndex) => new()
			{
				Index = UnsetIndex, FaceIndex = faceIndex, EdgeIndex = edgeIndex, StartVertexIndex = vertIndex,
				PrevRadialLoopIndex = prevRadialLoopIndex, NextRadialLoopIndex = nextRadialLoopIndex,
				PrevLoopIndex = prevLoopIndex, NextLoopIndex = nextLoopIndex,
			};

			/// <summary>
			/// True if the element hasn't been flagged for deletion.
			/// </summary>
			public bool IsValid => Index != UnsetIndex;

			/// <summary>
			/// Returns true if the loop is on a border (no other radial loops, just itself).
			/// </summary>
			/// <returns></returns>
			public bool IsBorderLoop() => Index == PrevRadialLoopIndex && Index == NextRadialLoopIndex;

			public override string ToString() => $"Loop [{Index}] of Face [{FaceIndex}], Edge [{EdgeIndex}], Vertex [{StartVertexIndex}], " +
			                                     $"Cycle <{PrevLoopIndex}°{NextLoopIndex}>, Radial <{PrevRadialLoopIndex}°{NextRadialLoopIndex}>";

			internal void SetRadialLoopIndices(int loopIndex) => PrevRadialLoopIndex = NextRadialLoopIndex = loopIndex;

			internal void Invalidate() => Index = UnsetIndex;
		}

		/// <summary>
		/// A face represents a closed polygon. Its loop cycle determines the winding order of its vertices/edges.
		/// 
		/// The graph allows for faces with any number of vertices, convex and concave faces, and even faces whose
		/// vertices do not all lie on the same plane. These are things that may not be supported during conversion
		/// of the graph to a Unity Mesh, thus the GMesh consumer must ensure that faces adhere to necessary specifications.
		/// </summary>
		[Serializable] [BurstCompile] [StructLayout(LayoutKind.Sequential)]
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

			internal static Face Create(int itemCount, int firstLoopIndex = UnsetIndex, int materialIndex = 0) => new()
				{ Index = UnsetIndex, FirstLoopIndex = firstLoopIndex, ElementCount = itemCount };

			/// <summary>
			/// True if the face hasn't been marked as deleted.
			/// </summary>
			public bool IsValid => Index != UnsetIndex;

			public override string ToString() => $"Face [{Index}] has {ElementCount} verts, first Loop [{FirstLoopIndex}]";

			internal void Invalidate() => Index = UnsetIndex;
		}
	}
}