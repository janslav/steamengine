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

using System.IO;
using System;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using SharpSvn;

namespace SteamEngine.Common {
	public static class VersionControl {
		public static void SVNUpdateProject() {
			string path = Path.GetFullPath(".");

			using (StopWatch.StartAndDisplay("SVN Update '" + path + "'...")) {
				SvnClient client = new SvnClient();
				client.Notify += new EventHandler<SvnNotifyEventArgs>(client_Notify);
				//client.Progress += new EventHandler<SvnProgressEventArgs>(client_Progress);
				client.SvnError += new EventHandler<SvnErrorEventArgs>(client_SvnError);
				client.Update(path);
			}
		}

		static void client_SvnError(object sender, SvnErrorEventArgs e) {
			//untested
			Logger.WriteWarning("SVN Error, Cancel: " + e.Cancel, e.Exception);
		}

		static void client_Notify(object sender, SvnNotifyEventArgs e) {
			SvnNotifyAction action = e.Action;
			switch (action) {
				case SvnNotifyAction.UpdateAdd:
					LogMessage("Added", e.FullPath, e.Error);
					break;
				case SvnNotifyAction.UpdateCompleted:
					Console.WriteLine("Completed at revision " + e.Revision);
					break;
				case SvnNotifyAction.UpdateDelete:
					LogMessage("Deleted", e.FullPath, e.Error);
					break;
				case SvnNotifyAction.UpdateReplace:
					LogMessage("Replaced", e.FullPath, e.Error);
					break;
				case SvnNotifyAction.UpdateUpdate:
					LogMessage("Updated", e.FullPath, e.Error);
					break;
				default:
					LogMessage(action.ToString(), e.FullPath, e.Error);
					break;
			}
		}

		private static void LogMessage(string action, string path, Exception ex) {
			string msg;
			if (string.IsNullOrEmpty(path)) {
				msg = "SVN " + action + ".";
			} else {
				msg = "SVN " + action + ": '" + path + "'.";
			}
			if (ex == null) {
				Console.WriteLine(msg);
			} else {
				Logger.WriteWarning(msg, ex);
			}
		}
	}
}
