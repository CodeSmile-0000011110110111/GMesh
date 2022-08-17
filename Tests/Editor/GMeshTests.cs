using CodeSmile.GMesh;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

[TestFixture]
public sealed partial class GMeshTests
{
	private readonly float3[] _triangleVertices = { new(0f, 0f, 0f), new(1f, .1f, 1f), new(2f, 2f, 2f) };
	private readonly float3[] _triangleVertices2 = { new(0f, 0f, 0f), new(-1f, -.1f, -1f), new(-2f, -2f, -2f) };
	private readonly float3[] _quadVertices = { new(0f, 0f, 0f), new(1f, .1f, 1f), new(2f, .2f, 2f), new(3f, 3f, 3f) };
	private readonly float3[] _pentagonVertices =
		{ new(0f, 0f, 0f), new(1f, .1f, 1f), new(2f, .2f, 2f), new(3f, .3f, 3f), new(4f, 4f, 4f) };
	private readonly float3[] _hexagonVertices =
		{ new(0f, 0f, 0f), new(1f, .1f, 1f), new(2f, .2f, 2f), new(3f, .3f, 3f), new(4f, .4f, 4f), new(5f, 5f, 5f) };

	private GMesh _gMesh;

	[SetUp] public void SetUp() => _gMesh = new GMesh();
	[TearDown] public void TearDown() => _gMesh?.Dispose();

