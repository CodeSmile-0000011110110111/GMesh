// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
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

			// SETUP
			var totalVertCount = 0;
			var totalIndexCount = 0;
			var faceCount = FaceCount;
			var triangleStartIndices = new NativeArray<int>(faceCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			for (var i = 0; i < faceCount; i++)
			{
				triangleStartIndices[i] = totalIndexCount;
				var faceElementCount = Faces[i].ElementCount;
				totalVertCount += faceElementCount;
				totalIndexCount += (faceElementCount - 2) * 3;
			}

			var indicesAre16Bit = totalIndexCount < ushort.MaxValue;
			var meshIndices16 = indicesAre16Bit ? new NativeArray<ushort>(totalIndexCount, Allocator.TempJob) : default;
			var meshIndices32 = indicesAre16Bit == false ? new NativeArray<uint>(totalIndexCount, Allocator.TempJob) : default;
			var meshVertices = new NativeArray<JMesh.VertexPositionNormalUV>(totalVertCount, Allocator.TempJob);

			// MESHDATA
			var meshDataArray = Mesh.AllocateWritableMeshData(1);
			var meshData = meshDataArray[0];


			try
			{
				// TODO: try triangle strip triangulation
				// 2->0->1 then 3->2->1 then 4->2->3 then 5->4->3
				// https://en.wikipedia.org/wiki/Triangle_strip

				// TRI(STR)ANGULATION
				var triangulateJob = new JMesh.FanTriangulateFaces16Bit
				{
					Faces = Faces, Loops = Loops, Vertices = Vertices, VBuffer = meshVertices, IBuffer = meshIndices16,
					TriangleStartIndices = triangleStartIndices
				};
				var triangulateHandle = triangulateJob.Schedule(LoopCount, 1);
				triangleStartIndices.Dispose(triangulateHandle);
				triangulateHandle.Complete();

				/*
				uint vIndex = 0;
				var faceCount = FaceCount;
				for (var faceIndex = 0; faceIndex < faceCount; faceIndex++)
				{
					var triangleStartVertIndex = vIndex;
					uint triangleVertIndex = 0;

					// Fan triangulation: Tesselate into triangles where all originate from loop's first vertex
					// => only guaranteed to work with convex shapes
					ForEachLoop(faceIndex, loop =>
					{
						var loopVert = GetVertex(loop.StartVertexIndex);

						if (triangleVertIndex > 2)
						{
							// add extra fan triangles from first vertex to last vertex
							if (indicesAre16Bit)
							{
								meshIndices16.Add((ushort)triangleStartVertIndex);
								meshIndices16.Add((ushort)(triangleStartVertIndex + triangleVertIndex - 1));
							}
							else
							{
								meshIndices32.Add(triangleStartVertIndex);
								meshIndices32.Add(triangleStartVertIndex + triangleVertIndex - 1);
							}
						}

						if (indicesAre16Bit)
							meshIndices16.Add((ushort)(triangleStartVertIndex + triangleVertIndex));
						else
							meshIndices32.Add(triangleStartVertIndex + triangleVertIndex);

						meshVertices.Add(new JMesh.VertexPositionNormalUV(loopVert.Position, math.up(), float2.zero));

						triangleVertIndex++;
						vIndex++;
					});
				}
				*/

				// FIXME: write directly to buffers!

				// VERTEX BUFFER PARAMS
				var attributes = new NativeArray<VertexAttributeDescriptor>(JMesh.VertexPositionNormalUV.AttributeCount, Allocator.Temp,
					NativeArrayOptions.UninitializedMemory);
				JMesh.VertexPositionNormalUV.GetAttributes(ref attributes);

				var vCount = meshVertices.Length;
				meshData.SetVertexBufferParams(vCount, attributes);
				attributes.Dispose();

				// VERTEX BUFFER
				var vertexData = meshData.GetVertexData<JMesh.VertexPositionNormalUV>();
				NativeArray<JMesh.VertexPositionNormalUV>.Copy(meshVertices, vertexData);

				// INDEX BUFFER
				var iCount = indicesAre16Bit ? meshIndices16.Length : meshIndices32.Length;
				if (indicesAre16Bit && iCount >= ushort.MaxValue)
					throw new InvalidOperationException("index count exceeds ushort.MaxValue - assumption facecount*3 < index count");

				meshData.SetIndexBufferParams(iCount, indicesAre16Bit ? IndexFormat.UInt16 : IndexFormat.UInt32);
				if (indicesAre16Bit)
					NativeArray<ushort>.Copy(meshIndices16, meshData.GetIndexData<ushort>());
				else
					NativeArray<uint>.Copy(meshIndices32, meshData.GetIndexData<uint>());

				// SUBMESH
				meshData.subMeshCount = 1;
				meshData.SetSubMesh(0, new SubMeshDescriptor(0, iCount) { vertexCount = vCount });
			}
			finally
			{
				// APPLY MESHDATA & DISPOSE
				Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
				meshVertices.Dispose();

				if (meshIndices16.IsCreated)
					meshIndices16.Dispose();
				if (meshIndices32.IsCreated)
					meshIndices32.Dispose();
			}
			// RECALCULATE & OPTIMIZE
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			//mesh.RecalculateTangents();
			//mesh.RecalculateUVDistributionMetrics();
			//mesh.Optimize();

			//mesh.name = ToString();
			return mesh;
		}

		private static class JMesh
		{
			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct FanTriangulateFaces16Bit : IJobParallelFor
			{
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Face>.ReadOnly Faces;
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Loop>.ReadOnly Loops;
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Vertex>.ReadOnly Vertices;
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<int> TriangleStartIndices;

				[WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<VertexPositionNormalUV> VBuffer;
				[WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<ushort> IBuffer;

				//public NativeReference<int> IndexBufferIndex;

				public void Execute(int loopIndex)
				{
					// Fan triangulation: Tesselate into triangles where all originate from loop's first vertex
					// => only guaranteed to work with convex polygons
					var loop = Loops[loopIndex];
					var face = Faces[loop.FaceIndex];
					if (Hint.Unlikely(face.FirstLoopIndex == loopIndex) && Hint.Likely(face.IsValid))
					{
						var iIndex = TriangleStartIndices[face.Index];
						var vIndex = loopIndex;
						var triangleStartVertIndex = vIndex;
						uint triangleVertIndex = 0;

						var elementCount = face.ElementCount;
						for (var i = 0; i < elementCount; i++)
						{
							var loopVert = Vertices[loop.StartVertexIndex];
							if (triangleVertIndex > 2)
							{
								// add extra fan triangles from first vertex to last vertex
								IBuffer[iIndex++] = (ushort)triangleStartVertIndex;
								IBuffer[iIndex++] = (ushort)(triangleStartVertIndex + triangleVertIndex - 1);
							}

							IBuffer[iIndex++] = (ushort)(triangleStartVertIndex + triangleVertIndex);
							VBuffer[vIndex++] = new VertexPositionNormalUV(loopVert.Position, float3.zero, float2.zero);

							triangleVertIndex++;

							loop = Loops[loop.NextLoopIndex];
						}
					}
				}
			}

			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct VertexPositionNormalUV
			{
				public readonly float3 Position;
				public readonly float3 Normal;
				public readonly float2 UV;

				public override string ToString() => $"Pos {Position}";

				public VertexPositionNormalUV(float3 position, float3 normal, float2 uv)
				{
					Position = position;
					Normal = normal;
					UV = uv;
				}

				public static int AttributeCount => 3;

				public static void GetAttributes(ref NativeArray<VertexAttributeDescriptor> attributes)
				{
					attributes[0] = new VertexAttributeDescriptor
						{ attribute = VertexAttribute.Position, format = VertexAttributeFormat.Float32, dimension = 3, stream = 0 };
					attributes[1] = new VertexAttributeDescriptor
						{ attribute = VertexAttribute.Normal, format = VertexAttributeFormat.Float32, dimension = 3, stream = 0 };
					attributes[2] = new VertexAttributeDescriptor
						{ attribute = VertexAttribute.TexCoord0, format = VertexAttributeFormat.Float32, dimension = 2, stream = 0 };
				}
			}
		}
	}
}