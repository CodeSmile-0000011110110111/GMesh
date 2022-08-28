// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Collections.Generic;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Calls Dispose() on all non-null meshes in the collection that have not been disposed yet.
		/// </summary>
		/// <param name="meshes"></param>
		private static void DisposeAll(IEnumerable<GMesh> meshes)
		{
			if (meshes != null)
			{
				foreach (var mesh in meshes)
				{
					if (mesh != null && mesh.IsDisposed == false)
						mesh.Dispose();
				}
			}
		}

		/// <summary>
		/// Moves (snaps) all vertex positions to an imaginary grid given by gridSize.
		/// For example, if gridSize is 0.01f all vertices are snapped to the nearest 1cm coordinate.
		/// </summary>
		/// <param name="gridSize"></param>
		public void SnapVerticesToGrid(float gridSize)
		{
			for (var i = 0; i < ValidVertexCount; i++)
			{
				var vertex = GetVertex(i);
				if (vertex.IsValid)
				{
					vertex.SnapPosition(gridSize);
					SetVertex(vertex);
				}
			}
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

		private void GetVerticesPreferBaseEdgeVertex(in Edge edge, out Vertex baseVertex, out Vertex otherVertex)
		{
			var vertexA = GetVertex(edge.AVertexIndex);
			var vertexO = GetVertex(edge.OVertexIndex);
			baseVertex = edge.Index == vertexA.BaseEdgeIndex ? vertexA : vertexO;
			otherVertex = baseVertex.Index == vertexO.Index ? vertexA : vertexO;
		}

		private (int, int) GetBaseEdgeDiskCycleIndices(int vertexIndex)
		{
			// get prev/next edge from vertex base edge
			var vertex = GetVertex(vertexIndex);
			var baseEdge = GetEdge(vertex.BaseEdgeIndex);
			return baseEdge.GetDiskCycleIndices(vertexIndex);
		}
	}
}