// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using System;

namespace CodeSmile.GMesh
{
	public sealed partial class GMesh
	{
		/// <summary>
		/// Enumerates over all loops in a face.
		/// </summary>
		/// <param name="faceIndex">The index of the face whose loops to enumerate over.</param>
		/// <param name="callback">Action to call for each loop. If you modify loop you need to call SetLoop(loop) to store it!</param>
		public void ForEachLoop(int faceIndex, Action<Loop> callback) => ForEachLoop(GetFace(faceIndex), callback);

		/// <summary>
		/// Enumerates over all loops in a face.
		/// </summary>
		/// <param name="face">the face whose loops to enumerate over</param>
		/// <param name="callback">Action to call for each loop. If you modify loop you need to call SetLoop(loop) to store it!</param>
		public void ForEachLoop(in Face face, Action<Loop> callback)
		{
			// assumption: if a face is valid, all its loops are supposed to be valid too! (at least when we start)
			if (face.IsValid)
			{
				var firstLoopIndex = face.FirstLoopIndex;
				var loop = GetLoop(firstLoopIndex);
				do
				{
					callback.Invoke(loop);
					loop = GetLoop(loop.NextLoopIndex);
				} while (loop.IsValid && loop.Index != firstLoopIndex);
			}
		}
	}
}