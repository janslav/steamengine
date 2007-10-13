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
using System.Data;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Common;
using SteamEngine.Persistence;
using MySql.Data.MySqlClient;

namespace SteamEngine.CompiledScripts {
	[HasSavedMembers]
	public static class DbManager {
		[SavedMember("DBConfig","Database configuration")]
		[Remark("This will appear as a subdialog in the settings dialog, allowing us to set "+
				"database parametes online.")]
		private static DbConfig config = new DbConfig();

		public static DbConfig Config { get {
			return config;
		} }

		private static MySqlConnection connection;
		public static MySqlConnection Connection { get {
			if ((connection == null) || (connection.State != ConnectionState.Open)) {
				InitConnection();
			}
			return connection;
		} }

		private static void InitConnection() {
			if (connection != null)
				connection.Close();

			string connStr = String.Format("server={0};user id={1}; password={2}; database={3}; pooling=false",
				config.server, config.user, config.password, config.dbName);

			Logger.WriteDebug("Connecting to MySql server at "+config.server);
			connection = new MySqlConnection(connStr);
			connection.Open();
			Logger.WriteDebug("Connected to MySql server version "+connection.ServerVersion);
		}

		public static void CloseConnection() {
			if (connection != null) {
				Logger.WriteDebug("Closing MySql connection.");
				connection.Close();
			}
		}

		public class E_DbConnection_Global : CompiledTriggerGroup {
			public void On_Shutdown(Globals ignored1, ScriptArgs ignored2) {
				CloseConnection();
			}
		}
	}

	[SaveableClass][Dialogs.ViewableClass("Database Settings")]
	public class DbConfig {
		[LoadingInitializer]
		public DbConfig() {
		}

		[SaveableData("IPAddress")]
		public IPAddress server = IPAddress.Loopback;
		[SaveableData("root")]
		public String user = "root";
		[SaveableData("password")]
		public String password = "";
		[SaveableData("use DB")]
		public bool useDb = false;
		[SaveableData("DB Name")]
		public string dbName = "steamengine";
	}
}