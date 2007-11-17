/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using SteamEngine.Common;

namespace SteamEngine.Network {
	public abstract class SteamSocket : Poolable {
		internal Socket socket;
		internal Buffer receivingBuffer;
		internal int startOfData;
		internal int endOfData;

		private readonly object lockObject = new object();

		public SteamSocket() {
			this.Reset();
		}

		protected internal override void Reset() {
			receivingBuffer = Pool<Buffer>.Acquire();
			startOfData = 0;
			endOfData = 0;

			base.Reset();
		}

		public void Close(string reason) {
			lock (lockObject) {
				if (!this.IsDisposed) {
					On_Close(reason);
					base.Dispose();
				}
			}
		}

		public void Close(LogStr reason) {
			lock (lockObject) {
				if (!this.IsDisposed) {
					On_Close(reason);
					base.Dispose();
				}
			}
		}

		public virtual void On_Close(string reason) {
		}

		public virtual void On_Close(LogStr reason) {
		}

		public override void Dispose() {
			this.Close("Dispose() called.");
		}

		protected override void DisposeUnmanagedResources() {
			try {
				socket.Close();
			} catch {
			}
			socket = null;

			base.DisposeUnmanagedResources();
		}

		protected override void DisposeManagedResources() {
			receivingBuffer.Dispose();
			base.DisposeManagedResources();
		}

		public virtual IEncryption Encryption {
			get {
				return null;
			}
		}

		public EndPoint EndPoint {
			get {
				return socket.RemoteEndPoint;
			}
		}

		public abstract void Handle(IncomingPacket packet);


	}


	public interface IEncryption {
		// Encrypt outgoing data
		int Encrypt(byte[] bytesIn, int offsetIn, int lengthIn, byte[] bytesOut, int offsetOut);

		// Decrypt incoming data
		int Decrypt(byte[] bytesIn, int offsetIn, int lengthIn, byte[] bytesOut, int offsetOut);
	}

	public interface ICompression {
		int Compress(byte[] bytesIn, int offsetIn, int lengthIn, byte[] bytesOut, int offsetOut);
		int Decompress(byte[] bytesIn, int offsetIn, int lengthIn, byte[] bytesOut, int offsetOut);
	}
}