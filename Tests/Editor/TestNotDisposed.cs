// Copyright (C) 2021-2022 Steffen Itterheim
// Refer to included LICENSE file for terms and conditions.

using Unity.Collections;

namespace Tests.Editor
{
	public class TestNotDisposed
	{
		private NestedStruct _nested = new(Allocator.Persistent);
		public TestNotDisposed() {}
		public TestNotDisposed(TestNotDisposed other) => _nested = new NestedStruct(other._nested);
		public void Dispose() => _nested.Dispose();

		private struct NestedStruct
		{
			private NativeArray<int> _values;
			public NestedStruct(Allocator allocator) => _values = new NativeArray<int>(4, allocator);

			public NestedStruct(NestedStruct other)
				: this(Allocator.Persistent) => _values.CopyFrom(other._values);

			public void Dispose() => _values.Dispose();
		}
	}
}