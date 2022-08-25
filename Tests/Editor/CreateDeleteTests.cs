using CodeSmile;
using CodeSmile.GraphMesh;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

[TestFixture]
public sealed class CreateDeleteTests
{
	[Test] public void DeleteVertex_1Triangle()
	{
		using (var gMesh = GMesh.Triangle())
		{
			// this should clear the entire mesh from the bottom up
			Assert.DoesNotThrow(() => { gMesh.DeleteVertex(0); });
			Validate.MeshElementCount(gMesh, 0, 0, 0, 0);
		}
	}

	[Test] public void DeleteEdge_1Triangle()
	{
		using (var gMesh = GMesh.Triangle())
		{
			// this should clear the entire mesh from the bottom up
			Assert.DoesNotThrow(() => { gMesh.DeleteEdge(0); });
			Validate.MeshElementCount(gMesh, 0, 0, 0, 0);
		}
	}

	[Test] public void DeleteFace_1Triangle()
	{
		using (var gMesh = GMesh.Triangle())
		{
			// this should clear the entire mesh "face down"
			Assert.DoesNotThrow(() => { gMesh.DeleteFace(0); });
			Validate.MeshElementCount(gMesh, 0, 0, 0, 0);
		}
	}

	[Test] public void CreateFace_1Triangle()
	{
		using (var gMesh = GMesh.Triangle())
		{
			var elementCount = 3;
			Validate.MeshElementCount(gMesh, 1, elementCount, elementCount, elementCount);
			Assert.AreEqual(2, gMesh.GetEdge(0).APrevEdgeIndex);
			Assert.AreEqual(2, gMesh.GetEdge(0).ANextEdgeIndex);
			Assert.AreEqual(1, gMesh.GetEdge(0).OPrevEdgeIndex);
			Assert.AreEqual(1, gMesh.GetEdge(0).ONextEdgeIndex);
			Assert.AreEqual(0, gMesh.GetEdge(1).APrevEdgeIndex);
			Assert.AreEqual(0, gMesh.GetEdge(1).ANextEdgeIndex);
			Assert.AreEqual(2, gMesh.GetEdge(1).OPrevEdgeIndex);
			Assert.AreEqual(2, gMesh.GetEdge(1).ONextEdgeIndex);
			Assert.AreEqual(1, gMesh.GetEdge(2).APrevEdgeIndex);
			Assert.AreEqual(1, gMesh.GetEdge(2).ANextEdgeIndex);
			Assert.AreEqual(0, gMesh.GetEdge(2).OPrevEdgeIndex);
			Assert.AreEqual(0, gMesh.GetEdge(2).ONextEdgeIndex);
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(0), "edge cycle count from v0");
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(1), "edge cycle count from v1");
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(2), "edge cycle count from v2");
			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void CreateFace_1Quad()
	{
		using (var gMesh = GMesh.Quad())
		{
			var elementCount = 4;
			Validate.MeshElementCount(gMesh, 1, elementCount, elementCount, elementCount);
			Assert.AreEqual(3, gMesh.GetEdge(0).APrevEdgeIndex);
			Assert.AreEqual(3, gMesh.GetEdge(0).ANextEdgeIndex);
			Assert.AreEqual(1, gMesh.GetEdge(0).OPrevEdgeIndex);
			Assert.AreEqual(1, gMesh.GetEdge(0).ONextEdgeIndex);
			Assert.AreEqual(0, gMesh.GetEdge(1).APrevEdgeIndex);
			Assert.AreEqual(0, gMesh.GetEdge(1).ANextEdgeIndex);
			Assert.AreEqual(2, gMesh.GetEdge(1).OPrevEdgeIndex);
			Assert.AreEqual(2, gMesh.GetEdge(1).ONextEdgeIndex);
			Assert.AreEqual(1, gMesh.GetEdge(2).APrevEdgeIndex);
			Assert.AreEqual(1, gMesh.GetEdge(2).ANextEdgeIndex);
			Assert.AreEqual(3, gMesh.GetEdge(2).OPrevEdgeIndex);
			Assert.AreEqual(3, gMesh.GetEdge(2).ONextEdgeIndex);
			Assert.AreEqual(2, gMesh.GetEdge(3).APrevEdgeIndex);
			Assert.AreEqual(2, gMesh.GetEdge(3).ANextEdgeIndex);
			Assert.AreEqual(0, gMesh.GetEdge(3).OPrevEdgeIndex);
			Assert.AreEqual(0, gMesh.GetEdge(3).ONextEdgeIndex);
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(0), "edge cycle count from v0");
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(1), "edge cycle count from v1");
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(2), "edge cycle count from v2");
			Assert.AreEqual(2, gMesh.CalculateEdgeCount(3), "edge cycle count from v3");
			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void CreateFace_1Pentagon()
	{
		using (var gMesh = GMesh.Pentagon())
		{
			var elementCount = 5;
			Validate.MeshElementCount(gMesh, 1, elementCount, elementCount, elementCount);
			Validate.AllElementsAndRelations(gMesh);
		}
	}

	[Test] public void CreateFace_1Hexagon()
	{
		using (var gMesh = GMesh.Hexagon())
		{
			var elementCount = Constants.HexagonVertices.Length;
			Validate.MeshElementCount(gMesh, 1, elementCount, elementCount, elementCount);
			Validate.AllElementsAndRelations(gMesh);
		}
	}
}