	[Test]
	public void TryAddFacesWithNotEnoughVertices()
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
	public void DeleteVertex_OneTriangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });

		// this should clear the entire mesh from the bottom up
		Assert.DoesNotThrow(() => { _gMesh.DeleteVertex(0); });
		//_gMesh.DebugLogAllElements("AFTER DELETE");
		Assert.AreEqual(0, _gMesh.FaceCount);
		Assert.AreEqual(0, _gMesh.VertexCount);
		Assert.AreEqual(0, _gMesh.EdgeCount);
		Assert.AreEqual(0, _gMesh.LoopCount);
	}

	[Test]
	public void DeleteEdge_OneTriangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });

		// this should clear the entire mesh from the bottom up
		Assert.DoesNotThrow(() => { _gMesh.DeleteEdge(0); });
		//_gMesh.DebugLogAllElements("AFTER DELETE");
		Assert.AreEqual(0, _gMesh.FaceCount);
		Assert.AreEqual(0, _gMesh.VertexCount);
		Assert.AreEqual(0, _gMesh.EdgeCount);
		Assert.AreEqual(0, _gMesh.LoopCount);
	}

	[Test]
	public void DeleteFace_OneTriangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });

		// this should clear the entire mesh "face down"
		Assert.DoesNotThrow(() => { _gMesh.DeleteFace(0); });
		//_gMesh.DebugLogAllElements("AFTER DELETE");
		Assert.AreEqual(0, _gMesh.FaceCount);
		Assert.AreEqual(0, _gMesh.VertexCount);
		Assert.AreEqual(0, _gMesh.EdgeCount);
		Assert.AreEqual(0, _gMesh.LoopCount);
	}

	[Test]
	public void AddVertsThenFace_OneTriangle()
	{
		int[] vertIndices = default;
		Assert.DoesNotThrow(() => { vertIndices = _gMesh.CreateVertices(_triangleVertices); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(vertIndices); });
		Assert.AreEqual(1, _gMesh.FaceCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.VertexCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.EdgeCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.LoopCount);
		Validate.AllElementsAndRelations(_gMesh);
	}
	
	[Test]
	public void AddVertsThenFace_TwoTriangles()
	{
		int[] vertIndices = default;
		int[] vertIndices2 = default;
		Assert.DoesNotThrow(() => { vertIndices = _gMesh.CreateVertices(_triangleVertices); });
		Assert.DoesNotThrow(() => { vertIndices2 = _gMesh.CreateVertices(_triangleVertices2); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(vertIndices); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(vertIndices2); });
		Assert.AreEqual(2, _gMesh.FaceCount);
		Assert.AreEqual(_triangleVertices.Length + _triangleVertices2.Length, _gMesh.VertexCount);
		Assert.AreEqual(_triangleVertices.Length + _triangleVertices2.Length, _gMesh.EdgeCount);
		Assert.AreEqual(_triangleVertices.Length + _triangleVertices2.Length, _gMesh.LoopCount);
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void AddVertsThenFace_OneQuad()
	{
		int[] vertIndices = default;
		Assert.DoesNotThrow(() => { vertIndices = _gMesh.CreateVertices(_quadVertices); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(vertIndices); });
		Assert.AreEqual(_quadVertices.Length, _gMesh.VertexCount);
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void AddFace_OneTriangle()
	{
		Validate.CreateBMeshForComparison(_triangleVertices);
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });
		_gMesh.DebugLogAllElements();
		Assert.AreEqual(1, _gMesh.FaceCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.VertexCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.EdgeCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.LoopCount);
		Assert.AreEqual(2, _gMesh.GetEdge(0).V0PrevEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(0).V0NextEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(0).V1PrevEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(0).V1NextEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(1).V0PrevEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(1).V0NextEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(1).V1PrevEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(1).V1NextEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(2).V0PrevEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(2).V0NextEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(2).V1PrevEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(2).V1NextEdgeIndex);
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 0), "edge cycle count from v0");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 1), "edge cycle count from v1");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 2), "edge cycle count from v2");
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void AddFace_OneQuad()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_quadVertices); });
		_gMesh.DebugLogAllElements();
		Assert.AreEqual(_quadVertices.Length, _gMesh.VertexCount);
		Assert.AreEqual(3, _gMesh.GetEdge(0).V0PrevEdgeIndex);
		Assert.AreEqual(3, _gMesh.GetEdge(0).V0NextEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(0).V1PrevEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(0).V1NextEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(1).V0PrevEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(1).V0NextEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(1).V1PrevEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(1).V1NextEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(2).V0PrevEdgeIndex);
		Assert.AreEqual(1, _gMesh.GetEdge(2).V0NextEdgeIndex);
		Assert.AreEqual(3, _gMesh.GetEdge(2).V1PrevEdgeIndex);
		Assert.AreEqual(3, _gMesh.GetEdge(2).V1NextEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(3).V0PrevEdgeIndex);
		Assert.AreEqual(2, _gMesh.GetEdge(3).V0NextEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(3).V1PrevEdgeIndex);
		Assert.AreEqual(0, _gMesh.GetEdge(3).V1NextEdgeIndex);
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 0), "edge cycle count from v0");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 1), "edge cycle count from v1");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 2), "edge cycle count from v2");
		Assert.AreEqual(2, Validate.GetEdgeCycleCount(_gMesh, 3), "edge cycle count from v3");
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void AddFace_OnePentagon()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_pentagonVertices); });
		Assert.AreEqual(_pentagonVertices.Length, _gMesh.VertexCount);
		Validate.AllElementsAndRelations(_gMesh);
	}

	[Test]
	public void AddFace_OneHexagon()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_hexagonVertices); });
		Assert.AreEqual(_hexagonVertices.Length, _gMesh.VertexCount);
		Validate.AllElementsAndRelations(_gMesh);
	}
	
	
	[Test]
	public void AddFaces_TwoTriangles()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices2); });
		//_gMesh.DebugLogAllElements("TWO TRIANGLES");

		Assert.AreEqual(2, _gMesh.FaceCount);
		Assert.AreEqual(_triangleVertices.Length + _triangleVertices2.Length, _gMesh.VertexCount);
		Assert.AreEqual(_triangleVertices.Length + _triangleVertices2.Length, _gMesh.EdgeCount);
		Assert.AreEqual(_triangleVertices.Length + _triangleVertices2.Length, _gMesh.LoopCount);
		Validate.AllElementsAndRelations(_gMesh);
	}
}