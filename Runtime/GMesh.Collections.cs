// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		private NativeList<Vertex> _vertices = new(Allocator.Persistent);
		private NativeList<Edge> _edges = new(Allocator.Persistent);
		private NativeList<Loop> _loops = new(Allocator.Persistent);
		private NativeList<Face> _faces = new(Allocator.Persistent);

		private int _vertexCount;
		private int _edgeCount;
		private int _loopCount;
		private int _faceCount;

		/// <summary>
		/// Number of vertices in the mesh.
		/// </summary>
		public int VertexCount => _vertexCount;
		/// <summary>
		/// Number of edges in the mesh.
		/// </summary>
		public int EdgeCount => _edgeCount;
		/// <summary>
		/// Number of loops in the mesh.
		/// </summary>
		public int LoopCount => _loopCount;
		/// <summary>
		/// Number of faces in the mesh.
		/// </summary>
		public int FaceCount => _faceCount;

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
		/// Check if the GMesh needs disposing. For developers who get easily confused. :)
		/// 
		/// Rule: after you are done using a GMesh instance you need to manually call Dispose() on it.
		/// In convoluted code this can easily be cumbersome so I decided to add this check.
		/// Note that indiscriminately calling Dispose() multiple times will throw an exception.
		/// </summary>
		/// <value></value>
		public bool IsDisposed => !(_vertices.IsCreated && _edges.IsCreated && _loops.IsCreated && _faces.IsCreated);

		/// <summary>
		/// Dispose of internal native collections.
		/// Failure to call Dispose() in time will result in a big fat ugly Console error message.
		/// Calling Dispose() more than once will throw an InvalidOperationException.
		/// 
		/// Note: native collections cannot be disposed of automatically in the Finalizer, see:
		/// https://forum.unity.com/threads/why-disposing-nativearray-in-a-finalizer-is-unacceptable.531494/
		/// </summary>
		public void Dispose()
		{
			if (IsDisposed)
				throw new InvalidOperationException("GMesh has already been disposed. Do not call Dispose() again!");

			_vertices.Dispose();
			_edges.Dispose();
			_loops.Dispose();
			_faces.Dispose();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int AddVertex(ref Vertex vertex)
		{
			Debug.Assert(vertex.Index == UnsetIndex, "Index must not be set before Add(element)");
			vertex.Index = _vertices.Length;
			_vertices.Add(vertex);
			_vertexCount++;
			return vertex.Index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int AddEdge(ref Edge edge)
		{
			Debug.Assert(edge.Index == UnsetIndex, "Index must not be set before Add(element)");
			edge.Index = _edges.Length;
			_edges.Add(edge);
			_edgeCount++;
			return edge.Index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int AddLoop(ref Loop loop)
		{
			Debug.Assert(loop.Index == UnsetIndex, "Index must not be set before Add(element)");
			loop.Index = _loops.Length;
			_loops.Add(loop);
			_loopCount++;
			return loop.Index;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int AddFace(ref Face face)
		{
			Debug.Assert(face.Index == UnsetIndex, "Index must not be set before Add(element)");
			face.Index = _faces.Length;
			_faces.Add(face);
			_faceCount++;
			return face.Index;
		}

		//private void RemoveVertex(int index) => _vertices.RemoveAt(index);
		//private void RemoveEdge(int index) => _edges.RemoveAt(index);
		//private void RemoveLoop(int index) => _loops.RemoveAt(index);
		//private void RemoveFace(int index) => _faces.RemoveAt(index);

		private void InvalidateVertex(int index)
		{
			var vertex = GetVertex(index);
			Debug.Assert(vertex.Index > UnsetIndex, $"already invalidated {index}: {vertex}");
			Debug.Assert(_vertexCount > 0);
			vertex.Invalidate();
			_vertices[index] = vertex;
			_vertexCount--;
		}

		private void InvalidateEdge(int index)
		{
			var edge = GetEdge(index);
			Debug.Assert(edge.Index > UnsetIndex, $"already invalidated {index}: {edge}");
			Debug.Assert(_edgeCount > 0);
			edge.Invalidate();
			_edges[index] = edge;
			_edgeCount--;
		}

		private void InvalidateLoop(int index)
		{
			var loop = GetLoop(index);
			Debug.Assert(loop.Index > UnsetIndex, $"already invalidated {index}: {loop}");
			Debug.Assert(_loopCount > 0);
			loop.Invalidate();
			_loops[index] = loop;
			_loopCount--;
			Debug.Assert(_loopCount >= 0);
		}

		private void InvalidateFace(int index)
		{
			var face = GetFace(index);
			Debug.Assert(face.Index > UnsetIndex, $"already invalidated {index}: {face}");
			Debug.Assert(_faceCount > 0);
			face.Invalidate();
			_faces[index] = face;
			_faceCount--;
		}

		/*
		private void RemoveInvalidatedVertices()
		{
			// Vertex:
			// referencing edge
			// referenced by edge, loop
		}
		private void RemoveInvalidatedEdges()
		{
			// Edge:
			// referencing vertex, edge, loop
			// referenced by vertex, edge, loop
		}
		private void RemoveInvalidatedLoops()
		{
			// Loop:
			// referencing face, edge, loop, vertex
			// referenced by face, edge, loop
			
		}
		private void RemoveInvalidatedFaces()
		{
			// Faces:
			// referencing loops
			// referenced by loops
			
			var removeCount = 0;
			var faceCount = _faces.Length;
			for (int i = 0; i < (faceCount - removeCount); i++)
			{
				var face = _faces[i];
				if (face.Index == UnsetIndex)
				{
					removeCount++;
					_faces.RemoveAt(i);
					// no further updates, assuming the face has deleted its loops
				}
			}
		}
		*/

		private void RemoveInvalidatedElements()
		{
			// TODO: we'll just leave the deleted elements as is for now
			// perhaps we'll simply re-use them when adding new elements?

			// in order of causing least amount of overhead
			//RemoveInvalidatedVertices();
			//RemoveInvalidatedEdges();
			//RemoveInvalidatedLoops();
			//RemoveInvalidatedFaces();
		}

		~GMesh() => OnFinalizeVerifyCollectionsAreDisposed();

		/// <summary>
		/// This is the big fat ugly error message producer if user failed to call Dispose().
		/// </summary>
		private void OnFinalizeVerifyCollectionsAreDisposed()
		{
			if (IsDisposed == false)
			{
				// Make sure this doesn't go unnoticed! (I'd rather not throw an exception in the Finalizer)
				Debug.LogError("=====================================================================");
				Debug.LogError("=====================================================================");
				Debug.LogError($"GMesh: you forgot to call Dispose() on {this}! See the " +
				               "'A Native Collection has not been disposed, resulting in a memory leak.' error messages " +
				               "above and/or below this message? That's because of not calling Dispose() on this GMesh instance.");
				Debug.LogError("=====================================================================");
				Debug.LogError("=====================================================================");
			}
		}
	}
}