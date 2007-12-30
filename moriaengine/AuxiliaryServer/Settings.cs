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

		public static readonly List<GameServerInstanceSettings> knownGameServers = new List<GameServerInstanceSettings>();

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
				knownGameServers.Add(new GameServerInstanceSettings(section));
			}

			if (knownGameServers.Count == 0) {
				knownGameServers.Add(new GameServerInstanceSettings(ini.GetNewOrParsedSection("GameServer")));
			}

			knownGameServers.Sort(delegate(GameServerInstanceSettings a, GameServerInstanceSettings b) {
				return a.Number.CompareTo(b.Number);
			});

			ini.WriteToFile();

			Console.WriteLine(iniFileName+" loaded and written.");

			IPAddress[] wanIPs = Dns.GetHostAddresses(Dns.GetHostName());
			wanIP = wanIPs[0].GetAddressBytes();
			Sanity.IfTrueThrow(wanIP.Length != 4, "wanIP has not 4 bytes, need IPv6 compatibility implemented?");

			
			IPAddress[] lanIPs = Dns.GetHostAddresses("localhost");
			lanIP = lanIPs[0].GetAddressBytes();
			Sanity.IfTrueThrow(lanIP.Length != 4, "lanIP has not 4 bytes, need IPv6 compatibility implemented?");
		}

		public static GameServerInstanceSettings RememberGameServer(string iniPath) {
			foreach (GameServerInstanceSettings game in knownGameServers) {
				if (string.Equals(Path.GetFullPath(game.IniPath), Path.GetFullPath(iniPath), StringComparison.OrdinalIgnoreCase)) {
					return game;
				}
			}

			IniFile ini = new IniFile(iniFileName);
			IniFileSection section = ini.GetNewSection("GameServer");

			GameServerInstanceSettings newGameServer = new GameServerInstanceSettings(iniPath);
			newGameServer.WriteToIniSection(section);
			knownGameServers.Add(newGameServer);

			ini.WriteToFile();

			return newGameServer;
		}

		public static void RememberUser(string user, string password) {
			IniFile ini = new IniFile(iniFileName);
			IniFileSection usersSection = ini.GetNewOrParsedSection("Users");
			usersSection.SetValue<string>(user, password, null);
			ini.WriteToFile();
		}

		public static bool CheckUser(string user, string password) {
			IniFile ini = new IniFile(iniFileName);
			IniFileSection usersSection = ini.GetNewOrParsedSection("Users");
			string passToCompare;
			if (usersSection.TryGetValue<string>(user, out passToCompare)) {
				if (string.Equals(password, passToCompare, StringComparison.Ordinal)) {
					return true;
				}
			}
			return false;
		}

		internal static void Init() {
			
		}
	}

	public class GameServerInstanceSettings {
		private int number;
		private string iniPath;
		private string name;
		private ushort port;

		internal GameServerInstanceSettings(string iniPath) {
			this.number = Settings.knownGameServers.Count;
			this.iniPath = iniPath;

			ReadGameIni();
		}

		internal GameServerInstanceSettings(IniFileSection section) {
			this.number = section.GetValue<int>("number", 0, "Number to order the servers in shard list. Should be unique.");
			this.iniPath = Path.GetFullPath(section.GetValue<string>("iniPath", ".", "path to steamengine.ini of this instance"));

			ReadGameIni();

		}

		internal void WriteToIniSection(IniFileSection section) {
			section.SetValue<int>("number", this.number, "Number to order the servers in shard list. Should be unique.");
			section.SetValue<string>("iniPath", ".", "path to steamengine.ini of this instance");
		}

		private void ReadGameIni() {
			IniFile gameIni;

			iniPath = Path.Combine(iniPath, "steamengine.ini");
			if (File.Exists(iniPath)) {
				gameIni = new IniFile(iniPath);
			} else {
				throw new Exception("Can't find steamengine.ini on the path " + this.iniPath + ". It inecessary for the AuxiliaryServer operation.");
			}

			this.name = gameIni.GetSection("setup").GetValue<string>("name");
			this.port = gameIni.GetSection("ports").GetValue<ushort>("game");
		}

		public int Number {
			get {
				return this.number;
			}
		}

		public string IniPath {
			get {
				return this.iniPath;
			}
		}

		public string Name {
			get {
				return this.name;
			}
		}

		public ushort Port {
			get {
				return this.port;
			}
		}
	}
}
