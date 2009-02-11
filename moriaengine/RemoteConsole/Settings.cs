using System;
using System.ComponentModel;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public static class Settings {
		public const string iniFileName = "steamrc.ini";
		public const string epsSectionName = "endpoint";
		public const string linkSectionName = "linkByExtension";
		public const string generalSectionName = "general";

		public static bool saveEndpointPasswords = true;

		static Settings() {
			IniFile ini = new IniFile(iniFileName);

			foreach (IniFileSection section in ini.GetSections(epsSectionName)) {
				knownEndPoints.Add(LoadEPSFromIni(section));
			}
		
			IniFileSection generalSec = ini.GetNewOrParsedSection(generalSectionName);
			saveEndpointPasswords = generalSec.GetValue<bool>("saveEndpointPasswords", true, "true: Save passwords, false: Don't.");
		}

		internal static void Init() {

		}

		internal static void Save() {
			try {
				IniFile ini = new IniFile(iniFileName);

				Dictionary<string, EndPointSetting> pointsToSave = new Dictionary<string, EndPointSetting>();
				foreach (EndPointSetting eps in knownEndPoints) {
					pointsToSave.Add(eps.Name, eps);
				}

				List<IniFileSection> toRemove = new List<IniFileSection>();

				foreach (IniFileSection section in ini.GetSections(epsSectionName)) {
					string name = section.GetValue<string>("name");
					EndPointSetting eps;
					if (pointsToSave.TryGetValue(name, out eps)) {
						SaveEPSToIni(eps, section);
						pointsToSave.Remove(name);
					} else {
						toRemove.Add(section);
					}
				}

				foreach (EndPointSetting eps in pointsToSave.Values) {
					IniFileSection section = ini.GetNewSection(epsSectionName);
					SaveEPSToIni(eps, section);
				}

				foreach (IniFileSection section in toRemove) {
					ini.RemoveSection(section);
				}

				IniFileSection generalSec = ini.GetNewOrParsedSection(generalSectionName);
				generalSec.SetValue<bool>("saveEndpointPasswords", saveEndpointPasswords, "true: Save passwords, false: Don't.");

				ini.WriteToFile();
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		private static void SaveEPSToIni(EndPointSetting eps, IniFileSection section) {
			section.SetValue<string>("Name", eps.Name, null);
			section.SetValue<string>("Address", eps.Address, null);
			section.SetValue<string>("UserName", eps.UserName, null);
			if (saveEndpointPasswords) {
				section.SetValue<string>("Password", eps.Password, null);
			} else {
				section.RemoveValue("Password");
			}
			section.SetValue<int>("Port", eps.Port, null);
		}

		private static EndPointSetting LoadEPSFromIni(IniFileSection section) {
			EndPointSetting eps = new EndPointSetting();
			eps.Name = section.GetValue<string>("Name", eps.Name, null);
			eps.Address = section.GetValue<string>("Address", eps.Address, null);
			eps.UserName = section.GetValue<string>("UserName", eps.UserName, null);
			eps.Password = section.GetValue<string>("Password", eps.Password, null);
			if (!saveEndpointPasswords) {
				section.RemoveValue("Password");
			}
			eps.Port = section.GetValue<int>("Port", eps.Port, null);
			return eps;
		}

		public static BindingList<EndPointSetting> knownEndPoints = new BindingList<EndPointSetting>();

		internal static bool GetCommandLineForExt(string ext, out string exePath, out string argumentsToFormat) {
			IniFile ini = new IniFile(iniFileName);

			foreach (IniFileSection section in ini.GetSections(linkSectionName)) {
				string sectionExt = section.GetValue<string>("extension");
				if (string.Equals(ext, sectionExt, StringComparison.OrdinalIgnoreCase)) {
					exePath = section.GetValue<string>("exe");
					argumentsToFormat = section.GetValue<string>("arguments");
					return true;
				}
			}

			exePath = argumentsToFormat = null;
			return false;
		}
	}

	public class EndPointSetting {
		private int port = 2594;
		private string name = "New endpoint";
		private string address = "localhost";
		private string userName = "";
		private string password = "";

		public string Name {
			set { name = value; }
			get { return name; }
		}
		public string Address {
			set { address = value; }
			get { return address; }
		}
		public string UserName {
			set { userName = value; }
			get { return userName; }
		}
		public string Password {
			set { password = value; }
			get { return password; }
		}

		public int Port {
			set {
				if (value < 0 || value > 65535) {
					throw new SEException("Port number out of range");
				}
				port = value;
			}
			get { return port; }
		}

		public override string ToString() {
			return name;
		}
	}
}
