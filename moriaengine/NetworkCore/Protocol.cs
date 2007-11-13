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
	public abstract class Protocol<SSType> : Disposable where SSType : SteamSocket {

		private AsyncCallback onSend;
		private AsyncCallback onReceieve;


		Thread workerAlpha;
		Thread workerBeta;
		Thread workerGama;

		internal Queue<OutgoingMessage> outgoingPackets;
		internal Queue<IncomingMessage> incomingPackets;
		internal Queue<IncomingMessage> incomingPacketsWorking;

		internal AutoResetEvent outgoingEvent = new AutoResetEvent(false);

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
			internal readonly IncomingPacket packet;

			internal IncomingMessage(SSType ss, IncomingPacket packet) {
				this.ss = ss;
				this.packet = packet;
			}
		}

		internal void BeginReceive(SteamSocket ss) {
			ss.socket.BeginReceive(ss.receivingBuffer.bytes, ss.endOfData, 
				ss.receivingBuffer.bytes.Length - ss.endOfData, SocketFlags.None, onReceieve, ss);
		}

		internal void OnReceieve(IAsyncResult asyncResult) {
			SSType ss = (SSType) asyncResult.AsyncState;

			try {
				int byteCount = ss.socket.EndReceive(asyncResult);

				if (byteCount > 0) {
					ss.endOfData += byteCount;

					byte[] bytes = ss.receivingBuffer.bytes;

					while (true) {
						int count = ss.endOfData - ss.startOfData;
						byte id = bytes[ss.startOfData];
						IncomingPacket packet = this.GetPacketImplementation(id);
						int read;
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
				BeginReceive(ss);
			} catch (Exception e) {
				Logger.WriteError(e);
				ss.Close(e.Message);
			}
		}


		private void WorkerThreadMethod() {
			Queue<OutgoingMessage> secondQueue = new Queue<OutgoingMessage>();

			while (outgoingEvent.WaitOne()) {
				lock (this.outgoingPackets) {
					Queue<OutgoingMessage> temp = this.outgoingPackets;
					this.outgoingPackets = secondQueue;
					secondQueue = temp;
				}
				try {
					while (secondQueue.Count > 0) {
						OutgoingMessage msg = secondQueue.Dequeue();
						byte[] unencrypted;
						int len = msg.group.GetResult(out unencrypted);

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

		protected abstract IncomingPacket GetPacketImplementation(byte id);

	}
}
