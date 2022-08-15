// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;
using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		public static GMesh FromMesh(Mesh mesh)
		{
			if (mesh == null)
				throw new ArgumentNullException(nameof(mesh));

			var gmesh = new GMesh();
			// TODO
			
			return null;
		}
		
		public Mesh ToMesh(Mesh mesh = null)
		{
			if (mesh == null)
				mesh = new Mesh();
			else
				mesh.Clear();

			// TODO
			
			throw new NotImplementedException();
		}
	}
}