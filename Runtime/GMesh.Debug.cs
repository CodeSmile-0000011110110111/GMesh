// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		public override string ToString() => $"{GetType().Name} with {FaceCount} faces, {LoopCount} loops, {EdgeCount} edges, {VertexCount} vertices";

		/// <summary>
		/// Dump all elements for debugging purposes.
		/// </summary>
		public void DebugLogAllElements(string headerMessage = "")
		{
			if (string.IsNullOrWhiteSpace(headerMessage) == false)
				Debug.Log(headerMessage);
			
			Debug.Log(this);
			for (var i = 0; i < _faces.Length; i++)
				Debug.Log(GetFace(i));
			for (var i = 0; i < _loops.Length; i++)
				Debug.Log(GetLoop(i));
			for (var i = 0; i < _edges.Length; i++)
				Debug.Log(GetEdge(i));
			for (var i = 0; i < _vertices.Length; i++)
				Debug.Log(GetVertex(i));
		}
	}
}