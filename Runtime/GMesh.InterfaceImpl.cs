// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GraphMesh
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

			return _pivot.Equals(other._pivot) && _data == other._data;
		}

		public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is GMesh other && Equals(other);

		public override int GetHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(_pivot);
			hashCode.Add(_data);
			return hashCode.ToHashCode();
		}
	}
}