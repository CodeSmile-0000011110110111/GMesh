// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Enumerates over all loops in a face. Return true from predicate to break out of loop early.
		/// </summary>
		/// <param name="faceIndex">The index of the face whose loops to enumerate over.</param>
		/// <param name="predicate">Predicate to call for each loop. Return true to stop enumerating, false to continue with next element.</param>
		public void ForEachLoop(int faceIndex, Predicate<Loop> predicate) => ForEachLoopInternal(GetFace(faceIndex), null, predicate);

		/// <summary>
		/// Enumerates over all loops in a face.
		/// </summary>
		/// <param name="face">the face whose loops to enumerate over</param>
		/// <param name="predicate">Predicate to call for each loop. Return true to stop enumerating, false to continue with next element.</param>
		public void ForEachLoop(in Face face, Predicate<Loop> predicate) => ForEachLoopInternal(face, null, predicate);

		/// <summary>
		/// Enumerates over all loops in a face.
		/// </summary>
		/// <param name="faceIndex">The index of the face whose loops to enumerate over.</param>
		/// <param name="callback">Action to call for each loop. If you modify loop you need to call SetLoop(loop) to store it!</param>
		public void ForEachLoop(int faceIndex, Action<Loop> callback) => ForEachLoopInternal(GetFace(faceIndex), callback, null);

		/// <summary>
		/// Enumerates over all loops in a face.
		/// </summary>
		/// <param name="face">the face whose loops to enumerate over</param>
		/// <param name="callback">Action to call for each loop. If you modify loop you need to call SetLoop(loop) to store it!</param>
		public void ForEachLoop(in Face face, Action<Loop> callback) => ForEachLoopInternal(face, callback, null);

		/// <summary>
		/// Enumerates over all edges of a vertex. Return true from predicate to break out of loop early.
		/// </summary>
		/// <param name="vertexIndex">index of the vertex whose edges to enumerate</param>
		/// <param name="predicate">Predicate returning a bool. Return true to stop enumerating, false to continue with next element.</param>
		public void ForEachEdge(int vertexIndex, Predicate<Edge> predicate) => ForEachEdgeInternal(GetVertex(vertexIndex), null, predicate);

		/// <summary>
		/// Enumerates over all edges of a vertex. Return true from predicate to break out of loop early.
		/// </summary>
		/// <param name="vertex">vertex whose edges to enumerate</param>
		/// <param name="predicate">Predicate returning a bool. Return true to stop enumerating, false to continue with next element.</param>
		public void ForEachEdge(in Vertex vertex, Predicate<Edge> predicate) => ForEachEdgeInternal(vertex, null, predicate);

		/// <summary>
		/// Enumerates over all edges of a vertex.
		/// </summary>
		/// <param name="vertexIndex">index of the vertex whose edges are to be enumerated</param>
		/// <param name="callback">Action to call for each edge. If you modify edge you need to call SetEdge(edge) to store it!</param>
		public void ForEachEdge(int vertexIndex, Action<Edge> callback) => ForEachEdgeInternal(GetVertex(vertexIndex), callback, null);

		/// <summary>
		/// Enumerates over all edges of a vertex.
		/// </summary>
		/// <param name="vertex">the vertex whose edges are to be enumerated</param>
		/// <param name="callback">Action to call for each edge. If you modify edge you need to call SetEdge(edge) to store it!</param>
		public void ForEachEdge(in Vertex vertex, Action<Edge> callback) => ForEachEdgeInternal(vertex, callback, null);

		private void ForEachLoopInternal(in Face face, Action<Loop> callback, Predicate<Loop> predicate)
		{
			// assumption: if a face is valid, all its loops are supposed to be valid too! (at least when we start)
			if (face.IsValid)
			{
				var firstLoopIndex = face.FirstLoopIndex;
				var loop = GetLoop(firstLoopIndex);
				var usePredicate = predicate != null;
				do
				{
					if (usePredicate)
					{
						if (predicate.Invoke(loop))
							break;
					}
					else
						callback.Invoke(loop);

					loop = GetLoop(loop.NextLoopIndex);
				} while (loop.IsValid && loop.Index != firstLoopIndex);
			}
		}

		private void ForEachEdgeInternal(in Vertex vertex, Action<Edge> callback, Predicate<Edge> predicate)
		{
			// assumption: if a vertex is valid and has an edge index, its edge is supposed to be valid
			if (vertex.IsValid && vertex.BaseEdgeIndex != UnsetIndex)
			{
				var edge = GetEdge(vertex.BaseEdgeIndex);
				var usePredicate = predicate != null;
				do
				{
					if (usePredicate)
					{
						if (predicate.Invoke(edge))
							break;
					}
					else
						callback.Invoke(edge);

					edge = GetEdge(edge.GetNextEdgeIndex(vertex.Index));
				} while (edge.IsValid && edge.Index != vertex.BaseEdgeIndex);
			}
		}
	}
}