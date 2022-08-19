// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Mathematics;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		internal static readonly float3 DefaultScale = new(1f);

		private float3 _pivot = float3.zero;

		public float3 Pivot { get => _pivot; set => _pivot = value; }

		public void Transform(float3 translation, quaternion rotation) => Transform(translation, rotation, DefaultScale);

		public void Transform(float3 translation, quaternion rotation, float3 scale)
		{
			// FIXME: implement using Jobs since this is very Burst-friendly
			_pivot += translation;

			var transform = new RigidTransform(rotation, translation);
			for (var i = 0; i < Vertices.Length; i++)
			{
				var vertex = GetVertex(i);
				//var pos = math.rotate(rotation, vertex.Position + translation - Pivot) + Pivot;
				//vertex.Position = (pos - _pivot) * scale + _pivot;
				vertex.Position = math.transform(transform, vertex.Position) * scale;
				SetVertex(vertex);
			}
		}
	}
}