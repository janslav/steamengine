using System;
using System.Collections.Generic;
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.Communication {

	public class Buffer : Poolable {
		public const int bufferLen = 50 * 1024;

		public readonly byte[] bytes;

		public Buffer() {
			bytes = new byte[bufferLen];
		}

		public Buffer(int len) {
			bytes = new byte[len];
		}

		protected override void On_DisposeManagedResources() {
			if (this.bytes.Length == bufferLen) {
				base.On_DisposeManagedResources();
			}
		}
	}

	public class ListBuffer<T> : Poolable {
		public readonly List<T> list = new List<T>();

		protected override void On_Reset() {
			list.Clear();

			base.On_Reset();
		}
	}
}
