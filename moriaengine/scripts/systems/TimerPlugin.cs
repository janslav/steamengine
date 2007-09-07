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
using System.Collections;
using System.Collections.Generic;
using SteamEngine;
using SteamEngine.Timers;
using SteamEngine.Packets;
using SteamEngine.Persistence;
using SteamEngine.Common;


namespace SteamEngine.CompiledScripts {

	public partial class TimerPluginDef : PluginDef {
		
		public TimerPluginDef(String defname, String filename, Int32 headerLine) : 
				base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new TimerPlugin();
		}

		public static void Bootstrap() {
			PluginDef.RegisterPluginDef(typeof(TimerPluginDef), typeof(TimerPlugin));
		}
	}
	
	[DeepCopyableClassAttribute]
	[SaveableClassAttribute]
	public partial class TimerPlugin : Plugin {
		[DeepCopyImplementationAttribute]
		public TimerPlugin(TimerPlugin copyFrom) : 
				base(copyFrom) {
		}
		
		[LoadingInitializerAttribute]
		public TimerPlugin() {
		}

		private new TimerPlugin Def {
			get {
				return ((TimerPlugin) (this.Def));
			}
		}

		static TimerKey timerKey = TimerKey.Get("_pluginTimer_");

		public double Timer {
			get {
				BoundTimer timer = this.GetTimer(timerKey);
				if (timer != null) {
					return timer.DueInSeconds;
				}
				return -1;
			}
			set {
				BoundTimer timer = this.GetTimer(timerKey);
				if (timer == null) {
					timer = new PluginTimer();
					this.AddTimer(timerKey, timer);
				}
				timer.DueInSeconds = value;
			}
		}

		public BoundTimer TimerObject {
			get {
				return this.GetTimer(timerKey);
			}
		}

		[SaveableClass][DeepCopyableClass]
		public class PluginTimer : BoundTimer {
			[DeepCopyImplementation][LoadingInitializer]
			public PluginTimer() {
			}

			static TriggerKey timer = TriggerKey.Get("timer");

			protected override void OnTimeout(TagHolder cont) {
				((TimerPlugin) this.Cont).TryRun(timer, null);
			}
		}
	}
}
