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
	public class Server : Disposable {
		const int bufferLen = 10*1024;

		bool bound = false;

		int port;

		readonly object lockObject = new object();
		IProtocol protocol;
		IConnectionFactory factory;

		Queue<OutgoingMessage> outgoingPackets;
		SynchronisedQueue<IncomingMessage> incomingPackets;

		Thread workerAlpha;
		Thread workerBeta;
		Thread workerGama;

		AutoResetEvent outgoingEvent = new AutoResetEvent(false);

		private AsyncCallback onAccept;

		private AsyncCallback onSend;

		private AsyncCallback onReceieve;

		Socket listener;

		public Server(int port, IConnectionFactory factory, IProtocol protocol) {
			this.port = port;
			this.protocol = protocol;
			this.factory = factory;

			this.outgoingPackets = new Queue<OutgoingMessage>();
			this.incomingPackets = new SynchronisedQueue<IncomingMessage>(this.lockObject);


			this.workerAlpha = new Thread(WorkerThreadMethod);
			this.workerBeta = new Thread(WorkerThreadMethod);
			this.workerGama = new Thread(WorkerThreadMethod);
			this.workerAlpha.IsBackground = true;
			this.workerBeta.IsBackground = true;
			this.workerGama.IsBackground = true;
			this.workerAlpha.Start();
			this.workerBeta.Start();
			this.workerGama.Start();

			this.onAccept = this.OnAccept;
			this.onSend = this.OnSend;
			this.onReceieve = this.OnReceieve;

			this.Bind();
		}

		private class ConnectionSocketPair {
			internal readonly IConnection conn;
			internal readonly Socket socket;
			internal readonly byte[] bytes = new byte[bufferLen];
			internal int startOfData;
			internal int endOfData;

			internal ConnectionSocketPair(IConnection conn, Socket socket) {
				this.conn = conn;
				this.socket = socket;
			}
		}

		private struct OutgoingMessage {
			internal readonly Socket socket;
			internal readonly IConnection conn;
			internal readonly PacketGroup group;
			internal readonly Buffer buffer;

			internal OutgoingMessage(Socket socket, IConnection conn, PacketGroup group) {
				this.socket = socket;
				this.conn = conn;
				this.group = group;
				this.buffer = Pool<Buffer>.Acquire();
			}
		}

		private struct IncomingMessage {
			internal readonly IConnection conn;
			internal readonly IncomingPacket packet;

			internal IncomingMessage(IConnection conn, IncomingPacket packet) {
				this.conn = conn;
				this.packet = packet;
			}
		}

		//called from main loop
		public void SendPacketGroup(IConnection conn, PacketGroup group) {
			ThrowIfDisposed();

			lock (this.lockObject) {
				outgoingPackets.Enqueue(new OutgoingMessage(conn.Socket, conn, group));
			}

			outgoingEvent.Set();

		}

		//called from main loop
		public void Cycle() {
			lock (this.lockObject) {
				ThrowIfDisposed();


			}
		}

		private void WorkerThreadMethod() {
			Queue<OutgoingMessage> secondQueue = new Queue<OutgoingMessage>();

			while (outgoingEvent.WaitOne()) {
				lock (this.lockObject) {
					Queue<OutgoingMessage> temp = this.outgoingPackets;
					this.outgoingPackets = secondQueue;
					secondQueue = temp;
				}
				try {
					while (secondQueue.Count > 0) {
						OutgoingMessage msg = secondQueue.Dequeue();
						byte[] unencrypted;
						int len = msg.group.GetResult(out unencrypted);

						byte[] encrypted = msg.buffer.bytes;
						len = msg.conn.Encryption.ServerEncrypt(unencrypted, len, encrypted);

						msg.socket.BeginSend(encrypted, 0, len, SocketFlags.None, onSend, msg);
					}
				} catch (Exception e) {
					Logger.WriteError(e);
				}
			}
		}

		private Socket CreateSocket() {
			return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		public void Bind() {
			lock (this.lockObject) {
				if (!this.bound) {
					IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);

					listener = CreateSocket();

					try {
						listener.LingerState.Enabled = false;
#if !MONO
						listener.ExclusiveAddressUse = false;
#endif

						listener.Bind(ipep);
						listener.Listen(8);

						listener.BeginAccept(CreateSocket(), 0, onAccept, listener);
					} catch (Exception e) {
						throw new FatalException("Server socket bind failed.", e);

					}
				}
			}
		}

		private void OnAccept(IAsyncResult asyncResult) {
			Socket listener = (Socket) asyncResult.AsyncState;

			Socket accepted = null;

			try {
				accepted = listener.EndAccept(asyncResult);
			} catch (ObjectDisposedException) {
				return;
			} catch (Exception e) {
				Logger.WriteError(e);
			}

			if (accepted != null) {
				IConnection newConn;
				if (this.factory.NewConnection(accepted, out newConn)) {
					Enqueue(new ConnectionSocketPair(newConn, accepted));
				} else {
					Release(accepted);
				}
			}

			//continue in accepting
			try {
				listener.BeginAccept(CreateSocket(), 0, onAccept, listener);
			} catch (ObjectDisposedException) {
				return;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		private void Enqueue(ConnectionSocketPair pair) {
			//protocol.HandleInitConnection(pair.conn, pair.socket, initialData, 0, len);

			BeginReceive(pair);
		}

		public void Release(Socket socket) {
			try {
				socket.Shutdown(SocketShutdown.Both);
			} catch {
			}

			try {
				socket.Close();
			} catch {
			}
		}

		private void BeginReceive(ConnectionSocketPair pair) {
			pair.socket.BeginReceive(pair.bytes, pair.endOfData, bufferLen - pair.endOfData, SocketFlags.None, onReceieve, pair);
		}

		private void OnReceieve(IAsyncResult asyncResult) {
			ConnectionSocketPair pair = (ConnectionSocketPair) asyncResult.AsyncState;

			try {
				int byteCount = pair.socket.EndReceive(asyncResult);

				if (byteCount > 0) {
					pair.endOfData += byteCount;

					byte[] bytes = pair.bytes;

					while (true) {
						int count = pair.endOfData - pair.startOfData;
						byte id = bytes[pair.startOfData];
						IncomingPacket packet = protocol.GetPacketImplementation(id);
						int read;
						if (packet.Read(bytes, pair.startOfData, count, out read)) {
							incomingPackets.Enqueue(new IncomingMessage(pair.conn, packet));
						} else {
							packet.Dispose();
						}

						if (read > 0) {
							pair.startOfData += read;
						} else {
							break;
						}
					}

					if (pair.startOfData < pair.endOfData) {
						//if over half the buffer, we copy to the start
						//
						if (pair.startOfData > (bufferLen / 2)) {
							int len = pair.endOfData - pair.startOfData;
							System.Buffer.BlockCopy(bytes, pair.startOfData, bytes, 0, len);
							pair.startOfData = 0;
							pair.endOfData = len;
						} else if (pair.endOfData == bufferLen) {
							DisposeConnection(pair.conn, "Incoming data buffer full. This is bad.");
							return;
						}
					} else {//we read exactly what we had, perfect.
						pair.startOfData = 0;
						pair.endOfData = 0;
					}
				}
				BeginReceive(pair);
			} catch (Exception e) {
				Logger.WriteError(e);
				DisposeConnection(pair.conn, e.Message);
			}
		}

		private void OnSend(IAsyncResult asyncResult) {
			OutgoingMessage msg = (OutgoingMessage) asyncResult.AsyncState;

			SocketError err;
			msg.socket.EndSend(asyncResult, out err);

			if (err != SocketError.Success) {
				DisposeConnection(msg.conn, err.ToString());
			}

			msg.buffer.Dispose();
		}

		private void DisposeConnection(IConnection conn, string reason) {
			ClosingPacket fake = Pool<ClosingPacket>.Acquire();
			fake.message = reason;
			incomingPackets.Enqueue(new IncomingMessage(conn, fake));
		}

		public void UnBind() {
			lock (this.lockObject) {
				if (this.listener != null) {
					listener.Close();
					this.listener = null;
					this.bound = false;
				}
			}
		}

		protected override void DisposeUnmanagedResources() {
			UnBind();
		}


	}

	public class ClosingPacket : IncomingPacket {
		public string message;

		protected internal override void Handle(IConnection conn) {
			conn.Close(this.message);
		}

		protected override bool Read(int count) {
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
