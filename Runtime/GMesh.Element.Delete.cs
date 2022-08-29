// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Burst;
using Unity.Burst.CompilerServices;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Deletes a face and its associated loop cycle.
		/// Note: this does NOT delete edges and vertices. Can be used to create a "hole" (missing face) in the mesh,
		/// either intentionally or by accident.
		/// </summary>
		/// <param name="faceIndex"></param>
		public void DeleteFace(int faceIndex) => Delete.Face(_data, faceIndex);

		/// <summary>
		/// Deletes an edge, as well as its loops and thus all the faces connecting to it.
		/// </summary>
		/// <param name="edgeIndex"></param>
		public void DeleteEdge(int edgeIndex) => Delete.Edge(_data, edgeIndex);

		/// <summary>
		/// Deletes a vertex, as well as all edges and faces connected to it.
		/// In other words: if you had a GMesh with just a single face, then deleting any vertex will clear the whole mesh. 
		/// </summary>
		/// <param name="vertexIndex"></param>
		public void DeleteVertex(int vertexIndex) => Delete.Vertex(_data, vertexIndex);

		[BurstCompile]
		private readonly struct Delete
		{
			public static void Face(in GraphData data, int faceIndex)
			{
				var face = data.GetFace(faceIndex);
				if (Hint.Likely(face.IsValid))
				{
					var elementCount = face.ElementCount;
					var loop = data.GetLoop(face.FirstLoopIndex);

					for (var i = 0; Hint.Likely(i < elementCount); i++)
					{
						// detach loop from face, thus DeleteLoopFromFace knows it's okay to delete the loop
						loop.FaceIndex = UnsetIndex;
						DeleteLoopInternal_DeleteDetachedLoop(data, loop);

						loop = data.GetLoop(loop.NextLoopIndex);
					}
				}

				data.InvalidateFace(faceIndex);
			}

			private static void Loop(in GraphData data, int loopIndex)
			{
				var loop = data.GetLoop(loopIndex);

				// if loop is associated with a face, delete the face which will call DeleteLoopFromFace
				if (loop.IsValid && loop.FaceIndex != UnsetIndex)
				{
					Face(data, loop.FaceIndex);
					return;
				}

				DeleteLoopInternal_DeleteDetachedLoop(data, loop);
			}

			public static void Edge(in GraphData data, int edgeIndex)
			{
				var edge = data.GetEdge(edgeIndex);
				if (edge.IsValid)
					DeleteEdgeInternal_DeleteEdgeLoopsAndFaces(data, ref edge);

				// edge may have been deleted above
				if (edge.IsValid)
				{
					DeleteEdgeInternal_UpdateOrDeleteVertices(data, edge);
					DeleteEdgeInternal_RelinkDiskCycle(data, edge);
					data.InvalidateEdge(edgeIndex);
				}
			}

			public static void Vertex(in GraphData data, int vertexIndex)
			{
				var vertex = data.GetVertex(vertexIndex);
				while (Hint.Likely(vertex.IsValid && vertex.BaseEdgeIndex != UnsetIndex))
				{
					Edge(data, vertex.BaseEdgeIndex);

					// get again => vertex is modified by DeleteEdge
					vertex = data.GetVertex(vertexIndex);
				}

				if (Hint.Likely(vertex.IsValid))
					data.InvalidateVertex(vertex.Index);
			}

			private static void DeleteLoopInternal_UpdateOrDeleteEdge(in GraphData data, in Loop loop)
			{
				var loopEdge = data.GetEdge(loop.EdgeIndex);
				if (Hint.Unlikely(loop.NextRadialLoopIndex == loop.Index))
				{
					// It's the last loop for this edge thus clear edge's loop index:
					loopEdge.BaseLoopIndex = UnsetIndex;
					data.SetEdge(loopEdge);

					// this edge is no longer needed
					Edge(data, loopEdge.Index);
				}
				else
				{
					DeleteLoopInternal_DetachFromRadialLoopCycle(data, loop);

					// if edge refers to this loop, make it point to the next radial loop
					if (loopEdge.BaseLoopIndex == loop.Index)
					{
						loopEdge.BaseLoopIndex = loop.NextRadialLoopIndex;
						data.SetEdge(loopEdge);
					}
				}
			}

			private static void DeleteLoopInternal_DetachFromRadialLoopCycle(in GraphData data, in Loop loop)
			{
				var prevRadialLoop = data.GetLoop(loop.PrevRadialLoopIndex);
				prevRadialLoop.NextRadialLoopIndex = loop.NextRadialLoopIndex;
				data.SetLoop(prevRadialLoop);

				var nextRadialLoop = data.GetLoop(loop.NextRadialLoopIndex);
				nextRadialLoop.PrevRadialLoopIndex = loop.PrevRadialLoopIndex;
				data.SetLoop(nextRadialLoop);
			}

			private static void DeleteLoopInternal_DeleteDetachedLoop(in GraphData data, in Loop loop)
			{
				if (Hint.Likely(loop.IsValid))
				{
					DeleteLoopInternal_UpdateOrDeleteEdge(data, loop);
					data.InvalidateLoop(loop.Index);
				}
			}

			private static void DeleteEdgeInternal_DeleteEdgeLoopsAndFaces(in GraphData data, ref Edge edge)
			{
				// delete all the loops
				while (Hint.Likely(edge.BaseLoopIndex != UnsetIndex))
				{
					Loop(data, edge.BaseLoopIndex);

					// get again => edge is modified by DeleteLoop
					edge = data.GetEdge(edge.Index);
				}
			}

			private static void DeleteEdgeInternal_UpdateOrDeleteVertices(in GraphData data, in Edge edge)
			{
				var edgeIndex = edge.Index;

				var v0 = data.GetVertex(edge.AVertexIndex);
				if (edgeIndex == v0.BaseEdgeIndex)
				{
					if (edgeIndex != edge.ANextEdgeIndex)
					{
						v0.BaseEdgeIndex = edge.ANextEdgeIndex;
						data.SetVertex(v0);
					}
					else
					{
						// remove orphaned vertex
						data.InvalidateVertex(v0.Index);
					}
				}

				var v1 = data.GetVertex(edge.OVertexIndex);
				if (edgeIndex == v1.BaseEdgeIndex)
				{
					if (edgeIndex != edge.ONextEdgeIndex)
					{
						v1.BaseEdgeIndex = edge.ONextEdgeIndex;
						data.SetVertex(v1);
					}
					else
					{
						// remove orphaned vertex
						data.InvalidateVertex(v1.Index);
					}
				}
			}

			private static void DeleteEdgeInternal_RelinkDiskCycle(in GraphData data, in Edge edge)
			{
				var prev0Edge = data.GetEdge(edge.APrevEdgeIndex);
				if (prev0Edge.IsValid)
				{
					prev0Edge.SetNextEdgeIndex(edge.AVertexIndex, edge.ANextEdgeIndex);
					data.SetEdge(prev0Edge);
				}

				var next0Edge = data.GetEdge(edge.ANextEdgeIndex);
				if (next0Edge.IsValid)
				{
					next0Edge.SetPrevEdgeIndex(edge.AVertexIndex, edge.APrevEdgeIndex);
					data.SetEdge(next0Edge);
				}

				var prev1Edge = data.GetEdge(edge.OPrevEdgeIndex);
				if (prev1Edge.IsValid)
				{
					prev1Edge.SetNextEdgeIndex(edge.OVertexIndex, edge.ONextEdgeIndex);
					data.SetEdge(prev1Edge);
				}

				var next1Edge = data.GetEdge(edge.ONextEdgeIndex);
				if (next1Edge.IsValid)
				{
					next1Edge.SetPrevEdgeIndex(edge.OVertexIndex, edge.OPrevEdgeIndex);
					data.SetEdge(next1Edge);
				}
			}
		}
	}
}