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
using System.Threading;

using SteamEngine.Common;

namespace SteamEngine.Communication {
	public abstract class AbstractConnection<TConnection, TState, TEndPoint> : Poolable
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TConnection, TState, TEndPoint>, new() {

		private AsyncCore<TConnection, TState, TEndPoint> core;

		protected Buffer receivingBuffer;
		private Buffer decryptBuffer;
		private Buffer decompressBuffer;

		protected int receivedDataLength;

		private int decompressedDataOffset;
		private int decompressedDataLength;
		private bool encryptionInitialised;
		private bool useEncryption;

		private TState state;

		public AbstractConnection() {
			if (this.GetType() != typeof(TConnection)) {
				throw new Exception("The type must be the same as the TConnection generic parameter");
			}
		}

		protected override void On_Reset() {
			this.receivingBuffer = Pool<Buffer>.Acquire();
			this.decryptBuffer = Pool<Buffer>.Acquire();
			this.decompressBuffer = Pool<Buffer>.Acquire();
			this.decompressedDataOffset = 0;
			this.decompressedDataLength = 0;

			this.encryptionInitialised = false;
			this.useEncryption = true;

			base.On_Reset();
		}

		public TState State {
			get {
				return this.state;
			}
		}

		public override sealed void Dispose() {
			this.Close("Dispose() called");
		}

		protected override void On_DisposeManagedResources() {
			this.receivingBuffer.Dispose();
			this.receivingBuffer = null;
			this.decryptBuffer.Dispose();
			this.decryptBuffer = null;
			this.decompressBuffer.Dispose();
			this.decompressBuffer = null;

			this.core = null;

			base.On_DisposeManagedResources();
		}

		public void Close(string reason) {

			if (!this.disposed) {
				lock (this.core.LockObject) {
					this.state.On_Close(reason);
				}
			}
			//On_Close(reason);
			base.Dispose();
		}

		//public virtual void On_Close(string reason) {

		//}

		public abstract bool IsConnected { get; }

		public abstract TEndPoint EndPoint { get; }


		internal void Init(AsyncCore<TConnection, TState, TEndPoint> core) {
			this.core = core;
			this.On_Init();

			this.state = new TState();
			this.state.On_Init((TConnection) this);
		}

		protected virtual void On_Init() {

		}

		#region Receiving data
		protected void ProcessReceievedData(int length) {
			//ss.receievedDataLength += newBytesLength;
			byte[] bytes = this.receivingBuffer.bytes;
			length += this.receivedDataLength;
			int offset = 0;

			if (this.ProcessDecryption(ref length, ref bytes, ref offset)) {
				ICompression compression = this.State.Compression;
				if (compression != null) {
					length = compression.Decompress(bytes, offset, this.decompressBuffer.bytes,
						this.decompressedDataOffset + this.decompressedDataLength, length);
				} else {
					System.Buffer.BlockCopy(bytes, offset, this.decompressBuffer.bytes,
						this.decompressedDataOffset + this.decompressedDataLength, length);
				}

				bytes = this.decompressBuffer.bytes;
				offset = this.decompressedDataOffset;
				length += this.decompressedDataLength;


				//read all posible packets outa those data
				bool loop = true;
				while (loop && (length > 0)) {
					byte id = bytes[offset];
					IncomingPacket<TConnection, TState, TEndPoint> packet = this.core.protocol.GetPacketImplementation(id);
					length--;
					offset++;

					int read;
					ReadPacketResult result;
					if (packet != null) {
						Logger.WriteDebug("Handling packet 0x"+id.ToString("x"));
						result = packet.Read(bytes, offset, length, out read);
					} else {
						result = ReadPacketResult.DiscardAll;
						read = 0;
						Logger.WriteDebug("Unknown packet 0x"+id.ToString("x"));
						//TODO: write out packet
					}

					switch (result) {
						case ReadPacketResult.NeedMoreData:
							loop = false;
							break;
						case ReadPacketResult.Success:
							this.core.HandlePacket((TConnection) this, this.state, packet);
							goto default;
						case ReadPacketResult.DiscardAll:
							read = length;
							goto default;
						default:
							Sanity.IfTrueThrow(read < 0, "read bytes can't be negative!");
							if (read > 0) {
								offset += read;
								length -= read;
							}
							break;
					}
				}

				this.decompressedDataOffset = offset;
				this.decompressedDataLength = length;

				RecycleBufferIfNeeded(bytes, ref this.decompressedDataOffset, ref this.decompressedDataLength);
			} //else init failed with not enough data, we queue the data
		}

		private static void RecycleBufferIfNeeded(byte[] bytes, ref int offset, ref int length) {
			if (length > 0) {
				//if over half the buffer, we copy to the start
				if (offset > (Buffer.bufferLen / 2)) {
					System.Buffer.BlockCopy(bytes, offset, bytes, 0, length);
					offset = 0;
				} else if ((offset + length) == Buffer.bufferLen) {
					throw new Exception("Incoming data buffer full. This is bad.");
				}
			} else {//we read exactly what we had, perfect.
				offset = 0;
				length = 0;
			}
		}

		private bool ProcessDecryption(ref int length, ref byte[] bytes, ref int offset) {
			if (this.useEncryption) {
				IEncryption encryption = this.State.Encryption;
				if (encryption != null) {
					if (!this.encryptionInitialised) {
						int read;
						EncryptionInitResult result = encryption.Init(bytes, offset, length, out read);
						switch (result) {
							case EncryptionInitResult.SuccessUseEncryption:
								Console.WriteLine(this.State+": using encryption "+encryption);
								offset += read;
								length -= read;
								this.receivedDataLength = 0;
								this.encryptionInitialised = true;
								break;
							case EncryptionInitResult.SuccessNoEncryption:
								Console.WriteLine(this.State+": using no encryption");
								offset += read;
								length -= read;
								this.useEncryption = false;
								break;
							case EncryptionInitResult.InvalidData:
								throw new Exception("Encryption not recognised");
							case EncryptionInitResult.NotEnoughData:
								this.receivedDataLength = length;
								return false;
						}
					}

					if (this.useEncryption) {
						length = encryption.Decrypt(bytes, offset, this.decryptBuffer.bytes, 0, length);
						bytes = this.decryptBuffer.bytes;
						offset = 0;
					}
				}
			}
			return true;
		}
		#endregion Receiving data

		#region Sending data
		//called from main loop
		public void SendPacketGroup(PacketGroup group) {
			ThrowIfDisposed();

			this.core.EnqueueOutgoing((TConnection) this, group);
		}

		public void SendSinglePacket(OutgoingPacket packet) {
			PacketGroup pg = Pool<PacketGroup>.Acquire();
			pg.SetType(PacketGroupType.SingleUse);
			pg.AddPacket(packet);
			this.SendPacketGroup(pg);
		}

		protected class BufferToSend {
			public Buffer buffer;
			public int offset, len;

			internal BufferToSend(Buffer buffer, int offset, int len) {
				this.buffer = buffer;
				this.offset = offset;
				this.len = len;
			}
		}

		//caled in worker thread
		internal void ProcessSending(PacketGroup group) {
			try {
				byte[] unencrypted;
				int unencryptedLen = group.GetFinalBytes(this.state.Compression, out unencrypted);

				int encryptedLen;
				Buffer encryptedBuffer = Pool<Buffer>.Acquire();

				if (this.useEncryption) {
					IEncryption encryption = this.state.Encryption;
					if (encryption != null) {
						if (this.encryptionInitialised) {
							encryptedLen = encryption.Encrypt(unencrypted, 0, encryptedBuffer.bytes, 0, unencryptedLen);
						} else {
							this.Close("Tried sending data with encryption not initialised.");
							throw new Exception("Sending failed - encryption not initialised. Closing.");
						}
					} else {
						System.Buffer.BlockCopy(unencrypted, 0, encryptedBuffer.bytes, 0, unencryptedLen);
						encryptedLen = unencryptedLen;
					}
				} else {
					System.Buffer.BlockCopy(unencrypted, 0, encryptedBuffer.bytes, 0, unencryptedLen);
					encryptedLen = unencryptedLen;
				}

				BufferToSend toSend = new BufferToSend(encryptedBuffer, 0, encryptedLen);
				this.BeginSend(toSend);

				group.Dequeued();
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		protected abstract void BeginSend(BufferToSend toSend);

		#endregion Sending data
	}
}