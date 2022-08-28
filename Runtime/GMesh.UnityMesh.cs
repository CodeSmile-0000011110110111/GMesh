// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

			// CREATE MESHDATA
			var meshDataArray = Mesh.AllocateWritableMeshData(1);

			var faces = Faces;

			// COUNT VERTICES & TRIANGLES
			var triangleStartIndices = new NativeArray<int>(ValidFaceCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var totalVCount = new NativeReference<int>(Allocator.TempJob);
			var totalICount = new NativeReference<int>(Allocator.TempJob);
			var gatherDataJob = new JMesh.GatherFaceDataJob
			{
				Faces = faces, TriangleStartIndices = triangleStartIndices, TotalVertexCount = totalVCount,
				TotalIndexCount = totalICount,
			};
			var faceCount = _data.Faces.Length;
			var gatherDataHandle = gatherDataJob.Schedule(faceCount, default);

			// COUNT COMPLETE
			var meshData = meshDataArray[0];
			gatherDataHandle.Complete();

			// PREPARE VERTEX BUFFER
			var attributes = new NativeArray<VertexAttributeDescriptor>(JMesh.VertexPositionNormalUV.AttributeCount, Allocator.Temp,
				NativeArrayOptions.UninitializedMemory);
			JMesh.VertexPositionNormalUV.GetAttributes(ref attributes);
			var totalVertexCount = totalVCount.Value;
			meshData.SetVertexBufferParams(totalVertexCount, attributes);
			attributes.Dispose();

			// PREPARE INDEX BUFFER
			var totalIndexCount = totalICount.Value;
			var indicesAre16Bit = totalIndexCount < ushort.MaxValue;
			meshData.SetIndexBufferParams(totalIndexCount, indicesAre16Bit ? IndexFormat.UInt16 : IndexFormat.UInt32);

			// TRI(STR)ANGULATION
			JobHandle triangulateHandle;
			if (indicesAre16Bit)
			{
				var triangulateJob = new JMesh.FanTriangulateFaces16BitJob
				{
					Faces = faces, Loops = Loops, Vertices = Vertices, TriangleStartIndices = triangleStartIndices,
					VBuffer = meshData.GetVertexData<JMesh.VertexPositionNormalUV>(),
					IBuffer = meshData.GetIndexData<ushort>(),
				};
				triangulateHandle = triangulateJob.Schedule(ValidLoopCount, 4);
			}
			else
				throw new NotImplementedException("TODO: 32 bit mesh indices");
			
			// COMPLETE & DISPOSE
			totalVCount.Dispose();
			totalICount.Dispose();
			triangleStartIndices.Dispose(triangulateHandle);
			triangulateHandle.Complete();

			// APPLY MESHDATA
			meshData.subMeshCount = 1;
			meshData.SetSubMesh(0, new SubMeshDescriptor(0, totalIndexCount) { vertexCount = totalVertexCount });
			Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

			// RECALCULATE
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();

			//mesh.name = ToString();
			return mesh;
		}

		private static class JMesh
		{
			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct GatherFaceDataJob : IJobFor
			{
				[ReadOnly] public NativeArray<Face>.ReadOnly Faces;
				[WriteOnly] public NativeArray<int> TriangleStartIndices;

				public NativeReference<int> TotalVertexCount;
				public NativeReference<int> TotalIndexCount;

				public void Execute(int faceIndex)
				{
					var face = Faces[faceIndex];
					if (Hint.Likely(face.IsValid))
					{
						TriangleStartIndices[faceIndex] = TotalIndexCount.Value;
						var faceElementCount = face.ElementCount;
						TotalVertexCount.Value += faceElementCount;
						TotalIndexCount.Value += (faceElementCount - 2) * 3;
					}
				}
			}

			// TODO: split into two jobs, one for vertices and another for indices ?
			[BurstCompile] [StructLayout(LayoutKind.Sequential)]
			public struct FanTriangulateFaces16BitJob : IJobParallelFor
			{
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Face>.ReadOnly Faces;
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Loop>.ReadOnly Loops;
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<Vertex>.ReadOnly Vertices;
				[ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<int> TriangleStartIndices;

				[WriteOnly] [NoAlias] [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
				public NativeArray<VertexPositionNormalUV> VBuffer;

				[WriteOnly] [NoAlias] [NativeDisableParallelForRestriction] [NativeDisableContainerSafetyRestriction]
				public NativeArray<ushort> IBuffer;

				public void Execute(int loopIndex)
				{
					// TODO: try triangle strip triangulation
					// 2->0->1 then 3->2->1 then 4->2->3 then 5->4->3
					// https://en.wikipedia.org/wiki/Triangle_strip

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
							if (Hint.Likely(triangleVertIndex > 2))
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