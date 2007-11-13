using System;
using System.Collections.Generic;
using System.Text;

namespace SteamEngine.Network {

	class Buffer : Poolable {
		public const int bufferLen = 10*1024;

		public readonly byte[] bytes;

		public Buffer() {
			bytes = new byte[bufferLen];
		}

		public Buffer(int len) {
			bytes = new byte[len];
		}
	}
}
