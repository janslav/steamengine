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
using System.Collections.Generic;
using System.Text;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Common;
using System.IO;
using System.Net;
using System.Threading;
using SteamEngine.Regions;

namespace SteamEngine.Networking {
	public abstract class SyncQueue {
		private Thread thread;
		protected AutoResetEvent autoResetEvent = new AutoResetEvent(false);

		static protected bool enabled = false;

		protected SyncQueue() {
			this.thread = new Thread(this.Cycle);
			this.thread.IsBackground = true;
			this.thread.Start();
		}

		private void Cycle() {
			while (this.autoResetEvent.WaitOne()) {
				lock (MainClass.globalLock) {
					try {
						this.ProcessQueue();
					} catch (Exception e) { Logger.WriteError(e); }
				}
			}
		}

		protected abstract void ProcessQueue();

		public static void Enable() {
			enabled = true;
		}

		public static void Disable() {
			enabled = false;
		}

		public static bool IsEnabled {
			get {
				return enabled;
			}
		}

		public static void ProcessAll() {
			ItemSyncQueue.instance.ProcessQueue();
			CharSyncQueue.instance.ProcessQueue();
		}
	}
}