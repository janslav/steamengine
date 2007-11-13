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
	public abstract class Server<SSType> : Protocol<SSType> where SSType : SteamSocket, new() {
		bool bound = false;

		int port;

		

		private AsyncCallback onAccept;


		Socket listener;

		public Server(int port) {
			this.port = port;

			this.onAccept = this.OnAccept;

			this.Bind();
		}

		//called from main loop
		public void SendPacketGroup(SSType ss, PacketGroup group) {
			ThrowIfDisposed();

			group.Enqueued();

			lock (this.outgoingPackets) {
				outgoingPackets.Enqueue(new OutgoingMessage(ss, group));
			}


			outgoingEvent.Set();

		}

		//called from main loop
		//public void Cycle() {
		//    ThrowIfDisposed();

		//    lock (this.lockObject) {
		//        Queue<IncomingMessage> temp = this.incomingPacketsWorking;
		//        this.incomingPacketsWorking = this.incomingPackets;
		//        this.incomingPackets = temp;
		//    }

		//    while (this.incomingPacketsWorking.Count > 0) {
		//        IncomingMessage msg = this.incomingPacketsWorking.Dequeue();
		//        try {
		//            msg.packet.Handle(msg.ss);
		//        } catch (FatalException) {
		//            throw;
		//        } catch (Exception e) {
		//            Logger.WriteError(e);
		//        }
		//        msg.packet.Dispose();
		//    }
		//}

		private Socket CreateSocket() {
			return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		public void Bind() {
			lock (this) {
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
				SSType newSS = Pool<SSType>.Acquire();
				newSS.socket = accepted;

				if (this.On_NewClient(newSS)) {
					BeginReceive(newSS);
				} else {
					newSS.Close("On_NewClient returned false.");
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

		protected virtual bool On_NewClient(SSType newSS) {
			return true;
		}

		//private void DisposeConnection(SSType ss, string reason) {
		//    ClosingPacket fake = Pool<ClosingPacket>.Acquire();
		//    fake.message = reason;
		//    lock (this.lockObject) {
		//        incomingPackets.Enqueue(new IncomingMessage(ss, fake));
		//    }
		//}

		public void UnBind() {
			lock (this) {
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

	//public class ClosingPacket : IncomingPacket {
	//    public string message;

	//    protected internal override void Handle(IConnection conn) {
	//        conn.Close(this.message);
	//    }

	//    protected override bool Read(int count) {
	//        throw new Exception("The method or operation is not implemented.");
	//    }
	//}
}
