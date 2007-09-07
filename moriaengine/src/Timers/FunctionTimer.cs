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
	[DeepCopyableClass]
	[SaveableClass]
	public class FunctionTimer : BoundTimer {
		public ScriptHolder function;

		[SaveableData]
		[CopyableData]
		public string formatString;
		[SaveableData]
		[CopyableData]
		public object[] args;

		protected sealed override void OnTimeout(TagHolder cont) {
			ScriptArgs sa = new ScriptArgs(args);
			sa.FormatString = formatString;
			function.TryRun(cont, sa);
		}

		[LoadingInitializer]
		[DeepCopyImplementation]
		public FunctionTimer() {
		}
		
		public FunctionTimer(ScriptHolder function, string formatString, object[] args) {
			this.function = function;
			this.formatString = formatString;
			this.args = args;
		}

		[Save]
		public override void Save(SaveStream output) {
			output.WriteValue("function", function.name);
			base.Save(output);
		}

		[LoadLine]
		public override void LoadLine(string filename, int line, string name, string value) {
			if (name.Equals("function")) {
				function = ScriptHolder.GetFunction((string) ObjectSaver.OptimizedLoad_String(value));
				if (function == null) {
					throw new Exception("There is no function "+value);
				}
			}
			base.LoadLine(filename, line, name, value);
		}
	}
}
