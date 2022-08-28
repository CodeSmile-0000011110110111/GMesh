// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile;
using CodeSmile.GraphMesh;
using NUnit.Framework;
using Unity.Mathematics;

[TestFixture]
public class CreatePrimitivesTests
{
	[Test] public void CreatePlane_1Triangle()
	{
		using (var gMesh = GMesh.Triangle())
			Validate.MeshElementCount(gMesh, 1, 3, 3, 3);
	}

	[Test] public void CreatePlane_1Quad()
	{
		using (var gMesh = GMesh.Quad())
			Validate.MeshElementCount(gMesh, 1, 4, 4, 4);
	}

	[Test] public void CreatePlane_2Quads()
	{
		using (var gMesh = GMesh.Plane(new GMeshPlane(new int2(2, 3))))
			Validate.MeshElementCount(gMesh, 2, 8, 7, 6);
	}

	[Test] public void CreatePlane_4Quads()
	{
		using (var gMesh = GMesh.Plane(new GMeshPlane(new int2(3, 3))))
			Validate.MeshElementCount(gMesh, 4, 16, 12, 9);
	}

	[Test] public void CreatePlane_9Quads()
	{
		using (var gMesh = GMesh.Plane(new GMeshPlane(new int2(4))))
			Validate.MeshElementCount(gMesh, 9, 36, 24, 16);
	}

	[Test] public void CreateCube_6Quads()
	{
		using (var gMesh = GMesh.Cube(new GMeshCube(new int3(2))))
			Validate.MeshElementCount(gMesh, 6, 24, 12, 8);
	}

	[Test] public void CreateCube_24Quads()
	{
		using (var gMesh = GMesh.Cube(new GMeshCube(new int3(3))))
			Validate.MeshElementCount(gMesh, 24, 96, 48, 26);
	}

	[Test] public void CreateCube_24QuadsAndCopy()
	{
		using (var gMesh = GMesh.Cube(new GMeshCube(new int3(3))))
		{
			Validate.MeshElementCount(gMesh, 24, 96, 48, 26);
			
			using (var gMeshCopy1 = new GMesh(gMesh))
			{
				Validate.MeshElementCount(gMeshCopy1, 24, 96, 48, 26);
				Assert.IsFalse(gMeshCopy1.Equals(gMesh));
			}
		}
	}
}