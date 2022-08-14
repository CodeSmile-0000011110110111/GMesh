// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using UnityEngine;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		~GMesh() => OnFinalizeVerifyCollectionsAreDisposed();
		public bool IsDisposed() => !(_vertices.IsCreated && _edges.IsCreated && _loops.IsCreated && _faces.IsCreated);

		/// <summary>
		/// Dispose of internal native collections.
		/// Failure to call Dispose() in time will result in a big fat ugly Console error message.
		/// </summary>
		public void Dispose()
		{
			if (IsDisposed())
				return;

			_vertices.Dispose();
			_edges.Dispose();
			_loops.Dispose();
			_faces.Dispose();
		}

		/// <summary>
		/// This is the big fat ugly error message producer if user failed to call Dispose().
		/// </summary>
		private void OnFinalizeVerifyCollectionsAreDisposed()
		{
			// If Dispose() wasn't called before the destructor (Finalizer) user forgot to call Dispose() - let him/her know!
			// Note: native collections cannot be disposed of in the Finalizer, see:
			// https://forum.unity.com/threads/why-disposing-nativearray-in-a-finalizer-is-unacceptable.531494/
			if (IsDisposed() == false)
			{
				// Make sure this doesn't go unnoticed! (I'd rather not throw an exception in the Finalizer)
				Debug.LogError("=====================================================================");
				Debug.LogError("=====================================================================");
				Debug.LogError("GMesh: you forgot to call Dispose() on me before throwing me in the garbage! See the " +
				               "'A Native Collection has not been disposed, resulting in a memory leak.' error messages " +
				               "above and/or below this message? That's because of not calling Dispose() on this GMesh instance.");
				Debug.LogError("=====================================================================");
				Debug.LogError("=====================================================================");
			}
		}
	}
}