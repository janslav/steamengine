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
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using SteamEngine.Common;

namespace SteamEngine.Network {
	public enum PacketGroupType {
		SingleUse,
		MultiUse,
		Free
	}

	public class PacketGroup : Poolable {
		Buffer uncompressed;
		Buffer compressed;

		List<OutgoingPacket> packets = new List<OutgoingPacket>();

		private bool isWritten = false;
		private bool isCompressed = false;
		private int isQueued = 0;

		int uncompressedLen;
		int compressedLen;

		internal PacketGroupType type = PacketGroupType.MultiUse;

		public PacketGroup() {
		}

		public void SetType(PacketGroupType type) {
			this.type = type;
		}

		public void AddPacket(OutgoingPacket packet) {
			Sanity.IfTrueSay((isQueued > 0 || this.isCompressed || this.isWritten) , "Can't add new packets to a locked group. They're ignored.");
			packets.Add(packet);
		}

		internal protected override void Reset() {
			this.isWritten = false;
			this.isCompressed = false;
			this.isQueued = 0;
			this.type = PacketGroupType.MultiUse;

			base.Reset();
		}

		private void WritePackets() {
			if (!this.isWritten) {

				if (this.compressed == null) {
					this.uncompressed = Pool<Buffer>.Acquire();
					this.compressed = Pool<Buffer>.Acquire();
				}

				int position = 0;
				foreach (OutgoingPacket packet in packets) {
					position += packet.Write(this.compressed.bytes, position);
				}

				this.uncompressedLen = position;

				this.isWritten = true;
			}
		}

		internal int GetResult(ICompression compression, out byte[] bytes) {
			ThrowIfDisposed();

			WritePackets();

			if (!this.isCompressed) {
				if (compression != null) {
					this.compressedLen = compression.Compress(
						this.uncompressed.bytes, 0, this.uncompressedLen, this.compressed.bytes, 0);
				} else {
					this.uncompressed.Dispose();
					this.compressed = this.uncompressed;
					this.compressedLen = this.uncompressedLen;
				}
				this.isCompressed = true;
			}

			if (this.type == PacketGroupType.Free) {
				Buffer newCompressed = new Buffer(this.compressedLen);
				System.Buffer.BlockCopy(this.compressed.bytes, 0, newCompressed.bytes, 0, this.compressedLen);

				this.myPool = null;

				this.compressed.Dispose();
				this.compressed = null;
				this.uncompressed.Dispose();
				this.uncompressed = null;
				foreach (OutgoingPacket packet in this.packets) {
					packet.Dispose();
				}
				this.packets.Clear();

				this.compressed = newCompressed;

				this.type = PacketGroupType.MultiUse;
			}

			bytes = this.compressed.bytes;

			return compressedLen;
		}

		internal void Enqueued() {
			Interlocked.Increment(ref this.isQueued);
		}

		internal void Dequeued() {
			Interlocked.Decrement(ref this.isQueued);
			if (this.isQueued < 1) {
				if (this.type == PacketGroupType.SingleUse) {
					this.Dispose();
				}
			}
		}

		protected override void DisposeManagedResources() {
			if (this.uncompressed != null) {
				this.compressed.Dispose();
				this.uncompressed.Dispose();

				this.uncompressed = null;
			}

			this.compressed = null;

			foreach (OutgoingPacket packet in this.packets) {
				packet.Dispose();
			}
			this.packets.Clear();

			base.DisposeManagedResources();
		}
	}
}
