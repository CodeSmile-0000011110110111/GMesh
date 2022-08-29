// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System.Collections.Generic;
using UnityEngine;

namespace CodeSmile.GraphMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Check if the GMesh needs disposing. For the poor developer who got confused. :)
		/// No seriously, it can be useful from time to time to just check whether you still have to or not.
		/// 
		/// Rule: after you are done using a GMesh instance you need to manually call Dispose() on it.
		/// In convoluted code this can easily be cumbersome so I decided to add this check.
		/// Note that indiscriminately calling Dispose() multiple times will throw an exception.
		/// </summary>
		/// <value></value>
		public bool IsDisposed => _data.IsDisposed;

		/// <summary>
		/// Calls Dispose() on all non-null meshes in the collection that have not been disposed yet.
		/// </summary>
		/// <param name="meshes"></param>
		private static void DisposeAll(IEnumerable<GMesh> meshes)
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
		/// Disposes internal native collections and invalidates the graph.
		/// Calling Get/Set/Create/etc methods after Dispose() causes exceptions!
		/// Failure to call Dispose() in time will result in a big fat ugly Console error message to let you know about the mess you made. :)
		/// Calling Dispose() more than once will throw an InvalidOperationException.
		/// 
		/// Note: native collections cannot be disposed of automatically in the Finalizer, see:
		/// https://forum.unity.com/threads/why-disposing-nativearray-in-a-finalizer-is-unacceptable.531494/
		/// </summary>
		public void Dispose() => _data.Dispose();

		/// <summary>
		/// Finalizer - checks if GMesh was properly disposed of.
		/// </summary>
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
				Debug.LogError($"GMesh not disposed: {this} - The " +
				               "'A Native Collection has not been disposed, resulting in a memory leak.' error messages " +
				               "above and/or below this message are likely because of this.");
				Debug.LogError("=====================================================================");
				Debug.LogError("=====================================================================");
			}
		}
	}
}