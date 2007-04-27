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
using System.Reflection;
using System.Globalization;
using SteamEngine.Packets;
using SteamEngine.Common;

namespace SteamEngine {

	//class: Conn
	//base abstract class for GameConn and ConsConn

	abstract public class Conn : TagHolder {
		private IPAddress ip;
		protected readonly Socket client;
		internal GameAccount curAccount;
		public readonly int uid;

		private static int uids = 0;

		private bool closingStringDisplayed = false; //so that it wond show "client xxx disconnected" twice for one client

		internal Conn() {//local console
			this.uid = uids++;
			this.client = null;
		}

		internal Conn(Socket client) {
			this.uid = uids++;
			this.client = client;
			client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.NoDelay, 1);	//Tcp?
			LingerOption lingerOption = new LingerOption(false, 0);
			client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption);
			ip = ((IPEndPoint) client.RemoteEndPoint).Address;
		}

		public void EchoMessage(string msg) {
			Logger.Show(msg);
			WriteLine(msg);
		}

		//public abstract void WriteLine(LogStr data);
		public abstract void WriteLine(string data);
		public abstract bool IsLoggedIn { get; }

		internal abstract void Cycle();

		public GameAccount Account {
			get {
				return curAccount;
			}
		}

		public IPAddress IP {
			get {
				return ip;
			}
		}

		public bool IsConnected {
			get {
				return client.Connected;
			}
		}

		//called by GameAccount, or by ConsConn itself 
		//(ConsConn loggs in without the knowledge of the Account - so it would be null ;)
		internal virtual void LogIn(GameAccount acc) {
			curAccount = acc;
			Server.ConnLoggedIn(this);
			//Console.WriteLine(LogStr.Ident(this)+" logged in.");
		}

		public virtual void Close(LogStr reason) {
			if (!closingStringDisplayed) {
				Console.WriteLine(LogStr.Ident(this)+" disconnected: "+reason);
				closingStringDisplayed = true;
			}
			Close();
		}

		public virtual void Close(string reason) {
			if (!closingStringDisplayed) {
				Console.WriteLine(LogStr.Ident(this)+" disconnected: "+reason);
				closingStringDisplayed = true;
			}
			Close();
		}

		private void Close() {
			//Who ever said there was anything wrong with paranoia? - SL
			try {
				client.Shutdown(SocketShutdown.Both);
			} catch (Exception) { }
			try {
				client.Close();
			} catch (Exception) { }
		}

		public virtual byte Plevel {
			get {
				if (curAccount == null) {
					return 0;
				} else {
					return curAccount.PLevel;
				}
			}
		}

		public byte MaxPlevel {
			get {
				if (curAccount==null) {
					if ((this is ConsConn)&&(client==null))
						return Globals.maximalPlevel;
				} else {
					return curAccount.MaxPLevel;
				}
				return 0;
			}
		}

		public override string ToString() {
			string retVal = "uid="+uid;
			if (curAccount != null) {
				retVal += ", acc='"+curAccount.Name+"'";
			}
			if (ip != null) {
				retVal += ", IP="+ip;
			}
			return retVal;
		}

		private static List<TriggerGroup> registeredTGs = new List<TriggerGroup>();
		public static void RegisterTriggerGroup(TriggerGroup tg) {
			if (!registeredTGs.Contains(tg)) {
				registeredTGs.Add(tg);
			}
		}

		public override sealed void Trigger(TriggerKey td, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				registeredTGs[i].Run(this, td, sa);
			}
			base.TryTrigger(td, sa);
		}

		public override void TryTrigger(TriggerKey td, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				registeredTGs[i].TryRun(this, td, sa);
			}
			base.TryTrigger(td, sa);
		}

		public override bool CancellableTrigger(TriggerKey td, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				object retVal = registeredTGs[i].Run(this, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return base.TryCancellableTrigger(td, sa);
		}

		public override bool TryCancellableTrigger(TriggerKey td, ScriptArgs sa) {
			for (int i = 0, n = registeredTGs.Count; i<n; i++) {
				object retVal = registeredTGs[i].TryRun(this, td, sa);
				try {
					int retInt = Convert.ToInt32(retVal);
					if (retInt == 1) {
						return true;
					}
				} catch (Exception) {
				}
			}
			return base.TryCancellableTrigger(td, sa);
		}
	}
}