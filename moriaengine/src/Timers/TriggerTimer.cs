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
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using SteamEngine.Persistence;

namespace SteamEngine.Timers {

	[ManualDeepCopyClass]
	public class TriggerTimer : Timer {
		private TriggerKey trigger;
		private string formatString;

		protected override sealed void OnTimeout() {
			//Console.WriteLine("OnTimeout on timer "+this);
			ScriptArgs sa = new ScriptArgs(args);
			sa.FormatString = formatString;
			Globals.SetSrc(null);
			((PluginHolder) this.Cont).TryTrigger(trigger, sa);
		}
		
		public TriggerTimer(TimerKey name) : base(name) {
		}

		[DeepCopyImplementation]
		public TriggerTimer(TriggerTimer copyFrom)
			: base(copyFrom) {
			//copying constructor for copying of tagholders
			trigger = copyFrom.trigger;
			formatString = copyFrom.formatString;
		}

		public TriggerTimer(PluginHolder obj, TimerKey name, TimeSpan time, TriggerKey trigger, params object[] args): 
				base(obj, name, time, args) {
			this.trigger = trigger;
		}

		public TriggerTimer(PluginHolder obj, TimerKey name, TimeSpan time, TriggerKey trigger, string formatString, params object[] args)
			: base(obj, name, time, args) {
			this.formatString = formatString;
			this.trigger = trigger;
		}
		
		public override void Enqueue() {
			if (trigger == null) {
				throw new Exception("The timer does not have it`s 'trigger' field set");
			}
			base.Enqueue();
		}
		
		internal sealed override void Save(SaveStream output) {
			output.WriteValue("trigger", trigger);
			if (formatString != null) {
				output.WriteValue("formatString", formatString);
			}
			base.Save(output);
		}
		
		internal sealed override void LoadLine(string filename, int line, string name, string value) {
			switch (name) {
				case "trigger": 
					trigger = (TriggerKey) ObjectSaver.Load(value);
					return;
				case "formatstring":
					formatString = (string) ObjectSaver.Load(value);
					return;
			}
			
			base.LoadLine(filename, line, name, value);
		}
	}
}
