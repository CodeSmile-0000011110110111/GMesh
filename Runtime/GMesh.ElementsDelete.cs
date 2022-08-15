// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Deletes a face and its associated loop cycle.
		/// Note: this does NOT delete edges and vertices. Can be used to create a "hole" (missing face) in the mesh,
		/// either intentionally or by accident.
		/// </summary>
		/// <param name="faceIndex"></param>
		public void DeleteFace(int faceIndex)
		{
			var face = GetFace(faceIndex);
			Debug.Assert(face.IsValid);
			
			var loopIndex = face.FirstLoopIndex;
			var nextLoopIndex = UnsetIndex;
			
			do
			{
				var loop = GetLoop(loopIndex);
				nextLoopIndex = loop.NextLoopIndex;
				loop.FaceIndex = UnsetIndex; // detach loop from face, tells DeleteLoopFromFace that it's okay to delete the loop
				DeleteLoopInternal_DeleteDetachedLoop(loop);
				loopIndex = nextLoopIndex;
			} while (nextLoopIndex != face.FirstLoopIndex);

			InvalidateFace(faceIndex);
			RemoveInvalidatedElements();
		}

		/// <summary>
		/// Deletes a loop. Checks if the loop has a face associated with it, if so, it will also delete the face
		/// and any other loop associated with that face.
		/// </summary>
		/// <param name="loopIndex"></param>
		private void DeleteLoop(int loopIndex)
		{
			var loop = GetLoop(loopIndex);
			
			// if loop is associated with a face, delete the face which will call DeleteLoopFromFace
			if (loop.IsValid && loop.FaceIndex != UnsetIndex)
			{
				DeleteFace(loop.FaceIndex);
				return;
			}

			DeleteLoopInternal_DeleteDetachedLoop(loop);
		}
		/// <summary>
		/// Deletes an edge, as well as its loops and thus all the faces connecting to it.
		/// </summary>
		/// <param name="edgeIndex"></param>
		public void DeleteEdge(int edgeIndex)
		{
			var edge = GetEdge(edgeIndex);
			if (edge.IsValid)
				DeleteEdgeInternal_DeleteEdgeLoopsAndFaces(ref edge);
			
			// edge may have been deleted above
			if (edge.IsValid)
			{
				DeleteEdgeInternal_UpdateOrDeleteVertices(edge);
				DeleteEdgeInternal_UpdateEdgeCycle(edge);
				InvalidateEdge(edgeIndex);
			}
		}

		/// <summary>
		/// Deletes a vertex, as well as all edges and faces connected to it.
		/// In other words: if you had a GMesh with just a single face, then deleting any vertex will clear the whole mesh. 
		/// </summary>
		/// <param name="vertexIndex"></param>
		public void DeleteVertex(int vertexIndex)
		{
			var vertex = GetVertex(vertexIndex);
			while (vertex.IsValid && vertex.BaseEdgeIndex != UnsetIndex)
			{
				DeleteEdge(vertex.BaseEdgeIndex);

				// get again => vertex is modified by DeleteEdge
				vertex = GetVertex(vertexIndex);
			}

			if (vertex.IsValid)
				InvalidateVertex(vertex.Index);
		}

		/// <summary>
		/// When deleting a loop this updates the edge's references to that loop.
		/// CAUTION: Delete*Internal() methods should only be called by respective Delete*() methods.
		/// </summary>
		/// <param name="loop"></param>
		private void DeleteLoopInternal_UpdateOrDeleteEdge(in Loop loop)
		{
			var loopEdge = GetEdge(loop.EdgeIndex);
			if (loop.NextRadialLoopIndex == loop.Index)
			{
				// It's the last loop for this edge thus clear edge's loop index:
				loopEdge.LoopIndex = UnsetIndex;
				SetEdge(loopEdge);
				
				// this edge is no longer needed
				DeleteEdge(loopEdge.Index);
			}
			else
			{
				DeleteLoopInternal_DetachFromRadialLoopCycle(loop);

				// if edge refers to this loop, make it point to the next radial loop
				if (loopEdge.LoopIndex == loop.Index)
				{
					loopEdge.LoopIndex = loop.NextRadialLoopIndex;
					SetEdge(loopEdge);
				}
			}
		}

		/// <summary>
		/// Detaches a loop from the radial cycle of its edge.
		/// CAUTION: Delete*Internal() methods should only be called by respective Delete*() methods.
		/// </summary>
		/// <param name="loop"></param>
		private void DeleteLoopInternal_DetachFromRadialLoopCycle(in Loop loop)
		{
			var prevRadialLoop = GetLoop(loop.PrevRadialLoopIndex);
			prevRadialLoop.NextRadialLoopIndex = loop.NextRadialLoopIndex;
			SetLoop(prevRadialLoop);

			var nextRadialLoop = GetLoop(loop.NextRadialLoopIndex);
			nextRadialLoop.PrevRadialLoopIndex = loop.PrevRadialLoopIndex;
			SetLoop(nextRadialLoop);
		}

		/// <summary>
		/// Deletes a loop from a face without trying to delete the face as well.
		/// CAUTION: Delete*Internal() methods should only be called by respective Delete*() methods.
		/// </summary>
		/// <param name="loop"></param>
		private void DeleteLoopInternal_DeleteDetachedLoop(in Loop loop)
		{
			Debug.Assert(loop.FaceIndex == UnsetIndex, "DeleteLoopFromFace cannot be called on loops still attached to a face");
			if (loop.IsValid)
			{
				DeleteLoopInternal_UpdateOrDeleteEdge(loop);
				InvalidateLoop(loop.Index);
			}
		}
		

		/// <summary>
		/// Deletes all the loops of an edge. This can cause faces to be deleted as well.
		/// CAUTION: Delete*Internal() methods should only be called by respective Delete*() methods.
		/// </summary>
		/// <param name="edge">Edge passed by reference because it may be modified by DeleteLoop()</param>
		private void DeleteEdgeInternal_DeleteEdgeLoopsAndFaces(ref Edge edge)
		{
			// delete all the loops
			while (edge.LoopIndex != UnsetIndex)
			{
				DeleteLoop(edge.LoopIndex);

				// get again => edge is modified by DeleteLoop
				edge = GetEdge(edge.Index);
			}
		}

		/// <summary>
		/// Updates vertex edge indices of the edge that is about to be deleted.
		/// CAUTION: Delete*Internal() methods should only be called by respective Delete*() methods.
		/// </summary>
		/// <param name="edge"></param>
		private void DeleteEdgeInternal_UpdateOrDeleteVertices(in Edge edge)
		{
			var edgeIndex = edge.Index;

			var v0 = GetVertex(edge.Vertex0Index);
			if (edgeIndex == v0.BaseEdgeIndex)
			{
				if (edgeIndex != edge.V0NextRadialEdgeIndex)
				{
					v0.BaseEdgeIndex = edge.V0NextRadialEdgeIndex;
					SetVertex(v0);
				}
				else
				{
					// remove orphaned vertex
					InvalidateVertex(v0.Index);
				}
			}

			var v1 = GetVertex(edge.Vertex1Index);
			if (edgeIndex == v1.BaseEdgeIndex)
			{
				if (edgeIndex != edge.V1NextRadialEdgeIndex)
				{
					v1.BaseEdgeIndex = edge.V1NextRadialEdgeIndex;
					SetVertex(v1);
				}
				else
				{
					// remove orphaned vertex
					InvalidateVertex(v1.Index);
				}
			}
		}

		/// <summary>
		/// Updates the edge cycle of the to be deleted edge and its neighbours.
		/// CAUTION: Delete*Internal() methods should only be called by respective Delete*() methods.
		/// </summary>
		/// <param name="edge"></param>
		private void DeleteEdgeInternal_UpdateEdgeCycle(in Edge edge)
		{
			var prev0Edge = GetEdge(edge.V0PrevRadialEdgeIndex);
			if (prev0Edge.IsValid)
			{
				prev0Edge.SetNextRadialEdgeIndex(edge.Vertex0Index, edge.V0NextRadialEdgeIndex);
				SetEdge(prev0Edge);
			}

			var next0Edge = GetEdge(edge.V0NextRadialEdgeIndex);
			if (next0Edge.IsValid)
			{
				next0Edge.SetPrevRadialEdgeIndex(edge.Vertex0Index, edge.V0PrevRadialEdgeIndex);
				SetEdge(next0Edge);
			}

			var prev1Edge = GetEdge(edge.V1PrevRadialEdgeIndex);
			if (prev1Edge.IsValid)
			{
				prev1Edge.SetNextRadialEdgeIndex(edge.Vertex1Index, edge.V1NextRadialEdgeIndex);
				SetEdge(prev1Edge);
			}

			var next1Edge = GetEdge(edge.V1NextRadialEdgeIndex);
			if (next1Edge.IsValid)
			{
				next1Edge.SetPrevRadialEdgeIndex(edge.Vertex1Index, edge.V1PrevRadialEdgeIndex);
				SetEdge(next1Edge);
			}
		}
	}
}