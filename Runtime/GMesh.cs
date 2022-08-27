// Copyright (C) 2021-2022 Steffen Itterheim
// Usage is bound to the Unity Asset Store Terms of Service and EULA: https://unity3d.com/legal/as_terms

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace CodeSmile.GraphMesh
{
	/// <summary>
	/// GMesh - editable mesh geometry, Burst & Job System enabled.
	/// GMesh is a graph built of Vertices, Edges, Faces and Loops. This allows for editing the mesh using simple Euler operators.
	/// GMesh has ToMesh and FromMesh methods to convert to and from Unity Mesh instances.
	/// 
	/// There's one thing you need to know about the Jobs compatibility: element references (ie edge => vertex 0 and 1 or face => loops)
	/// do not exist! Instead, they are merely indices to the elements in their respective lists.
	///
	/// All elements (vertex, edge, loop, face) are structs and thus stored and passed by value (unless ref or in keywords are used).
	/// Therefore, if you need to loop up the face of a loop, you call GetFace(loop.FaceIndex) in order to get a COPY of the Face struct
	/// stored at that index. After making modifications to the face, you'll have to call SetFace(face) which uses face.Index internally
	/// to write the modified face back to the mesh graph (specifically: assigning the face back to its position in the faces list).
	///
	/// You are strongly advised to NOT keep local copies of indexes while you or anything else is possibly modifying the mesh graph
	/// (ie inserting, deleting, moving, swapping, replacing elements) as this can invalidate the indices.
	///
	/// Vertices are shared between faces, loops and edges. Whether final Mesh faces should share vertices is a setting in ToMesh().
	/// 
	/// You should also rely exclusively on the Euler operators (and combinations of them) in order to modify the mesh graph.
	/// See: https://en.wikipedia.org/wiki/Euler_operator_(digital_geometry)
	/// The same cautiuous warning exists in the Blender developer documentation, for good reason.
	/// For similar reason the element lists are not publicly exposed, use the Set/Get and Create/Delete element methods instead.
	/// 
	/// Note: Implementation closely follows Blender's BMesh and its C# port UnityBMesh (which is not Job System compatible).
	/// </summary>
	[BurstCompile(OptimizeFor = OptimizeFor.Performance, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
	public sealed partial class GMesh : IDisposable, ICloneable, IEquatable<GMesh>
	{
		/// <summary>
		/// This is used to indicate that the index referencing another element hasn't been set yet.
		/// Used internally to detect graph relation errors.
		/// </summary>
		public const int UnsetIndex = -1;

		/// <summary>
		/// GMesh may work with vertex positions on a reasonably sized grid (default: 1mm) in order to easily detect vertices
		/// which are close enough to be considered identical.
		/// For instance, rounding a position would be done as follows:
		/// var positionOnGrid = math.round(position * InvGridSize) * GridSize;
		/// </summary>
		public const float GridSize = 0.001f; // round all positions to 1mm grid

		/// <summary>
		/// The inverse of the grid, ie upscale factor. See GridSize.
		/// </summary>
		private static readonly double GridUpscale = 1.0 / GridSize; // inverse of grid size (eg 0.001 => 1000)

		/// <summary>
		/// Compares to GMesh instances for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(GMesh left, GMesh right) => Equals(left, right);

		/// <summary>
		/// Compares to GMesh instances for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(GMesh left, GMesh right) => !Equals(left, right);

		/// <summary>
		/// Creates an empty GMesh.
		/// </summary>
		public GMesh() => _data = new GraphData(Allocator.Persistent);

		/// <summary>
		/// Copy constructor. Creates an instance of GMesh that is the exact copy of another mesh.
		/// </summary>
		/// <param name="other"></param>
		public GMesh(GMesh other)
		{
			_pivot = other._pivot;
			_data = new GraphData(other._data);
		}

		/// <summary>
		/// Creates a GMesh with a single face using the supplied triangles. Same as calling CreateFace(vertexPositions) on an empty GMesh.
		/// </summary>
		/// <param name="vertexPositions"></param>
		public GMesh(in NativeArray<float3> vertexPositions, bool disposeInputArray = false)
			: this()
		{
			CreateFace(vertexPositions);
			if (disposeInputArray)
				vertexPositions.Dispose();
		}

		/// <summary>
		/// Clones the instance, returns a new GMesh instance that is an exact copy.
		/// </summary>
		/// <param name="other"></param>
		public object Clone() => new GMesh(this);

		public bool Equals(GMesh other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return _pivot.Equals(other._pivot) && _data.Equals(other._data);
		}

		public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is GMesh other && Equals(other);

		public override int GetHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(_pivot);
			hashCode.Add(_data);
			return hashCode.ToHashCode();
		}

		~GMesh() => OnFinalizeVerifyCollectionsAreDisposed();
	}
}