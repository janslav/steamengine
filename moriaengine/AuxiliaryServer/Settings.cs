using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class Settings {
		public static readonly byte[] lanIP;
		public static readonly byte[] wanIP;

		public static readonly string logPath;
		public static readonly sbyte timeZone;
		public static readonly IPEndPoint loginServerEndpoint;

		public static readonly IPEndPoint consoleServerEndpoint;

		public static readonly List<LoginServerInstanceSettings> loginSettings = new List<LoginServerInstanceSettings>();

		public static readonly string iniFileName = "steamaux.ini";

		static Settings() {
			IniFile ini = new IniFile(iniFileName);

			IniFileSection files = ini.GetNewOrParsedSection("Files");

			logPath = Path.GetFullPath(files.GetValue<string>("logPath", "logs", "Path to the log files"));


			IniFileSection loginServer = ini.GetNewOrParsedSection("LoginServer");

			timeZone = loginServer.GetValue<sbyte>("timeZone", 5, "What time-zone you're in. 0 is GMT, 5 is EST, etc.");

			loginServerEndpoint = new IPEndPoint(IPAddress.Any,
				loginServer.GetValue<int>("port", 2593, "The port to listen on for game client connections"));


			IniFileSection consoleServer = ini.GetNewOrParsedSection("ConsoleServer");

			consoleServerEndpoint = new IPEndPoint(IPAddress.Any,
				consoleServer.GetValue<int>("port", 2594, "The port to listen on for remote console connections"));



			foreach (IniFileSection section in ini.GetSections("GameServer")) {
				loginSettings.Add(new LoginServerInstanceSettings(section));
			}

			if (loginSettings.Count == 0) {
				loginSettings.Add(new LoginServerInstanceSettings(ini.GetNewOrParsedSection("GameServer")));
			}

			ini.WriteToFile();

			Console.WriteLine(iniFileName+" loaded and written.");




			IPAddress[] wanIPs = Dns.GetHostAddresses(Dns.GetHostName());
			wanIP = wanIPs[0].GetAddressBytes();
			Sanity.IfTrueThrow(wanIP.Length != 4, "wanIP has not 4 bytes, need IPv6 compatibility implemented?");

			
			IPAddress[] lanIPs = Dns.GetHostAddresses("localhost");
			lanIP = lanIPs[0].GetAddressBytes();
			Sanity.IfTrueThrow(lanIP.Length != 4, "lanIP has not 4 bytes, need IPv6 compatibility implemented?");
		}

		internal static void Init() {
			
		}
	}

	public class LoginServerInstanceSettings {
		public readonly int number;
		public readonly string iniPath;
		public readonly string name;
		public readonly ushort port;

		internal LoginServerInstanceSettings(IniFileSection section) {
			this.number = section.GetValue<int>("number", 0, "Number to order the servers in shard list");
			this.iniPath = Path.GetFullPath(section.GetValue<string>("iniPath", ".", "path to steamengine.ini of this instance"));

			IniFile ini = null;

			if (File.Exists(iniPath)) {
				ini = new IniFile(iniPath);

			} else {
				iniPath = Path.Combine(iniPath, "steamengine.ini");
				if (File.Exists(iniPath)) {
					ini = new IniFile(iniPath);
				}
			}

			if (ini == null) {
				throw new Exception("Can't find steamengine.ini on the path "+this.iniPath+". It inecessary for the AuxiliaryServer operation.");
			}

			this.name = ini.GetSection("setup").GetValue<string>("name");
			this.port = ini.GetSection("ports").GetValue<ushort>("game");

		}
	}
}
