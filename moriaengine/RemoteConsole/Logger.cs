using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SteamEngine.RemoteConsole {
	public class Logger : SteamEngine.Common.Logger {

		public static void Init(LogStrDisplay display) {
			new Logger();

			Logger.OnConsoleWrite += display.WriteThreadSafe;
			Logger.OnConsoleWriteLine += display.WriteLineThreadSafe;

			//SteamEngine.Common.Logger.OpenFile();

		}

		protected override string GetFilepath() {
			//DateTime.Now.GetDateTimeFormats()[4]
			DateTime dtnow = DateTime.Now;
			string filename = string.Format("SteamEngine.RemoteConsole.{0}-{1}-{2}.log",
				dtnow.Year, dtnow.Month.ToString("00"), dtnow.Day.ToString("00"));
			return Path.Combine("logs", filename);
		}
	}
}
