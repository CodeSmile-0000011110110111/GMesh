// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private GraphData _data;

		/// <summary>
		/// The read-only collection of vertices.
		/// </summary>
		public NativeArray<Vertex>.ReadOnly Vertices => _data.Vertices;
		/// <summary>
		/// The read-only collection of edges.
		/// </summary>
		public NativeArray<Edge>.ReadOnly Edges => _data.Edges;
		/// <summary>
		/// The read-only collection of loops.
		/// </summary>
		public NativeArray<Loop>.ReadOnly Loops => _data.Loops;
		/// <summary>
		/// The read-only collection of faces.
		/// </summary>
		public NativeArray<Face>.ReadOnly Faces => _data.Faces;

		/// <summary>
		/// Number of valid vertices in the mesh.
		/// NOT to be mistaken for the length of the list!
		/// </summary>
		public int VertexCount => _data.VertexCount;
		/// <summary>
		/// Number of valid edges in the mesh.
		/// NOT to be mistaken for the length of the list!
		/// </summary>
		public int EdgeCount => _data.EdgeCount;
		/// <summary>
		/// Number of valid loops in the mesh.
		/// NOT to be mistaken for the length of the list!
		/// </summary>
		public int LoopCount => _data.LoopCount;
		/// <summary>
		/// Number of valid faces in the mesh.
		/// NOT to be mistaken for the length of the list!
		/// </summary>
		public int FaceCount => _data.FaceCount;

		/// <summary>
		/// Check if the GMesh needs disposing. For the poor developer who got confused. :)
		/// No seriously, it can be useful from time to time to just check whether you still have to or not.
		/// 
		/// Rule: after you are done using a GMesh instance you need to manually call Dispose() on it.
		/// In convoluted code this can easily be cumbersome so I decided to add this check.
		/// Note that indiscriminately calling Dispose() multiple times will throw an exception.
		/// </summary>
		/// <value></value>
		public bool IsDisposed => _data.IsDisposed;

		/// <summary>
		/// Disposes internal native collections and invalidates the graph.
		/// Calling Get/Set/Create/etc methods after Dispose() causes exceptions!
		/// Failure to call Dispose() in time will result in a big fat ugly Console error message to let you know about the mess you made. :)
		/// Calling Dispose() more than once will throw an InvalidOperationException.
		/// 
		/// Note: native collections cannot be disposed of automatically in the Finalizer, see:
		/// https://forum.unity.com/threads/why-disposing-nativearray-in-a-finalizer-is-unacceptable.531494/
		/// </summary>
		public void Dispose() => _data.Dispose();

		/// <summary>
		/// Gets a vertex by its index. Does not check whether element has been invalidated (Index == UnsetIndex).
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Vertex GetVertex(int index) => _data.GetVertex(index);

		/// <summary>
		/// Gets an edge by its index. Does not check whether element has been invalidated (Index == UnsetIndex).
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Edge GetEdge(int index) => _data.GetEdge(index);

		/// <summary>
		/// Gets a loop by its index. Does not check whether element has been invalidated (Index == UnsetIndex).
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Loop GetLoop(int index) => _data.GetLoop(index);

		/// <summary>
		/// Gets a face by its index. Does not check whether element has been invalidated (Index == UnsetIndex).
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Face GetFace(int index) => _data.GetFace(index);

		/// <summary>
		/// Sets (updates) a vertex in the list using its index.
		/// </summary>
		/// <param name="v"></param>
		public void SetVertex(in Vertex v) => _data.SetVertex(v);

		/// <summary>
		/// Sets (updates) an edge in the list using its index.
		/// </summary>
		/// <param name="e"></param>
		public void SetEdge(in Edge e) => _data.SetEdge(e);

		/// <summary>
		/// Sets (updates) a loop in the list using its index.
		/// </summary>
		/// <param name="l"></param>
		public void SetLoop(in Loop l) => _data.SetLoop(l);

		/// <summary>
		/// Sets (updates) a face in the list using its index.
		/// </summary>
		/// <param name="f"></param>
		public void SetFace(in Face f) => _data.SetFace(f);

		/// <summary>
		/// Adds a vertex to the graph and sets its index to match the index in the collection.
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns>the new index</returns>
		internal int AddVertex(ref Vertex vertex) => _data.AddVertex(ref vertex);

		/// <summary>
		/// Adds an edge to the graph and sets its index to match the index in the collection.
		/// </summary>
		/// <param name="edge"></param>
		/// <returns>the new index</returns>
		internal int AddEdge(ref Edge edge) => _data.AddEdge(ref edge);

		/// <summary>
		/// Adds a loop to the graph and sets its index to match the index in the collection.
		/// </summary>
		/// <param name="loop"></param>
		internal void AddLoop(ref Loop loop) => _data.AddLoop(ref loop);

		/// <summary>
		/// Adds a face to the graph and sets its index to match the index in the collection.
		/// </summary>
		/// <param name="face"></param>
		/// <returns>the new index</returns>
		internal int AddFace(ref Face face) => _data.AddFace(ref face);

		//private void RemoveVertex(int index) => _vertices.RemoveAt(index);
		//private void RemoveEdge(int index) => _edges.RemoveAt(index);
		//private void RemoveLoop(int index) => _loops.RemoveAt(index);
		//private void RemoveFace(int index) => _faces.RemoveAt(index);

		/// <summary>
		/// Invalidates the vertex by setting its Index to UnsetIndex.
		/// Its IsValid property will return false from here on.
		/// Invalidating does not remove the element, it is merely flagged for deletion.
		/// </summary>
		/// <param name="index"></param>
		internal void InvalidateVertex(int index) => _data.InvalidateVertex(index);

		/// <summary>
		/// Invalidates the edge by setting its Index to UnsetIndex.
		/// Its IsValid property will return false from here on.
		/// Invalidating does not remove the element, it is merely flagged for deletion.
		/// </summary>
		/// <param name="index"></param>
		internal void InvalidateEdge(int index) => _data.InvalidateEdge(index);

		/// <summary>
		/// Invalidates the loop by setting its Index to UnsetIndex.
		/// Its IsValid property will return false from here on.
		/// Invalidating does not remove the element, it is merely flagged for deletion.
		/// </summary>
		/// <param name="index"></param>
		internal void InvalidateLoop(int index) => _data.InvalidateLoop(index);

		/// <summary>
		/// Invalidates the face by setting its Index to UnsetIndex.
		/// Its IsValid property will return false from here on.
		/// Invalidating does not remove the element, it is merely flagged for deletion.
		/// </summary>
		/// <param name="index"></param>
		internal void InvalidateFace(int index) => _data.InvalidateFace(index);

		private void RemoveInvalidatedElements()
		{
			// TODO: we'll just leave the deleted elements as is for now
			// perhaps we'll simply re-use them when adding new elements?

			//RemoveInvalidatedVertices();
			//RemoveInvalidatedEdges();
			//RemoveInvalidatedLoops();
			//RemoveInvalidatedFaces();
		}

		[BurstCompile]
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

			private NativeArray<int> _elementCounts;
			private NativeList<Vertex> _vertices;
			private NativeList<Edge> _edges;
			private NativeList<Loop> _loops;
			private NativeList<Face> _faces;

			internal NativeArray<Vertex> VerticesArray => _vertices;

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
			public int VertexCount { get => _elementCounts[(int)Element.Vertex]; internal set => _elementCounts[(int)Element.Vertex] = value; }
			/// <summary>
			/// Number of edges in the mesh.
			/// </summary>
			public int EdgeCount { get => _elementCounts[(int)Element.Edge]; internal set => _elementCounts[(int)Element.Edge] = value; }
			/// <summary>
			/// Number of loops in the mesh.
			/// </summary>
			public int LoopCount { get => _elementCounts[(int)Element.Loop]; internal set => _elementCounts[(int)Element.Loop] = value; }
			/// <summary>
			/// Number of faces in the mesh.
			/// </summary>
			public int FaceCount { get => _elementCounts[(int)Element.Face]; internal set => _elementCounts[(int)Element.Face] = value; }

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
				_elementCounts = new NativeArray<int>((int)Element.Count, allocator.ToAllocator);
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

			public int NextVertexIndex => _elementCounts[(int)Element.Vertex];
			public int NextEdgeIndex => _elementCounts[(int)Element.Edge];
			public int NextLoopIndex => _elementCounts[(int)Element.Loop];
			public int NextFaceIndex => _elementCounts[(int)Element.Face];

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
				vertex.Index = NextVertexIndex;
				if (vertex.Index < _vertices.Length) SetVertex(vertex);
				else _vertices.Add(vertex);
				VertexCount++;
				return vertex.Index;
			}

			internal int AddEdge(ref Edge edge)
			{
				edge.Index = NextEdgeIndex;
				if (edge.Index < _edges.Length) SetEdge(edge);
				else _edges.Add(edge);
				EdgeCount++;
				return edge.Index;
			}

			internal void AddLoop(ref Loop loop)
			{
				loop.Index = NextLoopIndex;
				if (loop.Index < _loops.Length) SetLoop(loop);
				else _loops.Add(loop);
				LoopCount++;
			}

			internal int AddFace(ref Face face)
			{
				face.Index = NextFaceIndex;
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