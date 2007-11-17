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
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using SteamEngine.Common;

namespace SteamEngine.Network {

	public class ClientSocket : SteamSocket {
		internal Client client;

		public ClientSocket() {

		}

		public override void Handle(IncomingPacket packet) {
			this.client.Handle(packet);
		}

		public override IEncryption Encryption {
			get {
				return client.Encryption;
			}
		}

		public override void On_Close(LogStr reason) {
			this.client.On_Close(reason);
			this.client.Dispose();
		}

		public override void On_Close(string reason) {
			this.client.On_Close(reason);
			this.client.Dispose();
		}
	}

	public abstract class Client : Protocol<ClientSocket> {
		ClientSocket ss;

		public Client() {
			this.ss = Pool<ClientSocket>.Acquire();
			this.ss.client = this;
		}

		public void Connect(string remoteHost, int remotePort) {
			this.ss.socket = this.CreateSocket();
			this.ss.socket.Connect(remoteHost, remotePort);

			this.BeginReceive(this.ss);
		}

		public virtual IEncryption Encryption {
			get {
				return null;
			}
		}

		public void SendPacketGroup(PacketGroup group) {
			ThrowIfDisposed();
			if ((ss.socket == null) || (!ss.socket.Connected)) {
				throw new InvalidOperationException("Client not connected");
			}

			group.Enqueued();

			lock (this.outgoingPackets) {
				outgoingPackets.Enqueue(new OutgoingMessage(this.ss, group));
			}

			outgoingPacketsWaitingEvent.Set();
		}

		protected override void DisposeUnmanagedResources() {
			this.ss.Dispose();

			base.DisposeUnmanagedResources();
		}

		protected internal abstract void Handle(IncomingPacket packet);

		protected internal virtual void On_Close(LogStr reason) {
		}

		protected internal virtual void On_Close(string reason) {
		}
	}
}
