using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SteamEngine.AuxiliaryServer {
	public class Logger : SteamEngine.Common.Logger {

		public static void Init() {
			new Logger();

			SteamEngine.Common.Logger.OpenFile();


			Logger.OnConsoleWrite += ConsoleServer.ConsoleServer.WriteAsAux;
			Logger.OnConsoleWriteLine += ConsoleServer.ConsoleServer.WriteLineAsAux;
		}


		protected override string GetFilepath() {
			//DateTime.Now.GetDateTimeFormats()[4]
			DateTime dtnow=DateTime.Now;
			string filename = string.Format("SteamEngine.AuxiliaryServer.{0}-{1}-{2}.log",
				dtnow.Year, dtnow.Month.ToString("00"), dtnow.Day.ToString("00"));
			return Path.Combine(Settings.logPath, filename);
		}
	}
}
