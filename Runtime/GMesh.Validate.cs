// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private const string NoIssues = "no issues";

		public bool ValidateFace(in Face face) => Validate.Face(_data, face);
		public bool ValidateFace(in Face face, out string issue) => Validate.Face(_data, face, out issue);
		public bool ValidateFaceLoopCycle(in Face face) => Validate.FaceLoopCycle(_data, face);
		public bool ValidateFaceLoopCycle(in Face face, out string issue) => Validate.FaceLoopCycle(_data, face, out issue);
		public bool ValidateLoop(in Loop loop) => Validate.Loop(_data, loop);
		public bool ValidateLoop(in Loop loop, out string issue) => Validate.Loop(_data, loop, out issue);
		public bool ValidateEdge(in Edge edge) => Validate.Edge(_data, edge);
		public bool ValidateEdge(in Edge edge, out string issue) => Validate.Edge(_data, edge, out issue);
		public bool ValidateEdgeRadialLoopCycle(in Edge edge) => Validate.EdgeRadialLoopCycle(_data, edge);
		public bool ValidateEdgeRadialLoopCycle(in Edge edge, out string issue) => Validate.EdgeRadialLoopCycle(_data, edge, out issue);
		public bool ValidateVertexDiskCycle(in Vertex vertex) => Validate.VertexDiskCycle(_data, vertex);
		public bool ValidateVertexDiskCycle(in Vertex vertex, out string issue) => Validate.VertexDiskCycle(_data, vertex, out issue);
		public bool ValidateVertex(in Vertex vertex) => Validate.Vertex(_data, vertex);
		public bool ValidateVertex(in Vertex vertex, out string issue) => Validate.Vertex(_data, vertex, out issue);
		public bool ValidateFaces() => Validate.Faces(_data);
		public bool ValidateLoops() => Validate.Loops(_data);
		public bool ValidateEdges() => Validate.Edges(_data);
		public bool ValidateVertices() => Validate.Vertices(_data);

		[BurstCompile]
		private readonly struct Validate
		{
			public static bool Face(in GraphData data, in Face face)
			{
				string issue = null;
				return Face(data, face, out issue);
			}

			public static bool Face(in GraphData data, in Face face, out string issue)
			{
				if (face.IsValid == false)
				{
					issue = $"Invalidated => {face}";
					return false;
				}
				if (face.Index < 0 || face.Index >= data.ValidFaceCount)
				{
					issue = $"Index out of bounds:\n{face}";
					return false;
				}
				if (face.FirstLoopIndex < 0 || face.FirstLoopIndex >= data.ValidLoopCount)
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

			public static bool FaceLoopCycle(in GraphData data, in Face face)
			{
				string issue = null;
				return FaceLoopCycle(data, face, out issue);
			}

			public static bool FaceLoopCycle(in GraphData data, in Face face, out string issue)
			{
				if (face.IsValid == false)
				{
					issue = $"Invalidated => {face}";
					return false;
				}

				// verify that face loop is closed and can be traversed in both directions
				var loopCount = face.ElementCount;
				var loop = data.GetLoop(face.FirstLoopIndex);
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
					var prevLoop = data.GetLoop(loop.PrevLoopIndex);
					if (prevLoop.NextLoopIndex != loop.Index)
					{
						issue = $"Loop's prev loop {prevLoop.Index} points to {prevLoop.NextLoopIndex} instead of:\n{loop}";
						return false;
					}
					var nextLoop = data.GetLoop(loop.NextLoopIndex);
					if (nextLoop.PrevLoopIndex != loop.Index)
					{
						issue = $"Loop's next loop {nextLoop.Index} points to {nextLoop.NextLoopIndex} instead of:\n{loop}";
						return false;
					}

					// verify connection to edge is valid and next loop connects to edge vertex that is not our StartVertex
					var edge = data.GetEdge(loop.EdgeIndex);
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
					loop = data.GetLoop(loop.NextLoopIndex);
				}

				// did we get back to first loop?
				if (loop.Index != face.FirstLoopIndex)
				{
					issue = $"loop cycle not closed:\nlast {loop}\nfirst {data.GetLoop(face.FirstLoopIndex)}\n{face}";
					return false;
				}

				issue = NoIssues;
				return true;
			}

			public static bool Loop(in GraphData data, in Loop loop)
			{
				string issue = null;
				return Loop(data, loop, out issue);
			}

			public static bool Loop(in GraphData data, in Loop loop, out string issue)
			{
				if (loop.IsValid == false)
				{
					issue = $"Invalidated => {loop}";
					return false;
				}
				if (loop.Index < 0 || loop.Index >= data.ValidLoopCount)
				{
					issue = $"Index out of bounds:\n{loop}";
					return false;
				}
				if (loop.FaceIndex < 0 || loop.FaceIndex >= data.ValidFaceCount)
				{
					issue = $"FaceIndex out of bounds:\n{loop}";
					return false;
				}
				if (loop.EdgeIndex < 0 || loop.EdgeIndex >= data.ValidEdgeCount)
				{
					issue = $"EdgeIndex out of bounds:\n{loop}";
					return false;
				}
				if (loop.StartVertexIndex < 0 || loop.StartVertexIndex >= data.ValidVertexCount)
				{
					issue = $"StartVertexIndex out of bounds:\n{loop}";
					return false;
				}
				if (loop.PrevLoopIndex < 0 || loop.PrevLoopIndex >= data.ValidLoopCount)
				{
					issue = $"PrevLoopIndex out of bounds:\n{loop}";
					return false;
				}
				if (loop.NextLoopIndex < 0 || loop.NextLoopIndex >= data.ValidLoopCount)
				{
					issue = $"NextLoopIndex out of bounds:\n{loop}";
					return false;
				}
				if (loop.PrevRadialLoopIndex < 0 || loop.PrevRadialLoopIndex >= data.ValidLoopCount)
				{
					issue = $"PrevRadialLoopIndex out of bounds:\n{loop}";
					return false;
				}
				if (loop.NextRadialLoopIndex < 0 || loop.NextRadialLoopIndex >= data.ValidLoopCount)
				{
					issue = $"NextRadialLoopIndex out of bounds:\n{loop}";
					return false;
				}

				issue = NoIssues;
				return true;
			}

			public static bool Edge(in GraphData data, in Edge edge)
			{
				string issue = null;
				return Edge(data, edge, out issue);
			}

			public static bool Edge(in GraphData data, in Edge edge, out string issue)
			{
				if (edge.IsValid == false)
				{
					issue = $"Invalidated => {edge}";
					return false;
				}
				if (edge.Index < 0 || edge.Index >= data.ValidEdgeCount)
				{
					issue = $"Index out of bounds:\n{edge}";
					return false;
				}
				if (edge.AVertexIndex < 0 || edge.AVertexIndex >= data.ValidVertexCount)
				{
					issue = $"AVertexIndex out of bounds:\n{edge}";
					return false;
				}
				if (edge.OVertexIndex < 0 || edge.OVertexIndex >= data.ValidVertexCount)
				{
					issue = $"OVertexIndex out of bounds:\n{edge}";
					return false;
				}
				if (edge.APrevEdgeIndex < 0 || edge.APrevEdgeIndex >= data.ValidEdgeCount)
				{
					issue = $"APrevEdgeIndex out of bounds:\n{edge}";
					return false;
				}
				if (edge.ANextEdgeIndex < 0 || edge.ANextEdgeIndex >= data.ValidEdgeCount)
				{
					issue = $"ANextEdgeIndex out of bounds:\n{edge}";
					return false;
				}
				if (edge.OPrevEdgeIndex < 0 || edge.OPrevEdgeIndex >= data.ValidEdgeCount)
				{
					issue = $"OPrevEdgeIndex out of bounds:\n{edge}";
					return false;
				}
				if (edge.ONextEdgeIndex < 0 || edge.ONextEdgeIndex >= data.ValidEdgeCount)
				{
					issue = $"ONextEdgeIndex out of bounds:\n{edge}";
					return false;
				}
				if (edge.BaseLoopIndex < 0 || edge.BaseLoopIndex >= data.ValidLoopCount)
				{
					issue = $"BaseLoopIndex out of bounds:\n{edge}";
					return false;
				}

				issue = NoIssues;
				return true;
			}

			public static bool EdgeRadialLoopCycle(in GraphData data, in Edge edge)
			{
				string issue = null;
				return EdgeRadialLoopCycle(data, edge, out issue);
			}

			public static bool EdgeRadialLoopCycle(in GraphData data, in Edge edge, out string issue)
			{
				var baseLoop = data.GetLoop(edge.BaseLoopIndex);
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
				if (baseLoop.PrevRadialLoopIndex < 0 || baseLoop.PrevRadialLoopIndex >= data.ValidLoopCount ||
				    baseLoop.NextRadialLoopIndex < 0 || baseLoop.NextRadialLoopIndex >= data.ValidLoopCount)
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
					prevLoop = data.GetLoop(prevLoop.NextRadialLoopIndex);
					nextLoop = data.GetLoop(nextLoop.NextRadialLoopIndex);
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

			public static bool VertexDiskCycle(in GraphData data, in Vertex vertex)
			{
				string issue = null;
				return VertexDiskCycle(data, vertex, out issue);
			}

			public static bool VertexDiskCycle(in GraphData data, in Vertex vertex, out string issue)
			{
				if (vertex.IsValid == false)
				{
					issue = $"Invalidated => {vertex}";
					return false;
				}
				var baseEdge = data.GetEdge(vertex.BaseEdgeIndex);
				if (baseEdge.IsValid == false)
				{
					issue = $"Invalidated => {baseEdge}";
					return false;
				}

				var vertexIndex = vertex.Index;
				var foundBaseEdgeCount = 0;
				var e = baseEdge;
				var maxIterations = 10000;
				do
				{
					if (e.IsValid == false)
					{
						issue = $"Invalidated disk cycle edge:\n{e}\n{data.GetVertex(vertexIndex)}";
						return false;
					}
					if (e.ContainsVertex(vertexIndex) == false)
					{
						issue =
							$"edge in disk cycle of vertex {vertexIndex} does not connect to that vertex:\n{e}\n{data.GetVertex(vertexIndex)}";
						return false;
					}

					// check prev/next and correct inclusion of base edge (must be found twice)
					var prevEdgeIndex = e.GetPrevEdgeIndex(vertexIndex);
					var nextEdgeIndex = e.GetNextEdgeIndex(vertexIndex);
					if (baseEdge.Index == prevEdgeIndex) foundBaseEdgeCount++;
					if (baseEdge.Index == nextEdgeIndex) foundBaseEdgeCount++;
					if (prevEdgeIndex == UnsetIndex || nextEdgeIndex == UnsetIndex)
					{
						issue = $"edge has invalid prev/next index at vertex {vertexIndex}:\n{e}\n{data.GetVertex(vertexIndex)}";
						return false;
					}
					if (prevEdgeIndex == nextEdgeIndex && prevEdgeIndex == e.Index)
					{
						issue = $"edge only points to itself at vertex {vertexIndex}:\n{e}\n{data.GetVertex(vertexIndex)}";
						return false;
					}

					// check self loop
					var otherPrevEdgeIndex = e.GetPrevEdgeIndex(e.GetOppositeVertexIndex(vertexIndex));
					var otherNextEdgeIndex = e.GetNextEdgeIndex(e.GetOppositeVertexIndex(vertexIndex));
					if (otherPrevEdgeIndex == prevEdgeIndex && otherPrevEdgeIndex == e.Index ||
					    otherPrevEdgeIndex == nextEdgeIndex && otherPrevEdgeIndex == e.Index ||
					    otherNextEdgeIndex == prevEdgeIndex && otherNextEdgeIndex == e.Index ||
					    otherNextEdgeIndex == nextEdgeIndex && otherNextEdgeIndex == e.Index)
					{
						issue = $"edge loops to itself (one end to the other):\n{e}\n{data.GetVertex(vertexIndex)}";
						return false;
					}

					e = data.GetEdge(e.GetNextEdgeIndex(vertex.Index));

					maxIterations--;
					if (maxIterations == 0)
						throw new Exception($"possible infinite loop due to malformed mesh graph around {e}");
				} while (e.Index != vertex.BaseEdgeIndex);

				if (foundBaseEdgeCount != 2)
				{
					issue = $"disk cycle does not fully enclose base edge:\nbase {baseEdge}\n{vertex}";
					return false;
				}

				issue = NoIssues;
				return true;
			}

			public static bool Vertex(in GraphData data, in Vertex vertex)
			{
				string issue = null;
				return Vertex(data, vertex, out issue);
			}

			public static bool Vertex(in GraphData data, in Vertex vertex, out string issue)
			{
				if (vertex.IsValid == false)
				{
					issue = $"Invalidated => {vertex}";
					return false;
				}
				if (vertex.Index < 0 || vertex.Index >= data.ValidVertexCount)
				{
					issue = $"Index out of bounds:\n{vertex}";
					return false;
				}
				if (vertex.BaseEdgeIndex < 0 || vertex.BaseEdgeIndex >= data.ValidEdgeCount)
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

			public static bool Faces(in GraphData data)
			{
				var faceCount = data.ValidFaceCount;
				for (var i = 0; i < faceCount; i++)
				{
					var face = data.GetFace(i);
					if (face.IsValid == false)
						continue;

					if (Face(data, face) == false)
						return false;
				}
				return true;
			}

			public static bool Loops(in GraphData data)
			{
				var loopCount = data.ValidLoopCount;
				for (var i = 0; i < loopCount; i++)
				{
					var loop = data.GetLoop(i);
					if (loop.IsValid == false)
						continue;

					if (Loop(data, loop) == false)
						return false;
				}
				return true;
			}

			public static bool Edges(in GraphData data)
			{
				var edgeCount = data.ValidEdgeCount;
				for (var i = 0; i < edgeCount; i++)
				{
					var edge = data.GetEdge(i);
					if (edge.IsValid == false)
						continue;

					if (Edge(data, edge) == false)
						return false;
				}
				return true;
			}

			public static bool Vertices(in GraphData data)
			{
				var vertexCount = data.ValidVertexCount;
				for (var i = 0; i < vertexCount; i++)
				{
					var vertex = data.GetVertex(i);
					if (vertex.IsValid == false)
						continue;

					if (Vertex(data, vertex) == false)
						return false;
				}
				return true;
			}

			public static void VertexCollection<T>(in NativeArray<T> vertices) where T : struct
			{
				if (vertices.Length < 3) throw new ArgumentException($"face with only {vertices.Length} vertices is not allowed");
			}
		}
	}
}