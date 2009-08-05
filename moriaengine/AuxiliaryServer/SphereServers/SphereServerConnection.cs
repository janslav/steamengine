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

		private SynchronizedQueue<CommandResponse> commandResponders = new SynchronizedQueue<CommandResponse>();

		delegate void ReceievingMode(TextReader reader);

		private ReceievingMode nonLoggedMode;
		private ReceievingMode loggedInMode;
		private ReceievingMode commandReplyMode;
		private ReceievingMode ignoreMode;

		private ReceievingMode receivingMode;

		internal SphereServerConnection(Socket socket, SphereServerSetup setup) {
			this.beginSendCallback = this.BeginSendCallback;
			this.beginReceiveCallback = this.BeginReceieveCallback;

			this.socket = socket;
			this.BeginReceive();

			this.state = new SphereServerClient(this, setup);

			this.nonLoggedMode = this.ModeNotLoggedIn;
			this.loggedInMode = this.ModeLoggedIn;
			this.commandReplyMode = this.ModeCommandReply;
			this.ignoreMode = this.ModeIgnore;

			this.receivingMode = this.nonLoggedMode;
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

		internal void StartLoginSequence(SphereServerSetup sphereServerSetup) {
			this.BeginSend(" ");
			this.BeginSend(sphereServerSetup.AdminAccount);
			this.BeginSend(sphereServerSetup.AdminPassword);
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

		static Regex loggedinRE = new Regex(@"(Login '(?<username>.+?)')|(?<badPass>Bad password for this account\.)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void ProcessReceievedData(int length) {
			StreamReader reader = new StreamReader(new MemoryStream(this.receivingBuffer.bytes, 0, length), Encoding.Default);

			this.receivingMode(reader);
		}

		private void ModeNotLoggedIn(TextReader reader) {
			string line;
			while ((line = reader.ReadLine()) != null) {
				Match m = loggedinRE.Match(line);
				if (m.Success) {
					if (m.Groups["badPass"].Value.Length == 0) {
						if (m.Groups["username"].Value.Equals(((SphereServerSetup) this.state.Setup).AdminAccount,
							StringComparison.OrdinalIgnoreCase)) {

							this.receivingMode = this.loggedInMode;
							this.receivingMode(reader); //continue if there's anything

							this.state.On_LoginSequenceEnded(true);
						} else {
							this.Close("unexpected username in login sequence, wtf?");
						}
					} else {
						this.receivingMode = this.ignoreMode;
						this.state.On_LoginSequenceEnded(false);						
					}
				}
			}
		}

		static Regex timeStampedLineRE = new Regex(
			@"^(?<time>[\d][\d]:[\d][\d]).*$",
			RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private void ModeLoggedIn(TextReader reader) {
			string line;
			while ((line = reader.ReadLine()) != null) {
				this.state.On_ReceievedLine(line);
			}
		}

		private void ModeCommandReply(TextReader reader) {
			if (this.commandResponders.Count > 0) {
				List<string> list = new List<string>();
				string line;
				while ((line = reader.ReadLine()) != null) { //timestamped lines are not part of the command response
					if (timeStampedLineRE.IsMatch(line)) {
						this.state.On_ReceievedLine(line);
					} else {
						list.Add(line);
					}
				}
				this.commandResponders.Dequeue().OnRespond(list); ;
			} else {
				this.receivingMode = this.loggedInMode;
				this.receivingMode(reader);
			}
		}

		private void ModeIgnore(TextReader reader) {
		}

		#endregion Receieve

		public void SendCommand(string command, CommandResponse responder) {
			this.BeginSend(command);
			this.commandResponders.Enqueue(responder);
			this.receivingMode = this.commandReplyMode;
		}

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

	public abstract class CommandResponse {

		internal abstract void OnRespond(IList<string> lines);
	}

	public delegate void ParserCallback<T>(IList<string> lines, T state);

	public class CallbackCommandResponse<T> : CommandResponse {
		ParserCallback<T> callback;
		T state;

		public CallbackCommandResponse(ParserCallback<T> callback, T state) {
			this.callback = callback;
			this.state = state;
		}

		internal override void OnRespond(IList<string> lines) {
			this.callback(lines, state);
		}
	}
}
