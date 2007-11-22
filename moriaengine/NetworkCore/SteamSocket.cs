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
		internal Buffer decryptBuffer;
		internal Buffer decompressBuffer;
		internal int decompressedDataOffset;
		internal int decompressedDataLength;

		internal int receivedDataLength;

		internal bool encryptionInitialised;
		internal bool useEncryption;

		private readonly object lockObject = new object();

		public SteamSocket() {
			this.Reset();
		}

		protected internal override void Reset() {
			this.receivingBuffer = Pool<Buffer>.Acquire();
			this.decryptBuffer = Pool<Buffer>.Acquire();
			this.decompressBuffer = Pool<Buffer>.Acquire();
			this.decompressedDataOffset = 0;
			this.decompressedDataLength = 0;

			this.encryptionInitialised = false;
			this.useEncryption = true;

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

		public virtual void On_Connect() {

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
			this.receivingBuffer.Dispose();
			this.decryptBuffer.Dispose();
			this.decompressBuffer.Dispose();
			base.DisposeManagedResources();
		}	

		public virtual IEncryption Encryption {
			get {
				return null;
			}
		}

		public virtual ICompression Compression {
			get {
				return null;
			}
		}

		public IPEndPoint EndPoint {
			get {
				return (IPEndPoint) socket.RemoteEndPoint;
			}
		}
	}

	public enum EncryptionInitResult {
		SuccessUseEncryption, 
		SuccessNoEncryption,
		NotEnoughData,
		InvalidData //
	}

	public interface IEncryption {
		// 
		EncryptionInitResult Init(byte[] bytesIn, int offsetIn, int lengthIn, out int bytesUsed);

		// Encrypt outgoing data
		int Encrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length);

		// Decrypt incoming data
		int Decrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length);
	}

	public interface ICompression {
		int Compress(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length);
		int Decompress(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length);
	}
}