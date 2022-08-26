// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		[BurstCompile] [StructLayout(LayoutKind.Sequential)]
		internal struct GraphData : IDisposable, ICloneable, IEquatable<GraphData>
		{
			private enum Element
			{
				Vertex = 0,
				Edge = 1,
				Loop = 2,
				Face = 3,

				Count = 4,
			}

			// FIXME: make private !
			internal NativeArray<int> _elementCounts;
			internal NativeList<Vertex> _vertices;
			internal NativeList<Edge> _edges;
			internal NativeList<Loop> _loops;
			internal NativeList<Face> _faces;

			/// <summary>
			/// The read-only collection of vertices.
			/// </summary>
			public NativeArray<Vertex>.ReadOnly Vertices => _vertices.AsParallelReader();
			/// <summary>
			/// The read-only collection of edges.
			/// </summary>
			public NativeArray<Edge>.ReadOnly Edges => _edges.AsParallelReader();
			/// <summary>
			/// The read-only collection of loops.
			/// </summary>
			public NativeArray<Loop>.ReadOnly Loops => _loops.AsParallelReader();
			/// <summary>
			/// The read-only collection of faces.
			/// </summary>
			public NativeArray<Face>.ReadOnly Faces => _faces.AsParallelReader();

			/// <summary>
			/// Number of vertices in the mesh.
			/// </summary>
			public int VertexCount { get => _elementCounts[(int)Element.Vertex]; private set => _elementCounts[(int)Element.Vertex] = value; }
			/// <summary>
			/// Number of edges in the mesh.
			/// </summary>
			public int EdgeCount { get => _elementCounts[(int)Element.Edge]; private set => _elementCounts[(int)Element.Edge] = value; }
			/// <summary>
			/// Number of loops in the mesh.
			/// </summary>
			public int LoopCount { get => _elementCounts[(int)Element.Loop]; private set => _elementCounts[(int)Element.Loop] = value; }
			/// <summary>
			/// Number of faces in the mesh.
			/// </summary>
			public int FaceCount { get => _elementCounts[(int)Element.Face]; private set => _elementCounts[(int)Element.Face] = value; }

			/// <summary>
			/// Check if the GMesh needs disposing. For developers who get easily confused. :)
			/// 
			/// Rule: after you are done using a GMesh instance you need to manually call Dispose() on it.
			/// In convoluted code this can easily be cumbersome so I decided to add this check.
			/// Note that indiscriminately calling Dispose() multiple times will throw an exception.
			/// </summary>
			/// <value></value>
			public bool IsDisposed =>
				!(_elementCounts.IsCreated && _vertices.IsCreated && _edges.IsCreated && _loops.IsCreated && _faces.IsCreated);

			public GraphData(AllocatorManager.AllocatorHandle allocator)
			{
				_elementCounts = new NativeArray<int>((int)Element.Count, Allocator.Persistent);
				_vertices = new NativeList<Vertex>(allocator);
				_edges = new NativeList<Edge>(allocator);
				_loops = new NativeList<Loop>(allocator);
				_faces = new NativeList<Face>(allocator);
			}

			internal GraphData(GraphData other)
				: this(Allocator.Persistent)
			{
				_elementCounts.CopyFrom(other._elementCounts);
				_vertices.CopyFrom(other._vertices);
				_edges.CopyFrom(other._edges);
				_loops.CopyFrom(other._loops);
				_faces.CopyFrom(other._faces);
			}

			public void Dispose()
			{
				if (IsDisposed)
					throw new InvalidOperationException("Already disposed! Use IsDisposed property to check disposed state.");

				_elementCounts.Dispose();
				_vertices.Dispose();
				_edges.Dispose();
				_loops.Dispose();
				_faces.Dispose();
			}

			public void InitializeVerticesWithSize(int initialSize) => _vertices.ResizeUninitialized(initialSize);
			public void InitializeEdgesWithSize(int initialSize) => _edges.ResizeUninitialized(initialSize);
			public void InitializeLoopsWithSize(int initialSize) => _loops.ResizeUninitialized(initialSize);
			public void InitializeFacesWithSize(int initialSize) => _faces.ResizeUninitialized(initialSize);

			public int GetNextVertexIndex() => _elementCounts[(int)Element.Vertex];
			public int GetNextEdgeIndex() => _elementCounts[(int)Element.Edge];
			public int GetNextLoopIndex() => _elementCounts[(int)Element.Loop];
			public int GetNextFaceIndex() => _elementCounts[(int)Element.Face];
			public Vertex GetVertex(int index) => _vertices[index];
			public Edge GetEdge(int index) => _edges[index];
			public Loop GetLoop(int index) => _loops[index];
			public Face GetFace(int index) => _faces[index];
			public void SetVertex(in Vertex vertex) => _vertices[vertex.Index] = vertex;
			public void SetEdge(in Edge edge) => _edges[edge.Index] = edge;
			public void SetLoop(in Loop loop) => _loops[loop.Index] = loop;
			public void SetFace(in Face face) => _faces[face.Index] = face;

			internal int AddVertex(ref Vertex vertex)
			{
				vertex.Index = GetNextVertexIndex();
				if (vertex.Index < _vertices.Length) SetVertex(vertex);
				else _vertices.Add(vertex);
				VertexCount++;
				return vertex.Index;
			}

			internal int AddEdge(ref Edge edge)
			{
				edge.Index = GetNextEdgeIndex();
				if (edge.Index < _edges.Length) SetEdge(edge);
				else _edges.Add(edge);
				EdgeCount++;
				return edge.Index;
			}

			internal void AddLoop(ref Loop loop)
			{
				loop.Index = GetNextLoopIndex();
				if (loop.Index < _loops.Length) SetLoop(loop);
				else _loops.Add(loop);
				LoopCount++;
			}

			internal int AddFace(ref Face face)
			{
				face.Index = GetNextFaceIndex();
				if (face.Index < _faces.Length) SetFace(face);
				else _faces.Add(face);
				FaceCount++;
				return face.Index;
			}

			internal void InvalidateVertex(int index)
			{
				var vertex = GetVertex(index);
				vertex.Invalidate();
				_vertices[index] = vertex;
				VertexCount--;
			}

			internal void InvalidateEdge(int index)
			{
				var edge = GetEdge(index);
				edge.Invalidate();
				_edges[index] = edge;
				EdgeCount--;
			}

			internal void InvalidateLoop(int index)
			{
				var loop = GetLoop(index);
				loop.Invalidate();
				_loops[index] = loop;
				LoopCount--;
			}

			internal void InvalidateFace(int index)
			{
				var face = GetFace(index);
				face.Invalidate();
				_faces[index] = face;
				FaceCount--;
			}

			public bool Equals(GraphData other) => _elementCounts.Equals(other._elementCounts) &&
			                                       _vertices.Equals(other._vertices) && _edges.Equals(other._edges) &&
			                                       _loops.Equals(other._loops) && _faces.Equals(other._faces);

			public override bool Equals(object obj) => obj is GraphData other && Equals(other);
			public override int GetHashCode() => HashCode.Combine(_elementCounts, _vertices, _edges, _loops, _faces);
			public static bool operator ==(in GraphData left, in GraphData right) => left.Equals(right);
			public static bool operator !=(in GraphData left, in GraphData right) => !left.Equals(right);
			public object Clone() => new GraphData(this);
		}
	}
}