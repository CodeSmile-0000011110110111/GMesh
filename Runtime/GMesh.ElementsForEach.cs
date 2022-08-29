// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Burst.CompilerServices;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Enumerates over all loops in a face.
		/// Note: use only where performance is not important.
		/// </summary>
		/// <param name="face">the face whose loops to enumerate over</param>
		/// <param name="predicate">Predicate to call for each loop. Return true to stop enumerating, false to continue with next element.</param>
		public void ForEachLoop(in Face face, Predicate<Loop> predicate) => ForEachLoopInternal(face, null, predicate);

		/// <summary>
		/// Enumerates over all loops in a face.
		/// Note: use only where performance is not important.
		/// </summary>
		/// <param name="face">the face whose loops to enumerate over</param>
		/// <param name="callback">Action to call for each loop. If you modify loop you need to call SetLoop(loop) to store it!</param>
		public void ForEachLoop(in Face face, Action<Loop> callback) => ForEachLoopInternal(face, callback, null);

		/// <summary>
		/// Enumerates over all radial loops around an edge.
		/// Note: use only where performance is not important.
		/// </summary>
		/// <param name="edge"></param>
		/// <param name="predicate"></param>
		public void ForEachRadialLoop(in Edge edge, Predicate<Loop> predicate) => ForEachRadialLoopInternal(edge, null, predicate);

		/// <summary>
		/// Enumerates over all radial loops around an edge.
		/// Note: use only where performance is not important.
		/// </summary>
		/// <param name="edge"></param>
		/// <param name="callback"></param>
		public void ForEachRadialLoop(in Edge edge, Action<Loop> callback) => ForEachRadialLoopInternal(edge, callback, null);

		/// <summary>
		/// Enumerates over all edges of a vertex. Return true from predicate to break out of loop early.
		/// Note: use only where performance is not important.
		/// </summary>
		/// <param name="vertex">vertex whose edges to enumerate</param>
		/// <param name="predicate">Predicate returning a bool. Return true to stop enumerating, false to continue with next element.</param>
		public void ForEachEdge(in Vertex vertex, Predicate<Edge> predicate) => ForEachEdgeInternal(vertex, null, predicate);

		/// <summary>
		/// Enumerates over all edges of a vertex.
		/// Note: use only where performance is not important.
		/// </summary>
		/// <param name="vertex">the vertex whose edges are to be enumerated</param>
		/// <param name="callback">Action to call for each edge. If you modify edge you need to call SetEdge(edge) to store it!</param>
		public void ForEachEdge(in Vertex vertex, Action<Edge> callback) => ForEachEdgeInternal(vertex, callback, null);

		private void ForEachLoopInternal(in Face face, Action<Loop> callback, Predicate<Loop> predicate)
		{
			// assumption: if a face is valid, all its loops are supposed to be valid too! (at least when we start)
			if (face.IsValid)
			{
				var elementCount = face.ElementCount;
				var loop = GetLoop(face.FirstLoopIndex);
				var usePredicate = predicate != null;

				for (var i = 0; Hint.Likely(i < elementCount); i++)
				{
					if (usePredicate)
					{
						if (predicate.Invoke(loop))
							break;
					}
					else
						callback.Invoke(loop);

					loop = GetLoop(loop.NextLoopIndex);
				}
			}
		}

		private void ForEachRadialLoopInternal(in Edge edge, Action<Loop> callback, Predicate<Loop> predicate)
		{
			if (edge.IsValid)
			{
				var firstLoopIndex = edge.BaseLoopIndex;
				var loop = GetLoop(firstLoopIndex);
				var usePredicate = predicate != null;
				var maxIterations = 10000;
				do
				{
					if (usePredicate)
					{
						if (predicate.Invoke(loop))
							break;
					}
					else
						callback.Invoke(loop);

					loop = GetLoop(loop.NextRadialLoopIndex);

					maxIterations--;
					if (maxIterations == 0)
						throw new Exception(
							$"{nameof(ForEachRadialLoopInternal)}: possible infinite loop due to malformed mesh graph around {loop}");
				} while (loop.Index != firstLoopIndex);
			}
		}

		private void ForEachEdgeInternal(in Vertex vertex, Action<Edge> callback, Predicate<Edge> predicate)
		{
			// assumption: if a vertex is valid and has an edge index, its edge is supposed to be valid
			if (vertex.IsValid)
			{
				if (vertex.BaseEdgeIndex == UnsetIndex)
					throw new Exception("vertex base index is unset: {vertex}");

				var edge = GetEdge(vertex.BaseEdgeIndex);
				var usePredicate = predicate != null;
				var maxIterations = 10000;
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

					maxIterations--;
					if (maxIterations == 0)
						throw new Exception($"{nameof(ForEachEdgeInternal)}: possible infinite loop due to malformed mesh graph around {edge}");
				} while (edge.Index != vertex.BaseEdgeIndex);
			}
		}
	}
}