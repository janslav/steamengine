using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;

using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer.SphereServers {
	public class SphereServerConnection : Disposable {
		readonly Socket socket;
		readonly SphereServerClient state;

		SteamEngine.Communication.Buffer receivingBuffer = Pool<SteamEngine.Communication.Buffer>.Acquire();

		private AsyncCallback beginSendCallback;
		private AsyncCallback beginReceiveCallback;

		private SynchronizedQueue<SphereOutputParser> parsersQueue = new SynchronizedQueue<SphereOutputParser>();

		internal SphereServerConnection(Socket socket, SphereServerSetup setup) {
			this.beginSendCallback = this.BeginSendCallback;
			this.beginReceiveCallback = this.BeginReceieveCallback;

			this.socket = socket;
			this.BeginReceive();

			this.state = new SphereServerClient(this, setup);

			
		}

		public SphereServerClient State {
			get {
				return state;
			}
		}

		public bool IsConnected {
			get {
				return this.socket.Connected;
			}
		}

		#region Receieve

		private void BeginReceive() {
			byte[] buffer = this.receivingBuffer.bytes;

			this.socket.BeginReceive(buffer, 0,
				buffer.Length, SocketFlags.None, this.beginReceiveCallback, null);
		}

		private void BeginReceieveCallback(IAsyncResult asyncResult) {
			try {
				if ((this.socket != null) && (this.socket.Handle != null)) {
					int length = this.socket.EndReceive(asyncResult);

					if (length > 0) {
						//we have new data
						this.ProcessReceievedData(length);
					} else {
						this.Close("Connection lost");
					}

					if (this.IsConnected) {
						this.BeginReceive();
					}
				}
			} catch (Exception e) {
				Logger.WriteDebug(e);
				this.Close(e.Message);
			}
		}

		private void ProcessReceievedData(int length) {
			StreamReader reader = new StreamReader(new MemoryStream(this.receivingBuffer.bytes, 0, length), Encoding.Default);
			string line;
			while ((line = reader.ReadLine()) != null) {
				line = line.Trim();
				if (!string.IsNullOrEmpty(line)) {
					this.ProcessReceievedLine(line);
				}
			}
		}

		private void ProcessReceievedLine(string line) {
			if (this.parsersQueue.Count > 0) {
				SphereOutputParser parser = this.parsersQueue.Peek();
				if (parser.Match(line)) {
					this.parsersQueue.Dequeue();
					return;
				}
			}

			this.state.On_ReceievedLine(line);			
		}

		public void EnqueueParser(SphereOutputParser parser) {
			this.parsersQueue.Enqueue(parser);
		}

		#endregion Receieve


		#region Send
		public void BeginSend(string message) {
			SteamEngine.Communication.Buffer buffer = Pool<SteamEngine.Communication.Buffer>.Acquire();

			message = message + "\n";
			int len = Encoding.Default.GetBytes(message, 0, message.Length, buffer.bytes, 0);

			this.socket.BeginSend(buffer.bytes, 0, len, SocketFlags.None, this.beginSendCallback, buffer);
		}

		private void BeginSendCallback(IAsyncResult asyncResult) {
			SteamEngine.Communication.Buffer toDispose = (SteamEngine.Communication.Buffer) asyncResult.AsyncState;

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
		#endregion Send

		#region Close
		public override sealed void Dispose() {
			this.Close("Dispose() called");
		}

		protected override void On_DisposeManagedResources() {
			this.receivingBuffer.Dispose();

			base.On_DisposeManagedResources();
		}

		protected override void On_DisposeUnmanagedResources() {
			try {
				this.socket.Shutdown(SocketShutdown.Both);
			} catch {
			}
			try {
				this.socket.Close();
			} catch {
			}

			base.On_DisposeUnmanagedResources();
		}

		public void Close(string reason) {
			if (!this.IsDisposed) {
				this.state.On_Close(reason);
			}
			base.Dispose();
		}
		#endregion Close
	}

	public abstract class SphereOutputParser {
		Regex re;

		protected SphereOutputParser(Regex re) {
			this.re = re;
		}

		internal bool Match(string line) {
			Match m = this.re.Match(line);

			if (m.Success) {
				return this.OnMatch(m);
			}
			return false;
		}

		protected abstract bool OnMatch(Match m);
	}

	public delegate bool ParserCallback<T>(Match m, T state);

	public class CallbackParser<T> : SphereOutputParser {
		ParserCallback<T> callback;
		T state;

		public CallbackParser(Regex re, ParserCallback<T> callback, T state) : base(re) {
			this.callback = callback;
			this.state = state;
		}

		protected override bool OnMatch(Match m) {
			return this.callback(m, state);
		}
	}
}
