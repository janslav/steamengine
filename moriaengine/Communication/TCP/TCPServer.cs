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

namespace SteamEngine.Communication.TCP {
	public class TCPServer<TState> : 
		AsyncCore<TCPConnection<TState>, TState, IPEndPoint>,
		IServer<TCPConnection<TState>, TState, IPEndPoint>
		where TState : IConnectionState<TCPConnection<TState>, TState, IPEndPoint>, new() {

		private AsyncCallback onAccept;
		Socket listener;

		public TCPServer(IPEndPoint endpoint, IProtocol<TCPConnection<TState>, TState, IPEndPoint> protocol, object lockObject)
				: base(protocol, lockObject) {
			this.onAccept = this.OnAccept;

			this.Bind(endpoint);
		}

		private static Socket CreateSocket() {
			return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		public void Bind(IPEndPoint ipep) {
			if (this.IsBound) {
				throw new Exception("Already bound");
			}

			listener = CreateSocket();

			try {
				listener.LingerState.Enabled = false;
#if !MONO
				listener.ExclusiveAddressUse = false;
#endif

				listener.Bind(ipep);
				listener.Listen(8);

				Console.WriteLine("Listening on port "+ipep.Port);

				listener.BeginAccept(CreateSocket(), 0, onAccept, listener);
			} catch (Exception e) {
				throw new FatalException("Server socket bind failed.", e);

			}
		}

		public IPEndPoint BoundTo {
			get {
				return (IPEndPoint) this.listener.LocalEndPoint;			
			}
		}

		public bool IsBound {
			get {
				if (this.listener != null) {
					return this.listener.IsBound;
				}
				return false;
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
				TCPConnection<TState> newConn = Pool<TCPConnection<TState>>.Acquire();
				newConn.socket = accepted;
				InitNewConnection(newConn);
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

		//protected virtual bool On_NewClient(TState newSS) {
		//    return true;
		//}

		public void UnBind() {
			if (this.IsBound) {
				Console.WriteLine("Stopped listening on port "+this.BoundTo.Port);
			}
			try {
				listener.Close();
			} catch { }
		}

		protected override void On_DisposeUnmanagedResources() {
			UnBind();

			base.On_DisposeUnmanagedResources();
		}
	}
}
