// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Collections;
using UnityEngine;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		private GraphData _data = new(Allocator.Persistent);

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
		/// Number of vertices in the mesh.
		/// </summary>
		public int VertexCount => _data.VertexCount;
		/// <summary>
		/// Number of edges in the mesh.
		/// </summary>
		public int EdgeCount => _data.EdgeCount;
		/// <summary>
		/// Number of loops in the mesh.
		/// </summary>
		public int LoopCount => _data.LoopCount;
		/// <summary>
		/// Number of faces in the mesh.
		/// </summary>
		public int FaceCount => _data.FaceCount;

		/// <summary>
		/// Check if the GMesh needs disposing. For developers who get easily confused. :)
		/// 
		/// Rule: after you are done using a GMesh instance you need to manually call Dispose() on it.
		/// In convoluted code this can easily be cumbersome so I decided to add this check.
		/// Note that indiscriminately calling Dispose() multiple times will throw an exception.
		/// </summary>
		/// <value></value>
		public bool IsDisposed => _data.IsDisposed;

		/// <summary>
		/// Dispose of internal native collections.
		/// Failure to call Dispose() in time will result in a big fat ugly Console error message.
		/// Calling Dispose() more than once will throw an InvalidOperationException.
		/// 
		/// Note: native collections cannot be disposed of automatically in the Finalizer, see:
		/// https://forum.unity.com/threads/why-disposing-nativearray-in-a-finalizer-is-unacceptable.531494/
		/// </summary>
		public void Dispose() => _data.Dispose();

		/// <summary>
		/// Gets a vertex by its index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Vertex GetVertex(int index) => _data.GetVertex(index);

		/// <summary>
		/// Gets an edge by its index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Edge GetEdge(int index) => _data.GetEdge(index);

		/// <summary>
		/// Gets a loop by its index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public Loop GetLoop(int index) => _data.GetLoop(index);

		/// <summary>
		/// Gets a face by its index.
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

		internal int AddVertex(ref Vertex vertex) => _data.AddVertex(ref vertex);
		internal int AddEdge(ref Edge edge) => _data.AddEdge(ref edge);
		internal void AddLoop(ref Loop loop) => _data.AddLoop(ref loop);
		internal int AddFace(ref Face face) => _data.AddFace(ref face);

		//private void RemoveVertex(int index) => _vertices.RemoveAt(index);
		//private void RemoveEdge(int index) => _edges.RemoveAt(index);
		//private void RemoveLoop(int index) => _loops.RemoveAt(index);
		//private void RemoveFace(int index) => _faces.RemoveAt(index);

		internal void InvalidateVertex(int index) => _data.InvalidateVertex(index);

		internal void InvalidateEdge(int index) => _data.InvalidateEdge(index);

		internal void InvalidateLoop(int index) => _data.InvalidateLoop(index);

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