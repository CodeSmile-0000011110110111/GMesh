// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Burst;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private Loop InsertLoop(ref Loop existingLoop, ref Edge newLoopEdge, int loopVertexIndex) =>
			Insert.Loop(_data, ref existingLoop, ref newLoopEdge, loopVertexIndex);

		internal void InsertEdgeInDiskCycle(int vertexIndex, ref Edge insertEdge) => Insert.EdgeInDiskCycle(_data, vertexIndex, ref insertEdge);

		[BurstCompile]
		private readonly struct Insert
		{
			public static void EdgeInDiskCycle(in GraphData data, int vertexIndex, ref Edge insertEdge)
			{
				var vertex = data.GetVertex(vertexIndex);
				var baseEdge = data.GetEdge(vertex.BaseEdgeIndex);
				var nextEdgeIndex = baseEdge.GetNextEdgeIndex(vertexIndex);

				// only one edge on this vertex?
				if (nextEdgeIndex == UnsetIndex || nextEdgeIndex == baseEdge.Index)
				{
					baseEdge.SetDiskCycleIndices(vertexIndex, insertEdge.Index);
					insertEdge.SetDiskCycleIndices(vertexIndex, baseEdge.Index);
					data.SetEdge(baseEdge);
				}
				else
				{
					var nextEdge = data.GetEdge(baseEdge.GetNextEdgeIndex(vertexIndex));
					baseEdge.SetNextEdgeIndex(vertexIndex, insertEdge.Index);
					nextEdge.SetPrevEdgeIndex(vertexIndex, insertEdge.Index);
					insertEdge.SetPrevEdgeIndex(vertexIndex, baseEdge.Index);
					insertEdge.SetNextEdgeIndex(vertexIndex, nextEdge.Index);
					data.SetEdge(baseEdge);
					data.SetEdge(nextEdge);
				}
			}

			public static Loop Loop(in GraphData data, ref Loop existingLoop, ref Edge newLoopEdge, int loopVertexIndex)
			{
				// Create and insert the new loop on the same face
				var newLoopIndex = data.ValidLoopCount;
				newLoopEdge.BaseLoopIndex = newLoopIndex;

				var newLoop = GMesh.Loop.Create(existingLoop.FaceIndex, newLoopEdge.Index, loopVertexIndex,
					newLoopIndex, newLoopIndex, existingLoop.Index, existingLoop.NextLoopIndex);
				data.AddLoop(ref newLoop);

				InsertLoopAfter(data, ref existingLoop, newLoopIndex);
				IncrementFaceElementCount(data, existingLoop.FaceIndex);

#if GMESH_VALIDATION
				// verification that loop's edge contains loop's vertex:
				if (newLoopEdge.ContainsVertex(newLoop.StartVertexIndex) == false)
					throw new Exception($"new: edge does not contain loop vertex:\n{newLoop}\n{newLoopEdge}");
				if (data.GetEdge(existingLoop.EdgeIndex).ContainsVertex(existingLoop.StartVertexIndex) == false)
					throw new Exception(
						$"existing: edge does not contain loop vertex:\n{existingLoop}\n{data.GetEdge(existingLoop.EdgeIndex)}");
#endif

				return newLoop;
			}

			private static void InsertLoopAfter(in GraphData data, ref Loop existingLoop, int newLoopIndex)
			{
				var nextLoopIndex = existingLoop.NextLoopIndex;
				existingLoop.NextLoopIndex = newLoopIndex;

				var nextLoop = data.GetLoop(nextLoopIndex);
				nextLoop.PrevLoopIndex = newLoopIndex;
				data.SetLoop(nextLoop);
			}

			private static void IncrementFaceElementCount(in GraphData data, int faceIndex)
			{
				var face = data.GetFace(faceIndex);
				face.ElementCount++;
				data.SetFace(face);
			}
		}
	}
}