using System;
using System.ComponentModel;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public static class Settings {
		public static readonly string iniFileName = "steamrc.ini";

		public static bool saveEndpointPasswords = true;

		static Settings() {
			IniFile ini = new IniFile(iniFileName);

			foreach (IniFileSection section in ini.GetSections("endpoint")) {
				knownEndPoints.Add(LoadEPSFromIni(section));
			}
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

				foreach (IniFileSection section in ini.GetSections("endpoint")) {
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
					IniFileSection section = ini.GetNewSection("endpoint");
					SaveEPSToIni(eps, section);
				}

				foreach (IniFileSection section in toRemove) {
					ini.RemoveSection(section);
				}

				ini.WriteToFile();
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		private static void SaveEPSToIni(EndPointSetting eps, IniFileSection section) {
			section.SetValue<string>("Name", eps.Name, null);
			section.SetValue<string>("Address", eps.Address, null);
			section.SetValue<string>("UserName", eps.UserName, null);
			section.SetValue<string>("Password", eps.Password, null);
			section.SetValue<int>("Port", eps.Port, null);
		}

		private static EndPointSetting LoadEPSFromIni(IniFileSection section) {
			EndPointSetting eps = new EndPointSetting();
			eps.Name = section.GetValue<string>("Name", eps.Name, null);
			eps.Address = section.GetValue<string>("Address", eps.Address, null);
			eps.UserName = section.GetValue<string>("UserName", eps.UserName, null);
			eps.Password = section.GetValue<string>("Password", eps.Password, null);
			eps.Port = section.GetValue<int>("Port", eps.Port, null);
			return eps;
		}

		public static BindingList<EndPointSetting> knownEndPoints = new BindingList<EndPointSetting>();
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
					throw new Exception("Port number out of range");
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
