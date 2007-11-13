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
	public abstract class Client<SSType> : Protocol<SSType>, IComponent where SSType : SteamSocket, new() {

		SSType ss;

		string remoteHost;
		int remotePort;

		public string RemoteHost {
			get {
				return this.remoteHost;
			}
			set {
				this.remoteHost = value;
			}
		}

		public int RemotePort {
			get {
				return this.remotePort;
			}
			set {
				this.remotePort = value;
			}
		}

		public Client() {
			this.ss = Pool<SSType>.Acquire();
		}

		public void SendPacketGroup(PacketGroup group) {
			ThrowIfDisposed();
			if ((ss.socket == null) || (!ss.socket.Connected)) {
				throw new InvalidOperationException("Client not connected");
			}

			group.Enqueued();

			lock (this.outgoingPackets) {
				outgoingPackets.Enqueue(new OutgoingMessage(ss, group));
			}

			outgoingEvent.Set();
		}


		#region IComponent implementation
		// Fields
		private ISite site;

		public event EventHandler Disposed;

		protected override void DisposeUnmanagedResources() {
			this.ss.Dispose();

			lock (this) {
				if (this.site != null) {
					IContainer cont = this.site.Container;
					if (cont != null) {
						cont.Remove(this);
					}
				}

				EventHandler handler = this.Disposed;
				if (handler != null) {
					handler(this, EventArgs.Empty);
				}
			}
			base.DisposeUnmanagedResources();
		}

		public override string ToString() {
			ISite site = this.site;
			if (site != null) {
				return (site.Name + " [" + this.GetType().FullName + "]");
			}
			return this.GetType().FullName;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
		public virtual ISite Site {
			get {
				return this.site;
			}
			set {
				this.site = value;
			}
		}

		#endregion IComponent implementation
	}
}
