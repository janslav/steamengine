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
using System.Timers;
using System.Text.RegularExpressions;
#if !MONO
using Microsoft.Win32;	//for RegistryKey
#endif
using SteamEngine.Packets;
using SteamEngine.Timers;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine {
			
	/*
		Class: Server
		Various global operations are done through this. Also, parts of the networking code are in here (The 
		parts dealing with multiple connections, etc. Things dealing with individual connections are done with
		InPackets or PacketSender, or in Conn, GameConn, or ConsConn.
	*/
	public static class Server  {
		public const int maxPacketLen = 65536;
		
		internal static LinkedList<GameConn> connections = new LinkedList<GameConn>();
		internal static TcpListener[] serverSockets;

		internal static List<IPAddress> omitIPs = new List<IPAddress>();
		internal static string routerIPstr = null;
		internal static byte[] routerIP = new byte[4];
        
		internal static byte[,] localIPs;
		internal static int numIPs = 0;
		internal static string[] ipStrings = null;

		internal static PacketHandler _in;

		public const int maxNotLoggedIn = 20; //maximum of connections that are not yet logged. this is a protection against some DoS attacks.
		private static LinkedList<GameConn> notLoggedIn = new LinkedList<GameConn>();
		private static SimpleQueue<GameConn> toBeClosed = new SimpleQueue<GameConn>();

		internal class IdleCheckTimer : SteamEngine.Timers.Timer {
			private static TimeSpan idleCheckTimerInterval = TimeSpan.FromMinutes(2);

			internal IdleCheckTimer() {
				this.DueInSpan = idleCheckTimerInterval;
				this.PeriodSpan = idleCheckTimerInterval;
			}

			protected sealed override void OnTimeout() {
				foreach (GameConn conn in connections) {
					if (conn.noResponse) {
						conn.Close("No response in last minute - connection lost");
					} else {
						conn.noResponse = true;
					}
				}
			}
		}

		internal static void AddOmitIP(IPAddress ip) {
			omitIPs.Add(ip);
		}

		internal static bool IsOmitIP(IPAddress ip) {
			for (int a=0; a<omitIPs.Count; a++) {
				if (ip.Equals(omitIPs[a])) {
					return true;
				}
			}
			return false;
		}

		internal static int IsLocalIP(byte[] ip) {
			for (int a=0; a<numIPs; a++) {
				if (localIPs[a,0]==ip[0] && localIPs[a,1]==ip[1] && localIPs[a,2]==ip[2]) {
					return a;
				}
			}
			return -1;
		}

		internal static byte[] GetIPFromString(String ip) {
			int pos=ip.IndexOf(':');
			IPHostEntry iphe;
			if (pos>-1) {
				iphe=Dns.GetHostEntry(ip.Substring(0,pos));
			} else {
				iphe=Dns.GetHostEntry(ip);
			}
			Logger.WriteDebug("iphe="+iphe);
			IPAddress[] ips=iphe.AddressList;
			byte[] b=ips[0].GetAddressBytes();
			Logger.WriteDebug("ips[0]="+ips[0]);
			return b;
		}

		/**
			Looks in the registry to find the path to the MUL files. If both 2d and 3d are installed,
			it isn't specified which this will find.
		*/
		public static string GetMulsPath() {
#if !MONO
			RegistryKey rk = Registry.LocalMachine;
			rk=rk.OpenSubKey("SOFTWARE");
			if (rk!=null) {
				rk=rk.OpenSubKey("Origin Worlds Online");
				if (rk!=null) {
					string[] names=rk.GetSubKeyNames();
					foreach (string name in names) {
						RegistryKey uoKey=rk.OpenSubKey(name);
						if (uoKey!=null) {
							string[] names2=uoKey.GetSubKeyNames();
							foreach (string name2 in names2) {
								RegistryKey verKey=uoKey.OpenSubKey(name2);
								object s=verKey.GetValue("InstCDPath");
								if (s!=null && s is string) {
									return (string)s;
								//} else {
									//Console.WriteLine("It ain't a string (Type is "+s.GetType().ToString()+") : "+s.ToString());
								}
							}
						} else {
							Logger.WriteWarning("Unable to open 'uoKeys' in registry");
						}
					}
				} else {
					Logger.WriteWarning("Unable to open 'Origin Worlds Online' in registry");
				}
			} else {
				Logger.WriteWarning("Unable to open 'SOFTWARE' in registry");
			}
#else
			Logger.WriteWarning("TODO: Implement some way to find the muls when running from MONO?");
#endif
			return null;
		}
		
		internal static void SetRouterIP(String ip) {
			try {
				if (ip!=null && ip.Length>0) {
					routerIPstr=ip;
					byte[] b=GetIPFromString(ip);
					for (int a=0; a<4; a++) {
						routerIP[a]=b[a];
					}
					
					Console.WriteLine("Router IP is {0}.{1}.{2}.{3} = {4}",routerIP[0], routerIP[1], routerIP[2], routerIP[3], ip);
				}
			} catch (SocketException) {
				Logger.WriteCritical("Unable to resolve "+ip+" to an IP address. Router IP *NOT* set.\n\n");
			}
		}
		
		
		//Your IP address is: 84.42.146.162
		private static Regex ipRE= new Regex(@"Your IP address is: (?<value>\d+\.\d+\.\d+\.\d+)",                   
			RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.Compiled|RegexOptions.Multiline);
		
		/*
			Method: GetMyExternalIPString
			Determines your external IP by checking a website which names it.
			If you use your web browser through a proxy which isn't your external IP, this will probaly return
			the proxy's IP instead.
		*/
		public static string GetMyExternalIPString() {
			CalcLocalIPs();
			//This can freeze if you're not online!
			try {
				Console.WriteLine("Please wait a moment while we attempt to locate http://www.ipaddy.com/...");
				
				WebClient temp = new WebClient();
				string str = temp.DownloadString("http://www.ipaddy.com/");
				
				Match m = ipRE.Match(str);
				//if (m.Success) {//if no success, it should return null, or something, I dont care really :P
				return m.Groups["value"].Value;
				//}
			} catch (FatalException) {
				throw;
			} catch (Exception) {
				return null;
			}
		}
		
		internal static string[] FindMyIP() {
			string msgBox="";
			//msgBox+="SteamEngine has checked with www.ipaddy.com to auto-detect your router IP and whether you need it set or not.\n\n";
			string ret=GetMyExternalIPString();
			if (ret==null) {
				msgBox="SteamEngine attempted to determine if you have an external IP different from your internal ones (by checking ipaddy.com), but couldn't reach ipaddy.com, so if you have a router or other external IP, you'll have to set routerIP in steamengine.ini yourself. And don't forget to set up port forwarding (on the router) for the ports you're going to run SteamEngine on!\n\n";
			} else {
				//Console.WriteLine("www.ipaddy.com says your IP is: "+ret);
				//Console.WriteLine("Checking against your local IPs...");
				for (int a=0; a<ipStrings.Length; a++) {
					if (ipStrings[a]==ret) {
						msgBox="SteamEngine checked ipaddy.com to see if you had a router or other external IP, and determined that you do not.\n\n";
						//Console.WriteLine("Okay, that's one of your local IPs. Looks like you don't have a router!");
						return new string[2] {msgBox, null};
					}
				}
				msgBox="SteamEngine checked ipaddy.com to see if you had a router or other external IP, and determined that you do.\n\nSteamEngine has been automagically set up to handle both LAN and Internet connections to your computer. All you need to do is make sure you've set up port forwarding (on the router) for the ports you're going to run SteamEngine on.";
				msgBox+="\n\nNote: If you havi a dynamic IP, you might want to gei a dynamic DNS name (For instance, from http://www.dyndns.org). Alternately, you can set alwaysUpdateRouterIPOnStartup in steamengine.ini to true, though you'll have to restart SteamEngine if your IP changes.\n\n";
			}
			return new string[2] {msgBox,ret};
		}
	
		internal static IPAddress[] CalcLocalIPs() {
			string strHostName = "";
			// Getting Ip addresses of local machine...
			strHostName = Dns.GetHostName();
			
			// Then using host name, get the IP address list..
			IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
			IPAddress[] ips = ipEntry.AddressList;
			int iplen=1;
			for (int a=0; a<ips.Length; a++) {
				//test for IPs to omit
				if (!IsOmitIP(ips[a])) {
					iplen++;
				}
			}
			
			ipStrings = new string[iplen];
			
			localIPs = new byte[iplen,4];
			numIPs = iplen;
			int w=0;
			for (int i = 0; i < ips.Length; i++){
				if (!IsOmitIP(ips[i])) {
					ipStrings[w]=ips[i].ToString();
					Byte[] b = ips[i].GetAddressBytes();
					for (int a=0; a<4; a++) {
						localIPs[w,a]=b[a];
					}
					string ipString = String.Format("{0}.{1}.{2}.{3}", localIPs[w,0], localIPs[w,1], localIPs[w,2], localIPs[w,3]);
					
					Console.WriteLine ("IP Address "+LogStr.Number(w)+" : "+LogStr.Ident(ipString));
					w++;
				}
			}
			ipStrings[iplen-1]="127.0.0.1"; //why is this being set? -tar
			localIPs[iplen-1,0]=127;
			localIPs[iplen-1,1]=0;
			localIPs[iplen-1,2]=0;
			localIPs[iplen-1,3]=1;
			return ips;
		}
		
		internal static void Init() {
			if (Globals.alwaysUpdateRouterIPOnStartup) {
				string[] ret=Server.FindMyIP();
				string routerIP=ret[1];
				if (routerIP==null) {
					Logger.WriteWarning("Unable to determine the routerIP. Using the value from steamengine.ini (if it's set).");
				} else {
					Server.SetRouterIP(routerIP);
				}
			}
			
			
			_in = new PacketHandler();
			
			IPAddress[] ips = CalcLocalIPs();
			Globals.ips = ips;
			
			ArrayList list = new ArrayList();
			TcpListener listener;
			
			bool loopbackUsed=false;
			for (int a=0; a<ips.Length; a++) {
				IPAddress ip = ips[a];
				if (!IsOmitIP(ip)) {
					if (ip.Equals(IPAddress.Loopback)) {
						loopbackUsed=true;
					}
					listener = new TcpListener(ip, Globals.port);
					listener.Start();
					list.Add(listener);
				}
			}
			if (!loopbackUsed) {
				listener = new TcpListener(IPAddress.Loopback,Globals.port);
				listener.Start();
				list.Add(listener);
			}

			serverSockets = (TcpListener[]) list.ToArray(typeof(TcpListener));

			Console.WriteLine("Listening on port "+LogStr.Number(Globals.port)+" for Game clients");
			
			MethodInfo mi = typeof(Server).GetMethod("CheckIdles");

			new IdleCheckTimer();
		}
		
		internal static void RemoveConn(GameConn conn) {
			toBeClosed.Enqueue(conn);
		}
		
		internal static void AddConn(GameConn conn) {
			notLoggedIn.AddLast(conn);
			connections.AddFirst(conn);
			NetState.Enable();//enable when there's more than 0 clients
		}
		
		internal static void ConnLoggedIn(GameConn conn) {
			notLoggedIn.Remove(conn);
		}
		
		private static void EnsureMaxNotLoggedIn() {
			while (notLoggedIn.Count > maxNotLoggedIn) {
				LinkedListNode<GameConn> node = notLoggedIn.First;//first, because we add to the end
				notLoggedIn.Remove(node);
				node.Value.Close("Too many lingering clients. possible DoS attack?");
			}
		}
		
		internal static void Cycle() {
			for (int i = 0, n = serverSockets.Length; i<n; i++) {
				TcpListener serverSocket=(TcpListener) serverSockets[i];
				while (serverSocket.Pending()) {
					EnsureMaxNotLoggedIn();
					IPEndPoint localEndpoint = (IPEndPoint) serverSocket.LocalEndpoint;
					Socket socket = serverSocket.AcceptSocket();
					if (localEndpoint.Port == Globals.port) {//gameconn
						if (connections.Count < Globals.maxConnections && RunLevelManager.IsRunning) {
							GameConn newConn = new GameConn(socket);
							Console.WriteLine(LogStr.Ident(newConn)+" connected.");
							AddConn(newConn);
							Globals.instance.TryTrigger(TriggerKey.clientAttach, new ScriptArgs(newConn)); // 
						} else {
							socket.Send(new byte[2] {0x82, 0x04});
							socket.Close();
							EndPoint remoteEndpoint = socket.RemoteEndPoint;
							Console.WriteLine("Connection (Ip {0}) rejected - server is full and/or paused.", remoteEndpoint);
							continue;
						}
					}
				}
			}
			
			//loop through incoming packets
			foreach (GameConn c in connections) {
				if (RunLevelManager.IsRunning) {
					c.Cycle();
				}
			}
			
			//clean up closed connections
			while (toBeClosed.Count > 0) {
				GameConn c = toBeClosed.Dequeue();
				int preCount = connections.Count;
				connections.Remove(c);
				if (connections.Count == 0) {
					NetState.Disable();//disable when no client connected
				}
				notLoggedIn.Remove(c);//could have been in there. Maybe ;)
			}
		}

		internal static void BackupLinksToCharacters() {
			foreach (GameConn conn in Server.AllGameConns) {
				conn.BackupLinksToCharacters();
			}
		}
		
		internal static void ReLinkCharacters() {
			foreach (GameConn conn in Server.AllGameConns) {
				conn.RelinkCharacter();
			}
		}

		internal static void RemoveBackupLinks() {
			foreach (GameConn conn in Server.AllGameConns) {
				conn.RemoveBackupLinks();
			}
		}

		/*
		    Method: PercentFull
		        Determines how full the server is by the # of connections and the maximum allowed number of connections.
		    Returns:
		        Returns a byte from 0-100, saying how full the server is.
		*/
		public static byte PercentFull() {
			return (byte) ((connections.Count * 100) / Globals.maxConnections);
		}

		internal static void Exit() {
			if (serverSockets != null) {
				for (int i = 0, n = serverSockets.Length; i<n; i++) {
					TcpListener serverSocket=(TcpListener) serverSockets[i];
					serverSocket.Stop();
				}
			}
			if (connections != null) {
				foreach (GameConn conn in connections) {
					conn.Close("exiting");
				}
			}
		}
		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendSystemMessage(GameConn c, string msg, ushort color) {
			SendMessage(c, null, 0xffff, "System", msg, SpeechType.Speech, 3, color);
		}
		
		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				msg - The cliloc # to send.
				color - The color of the message.
				args - Additional arguments needed for the cliloc message, if any.
		*/
		public static void SendClilocSysMessage(GameConn c, uint msg, ushort color, string args) {
			if (c != null) {
				PacketSender.PrepareClilocMessage(null, msg, SpeechType.Speech, 3, color, args);
				PacketSender.SendTo(c, true);
			}
		}

		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				msg - The cliloc # to send.
				color - The color of the message.
				args - Additional arguments needed for the cliloc message, if any.
		*/
		public static void SendClilocSysMessage(GameConn c, uint msg, ushort color, params string[] args) {
			if (c != null) {
				PacketSender.PrepareClilocMessage(null, msg, SpeechType.Speech, 3, color, string.Join("\t", args));
				PacketSender.SendTo(c, true);
			}
		}

		/* 
			Method: SendOverheadMessage
				Sends an overhead message to a particular game-connection. The message
				will appear above the head of that connection's currently logged in character.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessage(GameConn c, string msg, ushort color) {
			if (c != null) {
				AbstractCharacter cre = c.CurCharacter;
				if (cre==null) throw new ArgumentNullException("Cannot send an overhead message to a connection which hasn't logged on as a character yet.");
				SendMessage(c, cre, cre.Model, "System", msg, SpeechType.Speech, 3, color);
			}
		}
		
		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send.
				color - The color of the message.
		*/
		
		public static void SendNameFrom(GameConn c, Thing from, string msg, ushort color) {
			if (c != null) {
				if (from==null) throw new ArgumentNullException("from cannot be null in SendNameFrom.");
				SendMessage(c, from, from.Model, "", msg, SpeechType.Name, 3, color);
			}
		}
		
		
		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/
		
		public static void SendClilocNameFrom(GameConn c, Thing from, uint msg, ushort color, string args) {
			if (c != null) {
				if (from==null) throw new ArgumentNullException("from cannot be null in SendNameFrom.");
				PacketSender.PrepareClilocMessage(from, msg, SpeechType.Name, 3, color, args);
				PacketSender.SendTo(c, true);
			}
		}

		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/

		public static void SendClilocNameFrom(GameConn c, Thing from, uint msg, ushort color, string arg1, string arg2) {
			if (c != null) {
				if (from==null) throw new ArgumentNullException("from cannot be null in SendNameFrom.");
				PacketSender.PrepareClilocMessage(from, msg, SpeechType.Name, 3, color, string.Concat(arg1, "\t", arg2));
				PacketSender.SendTo(c, true);
			}
		}

		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/

		public static void SendClilocNameFrom(GameConn c, Thing from, uint msg, ushort color, params string[] args) {
			if (c != null) {
				if (from==null) throw new ArgumentNullException("from cannot be null in SendNameFrom.");
				PacketSender.PrepareClilocMessage(from, msg, SpeechType.Name, 3, color, string.Join("\t", args));
				PacketSender.SendTo(c, true);
			}
		}
		
		/*
			Method: SendOverheadMessageFrom
				Displays a message above a particular thing, but only to one game-connection. This works
				like speech, but is only sent to the chosen connection, so only that player will see it.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				from - The thing the message will appear above.
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessageFrom(GameConn c, Thing from, string msg, ushort color) {
			if (c != null) {
				if (from==null) throw new ArgumentNullException("from cannot be null in SendOverheadMessageFrom.");
				if (from is AbstractCharacter) {
					SendMessage(c, from, from.Model, from.Name, msg, SpeechType.Speech, 3, color);
				} else if (from is AbstractItem) {
					SendMessage(c, from, 0, from.Name, msg, SpeechType.Speech, 3, color);
				}
			}
		}
		/*
			Method: SendOverheadMessageFrom
				Displays a message above a particular static, but only to one game-connection. This works
				like speech, but is only sent to the chosen connection, so only that player will see it.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				from - The static the message will appear above.
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessageFrom(GameConn c, Static from, string msg, ushort color) {
			if (c != null) {
				if (from==null) throw new ArgumentNullException("from cannot be null in SendOverheadMessageFrom.");
				SendMessage(c, null, 0, from.Name, msg, SpeechType.Speech, 3, color);
			}
		}


		/*
			Method: SendOverheadServerMessage
				Displays a server message above the client's head, but only to that client.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadServerMessage(GameConn c, string msg, ushort color) {
			if (c==null) throw new ArgumentNullException("container cannot be null in SendOverheadServerMessage.");
			AbstractCharacter cre = c.CurCharacter;
			if (cre==null) throw new ArgumentNullException("Cannot send an overhead server message to a connection which hasn't logged on as a character yet.");
			SendMessage(c, cre, 0, "System", msg, SpeechType.Server, 0, color);
		}

		/*
			Method: SendServerMessage
				Displays a server message, but only to that client.
			Parameters:
				c - The GameConn to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendServerMessage(GameConn c, string msg, ushort color) {
			SendMessage(c, null, 0xffff, "System", msg, SpeechType.Server, 0, color);
		}

		public static void SendDenyResultMessage(GameConn c, Thing t, DenyResult trr) {
			switch (trr) {
				case DenyResult.Deny_RemoveFromView:
					if ((t != null)&&(!t.IsDeleted)) {
						PacketSender.PrepareRemoveFromView(t);
						PacketSender.SendTo(c, true);
					}
					break;
				case DenyResult.Deny_ThatDoesNotBelongToYou:
					SendClilocSysMessage(c, 500364, 0);		//You can't use that, it belongs to someone else.
					break;
				case DenyResult.Deny_ThatIsOutOfSight:
					SendClilocSysMessage(c, 3000269, 0);	//That is out of sight.
					break;
				case DenyResult.Deny_ThatIsTooFarAway:
					SendClilocSysMessage(c, 3000268, 0);	//That is too far away.
					break;
				case DenyResult.Deny_YouAreAlreadyHoldingAnItem:
					SendClilocSysMessage(c, 3000271, 0);	//You are already holding an item.
					break;
				case DenyResult.Deny_YouCannotPickThatUp:
					SendClilocSysMessage(c, 3000267, 0);	//You cannot pick that up.
					break;
				case DenyResult.Deny_ThatIsLocked:
					SendClilocSysMessage(c, 501283, 0);		//That is locked.
					break;
				case DenyResult.Deny_ContainerClosed:
					//SendClilocSysMessage(c, 500209, 0);		//You cannot peek into the container.
					SendSystemMessage(c, "Tento kontejner není otevøený.", 0);
					break;
				//case TryReachResult.Failed_NoMessage:
				//case TryReachResult.Succeeded:
				//default:
			}
		}

		/*
			Method: BroadCast
				Broadcast a message to all clients (as a server message).
			Parameters:
				msg - What to send.
		*/
		internal static void BroadCast(string msg) {
			Console.WriteLine("Broadcasting: "+msg);
			foreach (GameConn c in AllGameConns) {
				SendServerMessage(c, msg, Globals.serverMessageColor);
			}
		}

		//For use by Server's various message sending methods (Which send to one client).
		internal static void SendMessage(GameConn c, Thing from, ushort model, string sourceName, string msg, SpeechType type, ushort font, ushort color) {
			if (Globals.supportUnicode && font==3 && !(type==SpeechType.Name && Globals.asciiForNames)) {	//if it's another font, send it as ASCII
				PacketSender.PrepareUnicodeMessage(from, model, sourceName, msg, type, font, color, c.lang);
			} else {
				PacketSender.PrepareAsciiMessage(from, model, sourceName, msg, type, font, color);
			}
			PacketSender.SendTo(c, true);
		}
		
		public static void StartGame(GameConn c) {
			AbstractCharacter curChar = c.CurCharacter;
			using (BoundPacketGroup group = PacketSender.NewBoundGroup()) {
				PacketSender.PrepareInitialPlayerInfo(curChar);
				//(TODO): 0xbf map change (INI flag, etc)
				//(Not To-do, or to-do much later on): 0xbf map patches (INI flag) (.. We don't need this on custom maps, and it makes it more complicated to load the maps/statics)
				PacketSender.PrepareSeasonAndCursor(curChar.Season, curChar.Cursor);
				PacketSender.PrepareEnableFeatures(Globals.featuresFlags);
				
				//RunUO sends PLI twice, then sends light levels, then sends PLI again. But that really isn't necessary, heh.
				PacketSender.PrepareLocationInformation(c);
				//(TODO): 0x4e and 0x4f personal and global light levels

				//PacketSender.PrepareCharacterInformation(curChar, curChar.GetHighlightColorFor(curChar));

				PacketSender.PrepareWarMode(curChar);
				
				group.SendTo(c);
			}

			//0x1a and/or 0x78 for other things on screen:
			//curChar.SendNearbyStuff();
			//if (Globals.fastWalkPackets) {
				//c.FastWalk = new FastWalkStack(c.ToString());
				//PacketSender.SendInitFastwalk(c);
			//}
			
			if (c.UpdateRange!=18) {
				c.restoringUpdateRange=true;
				PacketSender.SendUpdateRange(c, c.UpdateRange);
			}

			PacketSender.PrepareStartGame();
			PacketSender.SendTo(c, true);

			//SendCharPropertiesTo(c, curChar, curChar);
			
			c.WriteLine("Welcome to "+Globals.serverName);

			new DelayedResyncTimer(c).DueInSeconds = 2;
		}

		internal class DelayedResyncTimer : SteamEngine.Timers.Timer {
			GameConn conn;

			internal DelayedResyncTimer(GameConn conn) {
				this.conn = conn;
			}

			protected sealed override void OnTimeout() {
				AbstractCharacter player = conn.CurCharacter;
				if (player != null) {
					player.Resync();
				}
			}
		}

		public static void SendCharPropertiesTo(GameConn viewerConn, AbstractCharacter viewer, AbstractCharacter target) {
			if (Globals.aos && viewerConn.Version.aosToolTips) {
				ObjectPropertiesContainer iopc = target.GetProperties();
				if (iopc != null) {
					iopc.SendIdPacket(viewerConn);
				}
				if (target.visibleLayers != null) {
					foreach (AbstractItem contained in target.visibleLayers) {
						if (viewer.CanSeeVisibility(contained)) {
							iopc = contained.GetProperties();
							if (iopc != null) {
								iopc.SendIdPacket(viewerConn);
							}
						}
					}
				}
			}
		}

		public static ushort MaxX(byte M) {
			return (ushort) (Map.GetMapSizeX(M)-1);
		}
		public static ushort MaxY(byte M) {
			return (ushort) (Map.GetMapSizeY(M)-1);
		}

		public static IEnumerable<GameConn> AllGameConns {
			get {
				return GetAllGameConns();
			}
		}

		public static IEnumerable<GameConn> GetAllGameConns() {
			GameConn[] arr = new GameConn[connections.Count];
			Server.connections.CopyTo(arr, 0);
			return arr;
		}
		
		public static IEnumerable<AbstractCharacter> AllPlayers {
			get {
				return GetAllPlayers();
			}
		}
		public static IEnumerable<AbstractCharacter> GetAllPlayers() {
			foreach (GameConn conn in Server.connections) {
				if (conn.CurCharacter != null) {
					yield return conn.CurCharacter;
				}
			}
		}
	}
}
