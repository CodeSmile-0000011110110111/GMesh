// Copyright (C) 2021-2022 Steffen Itterheim
// Usage is bound to the Unity Asset Store Terms of Service and EULA: https://unity3d.com/legal/as_terms

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
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
	public sealed partial class GMesh : IDisposable
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
		/// The inverse of the grid, ie upscale factor before rounding. See GridSize.
		/// </summary>
		public const float InvGridSize = 1f / GridSize; // inverse of grid size (eg 0.001 => 1000)
	}
}