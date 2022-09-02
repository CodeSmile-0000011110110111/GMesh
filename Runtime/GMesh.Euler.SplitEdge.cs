// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		public int SplitEdgeAtVertex(int splitEdgeIndex, int splitVertexIndex)
		{
			var splitEdge = GetEdge(splitEdgeIndex);
			var splitVertex = GetVertex(splitVertexIndex);
			return SplitEdgeAtVertex(ref splitEdge, ref splitVertex);
		}

		/// <summary>
		/// Test splitting at existing vertex - this needs more work / test cases.
		/// </summary>
		/// <param name="splitEdge"></param>
		/// <param name="splitVertex"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public int SplitEdgeAtVertex(ref Edge splitEdge, ref Vertex splitVertex)
		{
			if (splitEdge.IsValid == false || splitVertex.IsValid == false)
				return UnsetIndex;

			// prefer the vertex that's pointing to the split edge to remain with split edge
			SplitEdgeInternal_GetVerticesPreferBaseEdgeVertex(splitEdge, out var keepVert, out var otherVert);

			var insertedEdge = Edge.Create(splitVertex.Index, otherVert.Index);
			AddEdge(ref insertedEdge);

			// make new vertex point to new edge
			if (splitVertex.BaseEdgeIndex == UnsetIndex)
			{
				splitVertex.BaseEdgeIndex = insertedEdge.Index;
				SetVertex(splitVertex);
			}

			// in case both edge vertices' BaseEdge point to the splitEdge
			SplitEdgeInternal_UpdateOtherVertexBaseEdgeIfNeeded(ref otherVert, splitEdge.Index, insertedEdge.Index);
			// replace split edge in disk cycle where inserted edge took its place
			SplitEdgeInternal_ReplaceEdgeInDiskCycle(otherVert.Index, splitEdge, in insertedEdge);
			// connect the two edges together at the new vertex
			SplitEdgeInternal_ReconnectEdges(ref splitEdge, ref insertedEdge, splitVertex.Index, keepVert.Index, otherVert.Index);
			// Split and insert loops (one or both, depends on whether it is a border edge)
			SplitEdgeInternal_CreateAndInsertLoops(ref splitEdge, ref insertedEdge, splitVertex.Index);

			// write edges to graph
			SetEdge(insertedEdge);
			SetEdge(splitEdge);

#if GMESH_VALIDATION
			if (ValidateVertexDiskCycle(keepVert, out var issue) == false) throw new Exception(issue);
			if (ValidateVertexDiskCycle(otherVert, out var issue2) == false) throw new Exception(issue2);
#endif

			return insertedEdge.Index;
		}

		/// <summary>
		/// Splits an edge at its center. Adds a new vertex and updates face loops.
		/// </summary>
		/// <param name="splitEdgeIndex"></param>
		/// <returns>the index of the new edge</returns>
		public int SplitEdgeAndCreateVertex(int splitEdgeIndex) =>
			SplitEdgeAndCreateVertex(splitEdgeIndex, CalculateEdgeCenter(splitEdgeIndex));

		/// <summary>
		/// Splits an edge at its center. Adds a new vertex and updates face loops.
		/// </summary>
		/// <param name="splitEdge"></param>
		/// <returns>the index of the new edge</returns>
		public int SplitEdgeAndCreateVertex(ref Edge splitEdge) =>
			SplitEdgeAndCreateVertex(ref splitEdge, CalculateEdgeCenter(splitEdge));

		/// <summary>
		/// Splits an edge by inserting a vertex at the given position. Updates face loops.
		/// Note: no checks are performed regarding vertex position and the new face's shape. 
		/// </summary>
		/// <param name="splitEdgeIndex"></param>
		/// <param name="pos"></param>
		/// <returns>the index of the new edge</returns>
		public int SplitEdgeAndCreateVertex(int splitEdgeIndex, float3 pos)
		{
			var splitEdge = GetEdge(splitEdgeIndex);
			return SplitEdgeAndCreateVertex(ref splitEdge, pos);
		}

		/// <summary>
		/// Splits an edge by inserting a vertex at the given position. Updates face loops.
		/// Note: no checks are performed regarding vertex position and the new face's shape. 
		/// </summary>
		/// <param name="splitEdge"></param>
		/// <param name="pos"></param>
		/// <returns>the index of the new edge</returns>
		public int SplitEdgeAndCreateVertex(ref Edge splitEdge, float3 pos)
		{
			if (splitEdge.IsValid == false)
				return UnsetIndex;

			// prefer the vertex that's pointing to the split edge to remain with split edge
			SplitEdgeInternal_GetVerticesPreferBaseEdgeVertex(splitEdge, out var keepVert, out var otherVert);
			SplitEdgeInternal_CreateEdgeAndVertex(pos, otherVert.Index, out var insertedEdge, out var newVert);
			// in case both edge vertices' BaseEdge point to the splitEdge
			SplitEdgeInternal_UpdateOtherVertexBaseEdgeIfNeeded(ref otherVert, splitEdge.Index, insertedEdge.Index);
			// replace split edge in disk cycle where inserted edge took its place
			SplitEdgeInternal_ReplaceEdgeInDiskCycle(otherVert.Index, splitEdge, in insertedEdge);
			// connect the two edges together at the new vertex
			SplitEdgeInternal_ReconnectEdges(ref splitEdge, ref insertedEdge, newVert.Index, keepVert.Index, otherVert.Index);
			// Split and insert loops (one or both, depends on whether it is a border edge)
			SplitEdgeInternal_CreateAndInsertLoops(ref splitEdge, ref insertedEdge, newVert.Index);

			// write edges to graph
			SetEdge(insertedEdge);
			SetEdge(splitEdge);

#if GMESH_VALIDATION
			if (ValidateVertexDiskCycle(keepVert, out var issue) == false) throw new Exception(issue);
			if (ValidateVertexDiskCycle(otherVert, out var issue2) == false) throw new Exception(issue2);
#endif

			return insertedEdge.Index;
		}

		private void SplitEdgeInternal_GetVerticesPreferBaseEdgeVertex(in Edge edge, out Vertex baseVertex, out Vertex otherVertex)
		{
			var vertexA = GetVertex(edge.AVertexIndex);
			var vertexO = GetVertex(edge.OVertexIndex);
			baseVertex = edge.Index == vertexA.BaseEdgeIndex ? vertexA : vertexO;
			otherVertex = baseVertex.Index == vertexO.Index ? vertexA : vertexO;
		}

		private void SplitEdgeInternal_ReconnectEdges(ref Edge splitEdge, ref Edge insertedEdge, int newVertIndex, int keepVertIndex,
			int otherVertIndex)
		{
			insertedEdge.CopyDiskCycleFrom(otherVertIndex, splitEdge);
			insertedEdge.SetDiskCycleIndices(newVertIndex, splitEdge.Index);
			splitEdge.SetOppositeVertexIndex(keepVertIndex, newVertIndex);
			splitEdge.SetDiskCycleIndices(newVertIndex, insertedEdge.Index);
		}

		private void SplitEdgeInternal_ReplaceEdgeInDiskCycle(int vertexIndex, in Edge splitEdge, in Edge insertedEdge)
		{
			// replace split edge in disk cycle where inserted edge took its place
			var (otherPrevEdgeIndex, otherNextEdgeIndex) = splitEdge.GetDiskCycleIndices(vertexIndex);
			if (otherPrevEdgeIndex == otherNextEdgeIndex)
			{
				var otherEdge = GetEdge(otherPrevEdgeIndex);
				otherEdge.SetDiskCycleIndices(vertexIndex, insertedEdge.Index);
				SetEdge(otherEdge);
			}
			else
			{
				var otherPrevEdge = GetEdge(otherPrevEdgeIndex);
				otherPrevEdge.SetNextEdgeIndex(vertexIndex, insertedEdge.Index);
				var otherNextEdge = GetEdge(otherNextEdgeIndex);
				otherNextEdge.SetPrevEdgeIndex(vertexIndex, insertedEdge.Index);
				SetEdge(otherPrevEdge);
				SetEdge(otherNextEdge);
			}
		}

		private void SplitEdgeInternal_UpdateOtherVertexBaseEdgeIfNeeded(ref Vertex otherVert, int splitEdgeIndex, int insertedEdgeIndex)
		{
			// if both vertices of splitEdge had splitEdge as their base edge, set the other vertex edge to use inserted edge as base
			// this is done by preference: baseEdgeIndex should ideally point to a unique edge (but doesn't have to)
			// but: we cannot just set inserted edge as BaseEdgeIndex indiscriminately => avoid unnecessary changes to the graph
			if (otherVert.BaseEdgeIndex == splitEdgeIndex)
			{
				otherVert.BaseEdgeIndex = insertedEdgeIndex;
				SetVertex(otherVert);
			}
		}

		private void SplitEdgeInternal_CreateAndInsertLoops(ref Edge splitEdge, ref Edge insertedEdge, int newVertexIndex)
		{
			// get one of the loops around the split edge (currently stretching past the new vertex)
			var splitEdgeLoop = GetLoop(splitEdge.BaseLoopIndex);
			var splitEdgeOppositeLoop = GetLoop(splitEdgeLoop.NextRadialLoopIndex);

			// if the edge's base loop does not belong to us, update the edge's BaseLoop and switch loops
			if (splitEdge.ContainsVertex(splitEdgeLoop.StartVertexIndex) == false)
			{
				splitEdge.BaseLoopIndex = splitEdgeOppositeLoop.Index;
				var tempLoop = splitEdgeLoop;
				splitEdgeLoop = splitEdgeOppositeLoop;
				splitEdgeOppositeLoop = tempLoop;
			}

			var insertedLoop1 = InsertLoop(ref splitEdgeLoop, ref insertedEdge, newVertexIndex);

			// FIXME: assumption that there will only be at most two loops around an edge
			// check if we need to split the loop on the other side, too
			if (splitEdgeLoop.IsBorderLoop() == false)
			{
				splitEdgeOppositeLoop.EdgeIndex = insertedEdge.Index;
				insertedEdge.BaseLoopIndex = splitEdgeOppositeLoop.Index;

				var insertedLoop2 = InsertLoop(ref splitEdgeOppositeLoop, ref splitEdge, newVertexIndex);
				insertedLoop1.SetRadialLoopIndices(splitEdgeOppositeLoop.Index);
				insertedLoop2.SetRadialLoopIndices(splitEdgeLoop.Index);
				splitEdgeLoop.SetRadialLoopIndices(insertedLoop2.Index);
				splitEdgeOppositeLoop.SetRadialLoopIndices(insertedLoop1.Index);

				SetLoop(insertedLoop2);
				SetLoop(splitEdgeOppositeLoop);
			}

			SetLoop(insertedLoop1);
			SetLoop(splitEdgeLoop);
		}

		private void SplitEdgeInternal_CreateEdgeAndVertex(float3 pos, int otherVertexIndex, out Edge insertedEdge, out Vertex newVertex)
		{
			newVertex = GetVertex(CreateVertex(pos));
			insertedEdge = Edge.Create(newVertex.Index, otherVertexIndex);
			AddEdge(ref insertedEdge);

			// make new vertex point to new edge
			newVertex.BaseEdgeIndex = insertedEdge.Index;
			SetVertex(newVertex);
		}
	}
}