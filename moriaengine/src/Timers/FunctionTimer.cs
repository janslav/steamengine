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

//will be alternative to Timer that runs triggers...
//this runs scripted methods - functional ScriptHolders

using System;
using System.IO;
using System.Timers;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using SteamEngine.Persistence;

namespace SteamEngine.Timers {
	public class FunctionTimer : Timer {
		private ScriptHolder function;
		private string formatString;

		protected sealed override void OnTimeout() {
			//Console.WriteLine("OnTimeout on timer "+this);
			ScriptArgs sa = new ScriptArgs(args);
			sa.FormatString = formatString;
			function.TryRun(this.Obj, sa);
		}
		
		public FunctionTimer(TimerKey name) : base(name) {
		}
		
		protected FunctionTimer(FunctionTimer copyFrom, TagHolder assignTo) : base(copyFrom, assignTo) {
			//copying constructor for copying of tagholders
			function = copyFrom.function;
			formatString = copyFrom.formatString;
		}

		protected sealed override Timer Dupe(TagHolder assignTo) {
			return new FunctionTimer(this, assignTo);
		}
		
		public FunctionTimer(TagHolder obj, TimerKey name, TimeSpan time, ScriptHolder function, params object[] args): 
				base(obj, name, time, args) {
			this.function = function;
		}
		
		public FunctionTimer(TagHolder obj, TimerKey name, TimeSpan time, ScriptHolder function, string formatString, params object[] args): 
				base(obj, name, time, args) {
			this.function = function;
			this.formatString = formatString;
		}
		
		public override void Enqueue() {
			if (function == null) {
				throw new Exception("The timer does not have it`s 'function' field set");
			}
			base.Enqueue();
		}

		internal sealed override void Save(SaveStream output) {
			output.WriteValue("function", function.name);
			if (formatString != null) {
				output.WriteValue("formatString", formatString);
			}
			base.Save(output);
		}
		
		internal sealed override void LoadLine(string filename, int line, string name, string value) {
			switch (name) {
				case "function": 
					function = ScriptHolder.GetFunction((string) ObjectSaver.Load(value));
					if (function == null) {
						throw new Exception("There is no function "+value);
					}
					return;
				case "formatstring":
					formatString = (string) ObjectSaver.Load(value);
					return;
			}
			base.LoadLine(filename, line, name, value);
		}
	}
}
