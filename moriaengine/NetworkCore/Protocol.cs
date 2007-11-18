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

		internal void BeginReceive(SteamSocket ss) {
			ss.socket.BeginReceive(ss.receivingBuffer.bytes, ss.endOfData, 
				ss.receivingBuffer.bytes.Length - ss.endOfData, SocketFlags.None, onReceieve, ss);
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

		internal void OnReceieve(IAsyncResult asyncResult) {
			SSType ss = (SSType) asyncResult.AsyncState;

			try {
				int byteCount = ss.socket.EndReceive(asyncResult);

				if (byteCount > 0) {
					ss.endOfData += byteCount;

					byte[] bytes = ss.receivingBuffer.bytes;

					while (true) {
						int offset = ss.startOfData;
						int count = ss.endOfData - offset;

						int read;
						if (ss.useEncryption) {
							IEncryption encryption = ss.Encryption;
							if (encryption != null) {
								if (!ss.encryptionInitialised) {
									EncryptionInitResult result = encryption.Init(bytes, offset, count, out read);
									switch (result) {
										case EncryptionInitResult.SuccessUseEncryption:
											Console.WriteLine(ss+": using encryption "+encryption);
											ss.startOfData += read;
											ss.encryptionInitialised = true;
											continue;
										case EncryptionInitResult.SuccessNoEncryption:
											Console.WriteLine(ss+": using no encryption");
											ss.startOfData += read;
											ss.useEncryption = false;
											continue;
										case EncryptionInitResult.InvalidData:
											throw new Exception("Encryption not recognised");
										case EncryptionInitResult.NotEnoughData:
											break;
									}
									break;//EncryptionInitResult.NotEnoughData
								}

								count = encryption.Decrypt(bytes, offset, count, ss.decryptBuffer.bytes, 0);
								bytes = ss.decryptBuffer.bytes;
								offset = 0;
							}

							ICompression compression = ss.Compression;
							if (compression != null) {
								count = compression.Decompress(bytes, offset, count, ss.decompressBuffer.bytes, 0);
								bytes = ss.decompressBuffer.bytes;
								offset = 0;
							}

							byte id = bytes[ss.startOfData];
							IncomingPacket<SSType> packet = this.GetPacketImplementation(id);

							if (packet.Read(bytes, ss.startOfData, count, out read)) {
								lock (this.incomingPackets) {
									incomingPackets.Enqueue(new IncomingMessage(ss, packet));
								}
							} else {
								packet.Dispose();
							}

							if (read > 0) {
								ss.startOfData += read;
							} else {
								break;
							}
						}

						if (ss.startOfData < ss.endOfData) {
							//if over half the buffer, we copy to the start
							//
							if (ss.startOfData > (Buffer.bufferLen / 2)) {
								int len = ss.endOfData - ss.startOfData;
								System.Buffer.BlockCopy(bytes, ss.startOfData, bytes, 0, len);
								ss.startOfData = 0;
								ss.endOfData = len;
							} else if (ss.endOfData == Buffer.bufferLen) {
								ss.Close("Incoming data buffer full. This is bad.");
								return;
							}
						} else {//we read exactly what we had, perfect.
							ss.startOfData = 0;
							ss.endOfData = 0;
						}
					}
				}
				BeginReceive(ss);
			} catch (Exception e) {
				//Logger.WriteError(e);
				ss.Close(e.Message);
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
						int len = msg.group.GetResult(msg.ss.Compression, out unencrypted);

						IEncryption encryption = msg.ss.Encryption;
						if (encryption != null) {
							byte[] encrypted = msg.buffer.bytes;
							len = msg.ss.Encryption.Encrypt(unencrypted, 0, len, encrypted, 0);

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

			SocketError err;
			msg.ss.socket.EndSend(asyncResult, out err);

			if (err != SocketError.Success) {
				msg.ss.Close(err.ToString());
			}

			msg.buffer.Dispose();
			msg.group.Dequeued();
		}


		//void HandleInitConnection(IConnection conn, Socket socket, byte[] buffer, int offset, int count);

		protected abstract IncomingPacket<SSType> GetPacketImplementation(byte id);

	}
}
