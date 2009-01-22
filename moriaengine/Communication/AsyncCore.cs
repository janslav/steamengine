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
using SteamEngine.Communication;

namespace SteamEngine.Communication {
	public abstract class AsyncCore<TConnection, TState, TEndPoint> : Disposable//,
		//IAsyncCore<TConnection, TState, TEndPoint>
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : Poolable, IConnectionState<TConnection, TState, TEndPoint>, new() {



		//Queue<IncomingMessage> incomingPackets;
		//Queue<IncomingMessage> incomingPacketsWorking;

		private AutoResetEvent outgoingPacketsWaitingEvent = new AutoResetEvent(false);

		private ManualResetEvent workersNeedStopping = new ManualResetEvent(false);

		private Thread workerAlpha;
		//private Thread workerBeta;
		//private Thread workerGamma;
		private SimpleQueue<OutgoingMessage> outgoingPackets;

		internal readonly IProtocol<TConnection, TState, TEndPoint> protocol;

		private object lockObject;

		public AsyncCore(IProtocol<TConnection, TState, TEndPoint> protocol, object lockObject) {
			//this.incomingPackets = new Queue<IncomingMessage>();
			//this.incomingPacketsWorking = new Queue<IncomingMessage>();

			this.protocol = protocol;

			this.outgoingPackets = new SimpleQueue<OutgoingMessage>();

			string threadsName = this.GetType().Name;

			this.workerAlpha = CreateAndStartWorkerThread(threadsName + "_Worker_Alpha");
			//this.workerBeta = CreateAndStartWorkerThread(threadsName+"_Worker_Beta");
			//this.workerGamma = CreateAndStartWorkerThread(threadsName+"_Worker_Gamma");

			this.lockObject = lockObject;
		}

		private Thread CreateAndStartWorkerThread(string name) {
			Thread t = new Thread(this.WorkerThreadMethod);
			t.Name = name;
			t.IsBackground = true;
			t.Start();
			return t;
		}

		public object LockObject {
			get { return this.lockObject; }
		}

		//internal struct IncomingMessage {
		//    internal readonly TConnection conn;
		//    internal readonly IncomingPacket<TConnection, TState, TEndPoint> packet;

		//    internal IncomingMessage(TConnection conn, IncomingPacket<TConnection, TState, TEndPoint> packet) {
		//        this.conn = conn;
		//        this.packet = packet;
		//    }
		//}

		internal class OutgoingMessage {
			internal readonly TConnection conn;
			internal readonly PacketGroup group;

			internal OutgoingMessage(TConnection conn, PacketGroup group) {
				this.conn = conn;
				this.group = group;
			}
		}

		//called from main loop (!)
		//public void Cycle() {
		//    ThrowIfDisposed();

		//    lock (this.incomingPackets) {
		//        Queue<IncomingMessage> temp = this.incomingPacketsWorking;
		//        this.incomingPacketsWorking = this.incomingPackets;
		//        this.incomingPackets = temp;
		//    }

		//    while (this.incomingPacketsWorking.Count > 0) {
		//        IncomingMessage msg = this.incomingPacketsWorking.Dequeue();
		//        try {
		//            TConnection conn = msg.conn;
		//            msg.packet.Handle(conn, conn.State);
		//        } catch (FatalException) {
		//            throw;
		//        } catch (Exception e) {
		//            Logger.WriteError(e);
		//        }
		//        msg.packet.Dispose();
		//    }
		//}


		protected void InitNewConnection(TConnection newConn) {
			try {
				lock (this.lockObject) {
					newConn.Init(this);
				}
			} catch (Exception e) {
				Logger.WriteError(e);
				newConn.Close(e.Message);
			}
		}

		//from main loop
		internal void EnqueueOutgoing(TConnection conn, PacketGroup group) {
			group.Enqueued();

			lock (this.outgoingPackets) {
				this.outgoingPackets.Enqueue(new OutgoingMessage(conn, group));
			}
			outgoingPacketsWaitingEvent.Set();
		}

		//from async background thread
		internal void HandlePacket(TConnection conn, TState state, IncomingPacket<TConnection, TState, TEndPoint> packet) {
			//lock (this.incomingPackets) {
			//    this.incomingPackets.Enqueue(new IncomingMessage(conn, packet));
			//}
			lock (this.lockObject) {
				packet.Handle(conn, state);
			}
			packet.Dispose();
		}

		//outgoing packets
		private void WorkerThreadMethod() {
			SimpleQueue<OutgoingMessage> secondQueue = new SimpleQueue<OutgoingMessage>();

			while (outgoingPacketsWaitingEvent.WaitOne()) {
				lock (this.outgoingPackets) {
					SimpleQueue<OutgoingMessage> temp = this.outgoingPackets;
					this.outgoingPackets = secondQueue;
					secondQueue = temp;
				}

				while (secondQueue.Count > 0) {
					OutgoingMessage msg = secondQueue.Dequeue();
					msg.conn.ProcessSending(msg.group);
				}

				if (this.workersNeedStopping.WaitOne(0, false)) {
					return;
				}
			}
		}
	}
}
