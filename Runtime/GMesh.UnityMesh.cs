// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace CodeSmile.GraphMesh
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
				mesh = new Mesh();
			else
				mesh.Clear();

			mesh.subMeshCount = 1;

			var initialCapacity = FaceCount * 3;
			var meshVertices = new NativeList<VertexPositionNormalUV>(initialCapacity, Allocator.TempJob);
			var meshIndices = new NativeList<uint>(initialCapacity, Allocator.TempJob);

			// MESHDATA
			var meshDataArray = Mesh.AllocateWritableMeshData(1);
			var meshData = meshDataArray[0];

			try
			{
				var faceCount = FaceCount;
				for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
				{
					var firstVertexIndex = (uint)meshVertices.Length;
					var currentVertex = (uint)0;

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
			//mesh.Optimize();

			//mesh.name = ToString();
			return mesh;
		}

		private struct VertexPositionNormalUV
		{
			public readonly float3 Position;
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
	}
}