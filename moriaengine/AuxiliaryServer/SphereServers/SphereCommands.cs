using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;
using NAnt.Core;


namespace SteamEngine.AuxiliaryServer.SphereServers {
	public static class SphereCommands {
		public static void HandleCommand(ConsoleServer.ConsoleClient console, SphereServerClient sphere, string cmd) {
			string[] split = cmd.Split(Tools.whitespaceChars);

			switch (split[0].ToLower(System.Globalization.CultureInfo.InvariantCulture)) {
				case "filter":
					FlipFilter(console, sphere);
					return;
				case "help":
					DisplayHelp(console, sphere);
					return;
				case "exitlater":
					ExitLater(console, sphere, split);
					return;
			}

			console.WriteLine(sphere.ServerUid, "Unknown command '" + cmd + "'.");
		}

		private static void ExitLater(ConsoleServer.ConsoleClient console, SphereServerClient sphere, string[] split) {
			double minutes = 10;
			if (split.Length > 1) {
				minutes = ConvertTools.ParseDouble(split[1]);
			}
			sphere.ExitLater(TimeSpan.FromMinutes(minutes));
		}

		private static void DisplayHelp(ConsoleServer.ConsoleClient console, SphereServerClient sphere) {
			console.WriteLine(sphere.ServerUid, "Available commands:"
				+ "filter" + Environment.NewLine
				+ "help");
		}

		private static void FlipFilter(ConsoleServer.ConsoleClient console, SphereServerClient sphere) {
			if (console.filteredGameServers.Contains(sphere.ServerUid)) {
				console.filteredGameServers.Remove(sphere.ServerUid);
				console.WriteLine(sphere.ServerUid, "Filter disabled");
			} else {
				console.filteredGameServers.Add(sphere.ServerUid);
				console.WriteLine(sphere.ServerUid, "Filter enabled");
			}
		}
	}
}