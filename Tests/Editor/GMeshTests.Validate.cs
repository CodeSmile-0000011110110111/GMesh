// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile.GMesh;
using NUnit.Framework;

public sealed partial class GMeshTests
{
	private void AssertValidAllElements(bool assertVertexEdges = false, bool logElements = false)
	{
		if (logElements)
			_gMesh.DebugLogAllElements();

		AssertValidIndicesOfAllElements();
		AssertValidVertexElementIndices();
		AssertValidEdgeElementIndices(assertVertexEdges);
		AssertValidLoopElementIndices();
		AssertValidFaceElementIndices();
	}

	private void AssertValidFaceElementIndices()
	{
		var faceCount = _gMesh.FaceCount;
		for (var i = 0; i < faceCount; i++)
		{
			var face = _gMesh.GetFace(i);
			Assert.IsTrue(face.FirstLoopIndex >= 0 && face.FirstLoopIndex < faceCount);

			GMesh.Loop firstLoop = default;
			Assert.DoesNotThrow(() => { firstLoop = _gMesh.GetLoop(face.FirstLoopIndex); });
			Assert.AreEqual(face.Index, firstLoop.FaceIndex);

			// verify that loop is closed
			var loopCount = face.ElementCount;
			var loop = firstLoop;
			while (loopCount > 0)
			{
				loopCount--;
				loop = _gMesh.GetLoop(loop.NextLoopIndex);
				Assert.AreEqual(face.Index, loop.FaceIndex);
			}

			// did we get back to first loop?
			Assert.AreEqual(firstLoop.Index, loop.Index, $"loop not closed for {face}");
		}
	}

	private void AssertValidLoopElementIndices()
	{
		var loopCount = _gMesh.LoopCount;
		for (var i = 0; i < loopCount; i++)
		{
			var loop = _gMesh.GetLoop(i);

			GMesh.Face face = default;
			Assert.IsTrue(loop.FaceIndex >= 0 && loop.FaceIndex < _gMesh.FaceCount);
			Assert.DoesNotThrow(() => { face = _gMesh.GetFace(loop.FaceIndex); });

			GMesh.Edge edge = default;
			Assert.IsTrue(loop.EdgeIndex >= 0 && loop.EdgeIndex < _gMesh.EdgeCount);
			Assert.DoesNotThrow(() => { edge = _gMesh.GetEdge(loop.EdgeIndex); });
			Assert.AreEqual(loop.Index, edge.LoopIndex);

			GMesh.Vertex vertex = default;
			Assert.IsTrue(loop.VertexIndex >= 0 && loop.VertexIndex < _gMesh.VertexCount);
			Assert.DoesNotThrow(() => { vertex = _gMesh.GetVertex(loop.VertexIndex); });

			// prev/next loops
			{
				Assert.IsTrue(loop.PrevLoopIndex >= 0 && loop.PrevLoopIndex < loopCount);
				Assert.IsTrue(loop.NextLoopIndex >= 0 && loop.NextLoopIndex < loopCount);

				GMesh.Loop prevLoop = default;
				Assert.DoesNotThrow(() => { prevLoop = _gMesh.GetLoop(loop.PrevLoopIndex); });
				Assert.AreEqual(loop.Index, prevLoop.NextLoopIndex);

				GMesh.Loop nextLoop = default;
				Assert.DoesNotThrow(() => { nextLoop = _gMesh.GetLoop(loop.NextLoopIndex); });
				Assert.AreEqual(loop.Index, nextLoop.PrevLoopIndex);
			}

			// prev/next radial loops
			{
				Assert.IsTrue(loop.PrevRadialLoopIndex >= 0 && loop.PrevRadialLoopIndex < loopCount);
				Assert.IsTrue(loop.NextRadialLoopIndex >= 0 && loop.NextRadialLoopIndex < loopCount);

				GMesh.Loop prevRadialLoop = default;
				Assert.DoesNotThrow(() => { prevRadialLoop = _gMesh.GetLoop(loop.PrevRadialLoopIndex); });
				Assert.AreEqual(loop.Index, prevRadialLoop.NextRadialLoopIndex);

				GMesh.Loop nextRadialLoop = default;
				Assert.DoesNotThrow(() => { nextRadialLoop = _gMesh.GetLoop(loop.NextRadialLoopIndex); });
				Assert.AreEqual(loop.Index, nextRadialLoop.PrevRadialLoopIndex);
			}
		}
	}

