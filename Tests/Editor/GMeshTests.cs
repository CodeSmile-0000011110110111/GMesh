using CodeSmile.GMesh;
using NUnit.Framework;
using System;
using Unity.Mathematics;

[TestFixture]
public sealed partial class GMeshTests
{
	private readonly float3[] _triangleVertices = { new(0f, 0f, 0f), new(1f, 1f, 1f), new(2f, 2f, 2f) };
	private readonly float3[] _quadVertices = { new(0f, 0f, 0f), new(1f, 1f, 1f), new(2f, 2f, 2f), new(3f, 3f, 3f) };
	private readonly float3[] _hexagonVertices =
		{ new(0f, 0f, 0f), new(1f, 1f, 1f), new(2f, 2f, 2f), new(3f, 3f, 3f), new(4f, 4f, 4f), new(5f, 5f, 5f) };

	private GMesh _gMesh;

	[SetUp] public void SetUp() => _gMesh = new GMesh();
	[TearDown] public void TearDown() => _gMesh?.Dispose();

	[Test]
	public void TryAddFacesWithNotEnoughVertices()
	{
		Assert.Throws<ArgumentNullException>(() => { _gMesh.CreateFace(null); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] {}); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] { new(0f, 0f, 0f) }); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] { new(0f, 0f, 0f), new(1f, 1f, 1f) }); });
	}

	[Test]
	public void DeleteVertex_Triangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });

		// this should clear the entire mesh from the bottom up
		Assert.DoesNotThrow(() => { _gMesh.DeleteVertex(0); });
		_gMesh.DebugLogAllElements("AFTER DELETE");
		Assert.AreEqual(0, _gMesh.FaceCount);
		Assert.AreEqual(0, _gMesh.VertexCount);
		Assert.AreEqual(0, _gMesh.EdgeCount);
		Assert.AreEqual(0, _gMesh.LoopCount);
	}

	[Test]
	public void DeleteEdge_Triangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });

		// this should clear the entire mesh from the bottom up
		Assert.DoesNotThrow(() => { _gMesh.DeleteEdge(0); });
		_gMesh.DebugLogAllElements("AFTER DELETE");
		Assert.AreEqual(0, _gMesh.FaceCount);
		Assert.AreEqual(0, _gMesh.VertexCount);
		Assert.AreEqual(0, _gMesh.EdgeCount);
		Assert.AreEqual(0, _gMesh.LoopCount);
	}

	[Test]
	public void DeleteFace_Triangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });

		// this should clear the entire mesh "face down"
		Assert.DoesNotThrow(() => { _gMesh.DeleteFace(0); });
		_gMesh.DebugLogAllElements("AFTER DELETE");
		Assert.AreEqual(0, _gMesh.FaceCount);
		Assert.AreEqual(0, _gMesh.VertexCount);
		Assert.AreEqual(0, _gMesh.EdgeCount);
		Assert.AreEqual(0, _gMesh.LoopCount);
	}

	[Test]
	public void AddFace_Triangle()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_triangleVertices); });
		Assert.AreEqual(1, _gMesh.FaceCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.VertexCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.EdgeCount);
		Assert.AreEqual(_triangleVertices.Length, _gMesh.LoopCount);
		AssertAllElementsAreValidAndCorrectlyRelated();
	}

	[Test]
	public void AddFace_Quad()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_quadVertices); });
		Assert.AreEqual(_quadVertices.Length, _gMesh.VertexCount);
		Assert.AreEqual(1, _gMesh.FaceCount);
		AssertAllElementsAreValidAndCorrectlyRelated();
	}

	[Test]
	public void AddFace_Hexagon()
	{
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(_hexagonVertices); });
		Assert.AreEqual(_hexagonVertices.Length, _gMesh.VertexCount);
		Assert.AreEqual(1, _gMesh.FaceCount);
		AssertAllElementsAreValidAndCorrectlyRelated();
	}
}