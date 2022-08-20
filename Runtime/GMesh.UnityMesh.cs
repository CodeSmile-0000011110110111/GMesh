// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		public static GMesh FromMesh(Mesh mesh)
		{
			if (mesh == null)
				throw new ArgumentNullException(nameof(mesh));

			var gmesh = new GMesh();
			// TODO

			return null;
		}

		public Mesh ToMesh(Mesh mesh = null)
		{
			if (mesh == null)
			{
				mesh = new Mesh();
				mesh.subMeshCount = 1;
			}
			else
				mesh.Clear();

			var initialCapacity = FaceCount * 3;
			var meshVertices = new NativeList<VertexPositionNormalUV>(initialCapacity, Allocator.TempJob);
			var meshIndices = new NativeList<uint>(initialCapacity, Allocator.TempJob);

			// MESHDATA
			var meshDataArray = Mesh.AllocateWritableMeshData(1);
			var meshData = meshDataArray[0];

			try
			{
				//var innerFaceTriangleIndices = new NativeList<uint>(Allocator.TempJob);

				var faceCount = FaceCount;
				for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
				{
					var face = GetFace(faceIndex);
					var totalVertexCount = face.ElementCount;
					var firstVertexIndex = (uint)meshVertices.Length;
					var currentVertex = (uint)0;

					if (totalVertexCount > 4)
						Debug.LogWarning("Note: Faces with more than 4 vertices have to be convex - no convex checks are performed");

					{
						// Fan triangulation: Tesselate into triangles where all originate from loop's first vertex
						// => only guaranteed to work with convex shapes
						ForEachLoop(faceIndex, loop =>
						{
							var loopVert = GetVertex(loop.StartVertexIndex);
							if (currentVertex > 2)
							{
								// add extra fan triangles from first vertex to last vertex
								meshIndices.Add(firstVertexIndex);
								meshIndices.Add(firstVertexIndex + currentVertex - 1);
							}

							meshIndices.Add(firstVertexIndex + currentVertex);
							meshVertices.Add(new VertexPositionNormalUV(loopVert.Position, float3.zero, float2.zero));
							currentVertex++;
						});
						
						// TODO: try triangle strip triangulation
						// 2->0->1 then 3->2->1 then 4->2->3 then 5->4->3
						// https://en.wikipedia.org/wiki/Triangle_strip
					}


					
					/*
					else if (triangulationApproach >= 1)
					{
						// Approach #2: triangles created in sequence, then gaps are closed
						ForEachLoop(face, loop =>
						{
							var loopVert = GetVertex(loop.VertexIndex);
							meshVertices.Add(new VertexPositionNormalUV(loopVert.Position, loopVert.Normal, loop.UV));
							var triangleIndex = firstVertexIndex + currentVertex;
							meshIndices.Add(triangleIndex);

							currentVertex++;

							if (totalVertexCount > 3)
							{
								var isEvenVertex = currentVertex % 2 == 1;
								if (isEvenVertex)
								{
									// keep the even indices to close the inner triangles afterwards
									if (totalVertexCount > 4)
										innerFaceTriangleIndices.Add(triangleIndex);

									// duplicate every even-numbered index (except for first/last) when face has more than 1 triangle
									if (currentVertex != 1 && currentVertex < totalVertexCount)
										meshIndices.Add(triangleIndex);
								}
							}
						});

						// faces with even-numbered vertices need to close the last triangle by connecting to first vertex
						if (currentVertex % 2 == 0)
							meshIndices.Add(firstVertexIndex);

						// create inner triangles
						var innerCount = innerFaceTriangleIndices.Length;
						if (innerCount >= 3)
						{
							for (var i = 0; i < innerCount; i++)
							{
								meshIndices.Add(innerFaceTriangleIndices[i]);

								// duplicate every other index
								if (innerCount > 3 && i != 0 && i % 2 == 0)
									meshIndices.Add(innerFaceTriangleIndices[i]);
							}
							
							// again: even numbered vertex count => close last triangle by connecting to first vertex
							if (innerCount % 2 == 0)
								meshIndices.Add(firstVertexIndex);
						}
					}
					*/

					//innerFaceTriangleIndices.Clear();

					// TEST summation of triangles
					/*
					int tri = 1;
					for (int i = 0; i < meshIndices.Length; i+=3)
					{
						var v0 = meshVertices[(int)meshIndices[i+0]].Position;
						var v1 = meshVertices[(int)meshIndices[i+1]].Position;
						var v2 = meshVertices[(int)meshIndices[i+2]].Position;

						var cw = IsClockwise(v0, v1, v2);

						var e0 = v1 - v0;
						var e1 = v2 - v1;
						var e2 = v0 - v2;

						var ea = v1 - v0;
						var eb = v2 - v0;
						var eNormal = math.cross(ea, eb);
						var w = math.dot(eNormal, v0 - math.forward());
						var eNormalSum = math.csum(eNormal);
						
						var cross0 = math.cross(e0, math.forward());
						var cross1 = math.cross(e1, math.forward());
						var cross2 = math.cross(e2, math.forward());

						var l0 = math.length(cross0);
						var l1 = math.length(cross1);
						var l2 = math.length(cross2);
						var sum = l0+l1+l2;
						//{e0}/{e1}/{e2} =>  => {l0} / {l1} / {l2}
						//Debug.Log($"Tri {tri} => Sum: {sum}, eN: {eNormal}, eNm: {math.csum(eNormal)}, w: {w}");
						tri++;
					}
					*/
				}

				//innerFaceTriangleIndices.Dispose();

				/*
				// FIXME HACK: fix correct index count if something breaks
				var expectedIndexCount = (meshVertices.Length - 2) * 3;
				for (var i = meshIndices.Length; i < expectedIndexCount; i++)
				{
					Debug.LogWarning("FIXME: added missing index to prevent crash");
					meshIndices.Add(0);
				}
				*/

				/*
				for (int i = 0; i < meshIndices.Length; i+=3)
				{
					var i0 = (int) meshIndices[i];
					var i1 = (int)meshIndices[i+1];
					var i2 = (int)meshIndices[i+2];
					var v0 = meshVertices[i0].Position;
					var v1 = meshVertices[i1].Position;
					var v2 = meshVertices[i2].Position;
					Debug.Log($"Tri: {i0}-{i1}-{i2} => {v0}-{v1}-{v2}");
				}
				*/
				
				//throw new Exception();

				// VERTEX BUFFER
				meshData.SetVertexBufferParams(meshVertices.Length, VertexPositionNormalUV.Attributes);
				var vertexData = meshData.GetVertexData<VertexPositionNormalUV>();
				for (var i = 0; i < meshVertices.Length; i++)
					vertexData[i] = meshVertices[i];

				// INDEX BUFFER
				var indicesAre16Bit = meshIndices.Length < short.MaxValue;
				var indexFormat = indicesAre16Bit ? IndexFormat.UInt16 : IndexFormat.UInt32;
				meshData.SetIndexBufferParams(meshIndices.Length, indexFormat);
				if (indicesAre16Bit)
				{
					var indexData16 = meshData.GetIndexData<ushort>();
					for (var i = 0; i < meshIndices.Length; i++)
						indexData16[i] = (ushort)meshIndices[i];
				}
				else
				{
					var indexData32 = meshData.GetIndexData<uint>();
					for (var i = 0; i < meshIndices.Length; i++)
						indexData32[i] = meshIndices[i];
				}

				// SUBMESH INFO
				meshData.subMeshCount = 1;
				var subMeshDesc = new SubMeshDescriptor(0, meshIndices.Length) { vertexCount = meshVertices.Length };
				meshData.SetSubMesh(0, subMeshDesc);
			}
			finally
			{
				// APPLY MESHDATA & DISPOSE
				Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
				meshVertices.Dispose();
				meshIndices.Dispose();
			}

			// RECALCULATE & OPTIMIZE
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			//mesh.RecalculateUVDistributionMetrics();
			mesh.Optimize();

			mesh.name = ToString();
			return mesh;
		}

		private struct VertexPositionNormalUV
		{
			public float3 Position;
			public float3 Normal;
			public float2 UV;

			public VertexPositionNormalUV(float3 position, float3 normal, float2 uv)
			{
				Position = position;
				Normal = normal;
				UV = uv;
			}

			public static readonly VertexAttributeDescriptor[] Attributes =
			{
				new() { attribute = VertexAttribute.Position, format = VertexAttributeFormat.Float32, dimension = 3, stream = 0 },
				new() { attribute = VertexAttribute.Normal, format = VertexAttributeFormat.Float32, dimension = 3, stream = 0 },
				new() { attribute = VertexAttribute.TexCoord0, format = VertexAttributeFormat.Float32, dimension = 2, stream = 0 },
			};
		}

		/*
		public static bool IsClockwise(float3 p1, float3 p2, float3 p3)
		{
			var isClockWise = true;

			var determinant = p1.x * p2.y + p3.x * p1.y + p2.x * p3.y -
			                  p1.x * p3.y - p3.x * p2.y - p2.x * p1.y;
			var determinant3d = math.determinant(new float3x3(p1, p2, p3));

			if (determinant > 0f)
				isClockWise = false;

			return isClockWise;
		}
		*/
	}
}