	private void AssertValidEdgeElementIndices(bool assertVertexEdges = false)
	{
		var vertexCount = _gMesh.VertexCount;
		var edgeCount = _gMesh.EdgeCount;
		for (var i = 0; i < edgeCount; i++)
		{
			var edge = _gMesh.GetEdge(i);
			Assert.IsTrue(edge.Vertex0Index >= 0 && edge.Vertex0Index < vertexCount);
			Assert.IsTrue(edge.LoopIndex >= 0 && edge.LoopIndex < _gMesh.LoopCount);

			GMesh.Vertex v0 = default, v1 = default;
			Assert.DoesNotThrow(() => { v0 = _gMesh.GetVertex(edge.Vertex0Index); });
			Assert.DoesNotThrow(() => { v1 = _gMesh.GetVertex(edge.Vertex1Index); });
			Assert.AreEqual(edge.Index, v0.FirstEdgeIndex);
			Assert.IsTrue(v1.FirstEdgeIndex >= 0 && v1.FirstEdgeIndex < edgeCount);

			if (assertVertexEdges)
			{
				Assert.IsTrue(edge.Vertex0PrevEdgeIndex >= 0 && edge.Vertex0PrevEdgeIndex < edgeCount);
				Assert.IsTrue(edge.Vertex0NextEdgeIndex >= 0 && edge.Vertex0NextEdgeIndex < edgeCount);
				Assert.IsTrue(edge.Vertex1PrevEdgeIndex >= 0 && edge.Vertex1PrevEdgeIndex < edgeCount);
				Assert.IsTrue(edge.Vertex1NextEdgeIndex >= 0 && edge.Vertex1NextEdgeIndex < edgeCount);

				// TODO: verify these assumptions ...

				GMesh.Edge v0PrevEdge = default;
				Assert.DoesNotThrow(() => { v0PrevEdge = _gMesh.GetEdge(edge.Vertex0PrevEdgeIndex); });
				Assert.AreEqual(edge.Index, v0PrevEdge.Vertex1NextEdgeIndex);

				GMesh.Edge v0NextEdge = default;
				Assert.DoesNotThrow(() => { v0NextEdge = _gMesh.GetEdge(edge.Vertex0NextEdgeIndex); });
				Assert.AreEqual(edge.Index, v0NextEdge.Vertex1PrevEdgeIndex);

				GMesh.Edge v1PrevEdge = default;
				Assert.DoesNotThrow(() => { v1PrevEdge = _gMesh.GetEdge(edge.Vertex1PrevEdgeIndex); });
				Assert.AreEqual(edge.Index, v1PrevEdge.Vertex0NextEdgeIndex);

				GMesh.Edge v1NextEdge = default;
				Assert.DoesNotThrow(() => { v1NextEdge = _gMesh.GetEdge(edge.Vertex1NextEdgeIndex); });
				Assert.AreEqual(edge.Index, v1NextEdge.Vertex0PrevEdgeIndex);
			}
		}
	}

	private void AssertValidVertexElementIndices()
	{
		var vertexCount = _gMesh.VertexCount;
		for (var i = 0; i < vertexCount; i++)
		{
			var firstEdgeIndex = _gMesh.GetVertex(i).FirstEdgeIndex;
			Assert.IsTrue(firstEdgeIndex >= 0 && firstEdgeIndex < vertexCount);

			GMesh.Edge edge = default;
			Assert.DoesNotThrow(() => { edge = _gMesh.GetEdge(firstEdgeIndex); });
			Assert.AreEqual(i, edge.Vertex0Index);
		}
	}

	private void AssertValidIndicesOfAllElements()
	{
		for (var i = 0; i < _gMesh.FaceCount; i++)
			Assert.AreEqual(i, _gMesh.GetFace(i).Index);
		for (var i = 0; i < _gMesh.VertexCount; i++)
			Assert.AreEqual(i, _gMesh.GetVertex(i).Index);
		for (var i = 0; i < _gMesh.EdgeCount; i++)
			Assert.AreEqual(i, _gMesh.GetEdge(i).Index);
		for (var i = 0; i < _gMesh.LoopCount; i++)
			Assert.AreEqual(i, _gMesh.GetLoop(i).Index);
	}
}