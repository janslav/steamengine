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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using SteamEngine.Common;

namespace SteamEngine {
	public class CoreLogger : Logger {

		internal static void Init() {
			try {
				new CoreLogger();
			} catch (Exception globalexp) {
				WriteFatal(globalexp);
				MainClass.CommandExit();
			}

			if (Globals.LogToFiles) {
				OpenFile();
			}
		}

		protected override string GetFilepath() {
			//DateTime.Now.GetDateTimeFormats()[4]
			var dtnow = DateTime.Now;
			var filename = string.Format(CultureInfo.InvariantCulture,
				"SteamEngine.GameServer.{0}-{1}-{2}.log",
				dtnow.Year.ToString("0000", CultureInfo.InvariantCulture),
				dtnow.Month.ToString("00", CultureInfo.InvariantCulture),
				dtnow.Day.ToString("00", CultureInfo.InvariantCulture));
			return Path.Combine(Globals.LogPath, filename);
		}

		protected override LogStr RenderText(Exception e) {
			return RenderTransactionNumber(base.RenderText(e));
		}

		protected override LogStr RenderText(LogStr data) {
			return RenderTransactionNumber(base.RenderText(data));
		}

		protected override LogStr RenderText(string data) {
			return RenderTransactionNumber(base.RenderText(data));
		}

		public override LogStr RenderText(StackTrace stackTrace) {
			return RenderTransactionNumber(base.RenderText(stackTrace));
		}

		private static LogStr RenderTransactionNumber(LogStr r) {
			var tran = SeShield.TransactionNumber;
			if (tran.HasValue) {
				r = $"[t#{tran.Value:000000000000}] " + r;
			}
			return r;
		}
	}
}