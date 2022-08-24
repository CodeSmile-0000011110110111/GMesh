// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		public object Clone() => new GMesh(this);

		public bool Equals(GMesh other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return _vertexCount == other._vertexCount && _edgeCount == other._edgeCount && _loopCount == other._loopCount &&
			       _faceCount == other._faceCount && _pivot.Equals(other._pivot) && _faces.Equals(other._faces) &&
			       _vertices.Equals(other._vertices) && _edges.Equals(other._edges) && _loops.Equals(other._loops);
		}

		public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is GMesh other && Equals(other);

		public override int GetHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(_vertices);
			hashCode.Add(_edges);
			hashCode.Add(_loops);
			hashCode.Add(_faces);
			hashCode.Add(_vertexCount);
			hashCode.Add(_edgeCount);
			hashCode.Add(_loopCount);
			hashCode.Add(_faceCount);
			hashCode.Add(_pivot);
			return hashCode.ToHashCode();
		}
	}
}