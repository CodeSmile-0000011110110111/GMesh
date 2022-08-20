// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
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
		/// Calls Dispose() on all non-null meshes in the collection that have not been disposed yet.
		/// </summary>
		/// <param name="meshes"></param>
		public static void DisposeAll(IEnumerable<GMesh> meshes)
		{
			if (meshes != null)
			{
				foreach (var mesh in meshes)
				{
					if (mesh != null && mesh.IsDisposed == false)
						mesh.Dispose();
				}
			}
		}

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

		/// <summary>
		/// Gets a vertex by its index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Vertex GetVertex(int index) => _vertices[index];

		/// <summary>
		/// Gets an edge by its index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Edge GetEdge(int index) => _edges[index];

		/// <summary>
		/// Gets a loop by its index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Loop GetLoop(int index) => _loops[index];

		/// <summary>
		/// Gets a face by its index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public Face GetFace(int index) => _faces[index];

		/// <summary>
		/// Sets (updates) a vertex in the list using its index.
		/// </summary>
		/// <param name="v"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetVertex(in Vertex v) => _vertices[v.Index] = v;

		/// <summary>
		/// Sets (updates) an edge in the list using its index.
		/// </summary>
		/// <param name="e"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetEdge(in Edge e) => _edges[e.Index] = e;

		/// <summary>
		/// Sets (updates) a loop in the list using its index.
		/// </summary>
		/// <param name="l"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetLoop(in Loop l) => _loops[l.Index] = l;

		/// <summary>
		/// Sets (updates) a face in the list using its index.
		/// </summary>
		/// <param name="f"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetFace(in Face f) => _faces[f.Index] = f;

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