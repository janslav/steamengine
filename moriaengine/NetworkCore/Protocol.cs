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

namespace SteamEngine.Network {
	public abstract class Protocol<SSType> : Poolable where SSType : SteamSocket {

		private AsyncCallback onSend;
		private AsyncCallback onReceieve;

		Thread workerAlpha;
		Thread workerBeta;
		Thread workerGama;

		internal Queue<OutgoingMessage> outgoingPackets;
		Queue<IncomingMessage> incomingPackets;
		Queue<IncomingMessage> incomingPacketsWorking;

		internal AutoResetEvent outgoingPacketsWaitingEvent = new AutoResetEvent(false);

		internal ManualResetEvent workersNeedStopping = new ManualResetEvent(false);

		public Protocol() {

			this.outgoingPackets = new Queue<OutgoingMessage>();
			this.incomingPackets = new Queue<IncomingMessage>();
			this.incomingPacketsWorking = new Queue<IncomingMessage>();

			this.workerAlpha = new Thread(WorkerThreadMethod);
			this.workerBeta = new Thread(WorkerThreadMethod);
			this.workerGama = new Thread(WorkerThreadMethod);
			this.workerAlpha.IsBackground = true;
			this.workerBeta.IsBackground = true;
			this.workerGama.IsBackground = true;
			this.workerAlpha.Start();
			this.workerBeta.Start();
			this.workerGama.Start();

			this.onSend = this.OnSend;
			this.onReceieve = this.OnReceieve;
		}

		internal class OutgoingMessage {
			internal readonly SSType ss;
			internal readonly PacketGroup group;
			internal readonly Buffer buffer;

			internal OutgoingMessage(SSType ss, PacketGroup group) {
				this.ss = ss;
				this.group = group;
				this.buffer = Pool<Buffer>.Acquire();
			}
		}

		internal struct IncomingMessage {
			internal readonly SSType ss;
			internal readonly IncomingPacket<SSType> packet;

			internal IncomingMessage(SSType ss, IncomingPacket<SSType> packet) {
				this.ss = ss;
				this.packet = packet;
			}
		}
		//called from main loop (!)
		public void Cycle() {
			ThrowIfDisposed();

			lock (this.incomingPackets) {
				Queue<IncomingMessage> temp = this.incomingPacketsWorking;
				this.incomingPacketsWorking = this.incomingPackets;
				this.incomingPackets = temp;
			}

			while (this.incomingPacketsWorking.Count > 0) {
				IncomingMessage msg = this.incomingPacketsWorking.Dequeue();
				try {
					msg.packet.Handle(msg.ss);
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(e);
				}
				msg.packet.Dispose();
			}
		}

