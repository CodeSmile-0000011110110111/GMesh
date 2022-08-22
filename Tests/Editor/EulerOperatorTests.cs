// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using CodeSmile.GMesh;
using NUnit.Framework;
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
			for (var o = 0; o < 3; o++)
			{
				// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
				var edgeCount = mesh.EdgeCount;
				for (var i = 0; i < edgeCount; i++)
					mesh.SplitEdgeAndCreateVertex(i);
			}

			Validate.AllElementsAndRelations(mesh);
		}
	}

	[Test]
	public void SplitAllEdgesTwice_3Planes()
	{
		// Cube did not preserve correct loop order when splitting, this is the minimal test case where it occured
		// between: face 1 + 2, along edges: 5, 23, 14, 32 / vertices: 1, 21, 12, 30, 5
		// two loops connect 1=>21 and 21=>5 hopping over neighbouring edges, loop 19 is one of them
		var vertexCount = new int3(2);
		var f = GMesh.Plane(new PlaneParameters(vertexCount.xy, new float3(0f, 0f, 0.5f), new float3(0f, 180f, 0f)));
		var u = GMesh.Plane(new PlaneParameters(vertexCount.xz, new float3(0f, 0.5f, 0f), new float3(90f, 270f, 270f)));
		var r = GMesh.Plane(new PlaneParameters(vertexCount.zy, new float3(0.5f, 0f, 0f), new float3(0f, 270f, 0f)));
		using (var mesh = GMesh.Combine(new[] { f, u, r }, true))
		{
			Validate.AllElementsAndRelations(mesh);

			// CAUTION: keep iteration count minimal since edge count doubles with every iteration
			for (var o = 0; o < 2; o++)
			{
				// CAUTION: the local variable is important since we'll be adding more edges, thus increasing mesh.EdgeCount (=> infinite loop!)
				var edgeCount = mesh.EdgeCount;
				for (var i = 0; i < edgeCount; i++)
					mesh.SplitEdgeAndCreateVertex(i);
			}

			mesh.DebugLogAllElements();
			Validate.AllElementsAndRelations(mesh);
		}
	}
}