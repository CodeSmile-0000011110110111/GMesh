// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile.GMesh;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class Validate
{
	public static void AllElementsAndRelations(GMesh gMesh, bool logElements = false)
	{
		if (logElements)
			gMesh.DebugLogAllElements();

		ElementIndicesMatchListIndices(gMesh);
		VertexElements(gMesh);
		EdgeElements(gMesh);
		LoopElements(gMesh);
		FaceElements(gMesh);
		CanCreateUnityMesh(gMesh);
	}

	public static void CanCreateUnityMesh(GMesh gMesh)
	{
		Mesh mesh = null;
		Assert.DoesNotThrow(() => { mesh = gMesh.ToMesh(); });
		Assert.NotNull(mesh);
	}

	public static void FaceElements(GMesh gMesh)
	{
		var faceCount = gMesh.FaceCount;
		for (var i = 0; i < faceCount; i++)
		{
			var face = gMesh.GetFace(i);
			if (face.IsValid == false)
				continue;

			Assert.IsTrue(face.FirstLoopIndex >= 0 && face.FirstLoopIndex < gMesh.LoopCount);

			GMesh.Loop firstLoop = default;
			Assert.DoesNotThrow(() => { firstLoop = gMesh.GetLoop(face.FirstLoopIndex); });
			Assert.AreEqual(face.Index, firstLoop.FaceIndex);

			// verify that face loop is closed
			var loopCount = face.ElementCount;
			var loop = firstLoop;
			while (loopCount > 0)
			{
				Assert.AreEqual(loop.Index, gMesh.GetLoop(loop.PrevLoopIndex).NextLoopIndex, $"prev/next broken for {loop}");
				Assert.AreEqual(face.Index, loop.FaceIndex);

				Debug.Log(loop);
				
				// verify connection to edge is valid and next loop connects to edge vertex that is not our StartVertex
				var loopEdge = gMesh.GetEdge(loop.EdgeIndex);
				Assert.IsTrue(loopEdge.IsValid);
				Assert.IsTrue(loopEdge.ContainsVertex(loop.StartVertexIndex), $"loop edge does not contain StartVertex: " +
				                                                              $"{gMesh.GetVertex(loop.StartVertexIndex)}\n{loop}\n{loopEdge}");
				Assert.AreEqual(gMesh.GetLoop(loop.NextLoopIndex).StartVertexIndex, loopEdge.GetOppositeVertexIndex(loop.StartVertexIndex),
					$"loop edge does not connect to next loop's StartVertex!\n{loop}\nnext: {gMesh.GetLoop(loop.NextLoopIndex)}\n{loopEdge}");
				
				loopCount--;
				loop = gMesh.GetLoop(loop.NextLoopIndex);
			}

			// did we get back to first loop?
			Assert.AreEqual(firstLoop.Index, loop.Index, $"loop not closed for {face}");
		}
	}

	public static void LoopElements(GMesh gMesh)
	{
		var loopCount = gMesh.LoopCount;
		for (var i = 0; i < loopCount; i++)
		{
			var loop = gMesh.GetLoop(i);
			if (loop.IsValid == false)
				continue;

			GMesh.Face face = default;
			Assert.IsTrue(loop.FaceIndex >= 0 && loop.FaceIndex < gMesh.FaceCount);
			Assert.DoesNotThrow(() => { face = gMesh.GetFace(loop.FaceIndex); });
			Assert.IsTrue(face.IsValid);

			// can this loop be found when enumerating face loops?
			var found = false;
			gMesh.ForEachLoop(face, l => found = l.Index == loop.Index);
			Assert.IsTrue(found, $"{loop} not found in loop cycle of {face}");

			GMesh.Edge edge = default;
			Assert.IsTrue(loop.EdgeIndex >= 0 && loop.EdgeIndex < gMesh.EdgeCount);
			Assert.DoesNotThrow(() => { edge = gMesh.GetEdge(loop.EdgeIndex); });
			Assert.IsTrue(edge.IsValid);

			// can this edge be found when enumerating radial loop cycle?
			found = false;
			gMesh.ForEachRadialLoop(edge, l => found = l.Index == loop.Index);
			Assert.IsTrue(found, $"{loop} not found in radial loop cycle of {edge}");

			GMesh.Vertex vertex = default;
			Assert.IsTrue(loop.StartVertexIndex >= 0 && loop.StartVertexIndex < gMesh.VertexCount);
			Assert.DoesNotThrow(() => { vertex = gMesh.GetVertex(loop.StartVertexIndex); });
			Assert.IsTrue(vertex.IsValid);

			// prev/next loops
			{
				Assert.IsTrue(loop.PrevLoopIndex >= 0 && loop.PrevLoopIndex < loopCount);
				Assert.IsTrue(loop.NextLoopIndex >= 0 && loop.NextLoopIndex < loopCount);
				Assert.IsTrue(loop.PrevLoopIndex != loop.Index);
				Assert.IsTrue(loop.NextLoopIndex != loop.Index);

				// can we traverse back and forth?
				GMesh.Loop prevLoop = default;
				Assert.DoesNotThrow(() => { prevLoop = gMesh.GetLoop(loop.PrevLoopIndex); });
				Assert.IsTrue(prevLoop.IsValid);
				Assert.AreEqual(loop.Index, prevLoop.NextLoopIndex);

				GMesh.Loop nextLoop = default;
				Assert.DoesNotThrow(() => { nextLoop = gMesh.GetLoop(loop.NextLoopIndex); });
				Assert.IsTrue(nextLoop.IsValid);
				Assert.AreEqual(loop.Index, nextLoop.PrevLoopIndex);
			}

			// prev/next radial loops
			{
				Assert.IsTrue(loop.PrevRadialLoopIndex >= 0 && loop.PrevRadialLoopIndex < loopCount);
				Assert.IsTrue(loop.NextRadialLoopIndex >= 0 && loop.NextRadialLoopIndex < loopCount);

				// for the time being the assumption is that radial loops either both point to itself, or to the opposite loop
				// since in game engines we really should not have more than two faces connected to the same edge, sorry
				// (please correct me if I'm wrong and there is an actual requirement for such a thing)
				Assert.IsTrue(loop.PrevRadialLoopIndex == loop.NextRadialLoopIndex);

				// no other radial loops? both prev/next must point to the loop itself
				if (loop.PrevRadialLoopIndex == loop.Index)
					Assert.IsTrue(loop.NextRadialLoopIndex == loop.Index);
				if (loop.NextRadialLoopIndex == loop.Index)
					Assert.IsTrue(loop.PrevRadialLoopIndex == loop.Index);

				// can we traverse around in both directions?
				GMesh.Loop prevRadialLoop = default;
				Assert.DoesNotThrow(() => { prevRadialLoop = gMesh.GetLoop(loop.PrevRadialLoopIndex); });
				Assert.IsTrue(prevRadialLoop.IsValid);
				Assert.AreEqual(loop.Index, prevRadialLoop.NextRadialLoopIndex);

				GMesh.Loop nextRadialLoop = default;
				Assert.DoesNotThrow(() => { nextRadialLoop = gMesh.GetLoop(loop.NextRadialLoopIndex); });
				Assert.IsTrue(nextRadialLoop.IsValid);
				Assert.AreEqual(loop.Index, nextRadialLoop.PrevRadialLoopIndex);
			}
		}
	}

	public static void EdgeElements(GMesh gMesh)
	{
		var vertexCount = gMesh.VertexCount;
		var edgeCount = gMesh.EdgeCount;
		for (var i = 0; i < edgeCount; i++)
		{
			var edge = gMesh.GetEdge(i);
			if (edge.IsValid == false)
				continue;

			Assert.IsTrue(edge.AVertexIndex >= 0 && edge.AVertexIndex < vertexCount);
			Assert.IsTrue(edge.BaseLoopIndex >= 0 && edge.BaseLoopIndex < gMesh.LoopCount);

			GMesh.Vertex v0 = default, v1 = default;
			Assert.DoesNotThrow(() => { v0 = gMesh.GetVertex(edge.AVertexIndex); });
			Assert.DoesNotThrow(() => { v1 = gMesh.GetVertex(edge.OVertexIndex); });
			//Assert.IsTrue(edge.Index == v0.BaseEdgeIndex || edge.Index == v1.BaseEdgeIndex); // not always true
			Assert.IsTrue(v0.BaseEdgeIndex >= 0 && v0.BaseEdgeIndex < edgeCount);
			Assert.IsTrue(v1.BaseEdgeIndex >= 0 && v1.BaseEdgeIndex < edgeCount);

			Assert.IsTrue(edge.APrevEdgeIndex >= 0 && edge.APrevEdgeIndex < edgeCount);
			Assert.IsTrue(edge.ANextEdgeIndex >= 0 && edge.ANextEdgeIndex < edgeCount);
			Assert.IsTrue(edge.OPrevEdgeIndex >= 0 && edge.OPrevEdgeIndex < edgeCount);
			Assert.IsTrue(edge.ONextEdgeIndex >= 0 && edge.ONextEdgeIndex < edgeCount);

			// TODO: verify these assumptions ...

			GMesh.Edge v0PrevEdge = default;
			Assert.DoesNotThrow(() => { v0PrevEdge = gMesh.GetEdge(edge.APrevEdgeIndex); });
			//Assert.AreEqual(edge.Index, v0PrevEdge.V1NextEdgeIndex);

			GMesh.Edge v0NextEdge = default;
			Assert.DoesNotThrow(() => { v0NextEdge = gMesh.GetEdge(edge.ANextEdgeIndex); });
			//Assert.AreEqual(edge.Index, v0NextEdge.V1PrevEdgeIndex);

			GMesh.Edge v1PrevEdge = default;
			Assert.DoesNotThrow(() => { v1PrevEdge = gMesh.GetEdge(edge.OPrevEdgeIndex); });
			//Assert.AreEqual(edge.Index, v1PrevEdge.V0NextEdgeIndex);

			GMesh.Edge v1NextEdge = default;
			Assert.DoesNotThrow(() => { v1NextEdge = gMesh.GetEdge(edge.ONextEdgeIndex); });
			//Assert.AreEqual(edge.Index, v1NextEdge.V0PrevEdgeIndex);
		}
	}

	public static void VertexElements(GMesh gMesh)
	{
		var vertexCount = gMesh.VertexCount;
		var edgeCount = gMesh.EdgeCount;
		for (var i = 0; i < vertexCount; i++)
		{
			var vertex = gMesh.GetVertex(i);
			if (vertex.IsValid == false)
				continue;

			var baseEdgeIndex = vertex.BaseEdgeIndex;
			Assert.IsTrue(baseEdgeIndex >= 0 && baseEdgeIndex < edgeCount, $"baseEdgeIndex >= 0 && baseEdgeIndex < edgeCount => {vertex}");

			GMesh.Edge edge = default;
			Assert.DoesNotThrow(() => { edge = gMesh.GetEdge(baseEdgeIndex); });
			Assert.IsTrue(edge.ContainsVertex(i));
		}
	}

	public static void ElementIndicesMatchListIndices(GMesh gMesh)
	{
		for (var i = 0; i < gMesh.VertexCount; i++)
		{
			var vertex = gMesh.GetVertex(i);
			if (vertex.IsValid)
				Assert.AreEqual(i, vertex.Index);
		}

		for (var i = 0; i < gMesh.EdgeCount; i++)
		{
			var edge = gMesh.GetEdge(i);
			if (edge.IsValid)
				Assert.AreEqual(i, edge.Index);
		}

		for (var i = 0; i < gMesh.LoopCount; i++)
		{
			var loop = gMesh.GetLoop(i);
			if (loop.IsValid)
				Assert.AreEqual(i, loop.Index);
		}

		for (var i = 0; i < gMesh.FaceCount; i++)
		{
			var face = gMesh.GetFace(i);
			if (face.IsValid)
				Assert.AreEqual(i, face.Index);
		}
	}

	public static int GetEdgeCycleCount(GMesh gMesh, int vertexIndex)
	{
		var count = 0;
		gMesh.ForEachEdge(vertexIndex, edge => count++);
		return count;
	}

	public static void CreateBMeshForComparison(IList<float3> vertices)
	{
		var bmesh = new BMesh();
		bmesh.AddVertex(vertices[0]);
		bmesh.AddVertex(vertices[1]);
		bmesh.AddVertex(vertices[2]);
		bmesh.AddEdge(0, 1);
		bmesh.AddEdge(1, 2);
		bmesh.AddEdge(2, 0);
		bmesh.AddFace(0, 1, 2);
		bmesh.DebugLogAllElements();
	}

	public static void MeshElementCount(GMesh mesh, int faceCount, int loopCount, int edgeCount, int vertexCount)
	{
		Assert.AreEqual(faceCount, mesh.FaceCount, "FaceCount");
		Assert.AreEqual(loopCount, mesh.LoopCount, "LoopCount");
		Assert.AreEqual(edgeCount, mesh.EdgeCount, "EdgeCount");
		Assert.AreEqual(vertexCount, mesh.VertexCount, "VertexCount");
	}
}