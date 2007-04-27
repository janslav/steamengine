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
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using SteamEngine.Common;

namespace SteamEngine {
	public class ConsConn : Conn, ISrc {
		private bool loggedIn;
		private bool nativeConsole=false;
		private string buffer = "";
		private char[] nullchar= new char[1];
		private string username;
		private int packetLen;

		//public int index; //the index this instance has in Server.consoles

		private delegate void StringMode(string data);
		private delegate void VoidMode();

		//delage that begins pointing at InitHandle(), and after first receieved string it points
		//to NormalHandle(). The point is that the protocol behaves similarly to the one of sphere - 
		//ie after connect it waits for the first something and then asks for username etc
		VoidMode basicmode; //init (waiting for initial byte from the client) -> normal
		//delegate that points at a function which should do something with the string we got.
		//begins pointing at DoUsername(), which then switches it to DoPasswrod(), 
		//which switches it to DoCOmmand() where it stays until end of the session
		StringMode mode; //username (waiting for username) -> Password (...) -> Command

		//MethodInfo sendLocal;
		StringToSend sendLocal;
		//initiates the local winconsole representation
		internal ConsConn(StringToSend consSend)
			: base() {
			sendLocal = consSend;
			Logger.OnConsoleWriteLine+=new StringToSend(WriteLine);
			Logger.OnConsoleWrite+=new StringToSend(Write);
			curAccount=null;
			nativeConsole=true;
			mode = new StringMode(DoIgnore);
		}

		internal ConsConn(Socket s)
			: base(s) {
			curAccount= null;
			loggedIn = false;
			nativeConsole=false;
			mode=new StringMode(DoUsername);
			basicmode=new VoidMode(InitHandle);
			mode = new StringMode(DoIgnore);
		}

		public override bool IsLoggedIn {
			get {
				return loggedIn;
			}
		}

		public bool IsNativeConsole {
			get { return nativeConsole; }
		}

		public override void Close(string reason) {
			mode = new StringMode(DoIgnore);
			Server.RemoveConn(this);
			loggedIn = false;
			base.Close(reason);
		}

		public override void Close(LogStr reason) {
			mode = new StringMode(DoIgnore);
			Server.RemoveConn(this);
			loggedIn = false;
			base.Close(reason);
		}

		//ISrc implementation
		//AbstractCharacter ISrc.Character {
		//	get {
		//		throw new InvalidOperationException("Probably expected Character as src, but this is Console");
		//	}
		//}

		//ISrc
		//Conn ISrc.ConnObj {
		//	get {
		//		return this;
		//	}
		//}

		internal override void Cycle() {
			try {
				if (client.Poll(0, SelectMode.SelectRead)) {
					basicmode(); //InitHandle or NormalHandle
				}
			} catch (FatalException) {
				throw;
			} catch (Exception e) {	//Wow. It died AGAIN. Why was this called after the socket was discarded, anyhow? -SL
				Close("Error when writing: "+e.Message);
				Logger.WriteError(e);
			}
		}

		public override byte Plevel {
			get {
				if (nativeConsole) {
					return Globals.maximalPlevel;
				}
				return base.Plevel;
			}
		}

		//basicmodes:{
		private void InitHandle() {
			WriteLine("Username?:");
			basicmode = new VoidMode(NormalHandle);
			byte[] received = new byte[100];
			try {
				packetLen=client.Receive(received);
			} catch (FatalException) {
				throw;
			} catch (Exception) {	//The remote console disconnected.
				packetLen=-1;
			}
			if (packetLen<=0) {
				Close("Connection lost");
			}
		}

		private void NormalHandle() {
			byte[] receieved = new byte[100];
			//had to instantiate a new one each time, because Receieve() does not truncate /n if it was there already
			try {
				packetLen=client.Receive(receieved);
			} catch (FatalException) {
				throw;
			} catch (Exception) {	//The remote console disconnected.
				packetLen=-1;
			}
			if (packetLen<=0) {
				Close("Connection lost");
			}
			buffer = buffer+Encoding.UTF8.GetString(receieved).Trim(nullchar);
			//Console.WriteLine("receieved: {0}",buffer);
			int indexofeol=buffer.IndexOf("\n");
			if (indexofeol>-1) {
				//send it to the current handling func
				mode(buffer.Substring(0, indexofeol).Trim());
				buffer="";
			}
		}
		//	} endof basicmodes


		//modes:{
		private void DoUsername(string data) {
			//GameAccount acc=(GameAccount)Server.accounts[curAccount];
			//if ("127.0.0.1"==((IPEndPoint)client.RemoteEndPoint).Address.ToString()) {
			//	Console.WriteLine("Granting Admin Plevel for acc {0} - local address {1}",acc.name,curAccount);
			//	acc.plevel=Globals.maximalPlevel;
			//}
			username=data;
			WriteLine("Password?:");
			mode= new StringMode(DoPassword);
		}

		//Rewritten to use new GameAccount methods - Nov 08 2003 - SL
		private void DoPassword(string data) {
			GameAccount acc = GameAccount.HandleConsoleLoginAttempt(username, data);
			if (acc==null) {
				Close("Wrong password");
			} else {
				curAccount=acc;
				mode = new StringMode(DoCommand);
				Logger.OnConsoleWriteLine+=new StringToSend(WriteLine);
				// TODO: do we need to register OnConsoleWrite event?
				LogIn(acc);
			}
		}

		internal override sealed void LogIn(GameAccount acc) {
			loggedIn = true;
			base.LogIn(acc);
		}

		internal void DoCommand(string command) {
			Commands.ConsoleCommand(this, command);
		}

		void DoIgnore(string command) {
		}
		//	} endof modes

		private static string[] sendingArgs= new string[1];
		//method: WriteLine
		//send some text to this Conn.
		public override void WriteLine(string data) {
			Write(data+Environment.NewLine);
		}

		public void Write(string data) {
			if (client!=null) {
				//it is a remoteconsole
				try {
					client.Send(Encoding.UTF8.GetBytes(data));
				} catch (FatalException) {
					throw;
				} catch (Exception) {	//The remote console disconnected.
					//Server.CloseConsole(this);
					//this was causing an infinite loop!!
				}
			} else {
				//it is a local console	
				sendLocal(data);
			}
		}

		// TODO: is this method useless?
		//public override void WriteLine(LogStr data) {
		//	if (client!=null) {
		//		//it is a remoteconsole
		//		try {
		//			client.Send(Encoding.UTF8.GetBytes(data.NiceString+Environment.NewLine));
		//		} catch (FatalException) {
		//			throw;
		//		} catch (Exception) {	//The remote console disconnected.
		//			//Server.CloseConsole(this);
		//			//this was causing an infinite loop!!
		//		}
		//	} else {
		//		//it is a local console	
		//		sendLocal(data.NiceString);
		//	}
		//}

		public override int GetHashCode() {
			return uid;
		}

		public override string ToString() {
			return "Console client("+base.ToString()+")";
		}

		public override bool Equals(Object obj) {
			if (obj is ConsConn) {
				return (((ConsConn) obj).uid==this.uid);
			}
			return false;
		}
	}
}