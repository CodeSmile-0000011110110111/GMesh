// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private const string NoIssues = "no issues";

		public bool ValidateFace(in Face face)
		{
			string issue = null;
			return ValidateFace(face, out issue);
		}

		public bool ValidateFace(in Face face, out string issue)
		{
			if (face.IsValid == false)
			{
				issue = $"Invalidated => {face}";
				return false;
			}
			if (face.Index < 0 || face.Index >= ValidFaceCount)
			{
				issue = $"Index out of bounds:\n{face}";
				return false;
			}
			if (face.FirstLoopIndex < 0 || face.FirstLoopIndex >= ValidLoopCount)
			{
				issue = $"FirstLoopIndex out of bounds:\n{face}";
				return false;
			}
			if (face.ElementCount < 3)
			{
				issue = $"ElementCount less than what is sensible:\n{face}";
				return false;
			}

			issue = NoIssues;
			return true;
		}

		public bool ValidateFaceLoopCycle(in Face face)
		{
			string issue = null;
			return ValidateFaceLoopCycle(face, out issue);
		}

		public bool ValidateFaceLoopCycle(in Face face, out string issue)
		{
			if (face.IsValid == false)
			{
				issue = $"Invalidated => {face}";
				return false;
			}

			// verify that face loop is closed and can be traversed in both directions
			var loopCount = face.ElementCount;
			var loop = GetLoop(face.FirstLoopIndex);
			while (loopCount > 0)
			{
				if (loop.IsValid == false)
				{
					issue = $"Invalidated => {loop}";
					return false;
				}
				if (loop.FaceIndex != face.Index)
				{
					issue = $"loop does not match face Index {face.Index}:\n{loop}";
					return false;
				}
				var prevLoop = GetLoop(loop.PrevLoopIndex);
				if (prevLoop.NextLoopIndex != loop.Index)
				{
					issue = $"Loop's prev loop {prevLoop.Index} points to {prevLoop.NextLoopIndex} instead of:\n{loop}";
					return false;
				}
				var nextLoop = GetLoop(loop.NextLoopIndex);
				if (nextLoop.PrevLoopIndex != loop.Index)
				{
					issue = $"Loop's next loop {nextLoop.Index} points to {nextLoop.NextLoopIndex} instead of:\n{loop}";
					return false;
				}

				// verify connection to edge is valid and next loop connects to edge vertex that is not our StartVertex
				var edge = GetEdge(loop.EdgeIndex);
				if (edge.IsValid == false)
				{
					issue = $"Invalidated => {edge}";
					return false;
				}
				if (edge.ContainsVertex(loop.StartVertexIndex) == false)
				{
					issue = $"loop's edge does not contain loop's start vertex {loop.StartVertexIndex}\n{loop}\n{edge}";
					return false;
				}
				// edge's other vertex must be the start vertex of the next loop
				var otherVertexIndex = edge.GetOppositeVertexIndex(loop.StartVertexIndex);
				if (nextLoop.StartVertexIndex != edge.GetOppositeVertexIndex(loop.StartVertexIndex))
				{
					issue =
						$"next loop's start vertex is not the loop edge's other vertex {otherVertexIndex}\n{loop}\n{edge}\nnext: {nextLoop}";
					return false;
				}

				loopCount--;
				loop = GetLoop(loop.NextLoopIndex);
			}

			// did we get back to first loop?
			if (loop.Index != face.FirstLoopIndex)
			{
				issue =
					$"loop cycle did not return to face's first Loop {face.FirstLoopIndex}\nlast {loop}\nfirst {GetLoop(face.FirstLoopIndex)}\n{face}";
				return false;
			}

			issue = NoIssues;
			return true;
		}

		public bool ValidateLoop(in Loop loop)
		{
			string issue = null;
			return ValidateLoop(loop, out issue);
		}

		public bool ValidateLoop(in Loop loop, out string issue)
		{
			if (loop.IsValid == false)
			{
				issue = $"Invalidated => {loop}";
				return false;
			}
			if (loop.Index < 0 || loop.Index >= ValidLoopCount)
			{
				issue = $"Index out of bounds:\n{loop}";
				return false;
			}
			if (loop.FaceIndex < 0 || loop.FaceIndex >= ValidFaceCount)
			{
				issue = $"FaceIndex out of bounds:\n{loop}";
				return false;
			}
			if (loop.EdgeIndex < 0 || loop.EdgeIndex >= ValidEdgeCount)
			{
				issue = $"EdgeIndex out of bounds:\n{loop}";
				return false;
			}
			if (loop.StartVertexIndex < 0 || loop.StartVertexIndex >= ValidVertexCount)
			{
				issue = $"StartVertexIndex out of bounds:\n{loop}";
				return false;
			}
			if (loop.PrevLoopIndex < 0 || loop.PrevLoopIndex >= ValidLoopCount)
			{
				issue = $"PrevLoopIndex out of bounds:\n{loop}";
				return false;
			}
			if (loop.NextLoopIndex < 0 || loop.NextLoopIndex >= ValidLoopCount)
			{
				issue = $"NextLoopIndex out of bounds:\n{loop}";
				return false;
			}
			if (loop.PrevRadialLoopIndex < 0 || loop.PrevRadialLoopIndex >= ValidLoopCount)
			{
				issue = $"PrevRadialLoopIndex out of bounds:\n{loop}";
				return false;
			}
			if (loop.NextRadialLoopIndex < 0 || loop.NextRadialLoopIndex >= ValidLoopCount)
			{
				issue = $"NextRadialLoopIndex out of bounds:\n{loop}";
				return false;
			}

			issue = NoIssues;
			return true;
		}

		public bool ValidateEdge(in Edge edge)
		{
			string issue = null;
			return ValidateEdge(edge, out issue);
		}

		public bool ValidateEdge(in Edge edge, out string issue)
		{
			if (edge.IsValid == false)
			{
				issue = $"Invalidated => {edge}";
				return false;
			}
			if (edge.Index < 0 || edge.Index >= ValidEdgeCount)
			{
				issue = $"Index out of bounds:\n{edge}";
				return false;
			}
			if (edge.AVertexIndex < 0 || edge.AVertexIndex >= ValidVertexCount)
			{
				issue = $"AVertexIndex out of bounds:\n{edge}";
				return false;
			}
			if (edge.OVertexIndex < 0 || edge.OVertexIndex >= ValidVertexCount)
			{
				issue = $"OVertexIndex out of bounds:\n{edge}";
				return false;
			}
			if (edge.APrevEdgeIndex < 0 || edge.APrevEdgeIndex >= ValidEdgeCount)
			{
				issue = $"APrevEdgeIndex out of bounds:\n{edge}";
				return false;
			}
			if (edge.ANextEdgeIndex < 0 || edge.ANextEdgeIndex >= ValidEdgeCount)
			{
				issue = $"ANextEdgeIndex out of bounds:\n{edge}";
				return false;
			}
			if (edge.OPrevEdgeIndex < 0 || edge.OPrevEdgeIndex >= ValidEdgeCount)
			{
				issue = $"OPrevEdgeIndex out of bounds:\n{edge}";
				return false;
			}
			if (edge.ONextEdgeIndex < 0 || edge.ONextEdgeIndex >= ValidEdgeCount)
			{
				issue = $"ONextEdgeIndex out of bounds:\n{edge}";
				return false;
			}
			if (edge.BaseLoopIndex < 0 || edge.BaseLoopIndex >= ValidLoopCount)
			{
				issue = $"BaseLoopIndex out of bounds:\n{edge}";
				return false;
			}

			issue = NoIssues;
			return true;
		}

		public bool ValidateEdgeRadialLoopCycle(in Edge edge)
		{
			string issue = null;
			return ValidateEdgeRadialLoopCycle(edge, out issue);
		}

		public bool ValidateEdgeRadialLoopCycle(in Edge edge, out string issue)
		{
			var baseLoop = GetLoop(edge.BaseLoopIndex);
			if (baseLoop.IsValid == false)
			{
				issue = $"Invalidated => {baseLoop}";
				return false;
			}
			if (baseLoop.EdgeIndex != edge.Index)
			{
				issue = $"base loop does not point back to edge:\n{edge}\n{baseLoop}";
				return false;
			}
			if (baseLoop.PrevRadialLoopIndex < 0 || baseLoop.PrevRadialLoopIndex >= ValidLoopCount ||
			    baseLoop.NextRadialLoopIndex < 0 || baseLoop.NextRadialLoopIndex >= ValidLoopCount)
			{
				issue = $"radial loop index out of bounds:\n{baseLoop}";
				return false;
			}
			if (baseLoop.PrevRadialLoopIndex == baseLoop.Index && baseLoop.NextRadialLoopIndex != baseLoop.Index ||
			    baseLoop.NextRadialLoopIndex == baseLoop.Index && baseLoop.PrevRadialLoopIndex != baseLoop.Index)
			{
				issue = $"loop radial cannot both point to itself and another loop:\n{baseLoop}";
				return false;
			}
			if (baseLoop.PrevRadialLoopIndex != baseLoop.NextRadialLoopIndex)
			{
				issue = $"ASSUMPTION: only 1 or 2 loops in radial cycle expected:\n{baseLoop}";
				return false;
			}

			// traverse both directions
			var maxIterations = 1000;
			var prevLoop = baseLoop;
			var nextLoop = baseLoop;
			do
			{
				prevLoop = GetLoop(prevLoop.NextRadialLoopIndex);
				nextLoop = GetLoop(nextLoop.NextRadialLoopIndex);
				maxIterations--;
			} while (nextLoop.Index != baseLoop.Index && prevLoop.Index != baseLoop.Index || maxIterations == 0);

			if (maxIterations == 0)
			{
				issue = $"radial cycle not closed or not symmetric:\n{baseLoop}";
				return false;
			}

			issue = NoIssues;
			return true;
		}

		public bool ValidateVertexDiskCycle(in Vertex vertex)
		{
			string issue = null;
			return ValidateVertexDiskCycle(vertex, out issue);
		}

		public bool ValidateVertexDiskCycle(in Vertex vertex, out string issue)
		{
			if (vertex.IsValid == false)
			{
				issue = $"Invalidated => {vertex}";
				return false;
			}
			var baseEdge = GetEdge(vertex.BaseEdgeIndex);
			if (baseEdge.IsValid == false)
			{
				issue = $"Invalidated => {baseEdge}";
				return false;
			}

			var vertexIndex = vertex.Index;
			string loopIssue = null;
			var foundIssue = false;
			var foundBaseEdgeCount = 0;
			ForEachEdge(vertex, e =>
			{
				if (e.IsValid == false)
				{
					loopIssue = $"Invalidated disk cycle edge:\n{e}\n{GetVertex(vertexIndex)}";
					return foundIssue = true; // break loop
				}
				if (e.ContainsVertex(vertexIndex) == false)
				{
					loopIssue = $"edge in disk cycle of vertex {vertexIndex} does not connect to that vertex:\n{e}\n{GetVertex(vertexIndex)}";
					return foundIssue = true; // break loop
				}

				// check prev/next and correct inclusion of base edge (must be found twice)
				var prevEdgeIndex = e.GetPrevEdgeIndex(vertexIndex);
				var nextEdgeIndex = e.GetNextEdgeIndex(vertexIndex);
				if (baseEdge.Index == prevEdgeIndex) foundBaseEdgeCount++;
				if (baseEdge.Index == nextEdgeIndex) foundBaseEdgeCount++;
				if (prevEdgeIndex == UnsetIndex || nextEdgeIndex == UnsetIndex)
				{
					loopIssue = $"edge has invalid prev/next index at vertex {vertexIndex}:\n{e}\n{GetVertex(vertexIndex)}";
					return foundIssue = true; // break loop
				}
				if (prevEdgeIndex == nextEdgeIndex && prevEdgeIndex == e.Index)
				{
					loopIssue = $"edge only points to itself at vertex {vertexIndex}:\n{e}\n{GetVertex(vertexIndex)}";
					return foundIssue = true; // break loop
				}

				// check self loop
				var otherPrevEdgeIndex = e.GetPrevEdgeIndex(e.GetOppositeVertexIndex(vertexIndex));
				var otherNextEdgeIndex = e.GetNextEdgeIndex(e.GetOppositeVertexIndex(vertexIndex));
				if (otherPrevEdgeIndex == prevEdgeIndex && otherPrevEdgeIndex == e.Index ||
				    otherPrevEdgeIndex == nextEdgeIndex && otherPrevEdgeIndex == e.Index ||
				    otherNextEdgeIndex == prevEdgeIndex && otherNextEdgeIndex == e.Index ||
				    otherNextEdgeIndex == nextEdgeIndex && otherNextEdgeIndex == e.Index)
				{
					loopIssue = $"edge loops to itself (one end to the other):\n{e}\n{GetVertex(vertexIndex)}";
					return foundIssue = true; // break loop
				}

				return foundIssue; // continue loop
			});

			if (foundIssue)
			{
				issue = loopIssue;
				return false;
			}
			if (foundBaseEdgeCount != 2)
			{
				issue = $"disk cycle does not fully enclose base edge:\nbase {baseEdge}\n{vertex}";
				return false;
			}

			issue = NoIssues;
			return true;
		}

		public bool ValidateVertex(in Vertex vertex)
		{
			string issue = null;
			return ValidateVertex(vertex, out issue);
		}

		public bool ValidateVertex(in Vertex vertex, out string issue)
		{
			if (vertex.IsValid == false)
			{
				issue = $"Invalidated => {vertex}";
				return false;
			}
			if (vertex.Index < 0 || vertex.Index >= ValidVertexCount)
			{
				issue = $"Index out of bounds:\n{vertex}";
				return false;
			}
			if (vertex.BaseEdgeIndex < 0 || vertex.BaseEdgeIndex >= ValidEdgeCount)
			{
				issue = $"BaseEdgeIndex out of bounds:\n{vertex}";
				return false;
			}
			if (math.isnan(vertex.Position.x) || math.isnan(vertex.Position.y) || math.isnan(vertex.Position.z))
			{
				issue = $"Position NaN:\n{vertex}";
				return false;
			}
			if (math.isinf(vertex.Position.x) || math.isinf(vertex.Position.y) || math.isinf(vertex.Position.z))
			{
				issue = $"Position infinite:\n{vertex}";
				return false;
			}
			if (math.isinf(math.lengthsq(vertex.Position)))
			{
				issue = $"Position too far away from origin:\n{vertex}";
				return false;
			}

			issue = NoIssues;
			return true;
		}

		public bool ValidateFaces()
		{
			var faceCount = ValidFaceCount;
			for (var i = 0; i < faceCount; i++)
			{
				var face = GetFace(i);
				if (face.IsValid == false)
					continue;

				if (ValidateFace(face) == false)
					return false;
			}
			return true;
		}

		public bool ValidateLoops()
		{
			var loopCount = ValidLoopCount;
			for (var i = 0; i < loopCount; i++)
			{
				var loop = GetLoop(i);
				if (loop.IsValid == false)
					continue;

				if (ValidateLoop(loop) == false)
					return false;
			}
			return true;
		}

		public bool ValidateEdges()
		{
			var edgeCount = ValidEdgeCount;
			for (var i = 0; i < edgeCount; i++)
			{
				var edge = GetEdge(i);
				if (edge.IsValid == false)
					continue;

				if (ValidateEdge(edge) == false)
					return false;
			}
			return true;
		}

		public bool ValidateVertices()
		{
			var vertexCount = ValidVertexCount;
			for (var i = 0; i < vertexCount; i++)
			{
				var vertex = GetVertex(i);
				if (vertex.IsValid == false)
					continue;

				if (ValidateVertex(vertex) == false)
					return false;
			}
			return true;
		}
		
		private static void ValidateVertexCollection<T>(in NativeArray<T> vertices) where T : struct
		{
			var vertexCount = vertices.Length;
			if (vertexCount < 3)
				throw new ArgumentException($"face with only {vertexCount} vertices is technically possible but reasonably nonsensical");
		}

	}
}