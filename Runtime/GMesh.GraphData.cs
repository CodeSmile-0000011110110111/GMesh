// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		[BurstCompile] [StructLayout(LayoutKind.Sequential)]
		internal struct GraphData
		{
			private NativeList<Vertex> _vertices;
			private NativeList<Edge> _edges;
			private NativeList<Loop> _loops;
			private NativeList<Face> _faces;

			public GraphData(ref NativeList<Vertex> vertices, ref NativeList<Edge> edges, ref NativeList<Loop> loops,
				ref NativeList<Face> faces)
			{
				_vertices = vertices;
				_edges = edges;
				_loops = loops;
				_faces = faces;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void InitializeVerticesWithSize(int initialSize) => _vertices.ResizeUninitialized(initialSize);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void InitializeEdgesWithSize(int initialSize) => _edges.ResizeUninitialized(initialSize);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void InitializeLoopsWithSize(int initialSize) => _loops.ResizeUninitialized(initialSize);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void InitializeFacesWithSize(int initialSize) => _faces.ResizeUninitialized(initialSize);

			[MethodImpl(MethodImplOptions.AggressiveInlining)] public Vertex GetVertex(int index) => _vertices[index];
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public Edge GetEdge(int index) => _edges[index];
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public Loop GetLoop(int index) => _loops[index];
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public Face GetFace(int index) => _faces[index];

			[MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddVertex(in Vertex vertex) => _vertices.Add(vertex);
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddEdge(in Edge edge) => _edges.Add(edge);
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddLoop(in Loop loop) => _loops.Add(loop);
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public void AddFace(in Face face) => _faces.Add(face);

			[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVertex(in Vertex vertex) => _vertices[vertex.Index] = vertex;
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetEdge(in Edge edge) => _edges[edge.Index] = edge;
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetLoop(in Loop loop) => _loops[loop.Index] = loop;
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFace(in Face face) => _faces[face.Index] = face;

			[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetNextVertexIndex() => _vertices.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetNextEdgeIndex() => _edges.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetNextLoopIndex() => _loops.Length;
			[MethodImpl(MethodImplOptions.AggressiveInlining)] public int GetNextFaceIndex() => _faces.Length;
		}
	}
}