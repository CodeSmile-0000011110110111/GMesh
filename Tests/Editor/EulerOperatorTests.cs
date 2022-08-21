// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile.GMesh;
using NUnit.Framework;
using System.Collections.Generic;
using Tests.Editor;
using Unity.Mathematics;

[TestFixture]
public class EulerOperatorTests
{
	[Test]
	public void SplitAllEdges_1Triangle()
	{
		using (var mesh = new GMesh())
		{
			Assert.DoesNotThrow(() => { mesh.CreateFace(Constants.TriangleVertices); });
			Validate.AllElementsAndRelations(mesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = mesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				mesh.SplitEdgeAndCreateVertex(i);

			mesh.DebugLogAllElements();
			Assert.AreEqual(1, mesh.FaceCount);
			Assert.AreEqual(6, mesh.GetFace(0).ElementCount);
			Validate.AllElementsAndRelations(mesh);
		}
	}

	[Test]
	public void SplitAllEdges_1Hexagon()
	{
		using (var mesh = new GMesh())
		{
			Assert.DoesNotThrow(() => { mesh.CreateFace(Constants.HexagonVertices); });
			Validate.AllElementsAndRelations(mesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = mesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				mesh.SplitEdgeAndCreateVertex(i);

			mesh.DebugLogAllElements();
			Assert.AreEqual(1, mesh.FaceCount);
			Assert.AreEqual(12, mesh.GetFace(0).ElementCount);
			Validate.AllElementsAndRelations(mesh);
		}
	}

	[Test]
	public void SplitAllEdges_4x4Plane()
	{
		using (var mesh = GMesh.Plane(new PlaneParameters(new int2(4))))
		{
			Validate.MeshElementCount(mesh, 9, 36, 24, 16);
			Validate.AllElementsAndRelations(mesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = mesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				mesh.SplitEdgeAndCreateVertex(i);

			Validate.AllElementsAndRelations(mesh);
		}
	}
	
	
	[Test]
	public void SplitAllEdges_Cube()
	{
		using (var mesh = GMesh.Cube(new CubeParameters(new int3(3))))
		{
			Validate.MeshElementCount(mesh, 24, 96, 48, 26);
			Validate.AllElementsAndRelations(mesh);

			// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
			var edgeCount = mesh.EdgeCount;
			for (var i = 0; i < edgeCount; i++)
				mesh.SplitEdgeAndCreateVertex(i);

			Validate.AllElementsAndRelations(mesh);
		}
	}
	
	[Test]
	public void SplitAllEdgesMultipleTimes_Cube()
	{
		using (var mesh = GMesh.Cube(new CubeParameters(new int3(3))))
		{
			Validate.MeshElementCount(mesh, 24, 96, 48, 26);
			Validate.AllElementsAndRelations(mesh);

			// CAUTION: keep iteration count minimal since edge count doubles with every iteration
			for (int o = 0; o < 3; o++)
			{
				// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
				var edgeCount = mesh.EdgeCount;
				for (var i = 0; i < edgeCount; i++)
					mesh.SplitEdgeAndCreateVertex(i);
			}
			
			Validate.AllElementsAndRelations(mesh);
		}
	}
}