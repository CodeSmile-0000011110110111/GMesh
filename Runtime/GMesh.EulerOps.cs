// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
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
			SplitEdgeAndCreateVertex(ref splitEdge, CalculateCenter(splitEdge));

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
			GetVerticesPreferBaseEdgeVertex(splitEdge, out var keepVert, out var otherVert);
			SplitEdgeInternal_CreateEdgeAndVertex(pos, otherVert.Index, out var insertedEdge, out var newVertex);
			SplitEdgeInternal_TrySetOtherVertexBase(ref otherVert, splitEdge.Index, insertedEdge.Index);

			// update disk cycle of the new edge
			insertedEdge.CopyDiskCycleIndices(otherVert.Index, splitEdge); // move disk link to inserted edge at other vert
			insertedEdge.SetDiskCycleIndices(newVertex.Index, splitEdge.Index); // connect to split edge at the new vertex
			InsertEdgeInDiskCycle(insertedEdge, otherVert.Index);

			// relink existing edge to new vertex and update edge cycle for new vertex
			splitEdge.SetOppositeVertexIndex(keepVert.Index, newVertex.Index); // insert the new vertex
			splitEdge.SetDiskCycleIndices(newVertex.Index, insertedEdge.Index);

			// Split and insert loops (one or both, depends on whether it is a border edge)
			SplitEdgeInternal_CreateAndInsertLoops(ref splitEdge, ref insertedEdge, newVertex.Index);

			// write edges to graph
			SetEdge(insertedEdge);
			SetEdge(splitEdge);

			return insertedEdge.Index;
		}

		private void SplitEdgeInternal_TrySetOtherVertexBase(ref Vertex otherVert, int splitEdgeIndex, int insertedEdgeIndex)
		{
			// if both vertices of splitEdge had splitEdge as their base edge, set the other vertex edge to use inserted edge as base
			// this is done by preference, the baseEdgeIndex should rather point to a unique edge
			// we cannot just always set inserted edge as BaseEdgeIndex because we like to avoid unnecessary changes to the graph
			if (otherVert.BaseEdgeIndex == splitEdgeIndex)
			{
				otherVert.BaseEdgeIndex = insertedEdgeIndex;
				SetVertex(otherVert);
			}
		}

		private void SplitEdgeInternal_CreateAndInsertLoops(ref Edge splitEdge, ref Edge insertedEdge, int newVertexIndex)
		{
			// FIXME: assumption that there will only be at most two loops around an edge
			
			// get one of the loops around the edge (currently stretching past the new vertex)
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

			var insertedLoop1 = CreateAndInsertLoop(ref splitEdgeLoop, ref insertedEdge, newVertexIndex);

			// check if we need to split the loop on the other side, too
			if (splitEdgeLoop.IsBorderLoop() == false)
			{
				splitEdgeOppositeLoop.EdgeIndex = insertedEdge.Index;
				insertedEdge.BaseLoopIndex = splitEdgeOppositeLoop.Index;

				var insertedLoop2 = CreateAndInsertLoop(ref splitEdgeOppositeLoop, ref splitEdge, newVertexIndex);
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

		private void InsertEdgeInDiskCycle(in Edge insertedEdge, int vertexIndex)
		{
			var (prevEdgeIndex, nextEdgeIndex) = insertedEdge.GetDiskCycleIndices(vertexIndex);

			// insert new edge into disk cycle of otherVert (where new Edge connects to splitEdge's "other" vertex)
			var prevEdge = GetEdge(prevEdgeIndex);
			prevEdge.SetNextEdgeIndex(vertexIndex, insertedEdge.Index);
			SetEdge(prevEdge);

			// check if there were already more than 2 edges connected to this vertex, if so, update next edge too
			if (prevEdgeIndex != nextEdgeIndex)
			{
				var nextEdge = GetEdge(nextEdgeIndex);
				nextEdge.SetPrevEdgeIndex(vertexIndex, insertedEdge.Index);
				SetEdge(nextEdge);
			}
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

		private void GetVerticesPreferBaseEdgeVertex(in Edge edge, out Vertex baseVertex, out Vertex otherVertex)
		{
			var vertexA = GetVertex(edge.AVertexIndex);
			var vertexO = GetVertex(edge.OVertexIndex);
			baseVertex = edge.Index == vertexA.BaseEdgeIndex ? vertexA : vertexO;
			otherVertex = baseVertex.Index == vertexO.Index ? vertexA : vertexO;
		}

		private Loop CreateAndInsertLoop(ref Loop existingLoop, ref Edge newLoopEdge, int loopVertexIndex)
		{
			// Create and insert the new loop on the same face
			var newLoopIndex = LoopCount;
			newLoopEdge.BaseLoopIndex = newLoopIndex;

			var newLoop = Loop.Create(existingLoop.FaceIndex, newLoopEdge.Index, loopVertexIndex,
				newLoopIndex, newLoopIndex, existingLoop.Index, existingLoop.NextLoopIndex);
			var newLoopIndexAfterAdd = AddLoop(ref newLoop);
			if (newLoopIndexAfterAdd != newLoopIndex)
				throw new OperationCanceledException("index not as expected");

			InsertLoopAfter(ref existingLoop, newLoopIndex);
			IncrementFaceElementCount(existingLoop.FaceIndex);

			// verification that loop's edge contains loop's vertex:
			if (newLoopEdge.ContainsVertex(newLoop.StartVertexIndex) == false)
				throw new OperationCanceledException($"new: edge does not contain loop vertex:\n{newLoop}\n{newLoopEdge}");
			if (GetEdge(existingLoop.EdgeIndex).ContainsVertex(existingLoop.StartVertexIndex) == false)
				throw new OperationCanceledException(
					$"existing: edge does not contain loop vertex:\n{existingLoop}\n{GetEdge(existingLoop.EdgeIndex)}");

			return newLoop;
		}

		private void InsertLoopAfter(ref Loop existingLoop, int newLoopIndex)
		{
			var nextLoopIndex = existingLoop.NextLoopIndex;
			existingLoop.NextLoopIndex = newLoopIndex;

			var nextLoop = GetLoop(nextLoopIndex);
			nextLoop.PrevLoopIndex = newLoopIndex;
			SetLoop(nextLoop);
		}

		private void IncrementFaceElementCount(int faceIndex)
		{
			var face = GetFace(faceIndex);
			face.ElementCount++;
			SetFace(face);
		}

		/*
		 * JOIN EDGE KILL VERT:
		 * Takes a pointer to an edge (ke) and pointer to one of its vertices (kv) and collapses
		 * the edge on that vertex. First ke is removed from the disk cycle of both kv and tv.
		 * Then the edge oe is relinked to run between ov and tv and is added to the disk cycle of ov.
		 * Finally the radial cycle of oe is traversed and all of its face loops are updated.
		 * Note that in order for this euler to work, kv must have exactly only two edges coincident
		 * upon it (valance of 2).
		 * A more generalized edge collapse function can be built using a combination of
		 * split_face_make_edge, join_face_kill_edge and join_edge_kill_vert.
		 * Returns true for success
		 */
		public bool JoinEdgeAndDeleteVertex(int joinEdgeIndex, int deleteVertexIndex)
		{
			var joinEdge = GetEdge(joinEdgeIndex);
			var deleteVertex = GetVertex(deleteVertexIndex);
			return JoinEdgeAndDeleteVertex(ref joinEdge, ref deleteVertex);
		}

		public bool JoinEdgeAndDeleteVertex(ref Edge joinEdge, ref Vertex deleteVertex)
		{
			if (joinEdge.IsValid == false || deleteVertex.IsValid == false || CalculateEdgeCount(deleteVertex) != 2)
				return false;

			// remove edge from disk cycle of A and O
			// relink edge
			// join loops

			return true;
		}

		/*
		 * SPLIT FACE MAKE EDGE:
		 * Takes as input two vertices in a single face. An edge is created which divides
		 * the original face into two distinct regions. One of the regions is assigned to
		 * the original face and it is closed off. The second region has a new face assigned to it.
		 * Note that if the input vertices share an edge this will create a face with only two edges.
		 * Returns - new Face and new Edge indices
		 */
		public (int, int) SplitFaceAndCreateEdge(int faceIndex) => throw new NotImplementedException();

		/*
		 * JOIN FACE KILL EDGE:
		 * Takes two faces joined by a single 2-manifold edge and fuses them together.
		 * The edge shared by the faces must not be connected to any other edges which have
		 * both faces in its radial cycle.
		 * An illustration of this appears in the figure on the right. In this diagram,
		 * situation A is the only one in which join_face_kill_edge will return with a value
		 * indicating success. If the tool author wants to join two seperate faces which have
		 * multiple edges joining them as in situation B they should run JEKV on the excess
		 * edge(s) first. In the case of situation none of the edges joining the two faces
		 * can be safely removed because it would cause a face that loops back on itself.
		 * Also note that the order of arguments decides whether or not certain per-face
		 * attributes are present in the resultant face. For instance vertex winding,
		 * material index, smooth flags, ect are inherited from f1, not f2.
		 * Returns - true for success
		 */
		public bool JoinFacesAndDeleteEdge(int face0Index, int face1Index) => throw new NotImplementedException();

		/*
		 * Flip face by reversing its loops.
		 */
		public void FlipFace(int faceIndex) => throw new NotImplementedException();
	}
}