		internal Socket CreateSocket() {
			return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		internal void BeginReceive(SteamSocket ss) {
			int offset = ss.receivedDataLength;


			ss.socket.BeginReceive(ss.receivingBuffer.bytes, offset,
				Buffer.bufferLen - offset, SocketFlags.None, onReceieve, ss);
		}

		internal void OnReceieve(IAsyncResult asyncResult) {
			SSType ss = (SSType) asyncResult.AsyncState;

			try {
				int length = ss.socket.EndReceive(asyncResult);

				if (length > 0) {//we have new data, but still possibly have some old data.
					//ss.receievedDataLength += newBytesLength;
					byte[] bytes = ss.receivingBuffer.bytes;
					length += ss.receivedDataLength;
					int offset = 0;

					if (!ProcessDecryption(ss, ref length, ref bytes, ref offset)) {
						//init failed with not enough data, we queue the data
						BeginReceive(ss);
						return;
					}

					ICompression compression = ss.Compression;
					if (compression != null) {
						length = compression.Decompress(bytes, offset, ss.decompressBuffer.bytes,
							ss.decompressedDataOffset + ss.decompressedDataLength, length);
					} else {
						System.Buffer.BlockCopy(bytes, offset, ss.decompressBuffer.bytes,
							ss.decompressedDataOffset + ss.decompressedDataLength, length);
					}

					bytes = ss.decompressBuffer.bytes;
					offset = ss.decompressedDataOffset;
					length += ss.decompressedDataLength;


					//read all posible packets outa those data
					bool loop = true;
					while (loop && (length > 0)) {
						byte id = bytes[offset];
						IncomingPacket<SSType> packet = this.GetPacketImplementation(id);
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
								lock (this.incomingPackets) {
									incomingPackets.Enqueue(new IncomingMessage(ss, packet));
								}
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

					ss.decompressedDataOffset = offset;
					ss.decompressedDataLength = length;

					RecycleBufferIfNeeded(bytes, ref ss.decompressedDataOffset, ref ss.decompressedDataLength);
					BeginReceive(ss);
				} else {
					ss.Close("Connection lost");
				}
			} catch (Exception e) {
				//Logger.WriteError(e);
				ss.Close(e.Message);
			}
		}

		private static bool ProcessDecryption(SSType ss, ref int length, ref byte[] bytes, ref int offset) {
			if (ss.useEncryption) {
				IEncryption encryption = ss.Encryption;
				if (encryption != null) {
					if (!ss.encryptionInitialised) {
						int read;
						EncryptionInitResult result = encryption.Init(bytes, offset, length, out read);
						switch (result) {
							case EncryptionInitResult.SuccessUseEncryption:
								Console.WriteLine(ss+": using encryption "+encryption);
								offset += read;
								length -= read;
								ss.receivedDataLength = 0;
								ss.encryptionInitialised = true;
								break;
							case EncryptionInitResult.SuccessNoEncryption:
								Console.WriteLine(ss+": using no encryption");
								offset += read;
								length -= read;
								ss.useEncryption = false;
								break;
							case EncryptionInitResult.InvalidData:
								throw new Exception("Encryption not recognised");
							case EncryptionInitResult.NotEnoughData:
								ss.receivedDataLength = length;
								return false;
						}
					}

					if (ss.useEncryption) {
						length = encryption.Decrypt(bytes, offset, ss.decryptBuffer.bytes, 0, length);
						bytes = ss.decryptBuffer.bytes;
						offset = 0;
					}
				}
			}
			return true;
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

		private void WorkerThreadMethod() {
			Queue<OutgoingMessage> secondQueue = new Queue<OutgoingMessage>();

			while (outgoingPacketsWaitingEvent.WaitOne()) {
				lock (this.outgoingPackets) {
					Queue<OutgoingMessage> temp = this.outgoingPackets;
					this.outgoingPackets = secondQueue;
					secondQueue = temp;
				}
				try {
					while (secondQueue.Count > 0) {
						OutgoingMessage msg = secondQueue.Dequeue();
						byte[] unencrypted;
						int len = msg.group.GetFinalBytes(msg.ss.Compression, out unencrypted);

						IEncryption encryption = msg.ss.Encryption;
						if (encryption != null) {
							byte[] encrypted = msg.buffer.bytes;
							len = msg.ss.Encryption.Encrypt(unencrypted, 0, encrypted, 0, len);

							msg.ss.socket.BeginSend(encrypted, 0, len, SocketFlags.None, onSend, msg);
						} else {
							msg.ss.socket.BeginSend(unencrypted, 0, len, SocketFlags.None, onSend, msg);
						}
					}
				} catch (Exception e) {
					Logger.WriteError(e);
				}

				if (this.workersNeedStopping.WaitOne(0, false)) {
					return;
				}
			}
		}

		internal void OnSend(IAsyncResult asyncResult) {
			OutgoingMessage msg = (OutgoingMessage) asyncResult.AsyncState;

			try {
				SocketError err;
				msg.ss.socket.EndSend(asyncResult, out err);

				if (err != SocketError.Success) {
					msg.ss.Close(err.ToString());
				}


			} catch (Exception e) {
				msg.ss.Close(e.Message);
			} finally {
				msg.buffer.Dispose();
				msg.group.Dequeued();
			}
		}


		//void HandleInitConnection(IConnection conn, Socket socket, byte[] buffer, int offset, int count);

		protected abstract IncomingPacket<SSType> GetPacketImplementation(byte id);

	}
}
