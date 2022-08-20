// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile.GMesh;
using NUnit.Framework;
using Unity.Mathematics;

[TestFixture]
public class PrimitivesTests
{
	[Test]
	public void CreatePlane_1Quad()
	{
		var quadMesh = GMesh.Quad();
		Assert.AreEqual(1, quadMesh.FaceCount);
		Assert.AreEqual(4, quadMesh.LoopCount);
		Assert.AreEqual(4, quadMesh.EdgeCount);
		Assert.AreEqual(4, quadMesh.VertexCount);
	}
	
	[Test]
	public void CreatePlane_2Quads()
	{
		var planeMesh = GMesh.Plane(new PlaneParameters(new int2(2,3)));
		planeMesh.DebugLogAllElements();
		Assert.AreEqual(2, planeMesh.FaceCount, "faces");
		Assert.AreEqual(8, planeMesh.LoopCount, "loops");
		Assert.AreEqual(7, planeMesh.EdgeCount, "edges");
		Assert.AreEqual(6, planeMesh.VertexCount, "vertices");
	}
	
	[Test]
	public void CreatePlane_4Quads()
	{
		var planeMesh = GMesh.Plane(new PlaneParameters(new int2(3,3)));
		planeMesh.DebugLogAllElements();
		Assert.AreEqual(4, planeMesh.FaceCount);
		Assert.AreEqual(16, planeMesh.LoopCount);
		Assert.AreEqual(12, planeMesh.EdgeCount);
		Assert.AreEqual(9, planeMesh.VertexCount);
	}
	
	[Test]
	public void CreatePlane_9Quads()
	{
		var planeMesh = GMesh.Plane(new PlaneParameters(new int2(4,4)));
		planeMesh.DebugLogAllElements();
		Assert.AreEqual(9, planeMesh.FaceCount);
		Assert.AreEqual(36, planeMesh.LoopCount);
		Assert.AreEqual(24, planeMesh.EdgeCount);
		Assert.AreEqual(16, planeMesh.VertexCount);
	}
}
