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
using SharpSvn;

namespace SteamEngine.Common {
	public static class VersionControl {
		private static SvnClient CreateClient() {
			var client = new SvnClient();
			client.Notify += client_Notify;
			//client.Progress += new EventHandler<SvnProgressEventArgs>(client_Progress);
			client.SvnError += client_SvnError;
			return client;
		}

		public static void SvnUpdateProject(string path) {
			using (StopWatch.StartAndDisplay("SVN Update '" + path + "'...")) {
				var client = CreateClient();
				
				var args = new SvnUpdateArgs();
				args.ThrowOnCancel = false;
				args.ThrowOnError = false;
				client.Update(path, args);
			}
		}

		public static void SvnCleanUpProject(string path) {
			using (StopWatch.StartAndDisplay("SVN Cleanup '" + path + "'...")) {
				var client = CreateClient();

				var args = new SvnCleanUpArgs();
				args.ThrowOnCancel = false;
				args.ThrowOnError = false;
				client.CleanUp(path, args);
			}
		}

		static void client_SvnError(object sender, SvnErrorEventArgs e) {
			//untested
			Logger.WriteWarning("SVN Error, Cancel: " + e.Cancel, e.Exception);
		}

		static void client_Notify(object sender, SvnNotifyEventArgs e) {
			var action = e.Action;
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
