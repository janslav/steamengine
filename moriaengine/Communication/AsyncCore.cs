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
	public abstract class AsyncCore<TProtocol, TConnection, TState, TEndPoint> : Disposable,
		IAsyncCore<TProtocol, TConnection, TState, TEndPoint>
		where TProtocol : IProtocol<TProtocol, TConnection, TState, TEndPoint>, new()
		where TConnection : AbstractConnection<TProtocol, TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TProtocol, TConnection, TState, TEndPoint>, new() {



		Queue<IncomingMessage> incomingPackets;
		Queue<IncomingMessage> incomingPacketsWorking;

		internal AutoResetEvent outgoingPacketsWaitingEvent = new AutoResetEvent(false);

		internal ManualResetEvent workersNeedStopping = new ManualResetEvent(false);

		internal Thread workerAlpha;
		internal Thread workerBeta;
		internal Thread workerGama;
		internal Queue<OutgoingMessage> outgoingPackets;

		internal TProtocol protocol = new TProtocol();

		public AsyncCore() {
			this.incomingPackets = new Queue<IncomingMessage>();
			this.incomingPacketsWorking = new Queue<IncomingMessage>();

			this.outgoingPackets = new Queue<OutgoingMessage>();

			this.workerAlpha = new Thread(WorkerThreadMethod);
			this.workerBeta = new Thread(WorkerThreadMethod);
			this.workerGama = new Thread(WorkerThreadMethod);
			this.workerAlpha.IsBackground = true;
			this.workerBeta.IsBackground = true;
			this.workerGama.IsBackground = true;
			this.workerAlpha.Start();
			this.workerBeta.Start();
			this.workerGama.Start();

			
		}

		internal struct IncomingMessage {
			internal readonly TConnection conn;
			internal readonly IncomingPacket<TProtocol, TConnection, TState, TEndPoint> packet;

			internal IncomingMessage(TConnection conn, IncomingPacket<TProtocol, TConnection, TState, TEndPoint> packet) {
				this.conn = conn;
				this.packet = packet;
			}
		}

		internal class OutgoingMessage {
			internal readonly TConnection conn;
			internal readonly PacketGroup group;

			internal OutgoingMessage(TConnection conn, PacketGroup group) {
				this.conn = conn;
				this.group = group;
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
					TConnection conn = msg.conn;
					msg.packet.Handle(conn, conn.State);
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteError(e);
				}
				msg.packet.Dispose();
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
		internal void EnqueueIncoming(TConnection conn, IncomingPacket<TProtocol, TConnection, TState, TEndPoint> packet) {
			lock (this.incomingPackets) {
				this.incomingPackets.Enqueue(new IncomingMessage(conn, packet));
			}
		}

		//outgoing packets
		private void WorkerThreadMethod() {
			Queue<OutgoingMessage> secondQueue = new Queue<OutgoingMessage>();

			while (outgoingPacketsWaitingEvent.WaitOne()) {
				lock (this.outgoingPackets) {
					Queue<OutgoingMessage> temp = this.outgoingPackets;
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
