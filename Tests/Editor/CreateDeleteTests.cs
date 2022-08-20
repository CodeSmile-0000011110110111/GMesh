using CodeSmile.GMesh;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Tests.Editor;
using Unity.Mathematics;

[TestFixture]
public sealed class CreateDeleteTests
{
	private GMesh _gMesh;

	[SetUp] public void SetUp() => _gMesh = new GMesh();
	[TearDown] public void TearDown() => _gMesh?.Dispose();

	[Test]
	public void TryCreateFacesWithNotEnoughVertices()
	{
		Assert.Throws<ArgumentNullException>(() => { _gMesh.CreateFace(null as IEnumerable<float3>); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] {}); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] { new(0f, 0f, 0f) }); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] { new(0f, 0f, 0f), new(1f, 1f, 1f) }); });

		Assert.Throws<ArgumentNullException>(() => { _gMesh.CreateFace(null as IEnumerable<int>); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new int[] {}); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new[] { 0 }); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new[] { 0, 1 }); });
	}

	[Test]
	public void DeleteVertex_1Triangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.TriangleVertices); });

		// this should clear the entire mesh from the bottom up
		Assert.DoesNotThrow(() => { _gMesh.DeleteVertex(0); });
		Validate.MeshElementCount(_gMesh, 0, 0, 0, 0);
	}

	[Test]
	public void DeleteEdge_1Triangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.TriangleVertices); });

		// this should clear the entire mesh from the bottom up
		Assert.DoesNotThrow(() => { _gMesh.DeleteEdge(0); });
		Validate.MeshElementCount(_gMesh, 0, 0, 0, 0);
	}

	[Test]
	public void DeleteFace_1Triangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.TriangleVertices); });

		// this should clear the entire mesh "face down"
		Assert.DoesNotThrow(() => { _gMesh.DeleteFace(0); });
		Validate.MeshElementCount(_gMesh, 0, 0, 0, 0);
	}

	[Test]
	public void CreateVertsThenFace_1Triangle()
	{
		int[] vertIndices = default;
		Assert.DoesNotThrow(() => { vertIndices = _gMesh.CreateVertices(Constants.TriangleVertices); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(vertIndices); });
		var elementCount = Constants.TriangleVertices.Length;
		Validate.MeshElementCount(_gMesh, 1, elementCount, elementCount, elementCount);
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void CreateVertsThenFace_2Triangles()
	{
		int[] vertIndices = default;
		int[] vertIndices2 = default;
		Assert.DoesNotThrow(() => { vertIndices = _gMesh.CreateVertices(Constants.TriangleVertices); });
		Assert.DoesNotThrow(() => { vertIndices2 = _gMesh.CreateVertices(Constants.TriangleVertices2); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(vertIndices); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(vertIndices2); });
		var elementCount = Constants.TriangleVertices.Length + Constants.TriangleVertices2.Length;
		Validate.MeshElementCount(_gMesh, 2, elementCount, elementCount, elementCount);
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void CreateVertsThenFace_1Quad()
	{
		int[] vertIndices = default;
		Assert.DoesNotThrow(() => { vertIndices = _gMesh.CreateVertices(Constants.QuadVertices); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(vertIndices); });
		var elementCount = Constants.QuadVertices.Length;
		Validate.MeshElementCount(_gMesh, 1, elementCount, elementCount, elementCount);
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void CreateFace_1Triangle()
	{
		Validate.CreateBMeshForComparison(Constants.TriangleVertices);
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.TriangleVertices); });
		//_gMesh.DebugLogAllElements();
		var elementCount = Constants.TriangleVertices.Length;
		Validate.MeshElementCount(_gMesh, 1, elementCount, elementCount, elementCount);
		Assert.AreEqual(2, _gMesh.GetEdge(0).APrevEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(0).ANextEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(0).OPrevEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(0).ONextEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(1).APrevEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(1).ANextEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(1).OPrevEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(1).ONextEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(2).APrevEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(2).ANextEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(2).OPrevEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(2).ONextEdgeIndex);
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 0), "edge cycle count from v0");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 1), "edge cycle count from v1");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 2), "edge cycle count from v2");
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void CreateFace_1Quad()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.QuadVertices); });
		//_gMesh.DebugLogAllElements();
		var elementCount = Constants.QuadVertices.Length;
		Validate.MeshElementCount(_gMesh, 1, elementCount, elementCount, elementCount);
		Assert.AreEqual(3, _gMesh.GetEdge(0).APrevEdgeIndex);
		Assert.AreEqual(3, _gMesh.GetEdge(0).ANextEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(0).OPrevEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(0).ONextEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(1).APrevEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(1).ANextEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(1).OPrevEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(1).ONextEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(2).APrevEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(2).ANextEdgeIndex);
		Assert.AreEqual(3, _gMesh.GetEdge(2).OPrevEdgeIndex);
		Assert.AreEqual(3, _gMesh.GetEdge(2).ONextEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(3).APrevEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(3).ANextEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(3).OPrevEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(3).ONextEdgeIndex);
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 0), "edge cycle count from v0");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 1), "edge cycle count from v1");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 2), "edge cycle count from v2");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 3), "edge cycle count from v3");
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void CreateFace_1Pentagon()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.PentagonVertices); });
		var elementCount = Constants.PentagonVertices.Length;
		Validate.MeshElementCount(_gMesh, 1, elementCount, elementCount, elementCount);
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void CreateFace_1Hexagon()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.HexagonVertices); });
		var elementCount = Constants.HexagonVertices.Length;
		Validate.MeshElementCount(_gMesh, 1, elementCount, elementCount, elementCount);
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void CreateFaces_2Triangles()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.TriangleVertices); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(Constants.TriangleVertices2); });
		//_gMesh.DebugLogAllElements("TWO TRIANGLES");

		var elementCount = Constants.TriangleVertices.Length + Constants.TriangleVertices2.Length;
		Validate.MeshElementCount(_gMesh, 2, elementCount, elementCount, elementCount);
		Validate.AllElementsAndRelations(_gMesh);
	}
}