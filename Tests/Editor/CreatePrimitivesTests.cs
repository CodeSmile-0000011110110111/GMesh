// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile.GMesh;
using NUnit.Framework;
using Unity.Mathematics;

[TestFixture]
public class CreatePrimitivesTests
{
	[Test]
	public void CreatePlane_1Quad()
	{
		using (var mesh = GMesh.Quad())
		{
			Validate.MeshElementCount(mesh, 1, 4,4,4);
		}
	}
	
	[Test]
	public void CreatePlane_2Quads()
	{
		using (var mesh = GMesh.Plane(new PlaneParameters(new int2(2,3))))
		{
			Validate.MeshElementCount(mesh, 2, 8,7,6);
		}
	}
	
	[Test]
	public void CreatePlane_4Quads()
	{
		using (var mesh = GMesh.Plane(new PlaneParameters(new int2(3, 3))))
		{
			Validate.MeshElementCount(mesh, 4, 16,12,9);
		}
	}
	
	[Test]
	public void CreatePlane_9Quads()
	{
		using (var mesh = GMesh.Plane(new PlaneParameters(new int2(4))))
		{
			Validate.MeshElementCount(mesh, 9, 36,24,16);
		}
	}
	
	[Test]
	public void CreateCube_6Quads()
	{
		using (var mesh = GMesh.Cube(new CubeParameters(new int3(2))))
		{
			Validate.MeshElementCount(mesh, 6, 24,12,8);
		}
	}
	
	[Test]
	public void CreateCube_24Quads()
	{
		using (var mesh = GMesh.Cube(new CubeParameters(new int3(3))))
		{
			Validate.MeshElementCount(mesh, 24, 96,48,26);
		}
	}
}
