using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {
	public static class Settings {
		public static readonly string logPath;
		public static readonly sbyte timeZone;
		public static readonly int loginServerPort;

		public static readonly List<LoginServerInstanceSettings> loginSettings = new List<LoginServerInstanceSettings>();

		public static readonly string iniFileName = "steamaux.ini";

		static Settings() {
			IniFile ini = new IniFile(iniFileName);

			IniFileSection files = ini.GetNewOrParsedSection("Files", "paths to relevant files or directories");

			logPath = files.GetValue<string>("logPath", "logs", "Path to the log files");


			IniFileSection loginServer = ini.GetNewOrParsedSection("LoginServer", null);

			timeZone = loginServer.GetValue<sbyte>("timeZone", 5, "What time-zone you're in. 0 is GMT, 5 is EST, etc.");

			loginServerPort = loginServer.GetValue<int>("port", 2593, "The port to listen on for game client connections");


			foreach (IniFileSection section in ini.GetSections("gameserver")) {
				loginSettings.Add(new LoginServerInstanceSettings(section));
			}

			if (loginSettings.Count == 0) {
				loginSettings.Add(new LoginServerInstanceSettings(ini.GetNewOrParsedSection("gameserver", "The default gameserver entry.")));
			}

			ini.WriteToFile();
		}

		internal static void Init() {
			Console.WriteLine(iniFileName+" loaded.");
		}
	}

	public class LoginServerInstanceSettings {
		public readonly int number;
		public readonly string iniPath;
		public readonly string name;
		public readonly int port;

		internal LoginServerInstanceSettings(IniFileSection section) {
			this.number = section.GetValue<int>("number", 0, "Number to order the servers in shard list");
			this.iniPath = section.GetValue<string>("iniPath", ".", "path to steamengine.ini of this instance");

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
			this.port = ini.GetSection("ports").GetValue<int>("game");

		}
	}
}
