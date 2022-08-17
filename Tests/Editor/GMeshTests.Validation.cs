// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile.GMesh;
using NUnit.Framework;
using UnityEngine;

public sealed partial class GMeshTests
{
	private void CreateBMesh()
	{
		var bmesh = new BMesh();
		bmesh.AddVertex(_triangleVertices[0]);
		bmesh.AddVertex(_triangleVertices[1]);
		bmesh.AddVertex(_triangleVertices[2]);
		bmesh.AddEdge(2, 0);
		bmesh.AddEdge(0, 1);
		bmesh.AddEdge(1, 2);
		bmesh.AddFace(0, 1, 2);
		bmesh.DebugLogAllElements();
	}

	private void AssertAllElementsAreValidAndCorrectlyRelated(bool assertVertexEdges = false, bool logElements = false)
	{
		if (logElements)
			_gMesh.DebugLogAllElements();

		AssertValidIndicesOfAllElements();
		AssertValidVertexElementIndices();
		AssertValidEdgeElementIndices(assertVertexEdges);
		AssertValidLoopElementIndices();
		AssertValidFaceElementIndices();
		AssertValidUnityMesh();
	}

	private void AssertValidUnityMesh()
	{
		Mesh mesh = null;
		Assert.DoesNotThrow(() => { _gMesh.ToMesh(mesh); });
	}

	private void AssertValidFaceElementIndices()
	{
		var faceCount = _gMesh.FaceCount;
		for (var i = 0; i < faceCount; i++)
		{
			var face = _gMesh.GetFace(i);
			Assert.IsTrue(face.FirstLoopIndex >= 0 && face.FirstLoopIndex < _gMesh.LoopCount);

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
			Assert.IsTrue(edge.Index == v0.BaseEdgeIndex || edge.Index == v1.BaseEdgeIndex);
			Assert.IsTrue(v0.BaseEdgeIndex >= 0 && v0.BaseEdgeIndex < edgeCount);
			Assert.IsTrue(v1.BaseEdgeIndex >= 0 && v1.BaseEdgeIndex < edgeCount);

			if (assertVertexEdges)
			{
				Assert.IsTrue(edge.V0PrevRadialEdgeIndex >= 0 && edge.V0PrevRadialEdgeIndex < edgeCount);
				Assert.IsTrue(edge.V0NextRadialEdgeIndex >= 0 && edge.V0NextRadialEdgeIndex < edgeCount);
				Assert.IsTrue(edge.V1PrevRadialEdgeIndex >= 0 && edge.V1PrevRadialEdgeIndex < edgeCount);
				Assert.IsTrue(edge.V1NextRadialEdgeIndex >= 0 && edge.V1NextRadialEdgeIndex < edgeCount);

				// TODO: verify these assumptions ...

				GMesh.Edge v0PrevEdge = default;
				Assert.DoesNotThrow(() => { v0PrevEdge = _gMesh.GetEdge(edge.V0PrevRadialEdgeIndex); });
				Assert.AreEqual(edge.Index, v0PrevEdge.V1NextRadialEdgeIndex);

				GMesh.Edge v0NextEdge = default;
				Assert.DoesNotThrow(() => { v0NextEdge = _gMesh.GetEdge(edge.V0NextRadialEdgeIndex); });
				Assert.AreEqual(edge.Index, v0NextEdge.V1PrevRadialEdgeIndex);

				GMesh.Edge v1PrevEdge = default;
				Assert.DoesNotThrow(() => { v1PrevEdge = _gMesh.GetEdge(edge.V1PrevRadialEdgeIndex); });
				Assert.AreEqual(edge.Index, v1PrevEdge.V0NextRadialEdgeIndex);

				GMesh.Edge v1NextEdge = default;
				Assert.DoesNotThrow(() => { v1NextEdge = _gMesh.GetEdge(edge.V1NextRadialEdgeIndex); });
				Assert.AreEqual(edge.Index, v1NextEdge.V0PrevRadialEdgeIndex);
			}
		}
	}

	private void AssertValidVertexElementIndices()
	{
		var vertexCount = _gMesh.VertexCount;
		for (var i = 0; i < vertexCount; i++)
		{
			var firstEdgeIndex = _gMesh.GetVertex(i).BaseEdgeIndex;
			Assert.IsTrue(firstEdgeIndex >= 0 && firstEdgeIndex < vertexCount);

			GMesh.Edge edge = default;
			Assert.DoesNotThrow(() => { edge = _gMesh.GetEdge(firstEdgeIndex); });
			Assert.IsTrue(edge.IsAttachedToVertex(i));
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