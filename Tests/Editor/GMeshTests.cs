using CodeSmile.GMesh;
using NUnit.Framework;
using System;
using Unity.Mathematics;

[TestFixture]
public sealed partial class GMeshTests
{
	private GMesh _gMesh;

	[SetUp] public void SetUp() => _gMesh = new GMesh();
	[TearDown] public void TearDown() => _gMesh?.Dispose();

	[Test]
	public void AddFacesWithInsufficientVertices()
	{
		Assert.Throws<ArgumentNullException>(() => { _gMesh.CreateFace(null); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] {}); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] { new(0f, 0f, 0f) }); });
		Assert.Throws<ArgumentException>(() => { _gMesh.CreateFace(new float3[] { new(0f, 0f, 0f), new(1f, 1f, 1f) }); });
	}

	[Test]
	public void AddTriangleFace()
	{
		var positions = new float3[]
		{
			new(0f, 0f, 0f),
			new(1f, 1f, 1f),
			new(2f, 2f, 2f),
		};
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(positions); });
		Assert.AreEqual(1, _gMesh.FaceCount);
		Assert.AreEqual(positions.Length, _gMesh.VertexCount);
		Assert.AreEqual(positions.Length, _gMesh.EdgeCount);
		Assert.AreEqual(positions.Length, _gMesh.LoopCount);
		AssertValidAllElements();
	}

	[Test]
	public void AddQuadFace()
	{
		var positions = new float3[]
		{
			new(0f, 0f, 0f),
			new(1f, 1f, 1f),
			new(2f, 2f, 2f),
			new(3f, 3f, 3f),
		};
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(positions); });
		Assert.AreEqual(positions.Length, _gMesh.VertexCount);
		Assert.AreEqual(1, _gMesh.FaceCount);
		AssertValidAllElements();
	}

	[Test]
	public void AddHexagonFace()
	{
		var positions = new float3[]
		{
			new(0f, 0f, 0f),
			new(1f, 1f, 1f),
			new(2f, 2f, 2f),
			new(3f, 3f, 3f),
			new(4f, 4f, 4f),
			new(5f, 5f, 5f),
		};
		Assert.DoesNotThrow(() => { _gMesh.CreateFace(positions); });
		Assert.AreEqual(positions.Length, _gMesh.VertexCount);
		Assert.AreEqual(1, _gMesh.FaceCount);
		AssertValidAllElements();
	}
	
}