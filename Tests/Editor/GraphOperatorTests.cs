// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile;
using CodeSmile.GraphMesh;
using NUnit.Framework;
using Unity.Mathematics;

[TestFixture]
public class GraphOperatorTests
{
	[Test]
	public void InsertEdgeInDiskCycleTests()
	{
		// plane with 4 quads
		using (var gMesh = GMesh.Plane(new GMeshPlane(new int2(3))))
		{
			Validate.AllElementsAndRelations(gMesh);
			Assert.AreEqual(4, gMesh.CalculateEdgeCount(4), "edge count v4");
			Assert.AreEqual(3, gMesh.CalculateEdgeCount(1), "edge count v1");
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(2), "edge count v2");

			// create edge for insert testing
			// vertex 4 is in the center (4 edges in cycle)
			// vertex 2 is in a corner (2 edges in cycle)

			// Test 1: try with disk cycle of edge unset (gets disk cycle from base edge)
			{
				var insertEdge = GMesh.Edge.Create(4, 2);
				gMesh.AddEdge(ref insertEdge);

				gMesh.InsertEdgeInDiskCycle(4, ref insertEdge);
				gMesh.InsertEdgeInDiskCycle(2, ref insertEdge);
				gMesh.SetEdge(insertEdge);

				Assert.AreEqual(5, gMesh.CalculateEdgeCount(4));
				Assert.AreEqual(3, gMesh.CalculateEdgeCount(2));
			}

			// Test 2: try with valid disk cycle of insert edge
			{
				var insertEdge = GMesh.Edge.Create(4, 2);
				insertEdge.SetDiskCycleIndices(4, 2);
				insertEdge.SetDiskCycleIndices(2, 6);
				gMesh.AddEdge(ref insertEdge);

				gMesh.InsertEdgeInDiskCycle(4, ref insertEdge);
				gMesh.InsertEdgeInDiskCycle(2, ref insertEdge);
				gMesh.SetEdge(insertEdge);

				Assert.AreEqual(6, gMesh.CalculateEdgeCount(4));
				Assert.AreEqual(4, gMesh.CalculateEdgeCount(2));
			}
		}
	}

	[Test]
	public void RemoveEdgeFromDiskCycleTests()
	{
		// plane with 4 quads
		using (var gMesh = GMesh.Plane(new GMeshPlane(new int2(3))))
		{
			Validate.AllElementsAndRelations(gMesh);
			Assert.AreEqual(4, gMesh.CalculateEdgeCount(4));
			Assert.AreEqual(3, gMesh.CalculateEdgeCount(1));
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(2));

			// vertex 4 is in the center (4 edges in cycle)
			// edge 2 connects v1 + v4
			var edgeToCenter = gMesh.GetEdge(2);
			gMesh.RemoveEdgeFromDiskCycle(4, edgeToCenter);
			Assert.AreEqual(3, gMesh.CalculateEdgeCount(4));
			Assert.IsTrue(gMesh.GetVertex(3).BaseEdgeIndex != edgeToCenter.Index);

			// vertex 1 is in the middle of a border (3 edges in cycle)
			// edge 2 connects v1 + v4
			gMesh.RemoveEdgeFromDiskCycle(1, edgeToCenter);
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(1));
			Assert.IsTrue(gMesh.GetVertex(1).BaseEdgeIndex != edgeToCenter.Index);

			// vertex 2 is in a corner (2 edges in cycle)
			// edge 6 connects v1 + v2
			var borderEdge = gMesh.GetEdge(6);
			gMesh.RemoveEdgeFromDiskCycle(2, borderEdge);
			Assert.AreEqual(1, gMesh.CalculateEdgeCount(2));
			Assert.IsTrue(gMesh.GetVertex(2).BaseEdgeIndex != borderEdge.Index &&
			              gMesh.GetVertex(2).BaseEdgeIndex != GMesh.UnsetIndex);

			// edge 5 is remaining edge on vertex 2
			var disconnectedEdge = gMesh.GetEdge(5);
			// disk cycle should point to itself
			Assert.AreEqual(disconnectedEdge.Index, disconnectedEdge.GetPrevEdgeIndex(2));
			Assert.AreEqual(disconnectedEdge.Index, disconnectedEdge.GetNextEdgeIndex(2));

			// vertex 2 is now alone ...
			gMesh.RemoveEdgeFromDiskCycle(2, disconnectedEdge);
			Assert.AreEqual(0, gMesh.CalculateEdgeCount(2));
			Assert.IsTrue(gMesh.GetVertex(2).BaseEdgeIndex == GMesh.UnsetIndex);
		}
	}
}