using System;
using System.Collections.Generic;
using System.Text;

namespace SteamEngine.Network {

	class Buffer : Poolable {
		public readonly byte[] bytes;

		public Buffer() {
			bytes = new byte[4096];
		}

		public Buffer(int len) {
			bytes = new byte[len];
		}
	}
}
