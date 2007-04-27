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
using System.IO;
using System.Text;
using System.Diagnostics;
using SteamEngine.Common;

namespace SteamEngine {
	public class CoreLogger : Logger {
		public static void Init() {
			try {
				new CoreLogger();
			} catch (Exception globalexp) {
				Logger.WriteFatal(globalexp);
				MainClass.Exit();
			}
			
			if (Globals.logToFiles) {
				Logger.OpenFile();
			}
		}

	  	protected override string GetFilepath() {
	  		//DateTime.Now.GetDateTimeFormats()[4]
	  		DateTime dtnow=DateTime.Now;
			string filename = string.Format("SteamEngine.{0}-{1}-{2}.log", 
				dtnow.Year, dtnow.Month.ToString("00"), dtnow.Day.ToString("00"));
	  		return Path.Combine(Globals.logPath, filename);
		}
	}
}