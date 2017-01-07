using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace SteamEngine.AuxiliaryServer {
	public class Logger : SteamEngine.Common.Logger {

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "SteamEngine.AuxiliaryServer.Logger")]
		public static void Init() {
			new Logger();

			SteamEngine.Common.Logger.OpenFile();

			Logger.OnConsoleWrite += ConsoleServer.ConsoleServer.WriteAsAux;
			Logger.OnConsoleWriteLine += ConsoleServer.ConsoleServer.WriteLineAsAux;
		}


		protected override string GetFilepath() {
			//DateTime.Now.GetDateTimeFormats()[4]
			DateTime dtnow = DateTime.Now;
			System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.InvariantCulture;
			string filename = string.Concat("SteamEngine.AuxiliaryServer.",
				dtnow.Year.ToString("0000", ci), "-", dtnow.Month.ToString("00", ci), "-", dtnow.Day.ToString("00", ci), ".log");
			return Path.Combine(Settings.LogPath, filename);
		}
	}
}
