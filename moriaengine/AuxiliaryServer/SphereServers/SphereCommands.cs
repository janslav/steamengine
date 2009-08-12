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
			switch (cmd.ToLower(System.Globalization.CultureInfo.InvariantCulture)) {
				case "filter":
					FlipFilter(console, sphere);
					return;
				case "help":
					DisplayHelp(console, sphere);
					return;
			}

			console.WriteLine(sphere.ServerUid, "Unknown command '" + cmd + "'.");
		}

		private static void DisplayHelp(ConsoleServer.ConsoleClient console, SphereServerClient sphere) {
			console.WriteLine(sphere.ServerUid, "Available commands:"
				+ "filter" + Environment.NewLine
				+ "help");
		}



		private static void FlipFilter(SteamEngine.AuxiliaryServer.ConsoleServer.ConsoleClient console, SphereServerClient sphere) {
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
