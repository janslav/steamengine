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
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using SteamEngine.Common;
using SteamEngine.Communication;

namespace SteamEngine.Communication.NamedPipes {
	public sealed class NamedPipeConnection<TState> :
		AbstractConnection<NamedPipeConnection<TState>, TState, string>
		where TState : Poolable, IConnectionState<NamedPipeConnection<TState>, TState, string>, new() {

		private string pipename;
		private SafeFileHandle handle;
		private FileStream stream;
		private bool isConnected = false;

		private AsyncCallback onWrite;
		private AsyncCallback onRead;

		public NamedPipeConnection() {
			this.onWrite = this.OnWrite;
			this.onRead = this.OnRead;
		}

		internal void SetFields(string pipename, SafeFileHandle handle) {
			this.pipename = pipename;
			this.handle = handle;
			this.stream = new FileStream(handle, FileAccess.ReadWrite, Buffer.bufferLen, true);
		}

		public override bool IsConnected {
			get {
				return this.isConnected;
			}
		}

		public override string EndPoint {
			get {
				return this.pipename;
			}
		}

		protected override void On_Init() {
			this.isConnected = true;
			base.On_Init();
			this.BeginReceive();
		}

		private void BeginReceive() {

			int offset = this.receivedDataLength;
			byte[] buffer = this.receivingBuffer.bytes;

			this.stream.BeginRead(buffer, offset,
				buffer.Length - offset, onRead, null);
		}

		private void OnRead(IAsyncResult asyncResult) {
			try {
				int length = this.stream.EndRead(asyncResult);
				if (length > 0) {//we have new data, but still possibly have some old data.
					base.ProcessReceievedData(length);
				} else {
					this.Close("Other side closed the pipe.");
				}
			} catch (Exception e) {
				Logger.WriteError(e);
				this.Close(e.Message);
			}

			if (this.IsConnected) {
				this.BeginReceive();
			}
		}

		protected override void BeginSend(BufferToSend toSend) {
			this.stream.BeginWrite(toSend.buffer.bytes, toSend.offset, toSend.len, this.onWrite, toSend.buffer);
		}

		private void OnWrite(IAsyncResult asyncResult) {
			Buffer toDispose = (Buffer) asyncResult.AsyncState;

			try {
				this.stream.EndWrite(asyncResult);
			} catch (Exception e) {
				this.Close(e.Message);
			} finally {
				toDispose.Dispose();
			}
		}

		protected override void On_DisposeUnmanagedResources() {
			this.isConnected = false;
			try {
				this.stream.Close();
				this.stream = null;
			} catch { }
			try {
				this.handle.Close();
			} catch { }

			base.On_DisposeUnmanagedResources();
		}
	}
}