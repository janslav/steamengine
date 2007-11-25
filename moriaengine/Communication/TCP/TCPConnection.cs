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
	public sealed class TCPConnection<TProtocol, TState> :
		AbstractConnection<TProtocol, TCPConnection<TProtocol, TState>, TState, IPEndPoint>
		where TState : IConnectionState<TProtocol, TCPConnection<TProtocol, TState>, TState, IPEndPoint>, new()
		where TProtocol : IProtocol<TProtocol, TCPConnection<TProtocol, TState>, TState, IPEndPoint>, new() {


		internal Socket socket;

		private AsyncCallback onSend;
		private AsyncCallback onReceieve;

		public TCPConnection() {
			this.onSend = this.OnSend;
			this.onReceieve = this.OnReceieve;
		}

		public override IPEndPoint EndPoint {
			get {
				return (IPEndPoint) this.socket.RemoteEndPoint;
			}
		}

		public override bool IsConnected {
			get {
				if (this.socket != null) {
					return this.socket.Connected;
				}
				return false;
			}
		}

		protected override void On_Init() {
			this.BeginReceive();
			base.On_Init();
		}

		private void BeginReceive() {
			int offset = this.receivedDataLength;
			byte[] buffer = this.receivingBuffer.bytes;

			this.socket.BeginReceive(buffer, offset,
				buffer.Length - offset, SocketFlags.None, onReceieve, null);
		}

		private void OnReceieve(IAsyncResult asyncResult) {
			try {
				int length = this.socket.EndReceive(asyncResult);
				if (length > 0) {//we have new data, but still possibly have some old data.
					base.ProcessReceievedData(length);
				} else {
					this.Close("Connection lost");
				}
			} catch (Exception e) {
				//Logger.WriteError(e);
				this.Close(e.Message);
			}

			if (this.IsConnected) {
				this.BeginReceive();
			}
		}

		protected override void On_DisposeUnmanagedResources() {
			try {
				socket.Close();
			} catch { }
			socket = null;

			base.On_DisposeUnmanagedResources();
		}

		protected override void BeginSend(BufferToSend toSend) {
			this.socket.BeginSend(toSend.buffer.bytes, toSend.offset, toSend.len, SocketFlags.None, onSend, toSend.buffer);
		}

		private void OnSend(IAsyncResult asyncResult) {
			Buffer toDispose = (Buffer) asyncResult.AsyncState;

			try {
				SocketError err;
				this.socket.EndSend(asyncResult, out err);

				if (err != SocketError.Success) {
					this.Close(err.ToString());
				}
			} catch (Exception e) {
				this.Close(e.Message);
			} finally {
				toDispose.Dispose();
			}
		}
	}
}