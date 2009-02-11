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

namespace SteamEngine.Communication {
	public enum PacketGroupType {
		SingleUse,
		MultiUse,
		Free
	}

	public sealed class PacketGroup : Poolable {
		Buffer uncompressed;
		Buffer compressed;

		List<OutgoingPacket> packets = new List<OutgoingPacket>();

		private bool isWritten;
		private bool compressionDone;
		private int isQueued;
		private bool isEmpty;

		private bool isMadeFree;

		int uncompressedLen;
		int compressedLen;

		internal PacketGroupType type;

		public void SetType(PacketGroupType type) {
			if (this.isMadeFree) {
				throw new SEException("Can't change type once the group is made free");
			}
			this.type = type;
		}

		public void AddPacket(OutgoingPacket packet) {
			Sanity.IfTrueSay((isQueued > 0 || this.compressionDone || this.isWritten), "Can't add new packets to a locked group. They're ignored.");
			packets.Add(packet);
			this.isEmpty = false;
		}

		public T AcquirePacket<T>() where T : OutgoingPacket, new() {
			T packet = Pool<T>.Acquire();
			this.AddPacket(packet);
			return packet;
		}

		protected override void On_Reset() {
			this.isWritten = false;
			this.compressionDone = false;
			this.isQueued = 0;
			this.type = PacketGroupType.SingleUse;
			this.isMadeFree = false;
			this.isEmpty = true;

			this.uncompressed = Pool<Buffer>.Acquire();
			this.compressed = Pool<Buffer>.Acquire();

			base.On_Reset();
		}

		private void WritePackets() {
			if (!this.isWritten) {
				int position = 0;
				foreach (OutgoingPacket packet in packets) {
					position += packet.Write(this.uncompressed.bytes, position);
				}

				this.uncompressedLen = position;

				this.isWritten = true;
			}
		}

		internal int GetFinalBytes(ICompression compression, out byte[] bytes) {
			this.ThrowIfDisposed();

			this.WritePackets();

			if (!this.compressionDone) {
				if (compression != null) {
					this.compressedLen = compression.Compress(
						this.uncompressed.bytes, 0, this.compressed.bytes, 0, this.uncompressedLen);
				} else {
					this.compressed.Dispose();
					this.compressed = this.uncompressed;
					this.compressedLen = this.uncompressedLen;

					this.uncompressed = null;
				}
				this.compressionDone = true;
			}

			if (this.type == PacketGroupType.Free) {
				Buffer newCompressed = new Buffer(this.compressedLen);
				System.Buffer.BlockCopy(this.compressed.bytes, 0, newCompressed.bytes, 0, this.compressedLen);

				this.MyPool = null;

				this.compressed.Dispose();
				this.compressed = null;
				if (this.uncompressed != null) {
					this.uncompressed.Dispose();
					this.uncompressed = null;
				}
				foreach (OutgoingPacket packet in this.packets) {
					packet.Dispose();
				}
				this.packets.Clear();

				this.compressed = newCompressed;

				this.isMadeFree = true;

				this.type = PacketGroupType.MultiUse;
			}

			bytes = this.compressed.bytes;

			//#if DEBUG 
			//            foreach (OutgoingPacket packet in packets) {
			//                Logger.WriteDebug("Sending "+packet.FullName);
			//            }
			//#endif

			return compressedLen;
		}

		public override void Dispose() {
			if (this.isQueued > 0) {
				this.SetType(PacketGroupType.SingleUse);
			} else {
				base.Dispose();
			}
		}

		internal void Enqueued() {
			Interlocked.Increment(ref this.isQueued);
		}

		internal void Dequeued() {
			Interlocked.Decrement(ref this.isQueued);
			if (this.isQueued < 1) {
				if (this.type == PacketGroupType.SingleUse) {
					base.Dispose();
				}
			}
		}

		protected override void On_DisposeManagedResources() {
			if (this.compressed != null) {
				this.compressed.Dispose();
				this.compressed = null;
			}
			if (this.uncompressed != null) {
				this.uncompressed.Dispose();
				this.uncompressed = null;
			}

			foreach (OutgoingPacket packet in this.packets) {
				packet.Dispose();
			}
			this.packets.Clear();

			base.On_DisposeManagedResources();
		}

		public static PacketGroup CreateFreePG() {
			PacketGroup pg = new PacketGroup();
			pg.SetType(PacketGroupType.Free);
			return pg;
		}

		public static PacketGroup AcquireMultiUsePG() {
			PacketGroup pg = Pool<PacketGroup>.Acquire();
			pg.SetType(PacketGroupType.MultiUse);
			return pg;
		}

		public static PacketGroup AcquireSingleUsePG() {
			PacketGroup pg = Pool<PacketGroup>.Acquire();
			pg.SetType(PacketGroupType.SingleUse);
			return pg;
		}

		public bool IsEmpty {
			get {
				return this.isEmpty;
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder("PacketGroup (");
			sb.Append(this.packets.Count).Append(" packets - ");
			foreach (OutgoingPacket p in this.packets) {
				sb.Append(p.ToString()).Append(", ");
			}
			sb.Length -= 2;
			sb.Append(")");
			return sb.ToString();
		}
	}
}
