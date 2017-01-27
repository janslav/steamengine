using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SteamEngine.AuxiliaryServer.SphereServers {

	//simplified version of "Communication" framework, only communicates in cleartext - telnet style
	public static class SphereServerClientFactory {
		public static void Init() {
			foreach (var setup in Settings.KnownGameServersList) {
				var sphereSetup = setup as SphereServerSetup;
				if (sphereSetup != null) {
					Connect(sphereSetup, 0);
				}
			}
		}

		public static void Connect(SphereServerSetup setup, int ms) {
			var endpoint = new IPEndPoint(
				//Dns.GetHostAddresses("server.moria.cz")[0], 2593
				IPAddress.Loopback, setup.Port
				);

			var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			ScheduleConnect(new object[] { socket, endpoint, setup }, ms);			
		}

		private static void ScheduleConnect(object state, int ms) {
			//Timer t = 
			new Timer(ScheduledBeginConnect, state, ms, Timeout.Infinite);
		}

		private static void ScheduledBeginConnect(object state) {
			Console.WriteLine("ScheduledBeginConnect in");
			try {
				var arr = (object[]) state;
				var socket = (Socket) arr[0];
				var endpoint = (IPEndPoint) arr[1];

				socket.BeginConnect(endpoint, BeginConnectCallBack, state);
			} catch (Exception e) {
				Common.Logger.WriteError("Unexpected error in timer callback method", e);
			}
			Console.WriteLine("ScheduledBeginConnect out");
		}

		private static void BeginConnectCallBack(IAsyncResult result) {
			var arr = (object[]) result.AsyncState;
			var socket = (Socket) arr[0];
			//IPEndPoint endpoint = (IPEndPoint) arr[1];
			var setup = (SphereServerSetup) arr[2];

			try {
				socket.EndConnect(result);
			} catch (Exception e) {
				Console.WriteLine("Connecting to sphere at '" + setup.RamdiscIniPath + "' failed:" + e.Message);
				Common.Logger.WriteDebug(e);
				ScheduleConnect(arr, 5000);
				return;
			}

			//SphereServerConnection newConn = 
			new SphereServerConnection(socket, setup);
		}
	}
}
