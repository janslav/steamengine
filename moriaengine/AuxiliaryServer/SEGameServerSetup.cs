using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Common;

namespace SteamEngine.AuxiliaryServer {

	public class SEGameServerSetup : IGameServerSetup {
		private int iniID;
		private readonly string iniPath;
		private readonly string name;
		private readonly int port;

		internal SEGameServerSetup(string iniPath) {
			this.iniID = Settings.KnownGameServersList.Count;
			this.iniPath = Path.GetFullPath(iniPath);

			ReadGameIni(this.iniPath, out this.name, out this.port);
		}

		internal SEGameServerSetup(IniFileSection section) {
			this.iniID = section.GetValue<int>("number", 0, "Number to order the servers in shard list. Should be unique, starting with 0.");
			this.iniPath = Path.GetFullPath(section.GetValue<string>("iniPath", Path.GetFullPath("."), "path to steamengine.ini of this instance"));

			ReadGameIni(this.iniPath, out this.name, out this.port);
		}

		internal void WriteToIniSection(IniFileSection section) {
			section.SetValue<int>("number", this.iniID, "Number to order the servers in shard list. Should be unique, starting with 0.");
			section.SetValue<string>("iniPath", this.iniPath, "path to steamengine.ini of this instance");
		}

		private static void ReadGameIni(string iniPath, out string name, out int port) {
			IniFile gameIni;

			iniPath = Path.Combine(iniPath, "steamengine.ini");
			if (File.Exists(iniPath)) {
				gameIni = new IniFile(iniPath);
			} else {
				throw new SEException("Can't find steamengine.ini on the path " + iniPath + ". It is necessary for the AuxiliaryServer operation.");
			}

			name = gameIni.GetSection("setup").GetValue<string>("name");
			port = gameIni.GetSection("ports").GetValue<int>("game");
		}

		public int IniID {
			get {
				return this.iniID;
			}
		}

		void IGameServerSetup.InternalSetIniID(int iniID) {
			this.iniID = iniID;
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

		public int Port {
			get {
				return this.port;
			}
		}

		public override bool Equals(object obj) {
			SEGameServerSetup gsis = obj as SEGameServerSetup;
			if (gsis != null) {
				if (StringComparer.OrdinalIgnoreCase.Equals(this.IniPath, gsis.IniPath)) {
					return true;
				}
			}
			return false;
		}

		public override int GetHashCode() {
			return StringComparer.OrdinalIgnoreCase.GetHashCode(this.iniPath);
		}


		public void StartGameServerProcess(SEBuild build) {
			Console.WriteLine("Compiling " + build + " build of server at " + this.IniPath);
			new AuxServNantProjectStarter(build, this.IniPath, "buildCore", "gameCoreFileName");
		}


		public void SvnUpdate() {
			VersionControl.SvnUpdateProject(this.IniPath);
		}

		public void SvnCleanup() {
			VersionControl.SvnCleanUpProject(this.IniPath);
		}
	}
}
