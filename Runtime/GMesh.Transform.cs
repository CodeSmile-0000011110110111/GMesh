// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using Unity.Mathematics;
using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		internal static readonly float DefaultScale = 1f;

		private float3 _pivot = float3.zero;

		public float3 Pivot { get => _pivot; set => _pivot = value; }

		public void ApplyTransform(in Transform transform)
		{
			// FIXME: implement using Jobs since this is very Burst-friendly
			var rigidTransform = transform.ToRigidTransform();
			for (var i = 0; i < Vertices.Length; i++)
			{
				var vertex = GetVertex(i);
				var vPos = vertex.Position;
				// TODO: respect the pivot ...
				vertex.Position = math.transform(rigidTransform, vPos) * transform.Scale;
				SetVertex(vertex);
			}
		}

		[Serializable]
		public struct Transform
		{
			[Tooltip("Offset from origin (0,0,0)")]
			public float3 Translation;
			[Tooltip("Rotation is in degrees (Euler)")]
			public float3 Rotation;
			[Tooltip("Scale is self-explanatory")]
			public float3 Scale;

			public Transform(float3 translation, float3 rotation, float3 scale)
			{
				Translation = translation;
				Rotation = rotation;
				Scale = scale;
			}

			public RigidTransform ToRigidTransform() => new RigidTransform(quaternion.Euler(math.radians(Rotation)), Translation);
		}
	}
}