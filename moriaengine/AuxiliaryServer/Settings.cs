using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class Settings {
		private static readonly string logPath;
		private static readonly sbyte timeZone;
		private static readonly IPEndPoint loginServerEndpoint;
		private static readonly IPEndPoint consoleServerEndpoint;

		private static readonly HashSet<GameServerInstanceSettings> knownGameServersSet;
		private static readonly List<GameServerInstanceSettings> knownGameServersList;
		private static readonly System.Collections.ObjectModel.ReadOnlyCollection<GameServerInstanceSettings> knownGameServersListWrapper;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public const string iniFileName = "steamaux.ini";

		[CLSCompliant(false)]
		public static sbyte TimeZone {
			get { return Settings.timeZone; }
		}

		public static string LogPath {
			get { return Settings.logPath; }
		}

		public static System.Collections.ObjectModel.ReadOnlyCollection<GameServerInstanceSettings> KnownGameServersList {
			get {
				return knownGameServersListWrapper;
			}
		}

		public static IPEndPoint LoginServerEndpoint {
			get { return Settings.loginServerEndpoint; }
		}

		public static IPEndPoint ConsoleServerEndpoint {
			get { return Settings.consoleServerEndpoint; }
		}

		static Settings() {
			knownGameServersSet = new HashSet<GameServerInstanceSettings>();
			knownGameServersList = new List<GameServerInstanceSettings>();
			knownGameServersListWrapper = new System.Collections.ObjectModel.ReadOnlyCollection<GameServerInstanceSettings>(knownGameServersList);

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
				TryAddKnownGameServer(new GameServerInstanceSettings(section));
			}

			if (knownGameServersSet.Count == 0) {
				TryAddKnownGameServer(new GameServerInstanceSettings(ini.GetNewOrParsedSection("GameServer")));
			}

			knownGameServersList.Sort(delegate(GameServerInstanceSettings a, GameServerInstanceSettings b) {
				return a.Number.CompareTo(b.Number);
			});

			for (int i = 0, n = knownGameServersList.Count; i < n; i++) {
				knownGameServersList[i].Number = i;
			}

			ini.WriteToFile();

			Console.WriteLine(iniFileName + " loaded and written.");
		}

		public static bool TryAddKnownGameServer(GameServerInstanceSettings gsig) {
			int prevCount = knownGameServersSet.Count;
			knownGameServersSet.Add(gsig);
			if (knownGameServersSet.Count > prevCount) {
				knownGameServersList.Add(gsig);
				return true;
			}
			return false;
		}

		public static GameServerInstanceSettings RememberGameServer(string iniPath) {
			foreach (GameServerInstanceSettings game in knownGameServersSet) {
				if (string.Equals(game.IniPath, Path.GetFullPath(iniPath), StringComparison.OrdinalIgnoreCase)) {
					return game;
				}
			}

			IniFile ini = new IniFile(iniFileName);
			IniFileSection section = ini.GetNewSection("GameServer");

			GameServerInstanceSettings newGameServer = new GameServerInstanceSettings(iniPath);
			if (TryAddKnownGameServer(newGameServer)) {
				newGameServer.WriteToIniSection(section);
			}

			ini.WriteToFile();

			return newGameServer;
		}

		public static void ForgetUser(string user) {
			IniFile ini = new IniFile(iniFileName);
			IniFileSection usersSection = ini.GetNewOrParsedSection("Users");
			usersSection.RemoveValue(user);
			ini.WriteToFile();
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
		private readonly string iniPath;
		private readonly string name;
		private readonly ushort port;

		internal GameServerInstanceSettings(string iniPath) {
			this.number = Settings.KnownGameServersList.Count;
			this.iniPath = Path.GetFullPath(iniPath);

			ReadGameIni(this.iniPath, out this.name, out this.port);
		}

		internal GameServerInstanceSettings(IniFileSection section) {
			this.number = section.GetValue<int>("number", 0, "Number to order the servers in shard list. Should be unique, starting with 0.");
			this.iniPath = Path.GetFullPath(section.GetValue<string>("iniPath", Path.GetFullPath("."), "path to steamengine.ini of this instance"));

			ReadGameIni(this.iniPath, out this.name, out this.port);
		}

		internal void WriteToIniSection(IniFileSection section) {
			section.SetValue<int>("number", this.number, "Number to order the servers in shard list. Should be unique, starting with 0.");
			section.SetValue<string>("iniPath", this.iniPath, "path to steamengine.ini of this instance");
		}

		private static void ReadGameIni(string iniPath, out string name, out ushort port) {
			IniFile gameIni;

			iniPath = Path.Combine(iniPath, "steamengine.ini");
			if (File.Exists(iniPath)) {
				gameIni = new IniFile(iniPath);
			} else {
				throw new SEException("Can't find steamengine.ini on the path " + iniPath + ". It inecessary for the AuxiliaryServer operation.");
			}

			name = gameIni.GetSection("setup").GetValue<string>("name");
			port = gameIni.GetSection("ports").GetValue<ushort>("game");
		}

		public int Number {
			get {
				return this.number;
			}
			internal set {
				this.number = value;
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

		[CLSCompliant(false)]
		public ushort Port {
			get {
				return this.port;
			}
		}

		public override bool Equals(object obj) {
			GameServerInstanceSettings gsis = obj as GameServerInstanceSettings;
			if (gsis != null) {
				if (StringComparer.OrdinalIgnoreCase.Equals(gsis.iniPath, StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}
			return false;
		}

		public override int GetHashCode() {
			return StringComparer.OrdinalIgnoreCase.GetHashCode(this.iniPath);
		}
	}
}
