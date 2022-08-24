// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile;
using CodeSmile.GraphMesh;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

[TestFixture]
public class EulerOperatorTests
{
	[Test] public void SplitEdge_1Triangle()
	{
		using (var gMesh = GMesh.Triangle())
		{
			var edgeIndex = 0;
			var edgeBeforeSplit = gMesh.GetEdge(edgeIndex);
			var newEdgeIndex = gMesh.SplitEdgeAndCreateVertex(edgeIndex);
			var newEdge = gMesh.GetEdge(newEdgeIndex);
			var edgeAfterSplit = gMesh.GetEdge(edgeIndex);

			Debug.Log($"before: {edgeBeforeSplit}\nafter : {edgeAfterSplit}\nnew edge: {newEdge}\n");
			gMesh.DebugLogAllElements();
			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void SplitAllEdges_1Triangle()
	{
		using (var gMesh = GMesh.Triangle())
		{
			Validate.AllElementsAndRelations(gMesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = gMesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				gMesh.SplitEdgeAndCreateVertex(i);

			gMesh.DebugLogAllElements();
			Assert.AreEqual(1, gMesh.FaceCount);
			Assert.AreEqual(6, gMesh.GetFace(0).ElementCount);
			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void SplitAllEdges_1Quad()
	{
		using (var gMesh = GMesh.Quad())
		{
			Validate.AllElementsAndRelations(gMesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = gMesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				gMesh.SplitEdgeAndCreateVertex(i);

			gMesh.DebugLogAllElements();
			Assert.AreEqual(1, gMesh.FaceCount);
			Assert.AreEqual(8, gMesh.GetFace(0).ElementCount);
			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void SplitAllEdges_1Hexagon()
	{
		using (var gMesh = new GMesh())
		{
			Assert.DoesNotThrow(() => { gMesh.CreateFace(Constants.HexagonVertices); });
			Validate.AllElementsAndRelations(gMesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = gMesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				gMesh.SplitEdgeAndCreateVertex(i);

			gMesh.DebugLogAllElements();
			Assert.AreEqual(1, gMesh.FaceCount);
			Assert.AreEqual(12, gMesh.GetFace(0).ElementCount);
			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void SplitAllEdges_4x4Plane()
	{
		using (var gMesh = GMesh.Plane(new GMeshPlane(new int2(4))))
		{
			Validate.MeshElementCount(gMesh, 9, 36, 24, 16);
			Validate.AllElementsAndRelations(gMesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = gMesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				gMesh.SplitEdgeAndCreateVertex(i);

			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void SplitAllEdges_Cube()
	{
		using (var gMesh = GMesh.Cube(new GMeshCube(new int3(3))))
		{
			Validate.MeshElementCount(gMesh, 24, 96, 48, 26);
			Validate.AllElementsAndRelations(gMesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = gMesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				gMesh.SplitEdgeAndCreateVertex(i);

			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void SplitAllEdgesMultipleTimes_Cube()
	{
		using (var gMesh = GMesh.Cube(new GMeshCube(new int3(3))))
		{
			Validate.MeshElementCount(gMesh, 24, 96, 48, 26);
			Validate.AllElementsAndRelations(gMesh);

			// CAUTION: keep iteration count minimal since edge count doubles with every iteration
			for (var o = 0; o < 3; o++)
			{
				// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
				var edgeCount = gMesh.EdgeCount;
				for (var i = 0; i < edgeCount; i++)
					gMesh.SplitEdgeAndCreateVertex(i);
			}

			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void SplitAllEdgesTwice_3Planes()
	{
		// Cube did not preserve correct loop order when splitting, this is the minimal test case where it occured
		// between: face 1 + 2, along edges: 5, 23, 14, 32 / vertices: 1, 21, 12, 30, 5
		// two loops connect 1=>21 and 21=>5 hopping over neighbouring edges, loop 19 is one of them
		var vertexCount = new int3(2);
		var f = GMesh.Plane(new GMeshPlane(vertexCount.xy, new float3(0f, 0f, 0.5f), new float3(0f, 180f, 0f)));
		var u = GMesh.Plane(new GMeshPlane(vertexCount.xz, new float3(0f, 0.5f, 0f), new float3(90f, 270f, 270f)));
		var r = GMesh.Plane(new GMeshPlane(vertexCount.zy, new float3(0.5f, 0f, 0f), new float3(0f, 270f, 0f)));
		using (var gMesh = GMesh.Combine(new[] { f, u, r }, true))
		{
			Validate.AllElementsAndRelations(gMesh);

			// CAUTION: keep iteration count minimal since edge count doubles with every iteration
			for (var o = 0; o < 2; o++)
			{
				// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
				var edgeCount = gMesh.EdgeCount;
				for (var i = 0; i < edgeCount; i++)
					gMesh.SplitEdgeAndCreateVertex(i);
			}

			gMesh.DebugLogAllElements();
			Validate.AllElementsAndRelations(gMesh);
		}
	}
}